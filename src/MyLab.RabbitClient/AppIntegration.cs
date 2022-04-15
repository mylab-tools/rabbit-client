using System;
using Microsoft.Extensions.Configuration;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Connection;
using MyLab.RabbitClient.Consuming;
using MyLab.RabbitClient.Publishing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods for integration into application
    /// </summary>
    public static partial class AppIntegration
    {
        static readonly ServiceDescriptor RabbitPublisherServiceDescriptor = new ServiceDescriptor(typeof(IRabbitPublisher), typeof(DefaultRabbitPublisher), ServiceLifetime.Singleton);

        /// <summary>
        /// Adds Rabbit services
        /// </summary>
        public static IServiceCollection AddRabbit(this IServiceCollection srv, RabbitConnectionStrategy connectionStrategy = RabbitConnectionStrategy.Lazy)
        {
            srv.Add(RabbitPublisherServiceDescriptor);
            srv.AddSingleton<IRabbitChannelProvider, RabbitChannelProvider>();
            
            switch (connectionStrategy)
            {
                case RabbitConnectionStrategy.Lazy:
                    srv.AddSingleton<IRabbitConnectionProvider, LazyRabbitConnectionProvider>();
                    break;
                case RabbitConnectionStrategy.Background:
                    srv.AddSingleton<IRabbitConnectionProvider, BackgroundRabbitConnectionProvider>()
                        .AddSingleton<IBackgroundRabbitConnectionManager, BackgroundRabbitConnectionManager>()
                        .AddHostedService<RabbitConnectionStarter>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionStrategy), "Rabbit connection strategy must be defined");
            }

            return srv;
        }

        /// <summary>
        /// Configures Rabbit Client
        /// </summary>
        public static IServiceCollection ConfigureRabbit(this IServiceCollection srv, IConfiguration cfg, string sectionName = "MQ")
        {
            return srv.Configure<RabbitOptions>(cfg.GetSection(sectionName));
        }

        /// <summary>
        /// Configures Rabbit Client
        /// </summary>
        public static IServiceCollection ConfigureRabbit(this IServiceCollection srv, Action<RabbitOptions> configureAct)
        {
            return srv.Configure(configureAct);
        }

        /// <summary>
        /// Adds context for publishing message
        /// </summary>
        public static IServiceCollection AddRabbitPublishingContext<T>(this IServiceCollection srv)
            where T : class, IPublishingContext
        {
            return srv.AddSingleton<IPublishingContext, T>();
        }

        /// <summary>
        /// Adds emulation services and remove defaults
        /// </summary>
        public static IServiceCollection AddRabbitEmulation(this IServiceCollection srv, IConsumingLogicStrategy consumingLogicStrategy = null)
        {
            srv.Remove(ConsumerHostServiceDescriptor);
            srv.Remove(ConsumerManagerServiceDescriptor);
            srv.Remove(RabbitPublisherServiceDescriptor);

            srv.AddSingleton<IRabbitPublisher, EmulatorRabbitPublisher>();
            srv.AddSingleton<IConsumingLogicStrategy>(consumingLogicStrategy ?? new DefaultEmulatorConsumingLogicStrategy());

            return srv;
        }
    }
}
