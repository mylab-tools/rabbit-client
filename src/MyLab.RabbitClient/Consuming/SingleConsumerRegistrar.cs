using System;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Registers  single consumer
    /// </summary>
    public class SingleConsumerRegistrar : IRabbitConsumerRegistrar
    {
        private readonly string _queue;
        private readonly IRabbitConsumer _consumer;

        /// <summary>
        /// Initializes a new instance of <see cref="SingleConsumerRegistrar"/>
        /// </summary>
        public SingleConsumerRegistrar(string queue, IRabbitConsumer consumer)
        {
            _queue = queue;
            _consumer = consumer;
        }

        /// <inheritdoc />
        public void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider)
        {
            registry.Register(_queue, new SingleConsumerProvider(_consumer));
        }
    }
}