using dotenv.net;
using Dotnet.GenAI.ExtensionsConsoleAgent;
using Microsoft.Extensions.Hosting;

DotEnv.Load();

var model = "gpt-4.1-mini";

var builder = Host.CreateApplicationBuilder(args);

Startup.ConfigureServices(builder, model);

var host = builder.Build();

await ChatAgent.RunAsync(host.Services);