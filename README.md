## Video Converter (scaffold)

Minimal scaffold for a cross-platform video conversion service in C#/.NET 10 RC.

### Prerequisites
- .NET 10 RC SDK installed (install from the official preview channel)

### GitHub Copilot Integration
This repository includes instructions for GitHub Copilot in `.github/copilot-instructions.md`. For comprehensive AI assistant guidelines, see `AGENTS.md` at the root of the repository.

### Build and run
```bash
dotnet build
dotnet run --project src/VideoConverter.Api
dotnet test

dotnet ef database update --project src/VideoConverter.Api --startup-project src/VideoConverter.Api
```

### API
- POST `/api/conversions` (no profiles)
  - Body:
    ```json
    {
      "inputPath": "C:/videos/input.mp4",
      "outputDirectory": "C:/videos/out",
      "crf": 40,
      "audioBitrateKbps": 128,
      "audioChannels": "Stereo20",
      "resolution": 720,
      "audioLanguages": ["English", "Swedish"],
      "subtitleLanguages": ["English", "Swedish"],
      "outputFilenameTemplate": "{Title}.{Year}.S{Season:00}E{Episode:00}.{EpisodeName}.{Source}"
    }
    ```
  - Profiles cannot be used with this endpoint anymore.
  - Filename template precedence:
    1) If `outputFilenameTemplate` is provided, it is used
    2) Otherwise, the service default template is used (see Config)
  - Responses:
    - 201 Created with created item
    - 400 Bad Request if validation fails (see Validation section)

- GET `/api/conversions`
  - Returns conversions ordered by Status, then Rank, then CreatedAt
  - Example:
    ```json
    [
      {
        "id": 1,
        "inputPath": "C:/videos/input.mp4",
        "outputDirectory": "C:/videos/out",
        "createdAtUtc": "2025-10-06T12:00:00Z",
        "status": "Pending",
        "crf": 40,
        "audioBitrateKbps": 128,
        "audioChannels": "Stereo20",
        "resolution": 720,
        "metadataBasedFilename": "Inception.2010.mp4",
        "deletedAtUtc": null
      }
    ]
    ```

- POST `/api/conversions/queue/start`
  - Starts the next conversion in the queue (lowest by `Rank`)
  - **Queue-level operation**: Only one conversion can run at a time. If a conversion is already running, this request will be rejected with a 400 error indicating which conversion is currently running.
  - Response 200:
    ```json
    { "message": "Started", "processId": 1234, "command": "ffmpeg ...", "conversionId": 1 }
    ```
  - Response 404 if queue is empty; 400 if a conversion is already running

- POST `/api/conversions/queue/pause`
  - Pauses the currently running conversion in the queue.
  - Windows: suspends the process. Linux/macOS: sends SIGSTOP.
  - Response 200: `{ "message": "Paused", "pid": 1234, "conversionId": 1 }`
  - Response 404 if no conversion is currently running

- POST `/api/conversions/queue/stop`
  - Stops the currently running conversion in the queue.
  - Attempts graceful stop by sending `q` to ffmpeg stdin; if still running after 5s, kills the process tree.
  - Response 200: `{ "message": "Stopped" | "Stopped (forced)", "pid": 1234, "forced": true|false, "conversionId": 1 }`
  - Response 404 if no conversion is currently running

- POST `/api/conversions/start-next` (deprecated, use `/api/conversions/queue/start`)
  - Legacy endpoint for starting the next conversion. Use `/api/conversions/queue/start` instead.

- PATCH `/api/conversions/{id:int}/reorder`
  - Body:
    ```json
    { "beforeId": 12, "afterId": 8 }
    ```
  - Behavior:
    - Repositions the item by assigning a `Rank` between neighbors. Only the target item is updated.
    - At least one of `beforeId` or `afterId` must be provided.
  - Responses:
    - 200 OK with updated item
    - 400 if validation fails; 404 if item not found; 400 if not in `Pending` state

- PUT `/api/conversions/queue/reorder`
  - Body:
    ```json
    { "orderedIds": [3, 5, 2, 1] }
    ```
  - Behavior:
    - Applies a new order to the entire pending queue in a single call.
    - Only IDs for pending conversions are accepted; missing IDs retain their relative order after the provided list.
  - Responses:
    - 200 OK with the pending queue in its new order
    - 400 if the payload is invalid or contains duplicates
    - 404 if any referenced conversion is not pending/does not exist

- DELETE `/api/conversions/{id:int}`
  - Marks a conversion as deleted. Only conversions in `Pending` status can be deleted.
  - Behavior:
    - Sets the conversion status to `Deleted` and records the deletion timestamp in `DeletedAtUtc`.
    - The conversion remains in the database and is visible in the UI, but is excluded from queue operations.
    - Deleted conversions cannot be edited or reordered.
  - Responses:
    - 200 OK with the updated conversion (status: `Deleted`, `deletedAtUtc` set)
    - 400 if conversion is not in `Pending` status
    - 404 if conversion not found

- PATCH `/api/conversions/{id:int}`
  - Updates a conversion's settings. Only conversions in `Pending` status can be updated.
  - Body: JSON Patch document (RFC 6902) with `ConversionPatchDto`:
    ```json
    [
      { "op": "replace", "path": "/outputDirectory", "value": "C:/new/output" },
      { "op": "replace", "path": "/crf", "value": 28 },
      { "op": "replace", "path": "/audioBitrateKbps", "value": 192 },
      { "op": "replace", "path": "/audioChannels", "value": "Surround51" },
      { "op": "replace", "path": "/resolution", "value": 1080 },
      { "op": "replace", "path": "/outputFilenameTemplate", "value": "{Title}" },
      { "op": "replace", "path": "/audioLanguages", "value": ["English", "Spanish"] },
      { "op": "replace", "path": "/subtitleLanguages", "value": ["English"] }
    ]
    ```
  - Editable fields:
    - `outputDirectory` (string)
    - `crf` (int?)
    - `audioBitrateKbps` (int?)
    - `audioChannels` (AudioChannels?)
    - `resolution` (int?)
    - `outputFilenameTemplate` (string?)
    - `audioLanguages` (string[]?)
    - `subtitleLanguages` (string[]?)
  - Note: `inputPath` cannot be updated. If `audioLanguages` or `subtitleLanguages` are updated, the system will re-process stream matching.
  - Responses:
    - 200 OK with updated conversion
    - 400 if validation fails or conversion is not in `Pending` status (includes user-friendly message explaining why)
    - 404 if conversion not found

- POST `/api/conversions/queue-directory` (no profiles)
  - Recursively scans a directory, queues all matching files.
  - Default extensions: `.mkv`, `.mp4`. You can override via `extensions`.
  - Body:
    ```json
    {
      "directoryPath": "C:/media/in",
      "outputDirectory": "C:/media/out",
      "crf": 23,
      "audioBitrateKbps": 128,
      "audioChannels": "Stereo20",
      "resolution": 1080,
      "audioLanguages": ["English"],
      "subtitleLanguages": ["English"],
      "outputFilenameTemplate": "{Title}",
      "extensions": [".mkv", ".mp4"]
    }
    ```
  - Behavior:
    - Recursive enumeration (includes all subfolders)
    - Skips duplicates by `(inputPath + effective outputDirectory)`
  - Response 200:
    ```json
    {
      "totalFilesFound": 12,
      "queued": 10,
      "skippedDuplicates": 1,
      "invalid": 1,
      "outcomes": [
        { "inputPath": "C:/media/in/a.mkv", "status": "pending", "reason": null, "conversionId": 101 },
        { "inputPath": "C:/media/in/b.mp4", "status": "skipped", "reason": "duplicate", "conversionId": null },
        { "inputPath": "C:/media/in/c.mp4", "status": "invalid", "reason": "OutputDirectory: must exist", "conversionId": null }
      ]
    }
    ```

- GET `/api/conversions/{id}/progress`
  - Returns the current progress of a conversion by parsing ffmpeg output
  - Response 200:
    ```json
    {
      "status": "Running",
      "percentageComplete": 45.2,
      "currentTimeSeconds": 54.3,
      "totalDurationSeconds": 120.0,
      "estimatedTimeRemaining": "00:01:15"
    }
    ```

- GET `/api/file-browser/directories?path={encodedPath}`
  - Lists directories and files at a given path for file browser functionality
  - Query parameters:
    - `path` (optional): The directory path to browse. If omitted, returns root directory (Windows: first available drive, Linux/macOS: `/`)
  - Response 200:
    ```json
    {
      "currentPath": "C:\\videos",
      "parentPath": "C:\\",
      "directories": [
        { "name": "movies", "path": "C:\\videos\\movies" },
        { "name": "tv", "path": "C:\\videos\\tv" }
      ],
      "files": [
        { "name": "movie.mp4", "path": "C:\\videos\\movie.mp4", "size": 1073741824, "lastModified": "2024-01-15T10:30:00Z" }
      ]
    }
    ```
  - Behavior:
    - Only shows video files (filtered by common video extensions: `.mkv`, `.mp4`, `.avi`, `.mov`, `.wmv`, `.flv`, `.webm`, `.m4v`, `.mpg`, `.mpeg`, `.3gp`, `.ogv`, `.ts`, `.m2ts`)
    - Validates paths to prevent directory traversal attacks
    - Returns 404 if directory not found
    - Returns 403 if access denied
  - Security:
    - Paths are validated to prevent directory traversal (`..` segments)
    - Access is restricted to paths within the root directory
    - Supports both Windows and Linux path formats

### Web UI features

#### File Browser
- The web UI includes a file browser component that allows users to browse and select files/directories without manually typing paths
- Available in:
  - **Create Conversion** dialog: Browse button next to "Input File Path" and "Output Directory" fields
  - **Queue Directory** dialog: Browse button next to "Directory Path" and "Output Directory" fields
  - **Edit Conversion** dialog: Browse button next to "Output Directory" field
- Features:
  - Breadcrumb navigation for easy path traversal
  - Click directories to navigate into them
  - Click files to select them (in file selection mode)
  - Shows file metadata (size, last modified date)
  - Filters to show only video files
  - Works in all modern browsers (no browser-specific APIs required)

#### Queue Reordering
- The `Conversions` page shows a dedicated **Pending Queue** card listing all pending conversions in their current processing order.
- Drag and drop items within the card to preview a new order; unsaved changes are highlighted.
- Click **Save Queue Order** to persist the arrangement via `PUT /api/conversions/queue/reorder`.
- Queue controls (start, pause, stop) continue to respect the updated order immediately after saving.

#### Queue Item Deletion
- Pending conversions can be deleted from the queue using the delete button in the queue list or the delete action in the conversions table.
- Deleted items are marked with `Deleted` status and remain visible in the UI but are excluded from queue operations.
- The deletion timestamp is stored in `deletedAtUtc` for audit purposes.

- POST `/api/profiles/{id:int}/conversions`
  - Body:
    ```json
    {
      "inputPath": "C:/videos/input.mp4",
      "outputDirectory": "C:/videos/out"
    }
    ```
  - Behavior:
    - The specified profile supplies all optional conversion settings. Any setting not present on the profile falls back to application defaults.
    - The request `outputDirectory` takes precedence over the profile's.
  - Responses:
    - 201 Created with created item
    - 400 Bad Request if validation fails

- POST `/api/profiles/{id:int}/conversions/queue-directory`
  - Body:
    ```json
    {
      "directoryPath": "C:/media/in",
      "outputDirectory": "C:/media/out",
      "extensions": [".mkv", ".mp4"]
    }
    ```
  - Behavior (breaking change):
    - Immediately enqueues a batch for background processing and returns a batch id.
    - Uses the profile's optional settings for all queued items; unspecified settings fall back to application defaults.
    - Request `outputDirectory` takes precedence over the profile's.
    - Default extensions: `.mkv`, `.mp4`.
  - Response 202 Accepted:
    ```json
    { "batchId": 123 }
    ```

- POST `/api/conversions/queue-directory`
  - Body: `DirectoryQueueRequestDto` (same fields as above without profile context)
  - Response 202 Accepted:
    ```json
    { "batchId": 124 }
    ```

- GET `/api/conversions/queue-directory/batches/{id:int}`
  - Response 200 OK:
    ```json
    {
      "batchId": 123,
      "status": "Running",
      "totalItems": 25,
      "processedItems": 7,
      "unprocessedItems": 18,
      "currentItemPath": "C:/media/in/movie.mkv"
    }
    ```

### Persistence
- Uses SQLite via EF Core. Default connection string: `ConnectionStrings:Default = Data Source=conversions.db`
- On startup, the app applies pending EF Core migrations automatically (`Database.Migrate()`), so new columns (e.g., `AudioChannels`) are added seamlessly.
- If you previously ran an older build that created the database without migrations and you hit a schema error, restart the app to let it apply migrations. As a last resort for local dev, you can delete the `conversions.db` file to recreate from scratch.

#### Queue ordering
- Each queued item has a string `Rank` used for ordering. Drag-and-drop reorder and server-initiated reprioritization update only the item's `Rank`.

### Cleanup
- The service automatically cleans up finished conversions and directory batches (Completed/Failed) daily.
  - Config (`appsettings.json` or env via `CONFIG__`):
    - `Cleanup:Enabled` (bool, default `true`)
    - `Cleanup:RetentionDays` (int, default `30`)
- Manual cleanup endpoint:
  - DELETE `/api/conversions/finished?olderThanDays={int?}`
    - Without `olderThanDays`: deletes all finished conversions.
    - With `olderThanDays`: deletes only those older than N days.
    - Response: `{ "deleted": <count> }`
  - DELETE `/api/directory-batches/finished?olderThanDays={int?}`
    - Without `olderThanDays`: deletes all finished batches.
    - With `olderThanDays`: deletes only those older than N days.
    - Response: `{ "deleted": <count> }`
- Completing an item removes it from the queued set; the next item (lowest `Rank`) automatically becomes first.
- Existing rows are backfilled on startup with spaced ranks (`00001000`, `00002000`, …).
- Override via environment variables, user secrets, or appsettings.
- Profiles store `audioLanguages` and `subtitleLanguages` as JSON text columns.

### Testing
- Integration tests use `WebApplicationFactory` and override the SQLite path per run.
- Run: `dotnet test`

#### Test utilities
- `tests/VideoConverter.Tests/TestUtils/Builders`:
  - `ConversionCreateDtoBuilder`, `ProfileBuilder`, `MediaStreamsBuilder`
- `tests/VideoConverter.Tests/TestUtils/DbContextTestFactory`:
  - `CreateInMemory()`, `CreateSqliteInMemory(out SqliteConnection connection)`
- `tests/VideoConverter.Tests/TestUtils/TempDir`: disposable temp directory helper

Examples:

```csharp
// Build a request DTO
var dto = new ConversionCreateDtoBuilder()
    .WithInputPath("in.mp4")
    .WithOutputDirectory("out")
    .WithCrf(23)
    .WithAudioBitrateKbps(128)
    .WithAudioChannels(AudioChannels.Stereo20)
    .WithResolution(1080)
    .WithAudioLanguages("en")
    .WithSubtitleLanguages("sv")
    .Build();

// Create EF Core contexts for tests
await using var sqliteDb = DbContextTestFactory.CreateSqliteInMemory(out var _);
await using var inMemDb = DbContextTestFactory.CreateInMemory();
```

### Project layout
- `src/VideoConverter.Api`: Minimal ASP.NET Core web app
  - `Api/`: API endpoint definitions
  - `Services/`: Business logic services (conversion, media probing, language matching, etc.)
  - `Data/`: Entity Framework models and database context
  - `Dtos/`: Data transfer objects for API requests/responses
  - `Validation/`: Input validation logic and helpers
- `tests/VideoConverter.Tests`: xUnit test project with unit tests for core functionality

### Config
Environment/app settings:

- `Ffmpeg:Path` or `FFMPEG_PATH`: Path to ffmpeg executable (default: `ffmpeg` in PATH)
- `Ffmpeg:FfprobePath` or `FFPROBE_PATH`: Path to ffprobe executable (default: `ffprobe` in PATH)
- `Ffmpeg:DryRun` or `FFMPEG_DRYRUN`: When `true`, the service will not spawn ffmpeg; it returns the built command string. Useful for tests and diagnostics.
- TMDb API key (required for metadata-based file naming):
  - `Tmdb:ApiKey` (e.g., env var `Tmdb__ApiKey` or in `appsettings.json`)
  - `.env` (preferred for local dev): `CONFIG__TMDB__API_KEY=your_key`
  - Alternative env var: `TMDB__API_KEY`

Filename template default (service-wide):

```json
"FilenameTemplate": {
  "DefaultTemplate": "{Title}.{Year}.S{Season:00}E{Episode:00}.{EpisodeName}.{Source}"
}
```

Template directives:
- `{Title}`: Movie title or TV show name
- `{Year}`: Movie year or TV first aired year
- `{Season}` / `{Episode}`: Integers, support formatting like `{Season:00}`
- `{EpisodeName}`: TV episode title
- `{Source}`: Optional source tag (e.g., WEB-DL, HDTV)

Rules:
- Use curly braces for directives. Everything outside braces is literal and included as-is.
- Smart collapse: when a directive is empty, adjacent separators (`.`, `-`, `_`, space) and single-letter affixes touching it are removed (e.g., `S{Season:00}E{Episode:00}` disappears entirely for movies).

Template filters (strings only):
- `spaces=dot|underscore|hyphen|space`: Replaces spaces inside the token.
- `lower` / `upper` / `trim`: Case/whitespace transformations.

Notes:
- ASCII is always enforced (diacritics removed, non-ASCII dropped). Illegal filesystem characters are replaced with `_`.
- Numeric formatting (e.g., `{Season:00}`) is unchanged and cannot be combined with string filters for the same token.

Example:
```
{Title:spaces=dot}.S{Season:00}E{Episode:00}.{EpisodeName:spaces=dot}.{Source:spaces=dot}
```
Outputs dot-separated names for titles, episode names, and sources.

FFmpeg options (appsettings.json):

```json
"Ffmpeg": {
  "Path": "ffmpeg",
  "FfprobePath": "ffprobe",
  "DryRun": false,
  "VideoCodec": "libsvtav1",
  "PixelFormat": "yuv420p10le",
  "Preset": "6",
  "Tune": "0",
  "Gop": "120",
  "SvtParams": "film-grain=8:film-grain-denoise=0",
  "AudioCodec": "libopus",
  "DefaultAudioBitrateKbps": 128
}
```

- `Ffmpeg:VideoCodec`: Video codec (default `libsvtav1`)
- `Ffmpeg:PixelFormat`: Output pixel format (default `yuv420p10le`)
- `Ffmpeg:Preset`: Encoder preset (default `6`)
- `Ffmpeg:Tune`: Encoder tune (default `0`)
- `Ffmpeg:Gop`: GOP size (default `120`)
- `Ffmpeg:SvtParams`: SVT-AV1 parameter string (default `film-grain=8:film-grain-denoise=0`)
- `Ffmpeg:AudioCodec`: Audio codec (default `libopus`)
- `Ffmpeg:DefaultAudioBitrateKbps`: Default audio bitrate when request doesn’t specify (default `128`)

Environment variable overrides follow ASP.NET Core’s standard mapping. For example:

- `Ffmpeg__PixelFormat=yuv420p10le`
- `Ffmpeg__Preset=6`
- `Ffmpeg__DefaultAudioBitrateKbps=192`

### Validation
- `inputPath` must be a non-empty string and point to an existing file (not a directory).
- `outputDirectory` must be a non-empty string. If it does not exist, the service attempts to create it. It must be writable by the process user.
- `crf` (optional): integer 0–63. If omitted, defaults to 40.
- `audioBitrateKbps` (optional): integer 1–450. If omitted, defaults to 128 for 2.0 audio or 192 for 5.1 audio.
- `audioChannels` (optional): string enum value. Valid values: `"Stereo20"` (Stereo 2.0) or `"Surround51"` (Surround 5.1). If omitted, defaults to `"Stereo20"`.
- `resolution` (optional): one of 720, 1080, 2160. If specified higher than source height, API returns 400. If omitted, defaults to 720p scaling.
- `profileId` (optional): ID of a saved profile. When set, profile values override same-named request fields; if a profile field is null, the request value is used.
- `audioLanguages` (optional): array of full language names (e.g., "English", "Swedish", "Spanish", "Hindi").
  - When provided, the service probes the input with ffprobe and attempts to match each requested language using:
    - language tag metadata (e.g., `eng`, `en`, `swe`, `sv`, `spa`, `es`)
    - stream title/name with common aliases/shortenings (e.g., `Deutsch`, `DE`, `Ger`, `Español`, `Castellano`)
  - The service supports the top 50-100 most spoken languages worldwide, including major European, Asian, African, Middle Eastern, and other regional languages.
  - Languages can be specified by their full English name (e.g., "English", "Mandarin", "Arabic") and will be matched using ISO 639-1 (2-letter), ISO 639-3 (3-letter) codes, native names, and common variations.
  - If any requested language cannot be matched, the API returns 400 with an error.
  - If not provided, all audio tracks are included in the output.
- `subtitleLanguages` (optional): array of full language names.
  - When provided, the service probes the input and attempts to match each requested language across subtitle streams by language tag and title (with common aliases), similar to audio.
  - Supports the same comprehensive language set as `audioLanguages`.
  - If any requested language cannot be matched, the API returns 400 with an error.
  - If not provided, all subtitle tracks are included and copied.
  - Additionally, for each requested subtitle language, if no matching subtitle stream is found inside the video file, the service searches the same directory as the input file for external `.srt` files named `<video-filename>.<language-tag>.srt`.
    - Language tags are dynamically generated from the language's ISO codes and common aliases. For example:
      - English: `en`, `eng`, `english`
      - Swedish: `sv`, `sve`, `swe`, `svenska`, `swedish`
      - Spanish: `es`, `spa`, `esp`, `espanol`, `spanish`, `castellano`
      - Arabic: `ar`, `ara`, `arabic`, `arab`
      - And many more languages with their respective tags
    - When found, external subtitles are added as inputs and mapped to the output and copied (`-c:s copy`).
- Additional improvements suggested for future hardening:
  - Restrict allowed input file extensions (e.g., `.mp4`, `.mkv`, etc.).
  - Verify sufficient free disk space in `outputDirectory` before starting a conversion.
  - Health endpoint to proactively verify `ffmpeg` availability.

- Copy required environment variables from `.env.example` to `.env` (if present). The service uses `ASPNETCORE_ENVIRONMENT` and may use `FFMPEG_PATH` when FFmpeg is not installed in the image.

### Error Handling

The API uses a structured error handling system that provides consistent error responses across all endpoints. All errors are returned as JSON with the following structure:

```json
{
  "statusCode": 400,
  "errorCode": "ValidationFailed",
  "errorMessage": "The input path must point to an existing file.",
  "userFriendlyMessage": "Please provide a valid input file path.",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### Error Properties

- `statusCode`: HTTP status code (e.g., 400, 404, 500)
- `errorCode`: Programmatic error identifier (enum value)
- `errorMessage`: Technical message for developers/employees
- `userFriendlyMessage`: User-friendly message for end users
- `timestamp`: UTC timestamp when the error occurred

#### Error Codes

All error codes are returned as string enum values. The following values are available:

**Validation Errors (400)**
- `ValidationFailed`: General validation error
- `InvalidInput`: Invalid input parameters
- `MissingRequiredField`: Required field is missing
- `InvalidFileFormat`: Unsupported file format
- `InvalidResolution`: Invalid resolution value
- `InvalidBitrate`: Invalid bitrate value
- `InvalidCrf`: Invalid CRF value

**Not Found Errors (404)**
- `ConversionNotFound`: Conversion with specified ID not found
- `FileNotFound`: Input file not found
- `QueueEmpty`: No conversions in queue
- `ProfileNotFound`: Profile with specified ID not found

**Business Logic Errors (400)**
- `ConversionAlreadyStarted`: Conversion is already running
- `ConversionAlreadyCompleted`: Conversion is already completed
- `InvalidConversionState`: Invalid state for requested operation
- `ProcessStartFailed`: Failed to start conversion process
- `MediaProbeFailed`: Failed to probe media file
- `CropDetectionFailed`: Failed to detect crop parameters

**Server Errors (500)**
- `DatabaseError`: Database operation failed
- `FileSystemError`: File system operation failed
- `FfmpegError`: FFmpeg execution failed
- `MetadataServiceError`: Metadata service failed
- `InternalServerError`: Unexpected server error

### Enum Values

All enums in the API are returned as string values (not integers). The following enums are used throughout the API:

#### ConversionStatus (Domain)

Used in conversion entities and responses. Valid values:
- `Pending`: Conversion is queued and waiting to start
- `Running`: Conversion is currently in progress
- `Completed`: Conversion finished successfully
- `Failed`: Conversion failed with an error
- `Deleted`: Conversion has been marked as deleted (soft delete)

#### ConversionStatus (Progress DTO)

Used in progress tracking responses. Valid values:
- `Waiting`: Conversion is waiting to start (not yet tracked)
- `Running`: Conversion is currently running and being tracked

#### AudioChannels

Used for audio channel configuration in conversion requests and profiles. Valid values:
- `Stereo20`: Stereo 2.0 audio (2 channels)
- `Surround51`: Surround 5.1 audio (6 channels)

#### MediaType

Used for metadata classification. Valid values:
- `Movie`: Content is a movie
- `TvShow`: Content is a TV show

#### FetchFailureBehavior

Used for metadata fetch failure handling configuration. Valid values:
- `DefaultToInputName`: Use the original input filename when metadata lookup fails
- `QueueForManualResolution`: Add to metadata resolution queue for manual processing
- `SkipItem`: Skip the item entirely when metadata lookup fails

#### Exception Handling

The API includes a global exception handler middleware that automatically converts various exception types to structured API errors:

- `ArgumentException` → Validation error
- `ArgumentNullException` → Validation error
- `InvalidOperationException` → Business logic error
- `FileNotFoundException` → Not found error
- `DirectoryNotFoundException` → Not found error
- `UnauthorizedAccessException` → Forbidden error
- `TimeoutException` → Server error
- `ApiError` → Used directly (already structured)
- Other exceptions → Generic server error

### Notes
- Compiler/analyzer warnings are treated as errors via `Directory.Build.props`.
- Nullable reference types are enabled.
- Target framework is `net10.0`.

### FFmpeg settings used
- Video: AV1 via `libsvtav1`, 10-bit (`-pix_fmt yuv420p10le`), preset `6` (optimized for quality), GOP `120`, configurable CRF (`-crf <0..63>`, default 40)
- Audio: Opus `-c:a libopus -b:a <kbps>k -ac <channels>` (default 128k for 2.0, 192k for 5.1)
- Subtitles: copied `-c:s copy`
- Scaling: downscale to requested resolution (720/1080/2160) maintaining aspect ratio; uses Lanczos scaling filter
- Quality enhancements: Film grain synthesis enabled (`film-grain=8`) for better visual quality

### Metadata-based file naming
When a conversion is queued, the service automatically looks up metadata for the input file and generates a standardized output filename based on the content type:

#### Movie naming format
- **Pattern**: `[Movie.Name].[Year].[extension]`
- **Example**: `Inception.2010.mp4`

#### TV Show naming format  
- **Pattern**: `[ShowName].[SXXEXX].[Episode.Name].[extension]`
- **Example**: `Yellowstone.S01E01.Daybreak.mp4`

#### Metadata lookup process
1. **Filename parsing**: The service analyzes the input filename to detect if it's a movie or TV show
   - Movies: Detects year patterns like `(2020)`, `.2020`, `-2020`
   - TV Shows: Detects season/episode patterns like `S01E01`, `1x01`, `S1E1`
2. **Metadata lookup**: Queries The Movie Database (TMDb) API for official metadata
   - Movies: Searches by title and year
   - TV Shows: Searches by show name, season, and episode
3. **Filename generation**: Creates a clean, standardized filename using the official metadata
4. **Fallback**: If metadata lookup fails, uses the original filename

#### Configuration
- **TMDb API Key**: Set `Tmdb:ApiKey` in configuration or environment variables. The service authenticates to TMDb using the HTTP Authorization header with a Bearer token. Do not append `api_key` as a query parameter.
- **Filename sanitization**: Special characters are replaced with underscores for filesystem compatibility
- **Error handling (configurable)**:
  - `MetadataFetch:OnFailureBehavior` controls behavior when TMDb lookup fails (e.g., TMDb is down/unavailable, TMDb search returns no results, or episode not found in series):
    - `DefaultToInputName` (build minimal metadata from parsed filename to honor template; else keep original name)
    - `QueueForManualResolution` (adds a row to `MetadataResolutionQueue` and skips the item)
    - `SkipItem` (skips the item without queuing)
  - Default: `QueueForManualResolution`
  - Example `appsettings.json`:
    ```json
    {
      "MetadataFetch": { "OnFailureBehavior": "QueueForManualResolution" }
    }
    ```
  - Environment override: `MetadataFetch__OnFailureBehavior=SkipItem`
  - Database: unresolved items are stored in table `MetadataResolutionQueue` with parsed fields and failure reason/status.

#### TMDb search endpoints
- GET `/api/metadata/search/movies`
  - Query: `q` (required), `year` (optional int)
  - Returns: list of `{ title, year, overview?, posterUrl?, source, tmdbId }`
- GET `/api/metadata/search/tv`
  - Query: `q` (required), `firstAirYear` (optional int)
  - Returns: list of `{ showName, firstAiredYear, overview?, posterUrl?, source, tmdbId }`

#### TMDb rate limiting and transient failures
- The service transparently retries TMDb requests on HTTP 429 (Too Many Requests) and 503 (Service Unavailable).
- If the `Retry-After` header is present, it is honored (date or delta-seconds).
- Otherwise, exponential backoff with full jitter is used, up to 5 retries and a maximum cumulative wait of 60 seconds.
- All retries respect request cancellation.

### Metadata preservation
- **Video title**: Automatically set to the metadata-based filename (without extension) using `-metadata title=<filename>`
- **Audio metadata**: Language tags and titles from input streams are preserved using `-map_metadata 0`
- **Subtitles policy**: Only text-based subtitles are included (SubRip/SRT, ASS/SSA, WebVTT, mov_text). Image-based subtitles (e.g., PGS/VobSub) are ignored.
- **Subtitle transcoding**: All included subtitle streams are transcoded to SubRip (SRT) using `-c:s srt`.
- **Subtitle metadata**: Language tags and titles from input streams are preserved using `-map_metadata 0`
- **External subtitle metadata**: Language tags are automatically extracted from external subtitle filenames and set using `-metadata:s:s:<index> language=<tag>`
  - External subtitle files should be named `<video-name>.<language-tag>.srt` (e.g., `movie.en.srt`, `movie.sv.srt`)
  - The language tag from the filename is used as the subtitle track language metadata
- This ensures that media players and other tools can properly identify and display stream information

### Automatic crop detection
- When a new conversion is created, the service runs `ffmpeg` cropdetect to infer optimal cropping.
- It samples 15 timestamps evenly across the video duration (skipping the first/last ~2s when possible).
- It selects the crop with the largest area (`w*h`) among samples and persists it.
- If a crop is detected, it is applied before scaling in the filter chain: `-vf "crop=w:h:x:y,scale=..."`.

Example effective flags (simplified):
```bash
ffmpeg -y -i <input> -i <subtitle1.srt> -i <subtitle2.srt> -map 0:v:0 -map 0:a? -map 0:<sub-index-1> -map 0:<sub-index-2> -map 1:s:0 -map 2:s:0 -c:v libsvtav1 -pix_fmt yuv420p10le -preset 6 -tune 0 -g 120 -crf <crf> -svtav1-params "film-grain=8:film-grain-denoise=0" -vf "crop=w:h:x:y,scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos" -c:a libopus -b:a <kbps>k -ac <channels> -c:s srt -metadata title=<filename> -metadata:s:s:0 language=<tag1> -metadata:s:s:1 language=<tag2> -map_metadata 0 <output>.mkv
```

### Static Analysis

The project includes comprehensive static analysis using SonarAnalyzer for C# with configurable rules.

#### Build Integration
Static analysis runs automatically during build:
```bash
dotnet build  # Analysis runs as part of build
```

#### Standalone Analysis
Run analysis as a separate command:

**Windows (PowerShell):**
```powershell
.\scripts\analyze.ps1
.\scripts\analyze.ps1 -Verbose -Configuration Release
```

**Linux/macOS (Bash):**
```bash
./scripts/analyze.sh
./scripts/analyze.sh --verbose --configuration Release
```

#### Configuration
Static analysis rules can be configured in:

- **`sonar-project.properties`** - SonarQube rules and project settings
- **`.editorconfig`** - Code style and formatting rules  
- **`Directory.Build.props`** - Build-time analysis settings

#### Analysis Features
- **Security**: Vulnerability and security hotspot detection
- **Reliability**: Bug detection and potential issues
- **Maintainability**: Code smells and complexity analysis
- **Code Style**: Enforced via .editorconfig
- **Coverage**: Integration with test coverage reporting

#### Customizing Rules
To disable specific rules, add to `sonar-project.properties`:
```properties
# Example: Disable specific rules
sonar.cs.rules.S1000.enabled=false
sonar.cs.rules.S1001.enabled=false
```

#### Analysis Output
- Build-time: Warnings and errors shown in build output
- Standalone: Detailed report in `analysis-results/analysis-report.md`
- Test coverage: Integrated with test results

### Recent Improvements
The codebase has been refactored to improve maintainability and reduce complexity:

- **Service Layer**: Business logic extracted from API controllers into dedicated services (`ConversionService`, `ProcessManager`)
- **Separation of Concerns**: Audio and subtitle language matching split into separate services
- **Validation Helpers**: Common validation logic centralized in reusable helper methods
- **Process Management**: Unified process execution and output handling across FFmpeg operations
- **Test Organization**: Tests split into focused unit test classes for better maintainability

#### Architecture updates
- **Controllers**: Minimal APIs migrated to attribute-routed controllers suffixed with `Api` (e.g., `ConversionsApi`, `ProfilesApi`, `MetadataSearchApi`). Routes are unchanged.
- **Startup responsibilities**: Database migrations are applied synchronously in `Program.cs` before any hosted services start, ensuring all tables exist when services begin execution.
- **Feature DI**: Service registrations are grouped by feature via extensions:
  - `AddFfmpegIntegrations()`, `AddConversionsFeature()`, `AddProfilesFeature()`, `AddMetadataFeature()`.
- **FFmpeg options binding**: Environment fallbacks handled in `ConfigureFfmpegOptions`:
  - `FFMPEG_PATH` → `Ffmpeg:Path`, `FFMPEG_DRYRUN` → `Ffmpeg:DryRun`.

### CI/Quality
- Warnings-as-errors enforced; code style and analyzers enabled.
- Target code coverage ≥ 85% overall; new/changed code should meet ≥ 85%.
- Static analysis runs automatically in CI/CD pipelines.

