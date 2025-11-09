namespace VideoConverter.Web.Features.Integrations.Ffmpeg.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFfmpegIntegrations(this IServiceCollection services)
    {
        _ = services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<FfmpegOptions>, ConfigureFfmpegOptions>();
        _ = services.AddSingleton<IRunningProcessRegistry, RunningProcessRegistry>();
        _ = services.AddSingleton<IProcessController, ProcessController>();
        _ = services.AddSingleton<IProcessManager, ProcessManager>();
        _ = services.AddSingleton<Conversions.Services.IProgressTracker, Conversions.Services.ProgressTracker>();
        _ = services.AddSingleton<IProgressOutputParser, FfmpegProgressOutputParser>();
        _ = services.AddSingleton<IFfmpegPreflightChecker, FfmpegPreflightChecker>();
        _ = services.AddSingleton<IMediaProber, FfprobeMediaProber>();
        _ = services.AddSingleton<IConversionRunner, FfmpegConversionRunner>();
        _ = services.AddSingleton<IFfmpegArgumentsBuilder, DefaultFfmpegArgumentsBuilder>();
        _ = services.AddSingleton<ICommandDisplayFormatter, DefaultCommandDisplayFormatter>();
        _ = services.AddSingleton<Conversions.Services.IConversionRunCoordinator, Conversions.Services.ConversionRunCoordinator>();
        _ = services.AddSingleton<ICropDetector, FfmpegCropDetector>();
        return services;
    }
}
