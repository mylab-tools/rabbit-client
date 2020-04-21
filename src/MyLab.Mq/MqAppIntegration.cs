using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyLab.StatusProvider;

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
            return services.AddSingleton<IMqPublisher, DefaultMqPublisher>()
                .AddMqConsumerStatusProviding();
        }

        /// <summary>
        /// Add MQ consuming abilities
        /// </summary>
        public static IServiceCollection AddMqConsuming(
            this IServiceCollection services,
            Action<IMqConsumerRegistrar> consumerRegistration)
        {
            if (consumerRegistration == null) throw new ArgumentNullException(nameof(consumerRegistration));

            var registry = new DefaultMqConsumerRegistry();
            consumerRegistration(registry.CreateRegistrar());

            return services.AddSingleton<IMqConsumerRegistry>(registry)
                .AddSingleton<IMqConnectionProvider, DefaultMqConnectionProvider>()
                .AddSingleton<DefaultMqConsumerManager>()
                .AddMqConsumerStatusProviding();
        }

        /// <summary>
        /// Add MQ consuming abilities
        /// </summary>
        public static IServiceCollection AddMqConsuming(
            this IServiceCollection services,
            IMqConnectionProvider connectionProvider,
            Action<IMqConsumerRegistrar> consumerRegistration)
        {
            if (connectionProvider == null) throw new ArgumentNullException(nameof(connectionProvider));
            if (consumerRegistration == null) throw new ArgumentNullException(nameof(consumerRegistration));

            var registry = new DefaultMqConsumerRegistry();
            consumerRegistration(registry.CreateRegistrar());

            return services.AddSingleton<IMqConsumerRegistry>(registry)
                .AddSingleton(connectionProvider)
                .AddSingleton<DefaultMqConsumerManager>()
                .AddMqConsumerStatusProviding();
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
