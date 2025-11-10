namespace VideoConverter.Api.Features.Metadata.Services;

public interface ITitleSanitizer
{
    string CleanTitle(string title);

    string CleanTitlePreservingDots(string title);

    string CleanEpisodeTitle(string title);
}
