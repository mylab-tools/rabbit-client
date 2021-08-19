using System;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Provides the one consumer always
    /// </summary>
    public class SingleConsumerProvider : IRabbitConsumerProvider
    {
        private readonly IRabbitConsumer _consumer;

        /// <summary>
        /// Initializes a new instance of <see cref="SingleConsumerProvider"/>
        /// </summary>
        public SingleConsumerProvider(IRabbitConsumer consumer)
        {
            _consumer = consumer;
        }

        /// <inheritdoc />
        public IRabbitConsumer Provide(IServiceProvider serviceProvider)
        {
            return _consumer;
        }
    }
}