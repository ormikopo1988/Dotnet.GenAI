using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Threading.Tasks;

namespace Dotnet.GenAI.SemanticKernelConsoleAgent.Filters
{
    /// <summary>
    /// Example of function invocation filter to perform logging before and after function invocation.
    /// </summary>
    public sealed class LoggingFilter(ILogger<LoggingFilter> logger)
        : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(
            FunctionInvocationContext context, 
            Func<FunctionInvocationContext, Task> next)
        {
            logger.LogInformation(
                "IFunctionInvocationFilter -> LoggingFilter Function Invoking - {PluginName}.{FunctionName}", 
                context.Function.PluginName, 
                context.Function.Name);

            await next(context);

            logger.LogInformation(
                "IFunctionInvocationFilter -> LoggingFilter Function Invoked - {PluginName}.{FunctionName}",
                context.Function.PluginName,
                context.Function.Name);
        }
    }
}
