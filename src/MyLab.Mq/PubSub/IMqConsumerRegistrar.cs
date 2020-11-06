using System;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Defines MQ consumer registrar
    /// </summary>
    public interface IMqConsumerRegistrar
    {
        /// <summary>
        /// Registers new MQ consumer
        /// </summary>
        IDisposable AddConsumer(MqConsumer consumer);

        /// <summary>
        /// Unregisters consumer by the name
        /// </summary>
        void RemoveConsumer(string queueName);
    }
}