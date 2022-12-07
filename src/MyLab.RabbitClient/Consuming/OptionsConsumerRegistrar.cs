using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Registers consumer with options
    /// </summary>
    /// <typeparam name="TOptions">options type</typeparam>
    public class OptionsConsumerRegistrar<TOptions> : IRabbitConsumerRegistrar 
        where TOptions : class, new()
    {
        private readonly bool _optional;
        private readonly Func<TOptions, string> _queueProvider;
        private readonly IRabbitConsumerProvider _consumerProvider;

        /// <summary>
        /// Creates new instance of <see cref="OptionsConsumerRegistrar{T}"/> for generic type specified consumer
        /// </summary>
        public static OptionsConsumerRegistrar<TOptions> Create<TConsumer>(Func<TOptions, string> queueProvider, bool optional = false)
            where TConsumer : class, IRabbitConsumer
        {
            return new OptionsConsumerRegistrar<TOptions>(queueProvider, new TypedConsumerProvider<TConsumer>(), optional);
        }

        /// <summary>
        /// Creates new instance of <see cref="OptionsConsumerRegistrar{T}"/> for specified consumer
        /// </summary>
        public static OptionsConsumerRegistrar<TOptions> Create(IRabbitConsumer consumer, Func<TOptions, string> queueProvider, bool optional = false)
        {
            return new OptionsConsumerRegistrar<TOptions>(queueProvider, new SingleConsumerProvider(consumer), optional);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OptionsConsumerRegistrar{TOptions}"/>
        /// </summary>
        public OptionsConsumerRegistrar(Func<TOptions, string> queueProvider, IRabbitConsumerProvider consumerProvider, bool optional = false)
        {
            _optional = optional;
            _queueProvider = queueProvider ?? throw new ArgumentNullException(nameof(queueProvider));
            _consumerProvider = consumerProvider;
        }

        /// <inheritdoc />
        public void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetService<IOptions<TOptions>>();

            var queue = _queueProvider(options.Value);

            if (string.IsNullOrEmpty(queue))
            {
                if(_optional) return;
                throw new InvalidOperationException("Queue name for consuming is not specified");
            }

            registry.Register(queue, _consumerProvider);
        }
    }
}