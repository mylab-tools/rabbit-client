using System;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Registers initial MQ consumers
    /// </summary>
    public interface IMqInitialConsumerRegistrar
    {
        /// <summary>
        /// Registers MQ consumer
        /// </summary>
        void RegisterConsumer(MqConsumer consumer);

        /// <summary>
        /// Registers MQ consumer
        /// </summary>
        void RegisterConsumerByOptions<TOptions>(string queueName, Func<TOptions, MqConsumer> consumerFactory)
            where TOptions : class, new();
    }
}