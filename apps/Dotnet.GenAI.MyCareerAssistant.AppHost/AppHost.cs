var builder = DistributedApplication.CreateBuilder(args);

// Automatically provision an Application Insights resource
var insights = builder.ExecutionContext.IsPublishMode
    ? builder
        .AddAzureApplicationInsights(
            "MyCareerAssistantAppInsights")
    : builder
        .AddConnectionString(
            "MyCareerAssistantAppInsights", 
            "APPLICATIONINSIGHTS_CONNECTION_STRING");

var databaseName = "MyCareerAssistantDb";

var postgresDb = builder
    .AddPostgres("postgres")
    .WithImage("pgvector/pgvector:pg17")
    .WithEnvironment("POSTGRES_DB", databaseName)
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .AddDatabase("DefaultConnection", databaseName);

builder
    .AddContainer("pgadmin", "dpage/pgadmin4:latest")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "diktyas1988@gmail.com")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "root")
    .WithEndpoint(port: 82, targetPort: 80, scheme: "http", name: "pgadmin-http")
    .WithReference(postgresDb)
    .WaitFor(postgresDb);

builder
    .AddProject<Projects.Dotnet_GenAI_MyCareerAssistant>("DotnetGenAIMyCareerAssistant")
    .WithReference(postgresDb)
    .WaitFor(postgresDb)
    .WithReference(insights)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
