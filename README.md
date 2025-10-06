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
    - 400 Bad Request if `inputPath` or `outputDirectory` is missing/empty

- GET `/api/conversions`
  - Returns all queued conversions, newest first
  - Example:
    ```json
    [
      { "id": 1, "inputPath": "...", "outputDirectory": "...", "createdAtUtc": "2025-10-06T12:00:00Z" }
    ]
    ```

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
- Copy required environment variables from `.env.example` to `.env` (if present). The service uses `ASPNETCORE_ENVIRONMENT` and may use `FFMPEG_PATH` when FFmpeg is not installed in the image.

### Notes
- Compiler/analyzer warnings are treated as errors via `Directory.Build.props`.
- Nullable reference types are enabled.
- Target framework is `net10.0`.

### CI/Quality
- Warnings-as-errors enforced; code style and analyzers enabled.
- Target code coverage ≥ 85% overall; new/changed code should meet ≥ 85%.

