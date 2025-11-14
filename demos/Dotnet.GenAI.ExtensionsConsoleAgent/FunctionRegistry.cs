using Dotnet.GenAI.Common.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Dotnet.GenAI.ExtensionsConsoleAgent
{
    public static class FunctionRegistry
    {
        public static IEnumerable<AITool> GetTools(this IServiceProvider sp)
        {
            var githubClient = sp.GetRequiredService<GithubClient>();

            var getUserInformationFn = typeof(GithubClient)
                .GetMethod(nameof(GithubClient.GetUserInformationAsync),
                    [typeof(string), typeof(CancellationToken)])!;

            yield return AIFunctionFactory.Create(
                getUserInformationFn,
                githubClient,
                new AIFunctionFactoryOptions
                {
                    Name = "get_github_user_information",
                    Description = "Get information about a specified username from Github.",
                });

            var techPreferenceService = sp.GetRequiredService<TechnologyPreferenceService>();

            var getTechPreferencesFn = typeof(TechnologyPreferenceService)
                .GetMethod(nameof(TechnologyPreferenceService.ListPreferences),
                    [])!;

            yield return AIFunctionFactory.Create(
                getTechPreferencesFn,
                techPreferenceService,
                new AIFunctionFactoryOptions
                {
                    Name = "get_technology_preferences",
                    Description = "Lists all the technology preferences I have",
                });

            var emailService = sp.GetRequiredService<EmailService>();

            var emailFriendFn = typeof(EmailService)
                .GetMethod(nameof(EmailService.EmailFriend),
                    [typeof(string), typeof(string)])!;

            yield return AIFunctionFactory.Create(
                emailFriendFn,
                emailService,
                new AIFunctionFactoryOptions
                {
                    Name = "email_friend",
                    Description = "Sends an email to my friend with this name",
                });
        }
    }
}
