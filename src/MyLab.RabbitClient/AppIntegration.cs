using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Connection;
using MyLab.RabbitClient.Publishing;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods for integration into application
    /// </summary>
    public static partial class AppIntegration
    {
        /// <summary>
        /// Configures Rabbit Client
        /// </summary>
        public static IServiceCollection ConfigureRabbitClient(this IServiceCollection srv, IConfiguration cfg, string sectionName = "MQ")
        {
            return srv.Configure<RabbitOptions>(cfg.GetSection(sectionName));
        }

        /// <summary>
        /// Configures Rabbit Client
        /// </summary>
        public static IServiceCollection ConfigureRabbitClient(this IServiceCollection srv, Action<RabbitOptions> configureAct)
        {
            return srv.Configure(configureAct);
        }

        private static IServiceCollection TryAddCommon(this IServiceCollection srvColl)
        {
            srvColl.TryAddSingleton<IRabbitConnectionProvider, RabbitConnectionProvider>();
            srvColl.TryAddSingleton<IRabbitChannelProvider, RabbitChannelProvider>();

            return srvColl;
        }
    }
}
