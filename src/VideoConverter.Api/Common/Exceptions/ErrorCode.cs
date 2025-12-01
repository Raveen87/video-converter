namespace VideoConverter.Api.Common.Exceptions;

/// <summary>
/// Enumeration of error codes that can be used programmatically to identify specific errors.
/// </summary>
public enum ErrorCode
{
    // Validation errors (400)
    ValidationFailed,
    InvalidInput,
    MissingRequiredField,
    InvalidFileFormat,
    InvalidResolution,
    InvalidBitrate,
    InvalidCrf,

    // Resource not found errors (404)
    ConversionNotFound,
    FileNotFound,
    QueueEmpty,
    ProfileNotFound,

    // Business logic errors (400)
    ConversionAlreadyStarted,
    ConversionAlreadyCompleted,
    InvalidConversionState,
    ProcessStartFailed,
    MediaProbeFailed,
    CropDetectionFailed,
    MetadataResolutionFailed,

    // Server errors (500)
    DatabaseError,
    FileSystemError,
    FfmpegError,
    MetadataServiceError,
    InternalServerError,
}
