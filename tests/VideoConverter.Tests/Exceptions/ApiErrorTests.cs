using System.Net;

using VideoConverter.Api.Common.Exceptions;
using VideoConverter.Common.Errors;

namespace VideoConverter.Tests.Exceptions;

public class ApiErrorTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var statusCode = HttpStatusCode.BadRequest;
        var errorCode = ErrorCode.ValidationFailed;
        var errorMessage = "Test error message";
        var userFriendlyMessage = "User-friendly message";

        // Act
        var apiError = new ApiErrorException(statusCode, errorCode, errorMessage, userFriendlyMessage);

        // Assert
        Assert.Equal(statusCode, apiError.StatusCode);
        Assert.Equal(errorCode, apiError.ErrorCode);
        Assert.Equal(errorMessage, apiError.ErrorMessage);
        Assert.Equal(userFriendlyMessage, apiError.UserFriendlyMessage);
        Assert.Equal(errorMessage, apiError.Message);
    }

    [Fact]
    public void ValidationError_ShouldCreateCorrectApiError()
    {
        // Arrange
        var errorMessage = "Validation failed";
        var userFriendlyMessage = "Please check your input";

        // Act
        var apiError = ApiErrorException.ValidationError(errorMessage, userFriendlyMessage);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, apiError.StatusCode);
        Assert.Equal(ErrorCode.ValidationFailed, apiError.ErrorCode);
        Assert.Equal(errorMessage, apiError.ErrorMessage);
        Assert.Equal(userFriendlyMessage, apiError.UserFriendlyMessage);
    }

    [Fact]
    public void NotFound_ShouldCreateCorrectApiError()
    {
        // Arrange
        var errorCode = ErrorCode.ConversionNotFound;
        var errorMessage = "Conversion not found";
        var userFriendlyMessage = "The conversion could not be found";
        var innerException = new FileNotFoundException("File not found");

        // Act
        var apiError = ApiErrorException.NotFound(errorCode, errorMessage, userFriendlyMessage, innerException);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, apiError.StatusCode);
        Assert.Equal(errorCode, apiError.ErrorCode);
        Assert.Equal(errorMessage, apiError.ErrorMessage);
        Assert.Equal(userFriendlyMessage, apiError.UserFriendlyMessage);
        Assert.Equal(innerException, apiError.InnerException);
    }

    [Fact]
    public void BusinessError_ShouldCreateCorrectApiError()
    {
        // Arrange
        var errorCode = ErrorCode.ProcessStartFailed;
        var errorMessage = "Process start failed";
        var userFriendlyMessage = "Could not start the process";
        var innerException = new InvalidOperationException("Invalid operation");

        // Act
        var apiError = ApiErrorException.BusinessError(errorCode, errorMessage, userFriendlyMessage, innerException);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, apiError.StatusCode);
        Assert.Equal(errorCode, apiError.ErrorCode);
        Assert.Equal(errorMessage, apiError.ErrorMessage);
        Assert.Equal(userFriendlyMessage, apiError.UserFriendlyMessage);
        Assert.Equal(innerException, apiError.InnerException);
    }

    [Fact]
    public void ServerError_ShouldCreateCorrectApiError()
    {
        // Arrange
        var errorCode = ErrorCode.DatabaseError;
        var errorMessage = "Database connection failed";
        var userFriendlyMessage = "A server error occurred";
        var innerException = new InvalidOperationException("Database error");

        // Act
        var apiError = ApiErrorException.ServerError(errorCode, errorMessage, userFriendlyMessage, innerException);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, apiError.StatusCode);
        Assert.Equal(errorCode, apiError.ErrorCode);
        Assert.Equal(errorMessage, apiError.ErrorMessage);
        Assert.Equal(userFriendlyMessage, apiError.UserFriendlyMessage);
        Assert.Equal(innerException, apiError.InnerException);
    }

    [Fact]
    public void ToDictionary_ShouldReturnCorrectStructure()
    {
        // Arrange
        var statusCode = HttpStatusCode.BadRequest;
        var errorCode = ErrorCode.ValidationFailed;
        var errorMessage = "Test error message";
        var userFriendlyMessage = "User-friendly message";
        var apiError = new ApiErrorException(statusCode, errorCode, errorMessage, userFriendlyMessage);

        // Act
        var dictionary = apiError.ToDictionary();

        // Assert
        Assert.Equal(5, dictionary.Count);
        Assert.Equal((int)statusCode, dictionary["statusCode"]);
        Assert.Equal(errorCode.ToString(), dictionary["errorCode"]);
        Assert.Equal(errorMessage, dictionary["errorMessage"]);
        Assert.Equal(userFriendlyMessage, dictionary["userFriendlyMessage"]);
        Assert.True(dictionary.ContainsKey("timestamp"));
        _ = Assert.IsType<DateTime>(dictionary["timestamp"]);
    }

    [Theory]
    [InlineData(ErrorCode.ValidationFailed, "ValidationFailed")]
    [InlineData(ErrorCode.ConversionNotFound, "ConversionNotFound")]
    [InlineData(ErrorCode.ProcessStartFailed, "ProcessStartFailed")]
    [InlineData(ErrorCode.DatabaseError, "DatabaseError")]
    public void ErrorCode_ShouldHaveCorrectStringRepresentation(ErrorCode errorCode, string expectedName)
    {
        // Act
        var actualName = errorCode.ToString();

        // Assert
        Assert.Equal(expectedName, actualName);
    }
}
