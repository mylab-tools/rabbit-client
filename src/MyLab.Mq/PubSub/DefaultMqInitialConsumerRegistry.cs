using System.Collections.Generic;

namespace MyLab.Mq.PubSub
{
    class DefaultMqInitialConsumerRegistry : IMqInitialConsumerRegistry
    {
        readonly Dictionary<string, MqConsumer> _consumers = new Dictionary<string, MqConsumer>();

        public IMqInitialConsumerRegistrar CreateRegistrar()
        {
            return new InitialConsumerRegistrar(_consumers);
        }

        public IReadOnlyDictionary<string, MqConsumer> GetConsumers()
        {
            return _consumers;
        }

        class InitialConsumerRegistrar : IMqInitialConsumerRegistrar
        {
            private readonly IDictionary<string, MqConsumer> _consumers;

            public InitialConsumerRegistrar(IDictionary<string, MqConsumer> consumers)
            {
                _consumers = consumers;
            }

            public void RegisterConsumer(MqConsumer consumer)
            {
                if (!_consumers.ContainsKey(consumer.Queue))
                {
                    _consumers.Add(consumer.Queue, consumer);
                }
                else
                {
                    _consumers[consumer.Queue] = consumer;
                }
            }
        }
    }
}