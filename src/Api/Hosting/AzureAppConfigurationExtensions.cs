using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace Api.Hosting;

public static class AzureAppConfigurationHosting
{
    public const string ConnectionName = "appconfiguration";
    public const string SentinelKey = "Demo:Sentinel";

    public static TBuilder AddDemoAzureAppConfiguration<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        if (!HasAppConfigurationConnection(builder.Configuration))
        {
            builder.Services.AddFeatureManagement();
            return builder;
        }

        builder.AddAzureAppConfiguration(
            ConnectionName,
            configureOptions: options =>
            {
                options.Select("Demo:*", LabelFilter.Null);

                options.ConfigureRefresh(refresh => refresh
                    .Register(SentinelKey, refreshAll: true)
                    .SetRefreshInterval(TimeSpan.FromSeconds(5)));

                options.UseFeatureFlags(featureFlags =>
                {
                    featureFlags.SetRefreshInterval(TimeSpan.FromSeconds(5));
                });
            });

        builder.Services.AddAzureAppConfiguration();
        builder.Services.AddFeatureManagement();

        return builder;
    }

    public static bool HasAppConfigurationConnection(IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration.GetConnectionString(ConnectionName))
               || !string.IsNullOrWhiteSpace(configuration["Aspire:Microsoft:Extensions:Configuration:AzureAppConfiguration:Endpoint"])
               || !string.IsNullOrWhiteSpace(configuration[$"{ConnectionName}:endpoint"]);
    }
}
