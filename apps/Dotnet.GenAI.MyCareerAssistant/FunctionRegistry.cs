using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using Dotnet.GenAI.MyCareerAssistant.Options;
using Dotnet.GenAI.MyCareerAssistant.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Dotnet.GenAI.MyCareerAssistant
{
    public static class FunctionRegistry
    {
        public static IEnumerable<AITool> GetTools(
            this IServiceProvider sp)
        {
            var questionAndAnswerService = sp
                .GetRequiredService<IQuestionAndAnswerService>();

            var createQuestionAndAnswerFn = typeof(IQuestionAndAnswerService)
                .GetMethod(
                    nameof(IQuestionAndAnswerService.CreateAsync),
                    [
                        typeof(QuestionAndAnswerOptions), 
                        typeof(CancellationToken)
                    ])!;

            yield return AIFunctionFactory.Create(
                createQuestionAndAnswerFn,
                questionAndAnswerService,
                new AIFunctionFactoryOptions
                {
                    Name = "save_question_record_to_db",
                    Description =
                        "Always use this tool to save into the database any question a user has asked that couldn't be answered as you didn't know the answer.",
                });

            var getSemanticallySimilarQuestionFn = typeof(IQuestionAndAnswerService)
                .GetMethod(
                    nameof(IQuestionAndAnswerService.GetBySemanticMeaningAsync),
                    [
                        typeof(string),
                        typeof(CancellationToken)
                    ])!;

            yield return AIFunctionFactory.Create(
                getSemanticallySimilarQuestionFn,
                questionAndAnswerService,
                new AIFunctionFactoryOptions
                {
                    Name = "get_semantically_similar_question_record_from_db",
                    Description = 
                        "Use this tool to retrieve from the database a question record that is semantically similar to the user's question. This can help to not store a semantically similar question twice.",
                });

            var businessInquiryService = sp
                .GetRequiredService<IBusinessInquiryService>();

            var createBusinessInquiryFn = typeof(IBusinessInquiryService)
                .GetMethod(
                    nameof(IBusinessInquiryService.CreateAsync),
                    [
                        typeof(BusinessInquiryOptions),
                        typeof(CancellationToken)
                    ])!;

            yield return AIFunctionFactory.Create(
                createBusinessInquiryFn,
                businessInquiryService,
                new AIFunctionFactoryOptions
                {
                    Name = "save_business_inquiry_record_to_db",
                    Description =
                        "Always use this tool to save into the database any business inquiry from a user.",
                });

            var getSemanticallySimilarBusinessInquiryFn = typeof(IBusinessInquiryService)
                .GetMethod(
                    nameof(IBusinessInquiryService.GetBySemanticMeaningAsync),
                    [
                        typeof(string),
                        typeof(string),
                        typeof(CancellationToken)
                    ])!;

            yield return AIFunctionFactory.Create(
                getSemanticallySimilarBusinessInquiryFn,
                businessInquiryService,
                new AIFunctionFactoryOptions
                {
                    Name = "get_semantically_similar_business_inquiry_record_from_db",
                    Description =
                        "Use this tool to retrieve from the database a business inquiry record that is semantically similar to the user's inquiry request. This can help to not store a semantically similar business inquiry from the same user twice.",
                });

            var emailSender = sp
                .GetRequiredService<EmailSender>();

            var sendEmailFn = typeof(EmailSender)
                .GetMethod(
                    nameof(EmailSender.SendEmailAsync),
                    [
                        typeof(string), 
                        typeof(string), 
                        typeof(string), 
                        typeof(CancellationToken)
                    ])!;

            yield return AIFunctionFactory.Create(
                sendEmailFn,
                emailSender,
                new AIFunctionFactoryOptions
                {
                    Name = "send_email",
                    Description = 
                        "Send out an email with the given subject and message to a user with the specified email. Format the message as needed as an HTML body with proper structure before sending.",
                });

            var serperClient = sp
                .GetRequiredService<SerperClient>();

            var webSearchFn = typeof(SerperClient)
                .GetMethod(
                    nameof(SerperClient.WebSearchAsync),
                    [
                        typeof(string), 
                        typeof(CancellationToken)
                    ])!;

            yield return AIFunctionFactory.Create(
                webSearchFn,
                serperClient,
                new AIFunctionFactoryOptions
                {
                    Name = "web_search",
                    Description = 
                        "Use this tool to perform an online web search.",
                });
        }
    }
}
