using VideoConverter.Common.Models;

namespace VideoConverter.Web.Features.ManualResolution.Logic;

/// <summary>
/// Contains testable logic for the ResolveMetadataDialog component.
/// </summary>
public static class ResolveMetadataDialogLogic
{
    /// <summary>
    /// Determines whether a search should be triggered based on the key pressed and current state.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="isSearching">Whether a search is currently in progress.</param>
    /// <param name="searchQuery">The current search query.</param>
    /// <returns>True if a search should be triggered; otherwise, false.</returns>
    public static bool ShouldTriggerSearch(string? key, bool isSearching, string? searchQuery)
    {
        return string.Equals(key, "Enter", StringComparison.Ordinal) && !isSearching && !string.IsNullOrWhiteSpace(searchQuery);
    }

    /// <summary>
    /// Gets the default media type selection for the dialog.
    /// The default is always null (representing "All") regardless of the input kind.
    /// </summary>
    /// <param name="inputKind">The detected input kind from the file (Movie, TvShow, or Unknown).
    /// This parameter is intentionally unused as the default is always "All" regardless of input.</param>
    /// <returns>The default media type selection, which is always null (All).</returns>
#pragma warning disable IDE0060 // Remove unused parameter - parameter exists to document that default is independent of inputKind
    public static MediaType? GetDefaultMediaTypeSelection(string? inputKind)
#pragma warning restore IDE0060
    {
        // Per requirements 1.2, 6.1, 6.2, 6.3: Default is always "All" (null)
        // regardless of the detected InputKind
        return null;
    }

    /// <summary>
    /// Creates a metadata search request with the specified parameters.
    /// The search request uses the selected media type directly, not derived from InputKind.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="year">The optional year filter.</param>
    /// <param name="selectedMediaType">The selected media type (null for "All", Movie, or TvShow).</param>
    /// <returns>A MetadataSearchRequestDto with the specified parameters.</returns>
    public static MetadataSearchRequestDto CreateSearchRequest(string query, int? year, MediaType? selectedMediaType)
    {
        // Per requirements 1.3, 2.1, 2.2, 2.3: Use selected media type directly
        return new MetadataSearchRequestDto
        {
            Query = query,
            Year = year,
            Type = selectedMediaType,
        };
    }

    /// <summary>
    /// Determines whether episode group state should be cleared when a result is selected.
    /// Episode group state should always be cleared when any result is selected, regardless of type.
    /// This ensures clean state transitions, especially when switching from TV show to movie.
    /// </summary>
    /// <param name="selectedResultType">The media type of the selected result (null if no result selected).</param>
    /// <returns>True if episode group state should be cleared; always returns true.</returns>
    public static bool ShouldClearEpisodeGroupState(MediaType? selectedResultType)
    {
        // Per requirements 4.1, 4.2, 4.3: Episode group state should always be cleared
        // when a result is selected. This ensures:
        // - Clean state when switching from TV show to movie (4.3)
        // - Clean state when switching between different TV shows
        // - Episode groups are only shown for TV shows (4.1, 4.2)
        _ = selectedResultType; // Parameter documents that clearing is independent of type
        return true;
    }

    /// <summary>
    /// Determines whether episode groups should be fetched for the selected result.
    /// Episode groups should only be fetched when a TV show is selected.
    /// </summary>
    /// <param name="selectedResultType">The media type of the selected result (null if no result selected).</param>
    /// <returns>True if episode groups should be fetched; false otherwise.</returns>
    public static bool ShouldFetchEpisodeGroups(MediaType? selectedResultType)
    {
        // Per requirements 4.1, 4.2: Episode groups are only relevant for TV shows
        return selectedResultType is MediaType.TvShow;
    }

    /// <summary>
    /// Gets the display text for a media type.
    /// Per requirements 3.2 and 3.3: Movies display as "Movie", TV shows display as "TV Show".
    /// </summary>
    /// <param name="mediaType">The media type to format.</param>
    /// <returns>The formatted display text for the media type.</returns>
    public static string GetMediaTypeDisplayText(MediaType mediaType)
    {
        // Per requirements 3.2, 3.3: Display "Movie" for movies, "TV Show" for TV shows
        return mediaType switch
        {
            MediaType.Movie => "Movie",
            MediaType.TvShow => "TV Show",
            _ => mediaType.ToString(),
        };
    }

    /// <summary>
    /// Simulates the media type selection state after a sequence of user actions.
    /// Per requirements 5.1, 5.2: The selection persists within a session and is used for subsequent searches.
    /// This method models the state machine behavior of the media type selector.
    /// </summary>
    /// <param name="initialSelection">The initial media type selection (null for "All").</param>
    /// <param name="userSelections">A sequence of user selection actions (null represents "All" button click).</param>
    /// <returns>The final media type selection after all user actions.</returns>
    public static MediaType? SimulateMediaTypeSelectionState(MediaType? initialSelection, IEnumerable<MediaType?> userSelections)
    {
        // Per requirements 5.1, 5.2: Selection persists within session
        // Each user selection replaces the previous one
        var currentSelection = initialSelection;
        foreach (var selection in userSelections)
        {
            currentSelection = selection;
        }

        return currentSelection;
    }

    /// <summary>
    /// Gets the initial media type selection when a dialog is opened.
    /// Per requirement 5.3: The selection resets to "All" (null) when the dialog is reopened.
    /// </summary>
    /// <returns>The initial media type selection, which is always null (All).</returns>
    public static MediaType? GetInitialMediaTypeSelectionOnDialogOpen()
    {
        // Per requirement 5.3: Selection resets to "All" when dialog is reopened
        return null;
    }

    /// <summary>
    /// Determines whether the search results cache should be cleared based on query changes.
    /// Per requirement 7.4: When a user modifies the search query text, all cached results for all media types SHALL be cleared.
    /// </summary>
    /// <param name="previousQuery">The previous query value (can be null or empty).</param>
    /// <param name="currentQuery">The current query value (can be null or empty).</param>
    /// <returns>True if the cache should be cleared because the query changed; otherwise, false.</returns>
    public static bool ShouldClearCacheOnQueryChange(string? previousQuery, string? currentQuery)
    {
        // Per requirement 7.4: Clear cache when query text changes
        // Normalize null and empty strings to be equivalent for comparison
        var normalizedPrevious = string.IsNullOrEmpty(previousQuery) ? string.Empty : previousQuery;
        var normalizedCurrent = string.IsNullOrEmpty(currentQuery) ? string.Empty : currentQuery;

        return !string.Equals(normalizedPrevious, normalizedCurrent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Manages search results caching per media type for the ResolveMetadataDialog.
    /// Allows restoring previous results when switching between media types without making redundant API calls.
    /// </summary>
    public sealed class ResultsCache
    {
        // Use string key to represent MediaType? (null = "All", "Movie", "TvShow")
        private readonly Dictionary<string, List<MetadataSearchResultDto>> _cache = [];

        /// <summary>
        /// Gets the number of media types with cached results.
        /// </summary>
        public int CachedMediaTypeCount => _cache.Count;

        /// <summary>
        /// Caches the given results for the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type to cache results for (null for "All").</param>
        /// <param name="results">The search results to cache.</param>
        public void CacheResults(MediaType? mediaType, IEnumerable<MetadataSearchResultDto> results)
        {
            _cache[GetCacheKey(mediaType)] = [.. results];
        }

        /// <summary>
        /// Attempts to restore cached results for the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type to restore results for (null for "All").</param>
        /// <param name="results">The restored results if found; otherwise, an empty list.</param>
        /// <returns>True if cached results were found and restored; otherwise, false.</returns>
        public bool TryRestoreResults(MediaType? mediaType, out IReadOnlyList<MetadataSearchResultDto> results)
        {
            if (_cache.TryGetValue(GetCacheKey(mediaType), out var cached))
            {
                results = [.. cached]; // Return a copy to prevent external modification
                return true;
            }

            results = [];
            return false;
        }

        /// <summary>
        /// Clears all cached results for all media types.
        /// </summary>
        public void ClearAll()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Gets whether there are any cached results for the specified media type.
        /// </summary>
        /// <param name="mediaType">The media type to check (null for "All").</param>
        /// <returns>True if there are cached results for the media type; otherwise, false.</returns>
        public bool HasCachedResults(MediaType? mediaType)
        {
            return _cache.ContainsKey(GetCacheKey(mediaType));
        }

        private static string GetCacheKey(MediaType? mediaType)
        {
            return mediaType?.ToString() ?? "All";
        }
    }
}
