## Video Converter (scaffold)

Minimal scaffold for a cross-platform video conversion service in C#/.NET 10 RC.

### Prerequisites
- .NET 10 RC SDK installed (install from the official preview channel)

### Build and run
```bash
dotnet build
dotnet run --project src/VideoConverter.Web
dotnet test
```

### API
- POST `/api/conversions`
  - Body:
    ```json
    { "inputPath": "C:/videos/input.mp4", "outputDirectory": "C:/videos/out" }
    ```
  - Responses:
    - 201 Created with created item
    - 400 Bad Request if validation fails (see Validation section)

- GET `/api/conversions`
  - Returns all queued conversions, newest first
  - Example:
    ```json
    [
      { "id": 1, "inputPath": "...", "outputDirectory": "...", "createdAtUtc": "2025-10-06T12:00:00Z" }
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

### Persistence
- Uses SQLite via EF Core. Default connection string: `ConnectionStrings:Default = Data Source=conversions.db`
- Override via environment variables, user secrets, or appsettings.

### Testing
- Integration tests use `WebApplicationFactory` and override the SQLite path per run.
- Run: `dotnet test`

### Project layout
- `src/VideoConverter.Web`: Minimal ASP.NET Core web app
- `tests/VideoConverter.Tests`: xUnit test project

### Config
### Validation
- `inputPath` must be a non-empty string and point to an existing file (not a directory).
- `outputDirectory` must be a non-empty string. If it does not exist, the service attempts to create it. It must be writable by the process user.
- Additional improvements suggested for future hardening:
  - Restrict allowed input file extensions (e.g., `.mp4`, `.mkv`, etc.).
  - Verify sufficient free disk space in `outputDirectory` before starting a conversion.
  - Health endpoint to proactively verify `ffmpeg` availability.

- Copy required environment variables from `.env.example` to `.env` (if present). The service uses `ASPNETCORE_ENVIRONMENT` and may use `FFMPEG_PATH` when FFmpeg is not installed in the image.

### Notes
- Compiler/analyzer warnings are treated as errors via `Directory.Build.props`.
- Nullable reference types are enabled.
- Target framework is `net10.0`.

### FFmpeg settings used
- Video: AV1 via `libsvtav1`, 10-bit (`-pix_fmt yuv420p10le`), preset `4`, GOP `150`, CRF `40`
- Audio: Opus `-c:a libopus -b:a 128k`
- Subtitles: copied `-c:s copy`
- Scaling: downscale to 720p if larger, keep if ≤720p; uses Lanczos scaling filter

Example effective flags (simplified):
```bash
ffmpeg -y -i <input> -map 0:v:0 -map 0:a? -map 0:s? -c:v libsvtav1 -pix_fmt yuv420p10le -preset 4 -g 150 -crf 40 -vf "scale='min(1280,iw)':'min(720,ih)':force_original_aspect_ratio=increase:flags=lanczos" -c:a libopus -b:a 128k -c:s copy <output>.mkv
```

### CI/Quality
- Warnings-as-errors enforced; code style and analyzers enabled.
- Target code coverage ≥ 85% overall; new/changed code should meet ≥ 85%.

