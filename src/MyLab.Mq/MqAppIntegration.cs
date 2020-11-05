using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            services
                .AddSingleton<IMqPublisher, DefaultMqPublisher>()
                .TryAddSingleton<IMqStatusService, DefaultMqStatusService>();

            services.TryAddSingleton<IMqConnectionProvider, DefaultMqConnectionProvider>();

            return services;
        }

        /// <summary>
        /// Add MQ consuming abilities
        /// </summary>
        public static IServiceCollection AddMqConsuming(
            this IServiceCollection services,
            Action<IMqConsumerRegistrar> consumerRegistration,
            IInitiatorRegistrar initiatorRegistrar = null)
        {
            if (consumerRegistration == null) throw new ArgumentNullException(nameof(consumerRegistration));

            var registry = new DefaultMqConsumerRegistry();
            consumerRegistration(registry.CreateRegistrar());

            services.AddSingleton<IMqConsumerRegistry>(registry)
                .TryAddSingleton<IMqStatusService, DefaultMqStatusService>();

            (initiatorRegistrar ?? new DefaultQueueListenerRegistrar()).Register(services);
            
            services.TryAddSingleton<IMqConnectionProvider, DefaultMqConnectionProvider>();

            services
                .AddScoped<DefaultMqMessageAccessor>()
                .AddScoped<IMqMessageAccessor>(sp => sp.GetRequiredService<DefaultMqMessageAccessor>())
                .AddScoped<IMqMessageAccessorCore>(sp => sp.GetRequiredService<DefaultMqMessageAccessor>());

            return services;
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
