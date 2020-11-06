using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Extensions to MQ tools configuration
    /// </summary>
    public static class MqConfig
    {
        /// <summary>
        /// Default config section name 
        /// </summary>
        public const string DefaultConfigSectionName = "Mq";

        /// <summary>
        /// Loads configuration for MessageQueue connection
        /// </summary>
        public static IServiceCollection LoadMqConfig(
            this IServiceCollection services, 
            IConfiguration configuration, 
            string sectionName = DefaultConfigSectionName)
        {
            return services.Configure<MqOptions>(configuration.GetSection(sectionName));
        }

        /// <summary>
        /// Loads configuration for MessageQueue connection
        /// </summary>
        public static IServiceCollection ConfigureMq(
            this IServiceCollection services,
            Action<MqOptions> configurator)
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));
            return services.Configure(configurator);
        }
    }
}