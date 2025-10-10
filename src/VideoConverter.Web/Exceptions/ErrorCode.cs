namespace VideoConverter.Web.Exceptions;

/// <summary>
/// Enumeration of error codes that can be used programmatically to identify specific errors.
/// </summary>
public enum ErrorCode
{
    // Validation errors (400)
    ValidationFailed = 1001,
    InvalidInput = 1002,
    MissingRequiredField = 1003,
    InvalidFileFormat = 1004,
    InvalidResolution = 1005,
    InvalidBitrate = 1006,
    InvalidCrf = 1007,

    // Resource not found errors (404)
    ConversionNotFound = 2001,
    FileNotFound = 2002,
    QueueEmpty = 2003,

    // Business logic errors (400)
    ConversionAlreadyStarted = 3001,
    ConversionAlreadyCompleted = 3002,
    InvalidConversionState = 3003,
    ProcessStartFailed = 3004,
    MediaProbeFailed = 3005,
    CropDetectionFailed = 3006,

    // Server errors (500)
    DatabaseError = 4001,
    FileSystemError = 4002,
    FfmpegError = 4003,
    MetadataServiceError = 4004,
    InternalServerError = 4999
}
