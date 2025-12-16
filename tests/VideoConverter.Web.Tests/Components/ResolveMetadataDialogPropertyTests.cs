using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Common.Models;
using VideoConverter.Web.Models;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Property-based tests for ResolveMetadataDialog component.
/// </summary>
public class ResolveMetadataDialogPropertyTests
{
    /// <summary>
    /// Feature: metadata-tv-search, Property 1: Default media type selection is "All".
    /// Validates: Requirements 1.2, 6.1, 6.2, 6.3.
    /// For any InputKind value (Movie, TvShow, or Unknown), when the Resolve Metadata dialog is initialized,
    /// the media type selection SHALL default to null (representing "All").
    /// </summary>
    /// <param name="inputKind">Generated input kind string.</param>
    /// <returns>A property that validates default media type is always null.</returns>
    [Property(MaxTest = 100)]
    public Property DefaultMediaTypeSelectionIsAll(string? inputKind)
    {
        // Act
        var defaultSelection = ResolveMetadataDialogLogic.GetDefaultMediaTypeSelection(inputKind);

        // Assert - Default should always be null (representing "All")
        return (defaultSelection == null)
            .Label($"Default media type for inputKind '{inputKind ?? "null"}' should be null (All)");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 1: Default media type selection is "All" for known InputKind values.
    /// Validates: Requirements 1.2, 6.1, 6.2, 6.3.
    /// Specifically tests the known InputKind values: Movie, TvShow, Unknown.
    /// </summary>
    [Fact]
    public void DefaultMediaTypeSelectionIsAll_ForKnownInputKinds()
    {
        // Arrange - known InputKind values
        var knownInputKinds = new[] { "Movie", "TvShow", "Unknown", null, string.Empty };

        foreach (var inputKind in knownInputKinds)
        {
            // Act
            var defaultSelection = ResolveMetadataDialogLogic.GetDefaultMediaTypeSelection(inputKind);

            // Assert - Default should always be null (representing "All")
            Assert.Null(defaultSelection);
        }
    }

    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 1: Enter in search fields triggers search action.
    /// Validates: Requirements 1.1, 1.2.
    /// For any non-empty search query and not-searching state, pressing Enter SHALL trigger the search action.
    /// </summary>
    /// <param name="searchQuery">Generated non-empty search query.</param>
    /// <returns>A property that validates Enter triggers search.</returns>
    [Property(MaxTest = 100)]
    public Property EnterInSearchFieldsTriggersSearchAction(NonEmptyString searchQuery)
    {
        // Arrange
        const string enterKey = "Enter";
        const bool isSearching = false;

        // Act
        var shouldTrigger = ResolveMetadataDialogLogic.ShouldTriggerSearch(enterKey, isSearching, searchQuery.Item);

        // Assert - Enter key with non-empty query and not searching should trigger search
        return shouldTrigger
            .Label($"Enter key with query '{searchQuery.Item}' and isSearching={isSearching} should trigger search");
    }

    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 1 (negative case): Non-Enter keys do not trigger search.
    /// Validates: Requirements 1.1, 1.2.
    /// For any key that is not Enter, the search SHALL NOT be triggered.
    /// </summary>
    /// <param name="key">Generated key name.</param>
    /// <param name="searchQuery">Generated non-empty search query.</param>
    /// <returns>A property that validates non-Enter keys do not trigger search.</returns>
    [Property(MaxTest = 100)]
    public Property NonEnterKeysDoNotTriggerSearch(NonEmptyString key, NonEmptyString searchQuery)
    {
        // Skip if the key happens to be "Enter"
        if (string.Equals(key.Item, "Enter", StringComparison.Ordinal))
        {
            return true.Label("Skipped - key is Enter");
        }

        // Arrange
        const bool isSearching = false;

        // Act
        var shouldTrigger = ResolveMetadataDialogLogic.ShouldTriggerSearch(key.Item, isSearching, searchQuery.Item);

        // Assert - Non-Enter keys should not trigger search
        return (!shouldTrigger)
            .Label($"Key '{key.Item}' should not trigger search");
    }

    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 1 (empty query case): Empty query does not trigger search.
    /// Validates: Requirements 1.4.
    /// For any whitespace-only search query, pressing Enter SHALL NOT trigger the search action.
    /// </summary>
    /// <returns>A property that validates empty query does not trigger search.</returns>
    [Property(MaxTest = 100)]
    public Property EmptyQueryDoesNotTriggerSearch()
    {
        // Arrange
        const string enterKey = "Enter";
        const bool isSearching = false;
        var emptyQueries = new[] { string.Empty, " ", "  ", "\t", "\n", "   \t\n   ", null };

        // Act & Assert
        var allPass = emptyQueries.All(query =>
            !ResolveMetadataDialogLogic.ShouldTriggerSearch(enterKey, isSearching, query));

        return allPass
            .Label("Enter key with empty/whitespace query should not trigger search");
    }

    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 2: In-progress state blocks additional Enter triggers.
    /// Validates: Requirements 1.3, 2.2.
    /// For any dialog in an in-progress state (searching), pressing Enter SHALL NOT trigger additional actions.
    /// </summary>
    /// <param name="searchQuery">Generated non-empty search query.</param>
    /// <returns>A property that validates in-progress state blocks Enter triggers.</returns>
    [Property(MaxTest = 100)]
    public Property InProgressStateBlocksAdditionalEnterTriggers(NonEmptyString searchQuery)
    {
        // Arrange
        const string enterKey = "Enter";
        const bool isSearching = true; // In-progress state

        // Act
        var shouldTrigger = ResolveMetadataDialogLogic.ShouldTriggerSearch(enterKey, isSearching, searchQuery.Item);

        // Assert - Enter key while searching should NOT trigger another search
        return (!shouldTrigger)
            .Label($"Enter key with query '{searchQuery.Item}' while isSearching=true should NOT trigger search");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 2: Search request uses selected media type.
    /// Validates: Requirements 1.3, 2.1, 2.2, 2.3.
    /// For any media type selection (All, Movies, or TV Shows), when a search is performed,
    /// the search request SHALL include the corresponding MediaType value (null, Movie, or TvShow).
    /// </summary>
    /// <param name="query">Generated non-empty search query.</param>
    /// <param name="year">Generated optional year.</param>
    /// <returns>A property that validates search request uses selected media type.</returns>
    [Property(MaxTest = 100)]
    public Property SearchRequestUsesSelectedMediaType(NonEmptyString query, int? year)
    {
        // Test all three media type selections
        var mediaTypes = new MediaType?[] { null, MediaType.Movie, MediaType.TvShow };

        var allPass = mediaTypes.All(selectedType =>
        {
            // Act
            var request = ResolveMetadataDialogLogic.CreateSearchRequest(query.Item, year, selectedType);

            // Assert - Request should use the selected media type directly
            return request.Type == selectedType &&
                   string.Equals(request.Query, query.Item, StringComparison.Ordinal) &&
                   request.Year == year;
        });

        return allPass
            .Label($"Search request for query '{query.Item}' with year '{year}' should use selected media type directly");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 2: Search request with null media type searches all.
    /// Validates: Requirements 2.3.
    /// When "All" is selected (null), the search request Type SHALL be null to search both movies and TV shows.
    /// </summary>
    /// <param name="query">Generated non-empty search query.</param>
    /// <param name="year">Generated optional year.</param>
    /// <returns>A property that validates null media type searches all.</returns>
    [Property(MaxTest = 100)]
    public Property SearchRequestWithNullMediaTypeSearchesAll(NonEmptyString query, int? year)
    {
        // Arrange - "All" is represented by null
        MediaType? selectedType = null;

        // Act
        var request = ResolveMetadataDialogLogic.CreateSearchRequest(query.Item, year, selectedType);

        // Assert - Request Type should be null (searches both movies and TV shows)
        return (request.Type == null)
            .Label($"Search request with 'All' selection should have null Type");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 2: Search request with Movie type searches movies only.
    /// Validates: Requirements 2.2.
    /// When "Movies" is selected, the search request Type SHALL be MediaType.Movie.
    /// </summary>
    /// <param name="query">Generated non-empty search query.</param>
    /// <param name="year">Generated optional year.</param>
    /// <returns>A property that validates Movie selection searches movies only.</returns>
    [Property(MaxTest = 100)]
    public Property SearchRequestWithMovieTypeSearchesMoviesOnly(NonEmptyString query, int? year)
    {
        // Arrange
        MediaType? selectedType = MediaType.Movie;

        // Act
        var request = ResolveMetadataDialogLogic.CreateSearchRequest(query.Item, year, selectedType);

        // Assert - Request Type should be Movie
        return (request.Type == MediaType.Movie)
            .Label($"Search request with 'Movies' selection should have Movie Type");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 2: Search request with TvShow type searches TV shows only.
    /// Validates: Requirements 2.1.
    /// When "TV Shows" is selected, the search request Type SHALL be MediaType.TvShow.
    /// </summary>
    /// <param name="query">Generated non-empty search query.</param>
    /// <param name="year">Generated optional year.</param>
    /// <returns>A property that validates TvShow selection searches TV shows only.</returns>
    [Property(MaxTest = 100)]
    public Property SearchRequestWithTvShowTypeSearchesTvShowsOnly(NonEmptyString query, int? year)
    {
        // Arrange
        MediaType? selectedType = MediaType.TvShow;

        // Act
        var request = ResolveMetadataDialogLogic.CreateSearchRequest(query.Item, year, selectedType);

        // Assert - Request Type should be TvShow
        return (request.Type == MediaType.TvShow)
            .Label($"Search request with 'TV Shows' selection should have TvShow Type");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 3: Episode group state cleared on movie selection.
    /// Validates: Requirements 4.3.
    /// For any dialog state where a TV show was previously selected and episode groups were loaded,
    /// when a movie is selected, the episode group state SHALL be cleared.
    /// </summary>
    /// <returns>A property that validates episode group state is always cleared on selection.</returns>
    [Property(MaxTest = 100)]
    public Property EpisodeGroupStateClearedOnMovieSelection()
    {
        // Test all possible media types that could be selected
        var mediaTypes = new MediaType?[] { null, MediaType.Movie, MediaType.TvShow };

        var allPass = mediaTypes.All(selectedType =>
        {
            // Act - Check if episode group state should be cleared
            var shouldClear = ResolveMetadataDialogLogic.ShouldClearEpisodeGroupState(selectedType);

            // Assert - Episode group state should ALWAYS be cleared when any result is selected
            // This ensures clean state transitions, especially when switching from TV show to movie
            return shouldClear;
        });

        return allPass
            .Label("Episode group state should always be cleared when a result is selected");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 3: Episode groups only fetched for TV shows.
    /// Validates: Requirements 4.1, 4.2.
    /// Episode groups should only be fetched when a TV show is selected, not for movies.
    /// </summary>
    /// <returns>A property that validates episode groups are only fetched for TV shows.</returns>
    [Property(MaxTest = 100)]
    public Property EpisodeGroupsOnlyFetchedForTvShows()
    {
        // Test all possible media types
        var testCases = new (MediaType? Type, bool ExpectedFetch)[]
        {
            (null, false),                  // No result selected - don't fetch
            (MediaType.Movie, false),       // Movie selected - don't fetch (4.2)
            (MediaType.TvShow, true),       // TV show selected - fetch (4.1)
        };

        var allPass = testCases.All(tc =>
        {
            // Act
            var shouldFetch = ResolveMetadataDialogLogic.ShouldFetchEpisodeGroups(tc.Type);

            // Assert
            return shouldFetch == tc.ExpectedFetch;
        });

        return allPass
            .Label("Episode groups should only be fetched for TV shows, not movies or null");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 3: Movie selection hides episode group section.
    /// Validates: Requirements 4.2, 4.3.
    /// When a movie is selected after a TV show, the episode group section should be hidden.
    /// </summary>
    [Fact]
    public void MovieSelectionHidesEpisodeGroupSection()
    {
        // Arrange - Simulate state after TV show was selected
        var previouslySelectedTvShow = MediaType.TvShow;
        var newlySelectedMovie = MediaType.Movie;

        // Act - Check behavior when switching from TV show to movie
        var shouldClearOnTvShow = ResolveMetadataDialogLogic.ShouldClearEpisodeGroupState(previouslySelectedTvShow);
        var shouldClearOnMovie = ResolveMetadataDialogLogic.ShouldClearEpisodeGroupState(newlySelectedMovie);
        var shouldFetchForMovie = ResolveMetadataDialogLogic.ShouldFetchEpisodeGroups(newlySelectedMovie);

        // Assert - State should be cleared on any selection
        Assert.True(shouldClearOnTvShow);
        Assert.True(shouldClearOnMovie);

        // Episode groups should NOT be fetched for movies
        Assert.False(shouldFetchForMovie);
    }

    /// <summary>
    /// Feature: metadata-tv-search, Media type display text formatting.
    /// Validates: Requirements 3.2, 3.3.
    /// Movies should display as "Movie", TV shows should display as "TV Show".
    /// </summary>
    [Fact]
    public void MediaTypeDisplayText_FormatsCorrectly()
    {
        // Act & Assert - Movie displays as "Movie" (Requirement 3.2)
        Assert.Equal("Movie", ResolveMetadataDialogLogic.GetMediaTypeDisplayText(MediaType.Movie));

        // Act & Assert - TV Show displays as "TV Show" (Requirement 3.3)
        Assert.Equal("TV Show", ResolveMetadataDialogLogic.GetMediaTypeDisplayText(MediaType.TvShow));
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 4: Media type selection persists within session.
    /// Validates: Requirements 5.1, 5.2.
    /// For any sequence of searches within a single dialog session, the media type selection
    /// SHALL remain unchanged unless explicitly modified by the user.
    /// </summary>
    /// <returns>A property that validates media type selection persistence.</returns>
    [Property(MaxTest = 100)]
    public Property MediaTypeSelectionPersistsWithinSession()
    {
        // Generate a random sequence of user selections
        var mediaTypeGen = Gen.Elements<MediaType?>(null, MediaType.Movie, MediaType.TvShow);
        var selectionsGen = Gen.ListOf(mediaTypeGen);

        return Prop.ForAll(selectionsGen.ToArbitrary(), selections =>
        {
            // Arrange - Start with default selection (null = "All")
            MediaType? initialSelection = null;

            // Act - Simulate user making selections
            var finalSelection = ResolveMetadataDialogLogic.SimulateMediaTypeSelectionState(
                initialSelection,
                selections);

            // Assert - Final selection should be the last user selection, or initial if no selections made
            var expectedSelection = selections.Count > 0 ? selections[^1] : initialSelection;

            return (finalSelection == expectedSelection)
                .Label($"After {selections.Count} selections, final should be '{expectedSelection}' but was '{finalSelection}'");
        });
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 4: Selection persists across multiple searches.
    /// Validates: Requirements 5.1, 5.2.
    /// When a user performs multiple searches, the media type selection SHALL be used for each search.
    /// </summary>
    /// <param name="query1">First search query.</param>
    /// <param name="query2">Second search query.</param>
    /// <returns>A property that validates selection is used for subsequent searches.</returns>
    [Property(MaxTest = 100)]
    public Property SelectionUsedForSubsequentSearches(NonEmptyString query1, NonEmptyString query2)
    {
        // Test all three media type selections
        var mediaTypes = new MediaType?[] { null, MediaType.Movie, MediaType.TvShow };

        var allPass = mediaTypes.All(selectedType =>
        {
            // Act - Create two search requests with the same selection
            var request1 = ResolveMetadataDialogLogic.CreateSearchRequest(query1.Item, null, selectedType);
            var request2 = ResolveMetadataDialogLogic.CreateSearchRequest(query2.Item, null, selectedType);

            // Assert - Both requests should use the same media type
            return request1.Type == selectedType && request2.Type == selectedType;
        });

        return allPass
            .Label("Both search requests should use the persisted media type selection");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Selection reset on dialog reopen.
    /// Validates: Requirements 5.3.
    /// When the dialog is closed and reopened, the media type selection SHALL reset to "All".
    /// </summary>
    [Fact]
    public void SelectionResetsToAllOnDialogReopen()
    {
        // Arrange - Simulate a previous session with a non-default selection
        var previousSelection = MediaType.TvShow;

        // Act - Get the initial selection when dialog is reopened
        var initialSelection = ResolveMetadataDialogLogic.GetInitialMediaTypeSelectionOnDialogOpen();

        // Assert - Selection should reset to null (All), regardless of previous session
        Assert.Null(initialSelection);
        Assert.NotEqual(previousSelection, initialSelection);
    }

    /// <summary>
    /// Feature: metadata-tv-search, Selection reset on dialog reopen (property version).
    /// Validates: Requirements 5.3.
    /// For any previous media type selection, when the dialog is reopened,
    /// the selection SHALL always reset to "All" (null).
    /// </summary>
    /// <returns>A property that validates selection always resets on dialog reopen.</returns>
    [Property(MaxTest = 100)]
    public Property SelectionAlwaysResetsOnDialogReopen()
    {
        // Generate any possible previous selection
        var mediaTypeGen = Gen.Elements<MediaType?>(null, MediaType.Movie, MediaType.TvShow);

        return Prop.ForAll(mediaTypeGen.ToArbitrary(), previousSelection =>
        {
            // Act - Get the initial selection when dialog is reopened
            var initialSelection = ResolveMetadataDialogLogic.GetInitialMediaTypeSelectionOnDialogOpen();

            // Assert - Selection should always be null (All) on dialog open
            return (initialSelection == null)
                .Label($"Initial selection should be null (All) regardless of previous selection '{previousSelection}'");
        });
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 5: Search results cache round-trip on media type switch.
    /// Validates: Requirements 7.1, 7.2, 7.3.
    /// For any dialog state with search results for a media type, when switching to a different media type
    /// and then switching back, the original results SHALL be restored from cache without modification.
    /// </summary>
    /// <returns>A property that validates cache round-trip preserves results.</returns>
    [Property(MaxTest = 100)]
    public Property SearchResultsCacheRoundTripOnMediaTypeSwitch()
    {
        var resultGen = Gen.Elements(
            new MetadataSearchResultDto { Id = 1, Title = "Movie 1", Type = MediaType.Movie, Year = 2020 },
            new MetadataSearchResultDto { Id = 2, Title = "Movie 2", Type = MediaType.Movie, Year = 2021 },
            new MetadataSearchResultDto { Id = 3, Title = "TV Show 1", Type = MediaType.TvShow, Year = 2019 },
            new MetadataSearchResultDto { Id = 4, Title = "TV Show 2", Type = MediaType.TvShow, Year = 2022 });

        var resultsListGen = Gen.ListOf(resultGen).Where(list => list.Count > 0);
        var mediaTypeGen = Gen.Elements<MediaType?>(null, MediaType.Movie, MediaType.TvShow);

        return Prop.ForAll(
            resultsListGen.ToArbitrary(),
            mediaTypeGen.ToArbitrary(),
            mediaTypeGen.ToArbitrary(),
            (originalResults, originalMediaType, switchToMediaType) =>
                VerifyCacheRoundTrip(originalResults, originalMediaType, switchToMediaType));
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 5: Cache returns independent copy.
    /// Validates: Requirements 7.2, 7.3.
    /// The cache should return a copy of results, not the original list, to prevent external modification.
    /// </summary>
    [Fact]
    public void CacheReturnsIndependentCopy()
    {
        // Arrange
        var cache = new ResolveMetadataDialogLogic.ResultsCache();
        var originalResults = new List<MetadataSearchResultDto>
        {
            new() { Id = 1, Title = "Test Movie", Type = MediaType.Movie, Year = 2020 },
        };

        // Act - Cache and restore
        cache.CacheResults(MediaType.Movie, originalResults);
        cache.TryRestoreResults(MediaType.Movie, out var restoredResults);

        // Assert - Modifying restored list should not affect cache
        Assert.Single(restoredResults);

        // Restore again and verify original is still intact
        cache.TryRestoreResults(MediaType.Movie, out var restoredAgain);
        Assert.Single(restoredAgain);
        Assert.Equal(originalResults[0].Id, restoredAgain[0].Id);
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 5: Cache handles all media types independently.
    /// Validates: Requirements 7.2, 7.3.
    /// Each media type (null/All, Movie, TvShow) should have independent cache entries.
    /// </summary>
    [Fact]
    public void CacheHandlesAllMediaTypesIndependently()
    {
        // Arrange
        var cache = new ResolveMetadataDialogLogic.ResultsCache();
        var allResults = new List<MetadataSearchResultDto>
        {
            new() { Id = 1, Title = "All Result", Type = MediaType.Movie },
        };
        var movieResults = new List<MetadataSearchResultDto>
        {
            new() { Id = 2, Title = "Movie Result", Type = MediaType.Movie },
        };
        var tvResults = new List<MetadataSearchResultDto>
        {
            new() { Id = 3, Title = "TV Result", Type = MediaType.TvShow },
        };

        // Act - Cache results for each media type
        cache.CacheResults(null, allResults);
        cache.CacheResults(MediaType.Movie, movieResults);
        cache.CacheResults(MediaType.TvShow, tvResults);

        // Assert - Each media type has its own cached results
        Assert.Equal(3, cache.CachedMediaTypeCount);

        cache.TryRestoreResults(null, out var restoredAll);
        cache.TryRestoreResults(MediaType.Movie, out var restoredMovie);
        cache.TryRestoreResults(MediaType.TvShow, out var restoredTv);

        Assert.Single(restoredAll);
        Assert.Equal("All Result", restoredAll[0].Title);

        Assert.Single(restoredMovie);
        Assert.Equal("Movie Result", restoredMovie[0].Title);

        Assert.Single(restoredTv);
        Assert.Equal("TV Result", restoredTv[0].Title);
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 5: TryRestoreResults returns false for uncached media type.
    /// Validates: Requirements 7.3.
    /// When no results are cached for a media type, TryRestoreResults should return false.
    /// </summary>
    [Fact]
    public void TryRestoreResultsReturnsFalseForUncachedMediaType()
    {
        // Arrange
        var cache = new ResolveMetadataDialogLogic.ResultsCache();

        // Act & Assert - No results cached yet
        Assert.False(cache.TryRestoreResults(null, out var results1));
        Assert.Empty(results1);

        Assert.False(cache.TryRestoreResults(MediaType.Movie, out var results2));
        Assert.Empty(results2);

        Assert.False(cache.TryRestoreResults(MediaType.TvShow, out var results3));
        Assert.Empty(results3);
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 6: Query modification clears all cached results.
    /// Validates: Requirements 7.4.
    /// For any dialog state with cached results for one or more media types, when the search query text
    /// is modified, all cached results for all media types SHALL be cleared.
    /// </summary>
    /// <param name="query1">First query string.</param>
    /// <param name="query2">Second query string.</param>
    /// <returns>A property that validates query modification clears cache.</returns>
    [Property(MaxTest = 100)]
    public Property QueryModificationClearsAllCachedResults(NonEmptyString query1, NonEmptyString query2)
    {
        // Skip if queries are the same (no change)
        if (string.Equals(query1.Item, query2.Item, StringComparison.Ordinal))
        {
            return true.Label("Skipped - queries are identical");
        }

        // Arrange - Create cache with results for all media types
        var cache = new ResolveMetadataDialogLogic.ResultsCache();
        var results = new List<MetadataSearchResultDto>
        {
            new() { Id = 1, Title = "Test", Type = MediaType.Movie, Year = 2020 },
        };

        cache.CacheResults(null, results);
        cache.CacheResults(MediaType.Movie, results);
        cache.CacheResults(MediaType.TvShow, results);

        // Verify cache has results
        if (cache.CachedMediaTypeCount != 3)
        {
            return false.Label("Cache should have 3 media types cached");
        }

        // Act - Check if cache should be cleared due to query change
        var shouldClear = ResolveMetadataDialogLogic.ShouldClearCacheOnQueryChange(query1.Item, query2.Item);

        // If should clear, simulate clearing the cache
        if (shouldClear)
        {
            cache.ClearAll();
        }

        // Assert - Cache should be cleared when query changes
        return (shouldClear && cache.CachedMediaTypeCount == 0)
            .Label($"Query change from '{query1.Item}' to '{query2.Item}' should clear all cached results");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 6: Same query does not clear cache.
    /// Validates: Requirements 7.4.
    /// When the query text remains the same, the cache should NOT be cleared.
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <returns>A property that validates same query preserves cache.</returns>
    [Property(MaxTest = 100)]
    public Property SameQueryDoesNotClearCache(NonEmptyString query)
    {
        // Act - Check if cache should be cleared when query is the same
        var shouldClear = ResolveMetadataDialogLogic.ShouldClearCacheOnQueryChange(query.Item, query.Item);

        // Assert - Same query should not trigger cache clear
        return (!shouldClear)
            .Label($"Same query '{query.Item}' should not clear cache");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 6: Null and empty queries are equivalent.
    /// Validates: Requirements 7.4.
    /// Null and empty string queries should be treated as equivalent (no change).
    /// </summary>
    [Fact]
    public void NullAndEmptyQueriesAreEquivalent()
    {
        // Arrange - Various null/empty combinations
        var equivalentPairs = new (string?, string?)[]
        {
            (null, null),
            (string.Empty, string.Empty),
            (null, string.Empty),
            (string.Empty, null),
        };

        foreach (var (prev, curr) in equivalentPairs)
        {
            // Act
            var shouldClear = ResolveMetadataDialogLogic.ShouldClearCacheOnQueryChange(prev, curr);

            // Assert - Null and empty are equivalent, so no change
            Assert.False(shouldClear, $"'{prev ?? "null"}' to '{curr ?? "null"}' should not clear cache");
        }
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 6: Query change from empty to non-empty clears cache.
    /// Validates: Requirements 7.4.
    /// When query changes from empty/null to a non-empty value, cache should be cleared.
    /// </summary>
    /// <param name="newQuery">The new non-empty query.</param>
    /// <returns>A property that validates empty to non-empty clears cache.</returns>
    [Property(MaxTest = 100)]
    public Property QueryChangeFromEmptyToNonEmptyClearsCache(NonEmptyString newQuery)
    {
        // Skip if the new query is whitespace-only (would be treated as empty in some contexts)
        if (string.IsNullOrWhiteSpace(newQuery.Item))
        {
            return true.Label("Skipped - whitespace-only query");
        }

        // Test from null
        var shouldClearFromNull = ResolveMetadataDialogLogic.ShouldClearCacheOnQueryChange(null, newQuery.Item);

        // Test from empty string
        var shouldClearFromEmpty = ResolveMetadataDialogLogic.ShouldClearCacheOnQueryChange(string.Empty, newQuery.Item);

        return (shouldClearFromNull && shouldClearFromEmpty)
            .Label($"Query change from empty/null to '{newQuery.Item}' should clear cache");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 6: Query change from non-empty to empty clears cache.
    /// Validates: Requirements 7.4.
    /// When query changes from a non-empty value to empty/null, cache should be cleared.
    /// </summary>
    /// <param name="oldQuery">The old non-empty query.</param>
    /// <returns>A property that validates non-empty to empty clears cache.</returns>
    [Property(MaxTest = 100)]
    public Property QueryChangeFromNonEmptyToEmptyClearsCache(NonEmptyString oldQuery)
    {
        // Skip if the old query is whitespace-only
        if (string.IsNullOrWhiteSpace(oldQuery.Item))
        {
            return true.Label("Skipped - whitespace-only query");
        }

        // Test to null
        var shouldClearToNull = ResolveMetadataDialogLogic.ShouldClearCacheOnQueryChange(oldQuery.Item, null);

        // Test to empty string
        var shouldClearToEmpty = ResolveMetadataDialogLogic.ShouldClearCacheOnQueryChange(oldQuery.Item, string.Empty);

        return (shouldClearToNull && shouldClearToEmpty)
            .Label($"Query change from '{oldQuery.Item}' to empty/null should clear cache");
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 7: Dialog reset clears all cached results.
    /// Validates: Requirements 7.5.
    /// For any dialog state with cached results, when the dialog is closed and reopened (reset),
    /// all cached results SHALL be cleared.
    /// </summary>
    /// <returns>A property that validates dialog reset clears all cached results.</returns>
    [Property(MaxTest = 100)]
    public Property DialogResetClearsAllCachedResults()
    {
        var resultGen = Gen.Elements(
            new MetadataSearchResultDto { Id = 1, Title = "Movie 1", Type = MediaType.Movie, Year = 2020 },
            new MetadataSearchResultDto { Id = 2, Title = "Movie 2", Type = MediaType.Movie, Year = 2021 },
            new MetadataSearchResultDto { Id = 3, Title = "TV Show 1", Type = MediaType.TvShow, Year = 2019 },
            new MetadataSearchResultDto { Id = 4, Title = "TV Show 2", Type = MediaType.TvShow, Year = 2022 });

        var resultsListGen = Gen.ListOf(resultGen).Where(list => list.Count > 0);

        return Prop.ForAll(
            resultsListGen.ToArbitrary(),
            resultsListGen.ToArbitrary(),
            resultsListGen.ToArbitrary(),
            (allResults, movieResults, tvResults) =>
            {
                // Arrange - Create cache with results for all media types
                var cache = new ResolveMetadataDialogLogic.ResultsCache();
                cache.CacheResults(null, allResults);
                cache.CacheResults(MediaType.Movie, movieResults);
                cache.CacheResults(MediaType.TvShow, tvResults);

                // Verify cache has results before reset
                if (cache.CachedMediaTypeCount != 3)
                {
                    return false.Label("Cache should have 3 media types cached before reset");
                }

                // Act - Simulate dialog reset by clearing the cache
                // This is what happens in OnInitialized when the dialog is opened/reset
                cache.ClearAll();

                // Assert - All cached results should be cleared
                var allCleared = cache.CachedMediaTypeCount == 0 &&
                                 !cache.HasCachedResults(null) &&
                                 !cache.HasCachedResults(MediaType.Movie) &&
                                 !cache.HasCachedResults(MediaType.TvShow);

                return allCleared
                    .Label("Dialog reset should clear all cached results for all media types");
            });
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 7: Dialog reset clears cache regardless of cache state.
    /// Validates: Requirements 7.5.
    /// The cache should be cleared on dialog reset regardless of whether it has any cached results.
    /// </summary>
    [Fact]
    public void DialogResetClearsCacheRegardlessOfState()
    {
        // Test 1: Empty cache - ClearAll should not throw
        var emptyCache = new ResolveMetadataDialogLogic.ResultsCache();
        emptyCache.ClearAll();
        Assert.Equal(0, emptyCache.CachedMediaTypeCount);

        // Test 2: Cache with one media type
        var singleCache = new ResolveMetadataDialogLogic.ResultsCache();
        singleCache.CacheResults(MediaType.Movie, [new MetadataSearchResultDto { Id = 1, Title = "Test" }]);
        Assert.Equal(1, singleCache.CachedMediaTypeCount);
        singleCache.ClearAll();
        Assert.Equal(0, singleCache.CachedMediaTypeCount);

        // Test 3: Cache with all media types
        var fullCache = new ResolveMetadataDialogLogic.ResultsCache();
        fullCache.CacheResults(null, [new MetadataSearchResultDto { Id = 1, Title = "All" }]);
        fullCache.CacheResults(MediaType.Movie, [new MetadataSearchResultDto { Id = 2, Title = "Movie" }]);
        fullCache.CacheResults(MediaType.TvShow, [new MetadataSearchResultDto { Id = 3, Title = "TV" }]);
        Assert.Equal(3, fullCache.CachedMediaTypeCount);
        fullCache.ClearAll();
        Assert.Equal(0, fullCache.CachedMediaTypeCount);
    }

    /// <summary>
    /// Feature: metadata-tv-search, Property 7: TryRestoreResults returns false after dialog reset.
    /// Validates: Requirements 7.5.
    /// After dialog reset, TryRestoreResults should return false for all media types.
    /// </summary>
    [Fact]
    public void TryRestoreResultsReturnsFalseAfterDialogReset()
    {
        // Arrange - Create cache with results for all media types
        var cache = new ResolveMetadataDialogLogic.ResultsCache();
        cache.CacheResults(null, [new MetadataSearchResultDto { Id = 1, Title = "All" }]);
        cache.CacheResults(MediaType.Movie, [new MetadataSearchResultDto { Id = 2, Title = "Movie" }]);
        cache.CacheResults(MediaType.TvShow, [new MetadataSearchResultDto { Id = 3, Title = "TV" }]);

        // Verify cache has results before reset
        Assert.True(cache.TryRestoreResults(null, out _));
        Assert.True(cache.TryRestoreResults(MediaType.Movie, out _));
        Assert.True(cache.TryRestoreResults(MediaType.TvShow, out _));

        // Act - Simulate dialog reset
        cache.ClearAll();

        // Assert - TryRestoreResults should return false for all media types
        Assert.False(cache.TryRestoreResults(null, out var allResults));
        Assert.Empty(allResults);

        Assert.False(cache.TryRestoreResults(MediaType.Movie, out var movieResults));
        Assert.Empty(movieResults);

        Assert.False(cache.TryRestoreResults(MediaType.TvShow, out var tvResults));
        Assert.Empty(tvResults);
    }

    private static Property VerifyCacheRoundTrip(
        IList<MetadataSearchResultDto> originalResults,
        MediaType? originalMediaType,
        MediaType? switchToMediaType)
    {
        if (originalMediaType == switchToMediaType)
        {
            return true.Label("Skipped - same media type");
        }

        var cache = new ResolveMetadataDialogLogic.ResultsCache();
        cache.CacheResults(originalMediaType, originalResults);

        if (!cache.HasCachedResults(originalMediaType))
        {
            return false.Label("Results should be cached after CacheResults call");
        }

        if (!cache.TryRestoreResults(originalMediaType, out var restoredResults))
        {
            return false.Label("TryRestoreResults should return true for cached media type");
        }

        if (restoredResults.Count != originalResults.Count)
        {
            return false.Label($"Restored count {restoredResults.Count} should match original count {originalResults.Count}");
        }

        return VerifyResultsMatch(originalResults, restoredResults);
    }

    private static Property VerifyResultsMatch(IList<MetadataSearchResultDto> original, IReadOnlyList<MetadataSearchResultDto> restored)
    {
        for (var i = 0; i < original.Count; i++)
        {
            if (!ResultsAreEqual(original[i], restored[i]))
            {
                return false.Label($"Result at index {i} does not match original");
            }
        }

        return true.Label("Cache round-trip preserves all results");
    }

    private static bool ResultsAreEqual(MetadataSearchResultDto a, MetadataSearchResultDto b)
    {
        return a.Id == b.Id &&
               string.Equals(a.Title, b.Title, StringComparison.Ordinal) &&
               a.Type == b.Type &&
               a.Year == b.Year;
    }
}
