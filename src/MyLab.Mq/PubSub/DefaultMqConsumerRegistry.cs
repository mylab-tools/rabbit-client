using System.Collections.Generic;

namespace MyLab.Mq.PubSub
{
    class DefaultMqConsumerRegistry : IMqConsumerRegistry
    {
        readonly Dictionary<string, MqConsumer> _consumers = new Dictionary<string, MqConsumer>();

        public IMqConsumerRegistrar CreateRegistrar()
        {
            return new ConsumerRegistrar(_consumers);
        }

        public IReadOnlyDictionary<string, MqConsumer> GetConsumers()
        {
            return _consumers;
        }

        class ConsumerRegistrar : IMqConsumerRegistrar
        {
            private readonly IDictionary<string, MqConsumer> _consumers;

            public ConsumerRegistrar(IDictionary<string, MqConsumer> consumers)
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