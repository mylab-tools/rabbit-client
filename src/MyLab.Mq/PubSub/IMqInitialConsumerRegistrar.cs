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
        void RegisterConsumerByOptions<TOptions>(Func<TOptions, MqConsumer> consumerFactory)
            where TOptions : class, new();

        /// <summary>
        /// Registers MQ consumer if specified option was defined
        /// </summary>
        void RegisterConsumerByOptions<TOptions, TOption>(Func<TOptions, TOption> optionSelector, Func<TOption, MqConsumer> consumerFactory)
            where TOptions : class, new();
    }
}