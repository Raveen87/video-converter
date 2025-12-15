namespace VideoConverter.Api.Features.Metadata.Services;

/// <summary>
/// Maps TMDb episode group type codes to human-readable strings.
/// </summary>
public static class EpisodeGroupTypeMapper
{
    /// <summary>
    /// Converts a TMDb numeric episode group type code to a readable string.
    /// </summary>
    /// <param name="typeCode">The TMDb type code (1-7).</param>
    /// <returns>A human-readable type name.</returns>
    public static string GetTypeName(int typeCode)
    {
        return typeCode switch
        {
            1 => "Original Air Date",
            2 => "Absolute",
            3 => "DVD",
            4 => "Digital",
            5 => "Story Arc",
            6 => "Production",
            7 => "TV",
            _ => "Unknown",
        };
    }
}
