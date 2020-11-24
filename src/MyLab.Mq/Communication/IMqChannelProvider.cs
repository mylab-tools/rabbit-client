using RabbitMQ.Client;

namespace MyLab.Mq.Communication
{
    /// <summary>
    /// Provides MQ channels
    /// </summary>
    public interface IMqChannelProvider
    {
        /// <summary>
        /// Provides MQ channel with specified prefetch count
        /// </summary>
        IModel Provide(ushort prefetchCount = 1);
    }
}