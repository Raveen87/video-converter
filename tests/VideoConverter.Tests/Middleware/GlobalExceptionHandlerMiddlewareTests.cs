using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Web.Exceptions;
using VideoConverter.Web.Middleware;

namespace VideoConverter.Tests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<GlobalExceptionHandlerMiddleware>> _loggerMock;
    private readonly GlobalExceptionHandlerMiddleware _middleware;
    private readonly DefaultHttpContext _context;

    public GlobalExceptionHandlerMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        _middleware = new GlobalExceptionHandlerMiddleware(_nextMock.Object, _loggerMock.Object);
        _context = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        // Arrange
        _ = _nextMock.Setup(x => x(_context)).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        _nextMock.Verify(x => x(_context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenApiError_ShouldReturnApiErrorResponse()
    {
        // Arrange
        _context.Response.Body = new MemoryStream();
        var apiError = ApiErrorException.ValidationError(
            "Test error message",
            "User-friendly message");
        _ = _nextMock.Setup(x => x(_context)).Throws(apiError);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

        Assert.NotNull(errorResponse);
        Assert.Equal((int)HttpStatusCode.BadRequest, ((JsonElement)errorResponse["statusCode"]).GetInt32());
        Assert.Equal("ValidationFailed", ((JsonElement)errorResponse["errorCode"]).GetString());
        Assert.Equal("Test error message", ((JsonElement)errorResponse["errorMessage"]).GetString());
        Assert.Equal("User-friendly message", ((JsonElement)errorResponse["userFriendlyMessage"]).GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ShouldReturnValidationError()
    {
        // Arrange
        _context.Response.Body = new MemoryStream();
        var argumentException = new ArgumentException("Invalid argument");
        _ = _nextMock.Setup(x => x(_context)).Throws(argumentException);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

        Assert.NotNull(errorResponse);
        Assert.Equal((int)HttpStatusCode.BadRequest, ((JsonElement)errorResponse["statusCode"]).GetInt32());
        Assert.Equal("ValidationFailed", ((JsonElement)errorResponse["errorCode"]).GetString());
        Assert.Equal("Invalid argument", ((JsonElement)errorResponse["errorMessage"]).GetString());
        Assert.Equal("The provided input is invalid. Please check your request and try again.", ((JsonElement)errorResponse["userFriendlyMessage"]).GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenFileNotFoundException_ShouldReturnNotFoundError()
    {
        // Arrange
        _context.Response.Body = new MemoryStream();
        var fileNotFoundException = new FileNotFoundException("File not found");
        _ = _nextMock.Setup(x => x(_context)).Throws(fileNotFoundException);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

        Assert.NotNull(errorResponse);
        Assert.Equal((int)HttpStatusCode.NotFound, ((JsonElement)errorResponse["statusCode"]).GetInt32());
        Assert.Equal("FileNotFound", ((JsonElement)errorResponse["errorCode"]).GetString());
        Assert.Equal("File not found", ((JsonElement)errorResponse["errorMessage"]).GetString());
        Assert.Equal("The requested file could not be found.", ((JsonElement)errorResponse["userFriendlyMessage"]).GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenInvalidOperationException_ShouldReturnBusinessError()
    {
        // Arrange
        _context.Response.Body = new MemoryStream();
        var invalidOperationException = new InvalidOperationException("Invalid operation");
        _ = _nextMock.Setup(x => x(_context)).Throws(invalidOperationException);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

        Assert.NotNull(errorResponse);
        Assert.Equal((int)HttpStatusCode.BadRequest, ((JsonElement)errorResponse["statusCode"]).GetInt32());
        Assert.Equal("InvalidConversionState", ((JsonElement)errorResponse["errorCode"]).GetString());
        Assert.Equal("Invalid operation", ((JsonElement)errorResponse["errorMessage"]).GetString());
        Assert.Equal("The requested operation cannot be completed in the current state.", ((JsonElement)errorResponse["userFriendlyMessage"]).GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenGenericException_ShouldReturnServerError()
    {
        // Arrange
        _context.Response.Body = new MemoryStream();
        var genericException = new NotSupportedException("Generic error");
        _ = _nextMock.Setup(x => x(_context)).Throws(genericException);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, _context.Response.StatusCode);
        Assert.Equal("application/json", _context.Response.ContentType);

        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

        Assert.NotNull(errorResponse);
        Assert.Equal((int)HttpStatusCode.InternalServerError, ((JsonElement)errorResponse["statusCode"]).GetInt32());
        Assert.Equal("InternalServerError", ((JsonElement)errorResponse["errorCode"]).GetString());
        Assert.Equal("An unexpected error occurred.", ((JsonElement)errorResponse["errorMessage"]).GetString());
        Assert.Equal("An unexpected error occurred. Please try again later.", ((JsonElement)errorResponse["userFriendlyMessage"]).GetString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _ = _nextMock.Setup(x => x(_context)).Throws(exception);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unhandled exception occurred")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private async Task<string> GetResponseBody()
    {
        _ = _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
