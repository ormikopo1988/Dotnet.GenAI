using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Threading.Tasks;

namespace Dotnet.GenAI.SemanticKernelConsoleAgent.Filters
{
    /// <summary>
    /// Example of auto function invocation filter.
    /// </summary>
    public sealed class EarlyTerminationFilter(ILogger<EarlyTerminationFilter> logger) 
        : IAutoFunctionInvocationFilter
    {
        public async Task OnAutoFunctionInvocationAsync(
            AutoFunctionInvocationContext context, 
            Func<AutoFunctionInvocationContext, Task> next)
        {
            logger.LogInformation(
                "IAutoFunctionInvocationFilter -> EarlyTerminationFilter Function Invoking - {PluginName}.{FunctionName}",
                context.Function.PluginName,
                context.Function.Name);

            // Call the function first.
            await next(context);

            logger.LogInformation(
                "IAutoFunctionInvocationFilter -> EarlyTerminationFilter Function Invoked - {PluginName}.{FunctionName}",
                context.Function.PluginName,
                context.Function.Name);
        }
    }
}
