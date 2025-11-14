using Dotnet.GenAI.MyCareerAssistant.Entities;
using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services
{
    public class CosineSimilarityService : ICosineSimilarityService
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly ILogger<CosineSimilarityService> _logger;

        public CosineSimilarityService(
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            ILogger<CosineSimilarityService> logger)
        {
            _embeddingGenerator = embeddingGenerator;
            _logger = logger;
        }

        public async Task<T?> FindMostSimilarEntityAsync<T>(
            string input,
            IEnumerable<T> entities, 
            CancellationToken ct = default)
            where T : BaseEmbeddableEntity
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("Null or empty input provided");
                
                return null;
            }

            if (entities is null)
            {
                _logger.LogWarning("Null entities provided");

                return null;
            }

            if (!entities.Any())
            {
                return null;
            }

            // Generate embedding for input
            var inputEmbedding = await _embeddingGenerator
                .GenerateVectorAsync(
                    input,
                    cancellationToken: ct);

            // Convert input embedding to float array
            var inputVector = inputEmbedding.ToArray();

            float maxSimilarity = 0;

            T? mostSimilar = null;

            // Find most similar entity
            foreach (var entity in entities)
            {
                var storedVector = entity.Embedding
                    .Split(',')
                    .Select(float.Parse)
                    .ToArray();

                var similarity = CosineSimilarity(
                    inputVector,
                    storedVector);

                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    mostSimilar = entity;
                }
            }

            // Return match if similarity exceeds threshold
            const float similarityThreshold = 0.8f;

            if (maxSimilarity >= similarityThreshold &&
                mostSimilar is not null)
            {
                return mostSimilar;
            }

            return null;
        }

        private static float CosineSimilarity(
            float[] v1, float[] v2)
        {
            var dotProduct = v1
                .Zip(v2, (a, b) => a * b)
                .Sum();

            var magnitude1 = (float)Math
                .Sqrt(v1.Select(x => x * x).Sum());
            
            var magnitude2 = (float)Math
                .Sqrt(v2.Select(x => x * x).Sum());

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}
