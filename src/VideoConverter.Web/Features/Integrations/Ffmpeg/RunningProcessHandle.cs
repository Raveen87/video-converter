using System.Diagnostics;

namespace VideoConverter.Web.Features.Integrations.Ffmpeg;

public sealed record RunningProcessHandle(int ProcessId, Process Process, StreamWriter? StandardInput);
