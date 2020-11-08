using System;
using System.Collections.Generic;

namespace MyLab.Mq.PubSub
{
    class DefaultMqInitialConsumerRegistry : IMqInitialConsumerRegistry
    {
        readonly Dictionary<string, IInitialConsumerProvider> _consumers = new Dictionary<string, IInitialConsumerProvider>();

        public IMqInitialConsumerRegistrar CreateRegistrar()
        {
            return new InitialConsumerRegistrar(_consumers);
        }

        public IReadOnlyDictionary<string, IInitialConsumerProvider> GetConsumers()
        {
            return _consumers;
        }

        class InitialConsumerRegistrar : IMqInitialConsumerRegistrar
        {
            private readonly IDictionary<string, IInitialConsumerProvider> _consumers;

            public InitialConsumerRegistrar(IDictionary<string, IInitialConsumerProvider> consumers)
            {
                _consumers = consumers;
            }

            public void RegisterConsumer(MqConsumer consumer)
            {
                var cp = new ObjectInitialConsumerProvider(consumer);
                if (!_consumers.ContainsKey(consumer.Queue))
                    _consumers.Add(consumer.Queue, cp);
                else
                    _consumers[consumer.Queue] = cp;
            }

            public void RegisterConsumerByOptions<TOptions>(string queueName, Func<TOptions, MqConsumer> consumerFactory)
                where TOptions : class, new()
            {
                var cp = new ByOptionConsumerFactoryProvider<TOptions>(consumerFactory);
                if (!_consumers.ContainsKey(queueName))
                    _consumers.Add(queueName, cp);
                else
                    _consumers[queueName] = cp;
            }
        }
    }
}