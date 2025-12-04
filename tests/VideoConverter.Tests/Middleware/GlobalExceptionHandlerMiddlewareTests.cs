using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Api.Common.Middleware;

namespace VideoConverter.Tests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<GlobalExceptionHandlerMiddleware>> _loggerMock;
    private readonly GlobalExceptionHandlerMiddleware _middleware;
    private readonly IApiErrorMapper _mapper;
    private readonly DefaultHttpContext _context;

    public GlobalExceptionHandlerMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        _mapper = new ApiErrorMapper();
        _middleware = new GlobalExceptionHandlerMiddleware(_nextMock.Object, _loggerMock.Object, _mapper);
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
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_WithExceptionData_ShouldLogFormattedErrorDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        exception.Data["exitCode"] = -22;
        exception.Data["command"] = "ffmpeg -i test.mkv";
        exception.Data["stderr"] = "Error message";
        _ = _nextMock.Setup(x => x(_context)).Throws(exception);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error details:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_WithApiErrorDetails_ShouldLogFormattedErrorDetails()
    {
        // Arrange
        var details = new Dictionary<string, object>(StringComparer.Ordinal) { ["field"] = "value" };
        var apiError = ApiErrorException.ValidationError("Test error", "User message", null, details);
        _context.Response.Body = new MemoryStream();
        _ = _nextMock.Setup(x => x(_context)).Throws(apiError);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error details:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private async Task<string> GetResponseBody()
    {
        _ = _context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_context.Response.Body);
        return await reader.ReadToEndAsync(_context.RequestAborted).ConfigureAwait(false);
    }
}
