using System.Collections.Generic;
using System.Linq;

namespace MyLab.MqApp
{
    class ConsumerMap
    {
        readonly Dictionary<string, IMqConsumer> _consumers = new Dictionary<string, IMqConsumer>();

        public IMqConsumerRegistrar CreateRegistrar()
        {
            return new ConsumerRegistrar(_consumers);
        }

        public IDictionary<string, IMqConsumer> ToDictionary()
        {
            return new Dictionary<string, IMqConsumer>(_consumers);
        }

        class ConsumerRegistrar : IMqConsumerRegistrar
        {
            private readonly IDictionary<string, IMqConsumer> _consumers;

            public ConsumerRegistrar(IDictionary<string, IMqConsumer> consumers)
            {
                _consumers = consumers;
            }

            public void Register(string queueName, IMqConsumer consumer)
            {
                _consumers.Add(queueName, consumer);   
            }
        }
    }
}