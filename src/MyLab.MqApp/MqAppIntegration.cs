using System;
using Microsoft.Extensions.DependencyInjection;

namespace MyLab.MqApp
{
    /// <summary>
    /// Provides abilities to integrate MqApp login into application
    /// </summary>
    public static class MqAppIntegration
    {
        /// <summary>
        /// Add MQ publisher <see cref="IMqPublisher"/> in application dependency container
        /// </summary>
        public static IServiceCollection AddMqPublisher(this IServiceCollection services)
        {
            return services.AddSingleton<IMqPublisher>(new DefaultMqPublisher());
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
}
