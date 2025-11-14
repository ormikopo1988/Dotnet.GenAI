using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using Microsoft.SemanticKernel.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Dotnet.GenAI.MyCareerAssistant.Services.Ingestion
{
    public class PDFDirectorySource
        : IIngestionSource
    {
        private readonly string _sourceDirectory;

        public PDFDirectorySource(string sourceDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"The specified directory does not exist: {sourceDirectory}");
            }

            _sourceDirectory = sourceDirectory;
        }

        public static string SourceFileId(string path)
            => Path.GetFileName(path);

        public static string SourceFileVersion(string path)
            => File.GetLastWriteTimeUtc(path).ToString("o");

        public string SourceId =>
            $"{nameof(PDFDirectorySource)}:{_sourceDirectory}";

        public Task<IEnumerable<IngestedDocument>>
            GetNewOrModifiedDocumentsAsync(
                IReadOnlyList<IngestedDocument> existingDocuments)
        {
            var results = new List<IngestedDocument>();

            var sourceFiles = Directory
                .GetFiles(_sourceDirectory, "*.pdf");

            var existingDocumentsById = existingDocuments
                .ToDictionary(d => d.DocumentId);

            foreach (var sourceFile in sourceFiles)
            {
                var sourceFileId = SourceFileId(sourceFile);
                var sourceFileVersion = SourceFileVersion(sourceFile);

                var existingDocumentVersion = existingDocumentsById
                    .TryGetValue(sourceFileId, out var existingDocument) ?
                        existingDocument.DocumentVersion : null;

                if (existingDocumentVersion != sourceFileVersion)
                {
                    results.Add(
                        new()
                        {
                            Key = Guid.CreateVersion7().ToString(),
                            SourceId = SourceId,
                            DocumentId = sourceFileId,
                            DocumentVersion = sourceFileVersion
                        });
                }
            }

            return Task.FromResult((IEnumerable<IngestedDocument>)results);
        }

        public Task<IEnumerable<IngestedDocument>>
            GetDeletedDocumentsAsync(
                IReadOnlyList<IngestedDocument> existingDocuments)
        {
            var currentFiles = Directory
                .GetFiles(_sourceDirectory, "*.pdf");

            var currentFileIds = currentFiles
                .ToLookup(SourceFileId);

            var deletedDocuments = existingDocuments
                .Where(d =>
                    !currentFileIds.Contains(d.DocumentId));

            return Task.FromResult(deletedDocuments);
        }

        public Task<IEnumerable<IngestedChunk>>
            CreateChunksForDocumentAsync(
                IngestedDocument document)
        {
            using var pdf = PdfDocument.Open(
                Path.Combine(
                    _sourceDirectory,
                    document.DocumentId));

            var paragraphs = pdf
                .GetPages()
                .SelectMany(GetPageParagraphs)
                .ToList();

            return Task.FromResult(
                paragraphs.Select(
                    p => new IngestedChunk
                    {
                        Key = Guid.CreateVersion7().ToString(),
                        DocumentId = document.DocumentId,
                        PageNumber = p.PageNumber,
                        Text = p.Text,
                    }));
        }

        private static
            IEnumerable<(int PageNumber, int IndexOnPage, string Text)>
                GetPageParagraphs(Page pdfPage)
        {
            var letters = pdfPage.Letters;

            var words = NearestNeighbourWordExtractor
                .Instance
                .GetWords(letters);

            var textBlocks = DocstrumBoundingBoxes
                .Instance
                .GetBlocks(words);

            var pageText = string.Join(
                Environment.NewLine + Environment.NewLine,
                textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")));

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
            return TextChunker
                .SplitPlainTextParagraphs([pageText], 200)
                .Select((text, index) => (pdfPage.Number, index, text));
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only
        }
    }
}
