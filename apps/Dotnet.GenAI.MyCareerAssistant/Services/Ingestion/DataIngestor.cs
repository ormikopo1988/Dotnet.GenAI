using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services.Ingestion
{
    public class DataIngestor
    {
        private readonly VectorStoreCollection<string, IngestedChunk> _chunksCollection;
        private readonly VectorStoreCollection<string, IngestedDocument> _documentsCollection;
        private readonly ILogger<DataIngestor> _logger;

        public DataIngestor(
            VectorStoreCollection<string, IngestedChunk> chunksCollection,
            VectorStoreCollection<string, IngestedDocument> documentsCollection,
            ILogger<DataIngestor> logger)
        {
            _chunksCollection = chunksCollection;
            _documentsCollection = documentsCollection;
            _logger = logger;
        }

        public async static Task IngestDataAsync(
            IServiceProvider serviceProvider,
            IIngestionSource source)
        {
            using var scope = serviceProvider.CreateScope();

            var ingestor = scope
                .ServiceProvider
                .GetRequiredService<DataIngestor>();

            await ingestor.IngestDataAsync(source);
        }

        public async Task IngestDataAsync(IIngestionSource source)
        {
            await _chunksCollection.EnsureCollectionExistsAsync();
            await _documentsCollection.EnsureCollectionExistsAsync();

            var sourceId = source.SourceId;

            var documentsForSource = await _documentsCollection
                .GetAsync(
                    doc => doc.SourceId == sourceId,
                    top: int.MaxValue)
                .ToListAsync();

            var deletedDocuments = await source
                .GetDeletedDocumentsAsync(documentsForSource);

            foreach (var deletedDocument in deletedDocuments)
            {
                _logger.LogInformation(
                    "Removing ingested data for {DocumentId}",
                    deletedDocument.DocumentId);

                await DeleteChunksForDocumentAsync(deletedDocument);

                await _documentsCollection
                    .DeleteAsync(deletedDocument.Key);
            }

            var modifiedDocuments = await source
                .GetNewOrModifiedDocumentsAsync(documentsForSource);

            foreach (var modifiedDocument in modifiedDocuments)
            {
                _logger.LogInformation(
                    "Processing {DocumentId}",
                    modifiedDocument.DocumentId);

                await DeleteChunksForDocumentAsync(modifiedDocument);

                await _documentsCollection.UpsertAsync(modifiedDocument);

                var newRecords = await source
                    .CreateChunksForDocumentAsync(modifiedDocument);

                await _chunksCollection.UpsertAsync(newRecords);
            }

            _logger.LogInformation("Ingestion is up-to-date");
        }

        private async Task DeleteChunksForDocumentAsync(
            IngestedDocument document)
        {
            var documentId = document.DocumentId;

            var chunksToDelete = await _chunksCollection
                .GetAsync(
                    record => record.DocumentId == documentId,
                    int.MaxValue)
                .ToListAsync();

            if (chunksToDelete.Count != 0)
            {
                await _chunksCollection
                    .DeleteAsync(
                        chunksToDelete.Select(
                            r => r.Key));
            }
        }
    }
}
