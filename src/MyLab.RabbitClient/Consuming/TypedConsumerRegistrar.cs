using System;

namespace MyLab.RabbitClient.Consuming
{
    class TypedConsumerRegistrar<TConsumer> : IRabbitConsumerRegistrar
        where TConsumer : class, IRabbitConsumer
    {
        private readonly string _queue;

        public TypedConsumerRegistrar(string queue)
        {
            _queue = queue;
        }

        public void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider)
        {
            registry.Register(_queue, new TypedConsumerProvider<TConsumer>());
        }
    }
}