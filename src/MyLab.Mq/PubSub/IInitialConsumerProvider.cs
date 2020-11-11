using System;
using Microsoft.Extensions.Options;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Provides initial registered consumer
    /// </summary>
    public interface IInitialConsumerProvider
    {
        /// <summary>
        /// Provides a consumer
        /// </summary>
        MqConsumer Provide(IServiceProvider serviceProvider);
    }

    class ObjectInitialConsumerProvider: IInitialConsumerProvider
    {
        private readonly MqConsumer _consumer;

        public ObjectInitialConsumerProvider(MqConsumer consumer)
        {
            _consumer = consumer;
        }

        public MqConsumer Provide(IServiceProvider serviceProvider)
        {
            return _consumer;
        }
    }

    class ByOptionConsumerFactoryProvider<TOptions> : IInitialConsumerProvider
        where TOptions : class, new()
    {
        private readonly Func<TOptions, MqConsumer> _consumerFactory;

        public ByOptionConsumerFactoryProvider(Func<TOptions, MqConsumer> consumerFactory)
        {
            _consumerFactory = consumerFactory;
        }

        public MqConsumer Provide(IServiceProvider serviceProvider)
        {
            var options = (IOptions<TOptions>)serviceProvider.GetService(typeof(IOptions<TOptions>));
            return _consumerFactory(options.Value);
        }
    }

    class BySelectedOptionConsumerFactoryProvider<TOptions, TOption> : IInitialConsumerProvider
        where TOptions : class, new()
    {
        private readonly Func<TOptions, TOption> _optionSelector;
        private readonly Func<TOption, MqConsumer> _consumerFactory;

        public BySelectedOptionConsumerFactoryProvider(
            Func<TOptions, TOption> optionSelector,
            Func<TOption, MqConsumer> consumerFactory)
        {
            _optionSelector = optionSelector;
            _consumerFactory = consumerFactory;
        }

        public MqConsumer Provide(IServiceProvider serviceProvider)
        {
            var options = (IOptions<TOptions>)serviceProvider.GetService(typeof(IOptions<TOptions>));

            var option = _optionSelector(options.Value);

            if (option == null)
                return null;

            return _consumerFactory(option);
        }
    }
}