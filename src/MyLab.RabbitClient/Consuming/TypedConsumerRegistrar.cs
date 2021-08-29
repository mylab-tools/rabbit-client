using System;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Registers consumer by type
    /// </summary>
    /// <typeparam name="TConsumer">consumer type</typeparam>
    public class TypedConsumerRegistrar<TConsumer> : IRabbitConsumerRegistrar
        where TConsumer : class, IRabbitConsumer
    {
        private readonly string _queue;

        /// <summary>
        /// Initializes a new instance of <see cref="TypedConsumerRegistrar{TConsumer}"/>
        /// </summary>
        public TypedConsumerRegistrar(string queue)
        {
            _queue = queue;
        }

        /// <inheritdoc />
        public void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider)
        {
            registry.Register(_queue, new TypedConsumerProvider<TConsumer>());
        }
    }
}