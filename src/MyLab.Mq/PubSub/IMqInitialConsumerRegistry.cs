using System.Collections.Generic;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Defines consumer registry which contains initial defined consumers
    /// </summary>
    public interface IMqInitialConsumerRegistry
    {
        /// <summary>
        /// Gets registered consumer array
        /// </summary>
        IReadOnlyDictionary<string, MqConsumer> GetConsumers();
    }
}