using Dotnet.GenAI.MyCareerAssistant.Services.Ingestion;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.BackgroundServices
{
    public class DataIngestionService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<DataIngestionService> _logger;

        public DataIngestionService(
            IServiceProvider services,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<DataIngestionService> logger)
        {
            _services = services;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(
                _configuration.GetValue<int>(
                    "BackgroundServiceTimespanMin:DataIngestion")));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    using var scope = _services.CreateScope();

                    var ingestor = scope
                        .ServiceProvider
                        .GetRequiredService<DataIngestor>();

                    var source = new PDFDirectorySource(
                        Path.Combine(
                            _webHostEnvironment.WebRootPath, 
                            "Data"));

                    await ingestor.IngestDataAsync(source);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(
                    ex,
                    "{BackgroundService} is stopping.",
                    nameof(DataIngestionService));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{BackgroundService} failed with error.",
                    nameof(DataIngestionService));
            }
        }
    }
}
