using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyLab.RabbitClient.Consuming
{
    class ConsumerRegistrarSource : Collection<IRabbitConsumerRegistrar>
    {
        public ConsumerRegistrarSource()
        {
            
        }

        public ConsumerRegistrarSource(IList<IRabbitConsumerRegistrar> initial)
            :base(initial)
        {
            
        }
    }
}