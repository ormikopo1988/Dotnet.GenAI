using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.Common.Services
{
    public class DocumentationClient
    {
        private readonly string _docsDirectory;

        public DocumentationClient()
        {
            _docsDirectory = Path.Combine(
                AppContext.BaseDirectory, 
                "Docs");
        }

        public async Task<string?> GetDocumentationPageAsync(
            string pageName, 
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(pageName))
            {
                return null;
            }

            var safePageName = Path.GetFileName(pageName);
            
            var filePath = Path.Combine(
                _docsDirectory, 
                safePageName + ".md");

            if (!File.Exists(filePath))
            {
                return null;
            }

            return await File.ReadAllTextAsync(filePath, ct);
        }
    }
}
