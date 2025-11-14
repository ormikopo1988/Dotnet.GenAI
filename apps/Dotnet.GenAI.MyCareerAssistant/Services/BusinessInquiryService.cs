using Dotnet.GenAI.MyCareerAssistant.Data;
using Dotnet.GenAI.MyCareerAssistant.Dtos;
using Dotnet.GenAI.MyCareerAssistant.Entities;
using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using Dotnet.GenAI.MyCareerAssistant.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services
{
    public class BusinessInquiryService : IBusinessInquiryService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly ICosineSimilarityService _cosineSimilarityService;
        private readonly ILogger<BusinessInquiryService> _logger;

        public BusinessInquiryService(
            ApplicationDbContext dbContext,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            ICosineSimilarityService cosineSimilarityService,
            ILogger<BusinessInquiryService> logger)
        {
            _dbContext = dbContext;
            _embeddingGenerator = embeddingGenerator;
            _cosineSimilarityService = cosineSimilarityService;
            _logger = logger;
        }

        public async Task<int>
            CreateAsync(
                BusinessInquiryOptions options,
                CancellationToken ct = default)
        {
            if (options is null)
            {
                _logger.LogWarning(
                    "Null business inquiry options provided.");

                return 0;
            }

            if (string.IsNullOrWhiteSpace(
                options.Name))
            {
                _logger.LogWarning(
                    "Null or empty name provided.");

                return 0;
            }

            if (string.IsNullOrWhiteSpace(
                options.Email))
            {
                _logger.LogWarning(
                    "Null or empty email provided.");

                return 0;
            }

            if (string.IsNullOrWhiteSpace(
                options.Request))
            {
                _logger.LogWarning(
                    "Null or empty request provided.");

                return 0;
            }

            var requestEmbedding = await _embeddingGenerator
                .GenerateVectorAsync(
                    options.Request,
                    cancellationToken: ct);

            var newBusinessInquiry = new BusinessInquiry
            {
                Name = options.Name,
                Email = options.Email,
                Request = options.Request,
                Embedding = string.Join(",", requestEmbedding.ToArray())
            };

            _dbContext
                .BusinessInquiries
                .Add(newBusinessInquiry);

            try
            {
                return await _dbContext
                    .SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while saving the new business inquiry.");

                throw;
            }
        }

        public async Task<BusinessInquiryDto?>
            GetBySemanticMeaningAsync(
                string email,
                string request,
                CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Empty email provided");

                return null!;
            }

            if (string.IsNullOrWhiteSpace(request))
            {
                _logger.LogWarning("Empty request provided");

                return null!;
            }

            // Get all business inquiries from database for this email
            var businessInquiries = await _dbContext
                .BusinessInquiries
                .Where(bi => bi.Email == email)
                .ToListAsync(ct);

            var mostSimilarInquiry = await _cosineSimilarityService
                .FindMostSimilarEntityAsync(
                    request,
                    businessInquiries,
                    ct);

            if (mostSimilarInquiry is not null)
            {
                return new BusinessInquiryDto
                {
                    Name = mostSimilarInquiry.Name,
                    Email = mostSimilarInquiry.Email,
                    Request = mostSimilarInquiry.Request
                };
            }

            return null;
        }
    }
}
