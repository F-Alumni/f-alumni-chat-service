using ChatService.Repositories.Context;
using ChatService.Repositories.Implementations;
using ChatService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Repositories.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var connectionString = configuration?.GetConnectionString("DefaultConnection")
            ?? "Server=localhost\\SQLEXPRESS;Database=FAlumni_Chat_Db;Trusted_Connection=True;TrustServerCertificate=True;";

        var dbProvider = configuration?["DatabaseProvider"] ?? "SqlServer";
        if (dbProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ChatServiceDbContext>(options =>
            {
                options.UseInMemoryDatabase("FAlumni_Chat_InMemoryDb");
            });
        }
        else
        {
            services.AddDbContext<ChatServiceDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        }

        services.AddScoped<ISampleItemRepository, SampleItemRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();

        return services;
    }
}
