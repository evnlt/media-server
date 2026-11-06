using Application.Abstraction.Services;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ITranscodeService, TranscodeService>();

        services.AddSingleton<ITranscodeQueue, TranscodeQueue>();
        services.AddSingleton<ITranscodeProgressTracker, TranscodeProgressTracker>();
        services.AddHostedService<TranscodeWorker>();

        return services;
    }
}
