using Microsoft.Extensions.VectorData;

namespace Dotnet.GenAI.MyCareerAssistant.Services.Ingestion
{
    public class IngestedChunk
    {
        // 1536 is the default vector size for the OpenAI text-embedding-ada-002 model
        private const int VectorDimensions = 1536;
        private const string VectorDistanceFunction = DistanceFunction.CosineDistance;

        [VectorStoreKey]
        public required string Key { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public required string DocumentId { get; set; }

        [VectorStoreData]
        public int PageNumber { get; set; }

        [VectorStoreData]
        public required string Text { get; set; }

        [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
        public string? Vector => Text;
    }
}
