using Dotnet.GenAI.MyCareerAssistant.Data;
using Dotnet.GenAI.MyCareerAssistant.Dtos;
using Dotnet.GenAI.MyCareerAssistant.Entities;
using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using Dotnet.GenAI.MyCareerAssistant.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services
{
    public class QuestionAndAnswerService : IQuestionAndAnswerService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly ICosineSimilarityService _cosineSimilarityService;
        private readonly ILogger<QuestionAndAnswerService> _logger;

        public QuestionAndAnswerService(
            ApplicationDbContext dbContext,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            ICosineSimilarityService cosineSimilarityService,
            ILogger<QuestionAndAnswerService> logger)
        {
            _dbContext = dbContext;
            _embeddingGenerator = embeddingGenerator;
            _cosineSimilarityService = cosineSimilarityService;
            _logger = logger;
        }

        public async Task<int>
            CreateAsync(
                QuestionAndAnswerOptions options, 
                CancellationToken ct = default)
        {
            if (options is null)
            {
                _logger.LogWarning(
                    "Null question and answer options provided.");

                return 0;
            }

            if (string.IsNullOrWhiteSpace(
                options.Question))
            {
                _logger.LogWarning(
                    "Null or empty question provided.");

                return 0;
            }

            var questionEmbedding = await _embeddingGenerator
                .GenerateVectorAsync(
                    options.Question,
                    cancellationToken: ct);

            var newQuestion = new QuestionAndAnswer
            {
                Question = options.Question,
                Embedding = string.Join(",", questionEmbedding.ToArray())
            };

            _dbContext
                .QuestionAndAnswers
                .Add(newQuestion);

            try
            {
                return await _dbContext
                    .SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while saving the new question and answer.");

                throw;
            }
        }

        public async Task<IEnumerable<QuestionAndAnswerDto>>
            GetAllAsync(CancellationToken ct = default)
        {
            return await _dbContext
                .QuestionAndAnswers
                .Where(qa => !string.IsNullOrWhiteSpace(qa.Answer))
                .Select(qa =>
                    new QuestionAndAnswerDto
                    {
                        Question = qa.Question,
                        Answer = qa.Answer
                    })
                .ToListAsync(ct);
        }

        public async Task<QuestionAndAnswerDto?> 
            GetBySemanticMeaningAsync(
                string question, 
                CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                _logger.LogWarning("Empty question provided");

                return null!;
            }

            // Get all questions from database
            var questions = await _dbContext
                .QuestionAndAnswers
                .ToListAsync(ct);

            var mostSimilarQA = await _cosineSimilarityService
                .FindMostSimilarEntityAsync(
                    question,
                    questions,
                    ct);

            if (mostSimilarQA is not null)
            {
                return new QuestionAndAnswerDto
                {
                    Question = mostSimilarQA.Question,
                    Answer = mostSimilarQA.Answer
                };
            }

            return null;
        }
    }
}
