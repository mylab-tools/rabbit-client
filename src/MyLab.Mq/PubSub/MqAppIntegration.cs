using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyLab.Mq.Communication;
using MyLab.Mq.StatusProvider;
using MyLab.Mq.Test;

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
            services.TryAddSingleton<IMqChannelProvider, MqChannelProvider>();

            return services;
        }

        /// <summary>
        /// Add MQ consuming abilities
        /// </summary>
        public static IServiceCollection AddMqConsuming(
            this IServiceCollection services,
            Action<IMqInitialConsumerRegistrar> consumerRegistration)
        {
            if (consumerRegistration == null) 
                throw new ArgumentNullException(nameof(consumerRegistration));

            var registry = new DefaultMqInitialConsumerRegistry();
            consumerRegistration(registry.CreateRegistrar());

            services
                .AddSingleton<IMqInitialConsumerRegistry>(registry)
                .AddSingleton<IMqConsumerHost, MqConsumerHost>()
                .AddSingleton<IMqConsumerRegistrar, MqConsumerHost>();

            services.AddHostedService<MqConsumingStarter>();

            services.TryAddSingleton<IMqStatusService, DefaultMqStatusService>();
            services.TryAddSingleton<IMqConnectionProvider, DefaultMqConnectionProvider>();
            services.TryAddSingleton<IMqChannelProvider, MqChannelProvider>();

            services
                .AddScoped<DefaultMqMessageAccessor>()
                .AddScoped<IMqMessageAccessor>(sp => sp.GetRequiredService<DefaultMqMessageAccessor>())
                .AddScoped<IMqMessageAccessorCore>(sp => sp.GetRequiredService<DefaultMqMessageAccessor>());

            return services;
        }

        /// <summary>
        /// Add Emulator instead default MQ consuming tool
        /// </summary>
        public static IServiceCollection AddMqMsgEmulator(
            this IServiceCollection services)
        {
            var consumingStarter = services.FirstOrDefault(s => s.ImplementationType == typeof(MqConsumingStarter));
            if (consumingStarter != null)
                services.Remove(consumingStarter);

            services.AddSingleton<IInputMessageEmulator, DefaultInputMessageEmulator>();

            return services;
        }
    }
}
