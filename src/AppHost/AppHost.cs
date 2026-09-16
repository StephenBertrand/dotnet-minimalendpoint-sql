var builder = DistributedApplication.CreateBuilder(args);

var appConfiguration = builder.AddAzureAppConfiguration("appconfiguration");

if (builder.ExecutionContext.IsRunMode)
{
    appConfiguration.RunAsEmulator(emulator =>
    {
        // Aspire 13.5.3 defaults to 1.0.2, which has no linux/arm64 manifest.
        emulator.WithImageTag("1.2.0");
        emulator.WithLifetime(ContainerLifetime.Persistent);
        emulator.WithDataVolume();
    });

    appConfiguration.OnResourceReady(async (resource, _, cancellationToken) =>
        await AppConfigurationEmulatorSeeder.SeedIfMissingAsync(resource, cancellationToken));
}

// Azure SQL with Entra-only auth when published. Locally this is a SQL Server container
// (SQL auth, Aspire-generated password). The API still uses one connection name: "todos".
var sql = builder.AddAzureSqlServer("sql");

if (builder.ExecutionContext.IsRunMode)
{
    sql.RunAsContainer(container =>
    {
        container.WithLifetime(ContainerLifetime.Persistent);
        container.WithDataVolume();
    });
}

var todos = sql.AddDatabase("todos");

var api = builder.AddProject<Projects.Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(appConfiguration)
    .WaitFor(appConfiguration)
    .WithReference(todos)
    .WaitFor(todos);

builder.AddProject<Projects.Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
