using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
        /// <summary>
        /// <see cref="IRabbitChannelProvider"/> <see cref="ServiceDescriptor"/>
        /// </summary>
        public static readonly ServiceDescriptor ChannelProviderSrvDesc = new ServiceDescriptor(typeof(IRabbitChannelProvider), typeof(RabbitChannelProvider), ServiceLifetime.Singleton);


        /// <summary>
        /// <see cref="IRabbitPublisher"/> <see cref="ServiceDescriptor"/>
        /// </summary>
        public static readonly ServiceDescriptor PublisherSrvDesc = new ServiceDescriptor(typeof(IRabbitPublisher), typeof(DefaultRabbitPublisher), ServiceLifetime.Singleton);

        /// <summary>
        /// Lazy implementation of <see cref="IRabbitConnectionProvider"/> <see cref="ServiceDescriptor"/>
        /// </summary>
        public static readonly ServiceDescriptor LazyConnProviderSrvDesc = new ServiceDescriptor(typeof(IRabbitConnectionProvider), typeof(LazyRabbitConnectionProvider), ServiceLifetime.Singleton);

        /// <summary>
        /// Background implementation of <see cref="IRabbitConnectionProvider"/> <see cref="ServiceDescriptor"/>
        /// </summary>
        public static readonly ServiceDescriptor BgConnProviderSrvDesc = new ServiceDescriptor(typeof(IRabbitConnectionProvider), typeof(BackgroundRabbitConnectionProvider), ServiceLifetime.Singleton);

        /// <summary>
        /// Background connection manager <see cref="ServiceDescriptor"/>
        /// </summary>
        public static readonly ServiceDescriptor BgConnManagerSrvDesc = new ServiceDescriptor(typeof(IBackgroundRabbitConnectionManager), typeof(BackgroundRabbitConnectionManager), ServiceLifetime.Singleton);

        /// <summary>
        /// Background connection starter <see cref="ServiceDescriptor"/>
        /// </summary>
        public static readonly ServiceDescriptor BgConnStarterSrvDesc = new ServiceDescriptor(typeof(IHostedService), typeof(RabbitConnectionStarter), ServiceLifetime.Singleton);

        /// <summary>
        /// Adds Rabbit services
        /// </summary>
        public static IServiceCollection AddRabbit(this IServiceCollection srv, RabbitConnectionStrategy connectionStrategy = RabbitConnectionStrategy.Lazy)
        {
            srv.Add(PublisherSrvDesc);
            srv.Add(ChannelProviderSrvDesc);
            
            switch (connectionStrategy)
            {
                case RabbitConnectionStrategy.Lazy:
                    srv.Add(LazyConnProviderSrvDesc);
                    break;
                case RabbitConnectionStrategy.Background:
                {
                    srv.Add(BgConnManagerSrvDesc);
                    srv.Add(BgConnProviderSrvDesc);
                    srv.Add(BgConnStarterSrvDesc);
                }
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
            srv.Remove(ChannelProviderSrvDesc);
            srv.Remove(ConsumerHostSrvDesc);
            srv.Remove(ConsumerManagerSrvDesc);
            srv.Remove(PublisherSrvDesc);

            srv.Remove(LazyConnProviderSrvDesc);
            srv.Remove(BgConnManagerSrvDesc);
            srv.Remove(BgConnProviderSrvDesc);
            srv.Remove(BgConnStarterSrvDesc);

            srv.AddSingleton<IRabbitPublisher, EmulatorRabbitPublisher>();
            srv.AddSingleton<IConsumingLogicStrategy>(consumingLogicStrategy ?? new DefaultEmulatorConsumingLogicStrategy());

            return srv;
        }
    }
}
