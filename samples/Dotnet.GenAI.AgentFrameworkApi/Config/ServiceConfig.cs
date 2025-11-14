using Dotnet.GenAI.Common.Configuration;
using Microsoft.Extensions.Configuration;

namespace Dotnet.GenAI.AgentFrameworkApi.Config
{
    /// <summary>
    /// Service configuration.
    /// </summary>
    public sealed class ServiceConfig
    {
        private readonly HostConfig _hostConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceConfig"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public ServiceConfig(ConfigurationManager configurationManager)
        {
            _hostConfig = new HostConfig(configurationManager);
        }

        /// <summary>
        /// Host configuration.
        /// </summary>
        public HostConfig Host => _hostConfig;
    }
}
