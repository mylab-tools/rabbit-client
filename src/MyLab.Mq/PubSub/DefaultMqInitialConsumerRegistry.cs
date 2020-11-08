using System;
using System.Collections.Generic;
using System.Linq;

namespace MyLab.Mq.PubSub
{
    class DefaultMqInitialConsumerRegistry : IMqInitialConsumerRegistry
    {
        readonly List<IInitialConsumerProvider> _consumers = new List<IInitialConsumerProvider>();

        public IMqInitialConsumerRegistrar CreateRegistrar()
        {
            return new InitialConsumerRegistrar(_consumers);
        }

        public IEnumerable<MqConsumer> GetConsumers(IServiceProvider serviceProvider)
        {
            return _consumers.Select(c => c.Provide(serviceProvider));
        }

        class InitialConsumerRegistrar : IMqInitialConsumerRegistrar
        {
            private readonly List<IInitialConsumerProvider> _consumers;

            public InitialConsumerRegistrar(List<IInitialConsumerProvider> consumers)
            {
                _consumers = consumers;
            }

            public void RegisterConsumer(MqConsumer consumer)
            {
                var cp = new ObjectInitialConsumerProvider(consumer);
                _consumers.Add(cp);
            }

            public void RegisterConsumerByOptions<TOptions>(Func<TOptions, MqConsumer> consumerFactory)
                where TOptions : class, new()
            {
                var cp = new ByOptionConsumerFactoryProvider<TOptions>(consumerFactory);
                _consumers.Add(cp);
            }
        }
    }
}