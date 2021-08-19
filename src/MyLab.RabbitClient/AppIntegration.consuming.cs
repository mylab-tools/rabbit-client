using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MyLab.RabbitClient.Connection;
using MyLab.RabbitClient.Consuming;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class AppIntegration
    {
        /// <summary>
        /// Registers consumer with specified consumer for specified queue
        /// </summary>
        public static IServiceCollection AddRabbitConsumer(this IServiceCollection srvColl, string queue, IRabbitConsumer consumer)
        {
            return srvColl
                .TryAddConsuming()
                .TryAddCommon()
                .AddRabbitConsumers(new SingleConsumerRegistrar(queue, consumer));
        }

        /// <summary>
        /// Registers consumer with specified consumer for specified queue type
        /// </summary>
        public static IServiceCollection AddRabbitConsumer<TConsumer>(this IServiceCollection srvColl, string queue)
            where TConsumer : class, IRabbitConsumer
        {
            return srvColl
                .TryAddConsuming()
                .TryAddCommon()
                .AddRabbitConsumers(new TypedConsumerRegistrar<TConsumer>(queue));
        }

        /// <summary>
        /// Adds consumer registrar which allow to registers several consumers depends on dependent options and services
        /// </summary>
        public static IServiceCollection AddRabbitConsumers<TRegistrar>(this IServiceCollection srvColl)
            where TRegistrar : class, IRabbitConsumerRegistrar
        {
            return srvColl
                .TryAddConsuming()
                .TryAddCommon()
                .AddRabbitConsumers(new WrapperConsumerRegistrar<TRegistrar>());
        }

        /// <summary>
        /// Adds consumer registrar which allow to registers several consumers depends on dependent options and services
        /// </summary>
        public static IServiceCollection AddRabbitConsumers(this IServiceCollection srvColl, IRabbitConsumerRegistrar registrar)
        {
            return srvColl
                .TryAddConsuming()
                .TryAddCommon()
                .Configure<ConsumerRegistrarSource>(s => s.Add(registrar));
        }

        private static IServiceCollection TryAddConsuming(this IServiceCollection srvColl)
        {
            srvColl.TryAddSingleton<IHostedService, ConsumerHost>();
            srvColl.TryAddSingleton<IConsumerManager, ConsumerManager>();

            return srvColl;
        }
    }
}