using Dotnet.GenAI.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Dotnet.GenAI.SemanticKernelConsoleAgent
{
    public static class FunctionRegistry
    {
        public static IEnumerable<KernelPlugin> GetPlugins(this IServiceProvider sp)
        {
            var githubClient = sp.GetRequiredService<GithubClient>();

            var getUserInformationFn = typeof(GithubClient)
                .GetMethod(nameof(GithubClient.GetUserInformationAsync),
                    [typeof(string), typeof(CancellationToken)])!;

            yield return KernelPluginFactory
                .CreateFromFunctions("get_github_user_information_plugin",
                [
                    KernelFunctionFactory.CreateFromMethod(
                        method: getUserInformationFn,
                        target: githubClient,
                        functionName: "get_github_user_information",
                        description: "Get information about a specified username from Github."
                    )
                ]);

            var techPreferenceService = sp.GetRequiredService<TechnologyPreferenceService>();

            var getTechPreferencesFn = typeof(TechnologyPreferenceService)
                .GetMethod(nameof(TechnologyPreferenceService.ListPreferences),
                    [])!;

            yield return KernelPluginFactory
                .CreateFromFunctions("get_technology_preferences_plugin",
                [
                    KernelFunctionFactory.CreateFromMethod(
                        method: getTechPreferencesFn,
                        target: techPreferenceService,
                        functionName: "get_technology_preferences",
                        description: "Lists all the technology preferences I have."
                    )
                ]);

            var emailService = sp.GetRequiredService<EmailService>();

            var emailFriendFn = typeof(EmailService)
                .GetMethod(nameof(EmailService.EmailFriend),
                    [typeof(string), typeof(string)])!;

            yield return KernelPluginFactory
                .CreateFromFunctions("email_friend_plugin",
                [
                    KernelFunctionFactory.CreateFromMethod(
                        method: emailFriendFn,
                        target: emailService,
                        functionName: "email_friend",
                        description: "Sends an email to my friend with this name."
                    )
                ]);

            // Create plug-in from a prompt
            //var functionFromPrompt = KernelFunctionFactory
            //    .CreateFromPrompt(
            //        "Tell me a joke about {{$topic}}.", 
            //        new AzureOpenAIPromptExecutionSettings 
            //        { 
            //            MaxTokens = 150
            //        },
            //        "tell_joke",
            //        "A function that generates a joke about a topic.");

            //yield return KernelPluginFactory
            //    .CreateFromFunctions(
            //        "tell_joke_plugin", 
            //        [
            //            functionFromPrompt
            //        ]);

            // Create plug-in from a prompt in a YAML file
            var tellJokeYml = File.ReadAllText("./Templates/TellJoke.yml");
            
            var functionFromYaml = KernelFunctionYaml.FromPromptYaml(tellJokeYml);

            yield return KernelPluginFactory
                .CreateFromFunctions(
                    "tell_joke_plugin",
                    [
                        functionFromYaml
                    ]);
        }
    }
}
