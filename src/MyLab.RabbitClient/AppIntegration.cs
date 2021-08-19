using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MyLab.RabbitClient.Connection;
using MyLab.RabbitClient.Consuming;

namespace MyLab.RabbitClient
{
    /// <summary>
    /// Contains extension methods for integration into application
    /// </summary>
    public static class AppIntegration
    {
        /// <summary>
        /// Registers consumer with specified consumer for specified queue
        /// </summary>
        public static IServiceCollection AddRabbitConsumer(this IServiceCollection srvColl, string queue, IRabbitConsumer consumer)
        {
            return srvColl.TryAddConsuming()
                .AddRabbitConsumers(new SingleConsumerRegistrar(queue, consumer));
        }

        /// <summary>
        /// Registers consumer with specified consumer for specified queue type
        /// </summary>
        public static IServiceCollection AddRabbitConsumer<TConsumer>(this IServiceCollection srvColl, string queue)
            where TConsumer : class, IRabbitConsumer
        {
            return srvColl.TryAddConsuming()
                .AddRabbitConsumers(new TypedConsumerRegistrar<TConsumer>(queue));
        }

        /// <summary>
        /// Adds consumer registrar which allow to registers several consumers depends on dependent options and services
        /// </summary>
        public static IServiceCollection AddRabbitConsumers<TRegistrar>(this IServiceCollection srvColl)
            where TRegistrar : class, IRabbitConsumerRegistrar
        {
            return srvColl.TryAddConsuming()
                .AddRabbitConsumers(new WrapperConsumerRegistrar<TRegistrar>());
        }

        /// <summary>
        /// Adds consumer registrar which allow to registers several consumers depends on dependent options and services
        /// </summary>
        public static IServiceCollection AddRabbitConsumers(this IServiceCollection srvColl, IRabbitConsumerRegistrar registrar)
        {
            return srvColl.TryAddConsuming()
                .Configure<ConsumerRegistrarSource>(s => s.Add(registrar));
        }

        static IServiceCollection TryAddConsuming(this IServiceCollection srvColl)
        {
            srvColl.TryAddSingleton<IHostedService, ConsumerHost>();
            srvColl.TryAddSingleton<IConsumerManager, ConsumerManager>();
            srvColl.TryAddSingleton<IRabbitConnectionProvider, RabbitConnectionProvider>();
            srvColl.TryAddSingleton<IRabbitChannelProvider, RabbitChannelProvider>();

            return srvColl;
        }

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
    }
}
