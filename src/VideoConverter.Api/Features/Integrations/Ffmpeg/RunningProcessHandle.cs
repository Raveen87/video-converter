using System.Diagnostics;

namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

public sealed record RunningProcessHandle(int ProcessId, Process Process, StreamWriter? StandardInput);
