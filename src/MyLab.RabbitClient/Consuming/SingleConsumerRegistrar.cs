using System;

namespace MyLab.RabbitClient.Consuming
{
    class SingleConsumerRegistrar : IRabbitConsumerRegistrar
    {
        private readonly string _queue;
        private readonly IRabbitConsumer _consumer;

        public SingleConsumerRegistrar(string queue, IRabbitConsumer consumer)
        {
            _queue = queue;
            _consumer = consumer;
        }

        public void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider)
        {
            registry.Register(_queue, new SingleConsumerProvider(_consumer));
        }
    }
}