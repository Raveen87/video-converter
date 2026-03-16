namespace VideoConverter.Web.Tests;

public class AppSettingsJsonIntegrityTests
{
    [Fact]
    public void AppSettingsJsonIntegrity_EmptyContent_IsRejected()
    {
        var result = ValidateJsonObjectContent(string.Empty);

        Assert.False(result.IsValid);
        Assert.Equal("empty", result.ErrorClass);
    }

    [Fact]
    public void AppSettingsJsonIntegrity_MalformedContent_IsRejected()
    {
        var result = ValidateJsonObjectContent("{");

        Assert.False(result.IsValid);
        Assert.Equal("malformed", result.ErrorClass);
    }

    [Fact]
    public void AppSettingsJsonIntegrity_NonObjectRoot_IsRejected()
    {
        var result = ValidateJsonObjectContent("[]");

        Assert.False(result.IsValid);
        Assert.Equal("non-object", result.ErrorClass);
    }

    [Fact]
    public void AppSettingsJsonIntegrity_AllWebAppSettingsFiles_AreValidJsonObjects()
    {
        var appSettingsDirectory = Path.Combine(GetRepositoryRoot(), "src", "VideoConverter.Web", "wwwroot");
        var appSettingsFiles = Directory.GetFiles(appSettingsDirectory, "appsettings*.json", SearchOption.TopDirectoryOnly);

        Assert.NotEmpty(appSettingsFiles);

        foreach (var appSettingsFile in appSettingsFiles)
        {
            var fileContent = File.ReadAllText(appSettingsFile);
            var result = ValidateJsonObjectContent(fileContent);

            Assert.True(
                result.IsValid,
                $"Invalid appsettings JSON file '{appSettingsFile}'. Validation failed with class '{result.ErrorClass}' and message '{result.ErrorMessage}'.");
        }
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    private static (bool IsValid, string? ErrorClass, string? ErrorMessage) ValidateJsonObjectContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, "empty", "JSON content is empty or whitespace.");
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (false, "non-object", $"Root JSON value kind was '{document.RootElement.ValueKind}', expected 'Object'.");
            }
        }
        catch (JsonException ex)
        {
            return (false, "malformed", ex.Message);
        }

        return (true, null, null);
    }
}
