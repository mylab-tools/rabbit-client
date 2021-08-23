using System.Collections.Generic;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Registers consumers
    /// </summary>
    public interface IRabbitConsumerRegistry : IDictionary<string, IRabbitConsumerProvider>
    {
        /// <summary>
        /// Registers consumer with specified logic for specified queue
        /// </summary>
        void Register(string queue, IRabbitConsumerProvider rabbitConsumerProvider);
    }
}