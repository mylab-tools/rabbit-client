using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyLab.Mq
{
    /// <summary>
    /// Contains extensions to integrate Mq abilities into application
    /// </summary>
    public static class MqIntegration
    {
        /// <summary>
        /// Add MQ publisher <see cref="IMqPublisher"/> in application dependency container
        /// </summary>
        public static IServiceCollection AddMqPublisher(this IServiceCollection services)
        {
            return services.AddSingleton<IMqPublisher, DefaultMqPublisher>();
        }

        /// <summary>
        /// Add MQ consuming abilities
        /// </summary>
        public static IServiceCollection AddMqConsuming(
            this IServiceCollection services,
            Action<IMqConsumerRegistrar> consumerRegistration)
        {
            var map = new ConsumerMap();
            consumerRegistration(map.CreateRegistrar());
            var manager = new DefaultMqConsumerManager(map);

            return services.AddSingleton<IMqConsumerManager>(manager);
        }
    }

    public static class MqConfig
    {
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
    }
}
