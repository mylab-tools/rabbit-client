using System;
using System.Collections;
using System.Collections.Generic;

namespace MyLab.Mq
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