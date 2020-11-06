using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyLab.Mq.Communication;
using MyLab.Mq.StatusProvider;

namespace MyLab.Mq.PubSub
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
            Action<IMqInitialConsumerRegistrar> consumerRegistration,
            IInitiatorRegistrar initiatorRegistrar = null)
        {
            if (consumerRegistration == null) throw new ArgumentNullException(nameof(consumerRegistration));

            var registry = new DefaultMqInitialConsumerRegistry();
            consumerRegistration(registry.CreateRegistrar());

            services.AddSingleton<IMqInitialConsumerRegistry>(registry)
                .TryAddSingleton<IMqStatusService, DefaultMqStatusService>();

            (initiatorRegistrar ?? new DefaultQueueListenerRegistrar()).Register(services);
            services.AddSingleton<MqConsumerHost>();

            services.TryAddSingleton<IMqConnectionProvider, DefaultMqConnectionProvider>();

            services
                .AddScoped<DefaultMqMessageAccessor>()
                .AddScoped<IMqMessageAccessor>(sp => sp.GetRequiredService<DefaultMqMessageAccessor>())
                .AddScoped<IMqMessageAccessorCore>(sp => sp.GetRequiredService<DefaultMqMessageAccessor>());

            return services;
        }
    }
}
