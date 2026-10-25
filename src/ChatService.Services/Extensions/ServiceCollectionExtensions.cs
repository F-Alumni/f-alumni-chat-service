using ChatService.Repositories.Extensions;
using ChatService.Services.Implementations;
using ChatService.Services.Interfaces;
using Contracts.Grpc.UserProfile.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Services.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers both Service and Repository layers into the dependency injection container.
    /// Passes configuration to the repository layer for database connection string setup.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // 1. Register lower-level Repository layer dependencies
        services.AddRepositories(configuration);

        // 2. Register gRPC Client for User Service Profile Lookup (CH-04)
        var userGrpcUrl = configuration?["GrpcServices:UserProfile:Url"] ?? "http://localhost:5001";
        services.AddGrpcClient<UserProfileService.UserProfileServiceClient>(options =>
        {
            options.Address = new Uri(userGrpcUrl);
        });

        // 3. Register Service layer dependencies
        services.AddScoped<ISampleItemService, SampleItemService>();
        services.AddScoped<IUserProfileLookupService, UserProfileLookupService>();
        services.AddScoped<IChatKafkaPublisher, ChatKafkaPublisher>();
        services.AddScoped<IChatAppService, ChatAppService>();

        return services;
    }
}
