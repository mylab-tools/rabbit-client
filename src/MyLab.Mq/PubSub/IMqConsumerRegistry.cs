using System.Collections.Generic;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Defines consumer registry
    /// </summary>
    public interface IMqConsumerRegistry
    {
        /// <summary>
        /// Gets registered consumer array
        /// </summary>
        IReadOnlyDictionary<string, MqConsumer> GetConsumers();
    }
}