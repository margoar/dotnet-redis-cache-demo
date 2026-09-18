using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RedisCacheDemo.Infrastructure.Configuration;

namespace RedisCacheDemo.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisOptions>(
            configuration.GetSection(RedisOptions.SectionName));

        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var redisOptions = serviceProvider
                .GetRequiredService<IOptions<RedisOptions>>()
                .Value;

            return ConnectionMultiplexer.Connect(
                redisOptions.ConnectionString);
        });

        return services;
    }
}