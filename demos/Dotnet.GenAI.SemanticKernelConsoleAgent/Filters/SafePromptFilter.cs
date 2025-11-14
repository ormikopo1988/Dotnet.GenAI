using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Threading.Tasks;

namespace Dotnet.GenAI.SemanticKernelConsoleAgent.Filters
{
    /// <summary>
    /// Example of prompt render filter which overrides rendered prompt before sending it to AI.
    /// </summary>
    public class SafePromptFilter(ILogger<SafePromptFilter> logger) 
        : IPromptRenderFilter
    {
        public async Task OnPromptRenderAsync(
            PromptRenderContext context, 
            Func<PromptRenderContext, Task> next)
        {
            logger.LogInformation(
                "IPromptRenderFilter -> SafePromptFilter Function Invoking - {PluginName}.{FunctionName}",
                context.Function.PluginName,
                context.Function.Name);

            await next(context);

            // Example: override rendered prompt before sending it to AI
            if (ShouldModifyPrompt(context.Arguments))
            {
                logger.LogWarning(
                    "Inappropriate content detected - {PluginName}.{FunctionName}",
                    context.Function.PluginName,
                    context.Function.Name);

                context.RenderedPrompt = "Tell me a joke about programming.";
            }

            logger.LogInformation(
                "IPromptRenderFilter -> SafePromptFilter Function Invoked - {PluginName}.{FunctionName}",
                context.Function.PluginName,
                context.Function.Name);
        }

        // Helper method to determine if prompt needs modification
        static bool ShouldModifyPrompt(KernelArguments arguments)
        {
            if (!arguments.TryGetValue("topic", out var input))
            {
                return false;
            }

            var inputStr = input?.ToString() ?? string.Empty;

            return string.IsNullOrWhiteSpace(inputStr) ||
                inputStr.Contains("women", StringComparison.OrdinalIgnoreCase);
        }
    }
}
