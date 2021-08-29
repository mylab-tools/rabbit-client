using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Registers consumer with options
    /// </summary>
    /// <typeparam name="TOptions">options type</typeparam>
    /// <typeparam name="TConsumer">consumer type</typeparam>
    public class OptionsConsumerRegistrar<TOptions, TConsumer> : IRabbitConsumerRegistrar 
        where TOptions : class 
        where TConsumer : class, IRabbitConsumer
    {
        private readonly Func<TOptions, string> _queueProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="OptionsConsumerRegistrar{TOptions, TConsumer}"/>
        /// </summary>
        public OptionsConsumerRegistrar(Func<TOptions, string> queueProvider)
        {
            _queueProvider = queueProvider ?? throw new ArgumentNullException(nameof(queueProvider));
        }

        /// <inheritdoc />
        public void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetService<IOptions<TOptions>>();

            var queue = _queueProvider(options.Value);

            registry.Register(queue, new TypedConsumerProvider<TConsumer>());
        }
    }
}