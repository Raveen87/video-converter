## Video Converter (scaffold)

Minimal scaffold for a cross-platform video conversion service in C#/.NET 10 RC.

### Prerequisites
- .NET 10 RC SDK installed (install from the official preview channel)

### Build and run
```bash
dotnet build
dotnet run --project src/VideoConverter.Web
dotnet test

dotnet ef database update --project src/VideoConverter.Web --startup-project src/VideoConverter.Web
```

### API
- POST `/api/conversions`
  - Body:
    ```json
    {
      "inputPath": "C:/videos/input.mp4",
      "outputDirectory": "C:/videos/out",
      "crf": 40,
      "audioBitrateKbps": 128,
      "audioChannels": 2,
      "resolution": 720,
      "audioLanguages": ["English", "Swedish"],
      "subtitleLanguages": ["English", "Swedish"],
      "profileId": 1
    }
    ```
  - When `profileId` is provided, the profile's non-null fields override the corresponding request fields (`outputDirectory`, `crf`, `audioBitrateKbps`, `audioChannels`, `resolution`, `audioLanguages`, `subtitleLanguages`). For each property, if the profile value is present (non-null/non-empty for lists), it is used; otherwise, the request value is used. Values are never merged per-property.
  - Responses:
    - 201 Created with created item
    - 400 Bad Request if validation fails (see Validation section)

- GET `/api/conversions`
  - Returns all queued conversions, newest first
  - Example:
    ```json
    [
      {
        "id": 1,
        "inputPath": "C:/videos/input.mp4",
        "outputDirectory": "C:/videos/out",
        "createdAtUtc": "2025-10-06T12:00:00Z",
        "crf": 40,
        "audioBitrateKbps": 128,
        "audioChannels": 2,
        "resolution": 720,
        "metadataBasedFilename": "Inception.2010.mp4"
      }
    ]
    ```

- POST `/api/conversions/{id}/start`
  - Starts conversion for the specified id using ffmpeg (runs in background)
  - Response 200:
    ```json
    { "message": "Started", "processId": 1234, "command": "ffmpeg ..." }
    ```
  - Response 404 if id not found; 400 if already running or failed to start

- POST `/api/conversions/start-next`
  - Starts the next conversion in the queue (oldest by `createdAtUtc`)
  - Response 200:
    ```json
    { "message": "Started", "processId": 1234, "command": "ffmpeg ...", "conversionId": 1 }
    ```
  - Response 404 if queue is empty

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
  - Response 200 (conversion not running):
    ```json
    {
      "status": "Waiting",
      "percentageComplete": null,
      "currentTimeSeconds": null,
      "totalDurationSeconds": null,
      "estimatedTimeRemaining": null
    }
    ```
  - Response 404 if conversion ID not found

### Persistence
- Uses SQLite via EF Core. Default connection string: `ConnectionStrings:Default = Data Source=conversions.db`
- On startup, the app applies pending EF Core migrations automatically (`Database.Migrate()`), so new columns (e.g., `AudioChannels`) are added seamlessly.
- If you previously ran an older build that created the database without migrations and you hit a schema error, restart the app to let it apply migrations. As a last resort for local dev, you can delete the `conversions.db` file to recreate from scratch.
- Override via environment variables, user secrets, or appsettings.
- Profiles store `audioLanguages` and `subtitleLanguages` as JSON text columns.

### Testing
- Integration tests use `WebApplicationFactory` and override the SQLite path per run.
- Run: `dotnet test`

### Project layout
- `src/VideoConverter.Web`: Minimal ASP.NET Core web app
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
- `Tmdb:ApiKey` or `TMDB_API_KEY`: The Movie Database (TMDb) API key for metadata lookup (required for metadata-based file naming)

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
- `audioChannels` (optional): integer 2 (Stereo 2.0) or 6 (Surround 5.1). If omitted, defaults to 2 (Stereo 2.0).
- `resolution` (optional): one of 720, 1080, 2160. If specified higher than source height, API returns 400. If omitted, defaults to 720p scaling.
- `profileId` (optional): ID of a saved profile. When set, profile values override same-named request fields; if a profile field is null, the request value is used.
- `audioLanguages` (optional): array of full language names (e.g., "English", "Swedish").
  - When provided, the service probes the input with ffprobe and attempts to match each requested language using:
    - language tag metadata (e.g., `eng`, `en`, `swe`, `sv`)
    - stream title/name with common aliases/shortenings (e.g., `Deutsch`, `DE`, `Ger`)
  - If any requested language cannot be matched, the API returns 400 with an error.
  - If not provided, all audio tracks are included in the output.
- `subtitleLanguages` (optional): array of full language names.
  - When provided, the service probes the input and attempts to match each requested language across subtitle streams by language tag and title (with common aliases), similar to audio.
  - If any requested language cannot be matched, the API returns 400 with an error.
  - If not provided, all subtitle tracks are included and copied.
  - Additionally, for each requested subtitle language, if no matching subtitle stream is found inside the video file, the service searches the same directory as the input file for external `.srt` files named `<video-filename>.<language-tag>.srt`.
    - Supported tags:
      - Swedish: `sv`, `sve`, `swe`, `swedish`
      - English: `en`, `eng`, `english`
      - Vietnamese: `vi`, `vie`, `viet`, `vietnamese`
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
- **Error handling**: Graceful fallback to original filename if metadata service is unavailable

### Metadata preservation
- **Video title**: Automatically set to the metadata-based filename (without extension) using `-metadata title=<filename>`
- **Audio metadata**: Language tags and titles from input streams are preserved using `-map_metadata 0`
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
ffmpeg -y -i <input> -i <subtitle1.srt> -i <subtitle2.srt> -map 0:v:0 -map 0:a? -map 0:s? -map 1:s:0 -map 2:s:0 -c:v libsvtav1 -pix_fmt yuv420p10le -preset 6 -tune 0 -g 120 -crf <crf> -svtav1-params "film-grain=8:film-grain-denoise=0" -vf "crop=w:h:x:y,scale=w=1280:h=720:force_original_aspect_ratio=decrease:flags=lanczos" -c:a libopus -b:a <kbps>k -ac <channels> -c:s copy -metadata title=<filename> -metadata:s:s:0 language=<tag1> -metadata:s:s:1 language=<tag2> -map_metadata 0 <output>.mkv
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

### CI/Quality
- Warnings-as-errors enforced; code style and analyzers enabled.
- Target code coverage ≥ 85% overall; new/changed code should meet ≥ 85%.
- Static analysis runs automatically in CI/CD pipelines.

