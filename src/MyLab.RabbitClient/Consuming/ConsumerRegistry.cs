using System;
using System.Collections.Generic;
using System.Linq;
using MyLab.Log;

namespace MyLab.RabbitClient.Consuming
{
    class ConsumerRegistry : Dictionary<string, IRabbitConsumerProvider>, IRabbitConsumerRegistry
    {
        public void Register(string queue, IRabbitConsumerProvider rabbitConsumerProvider)
        {
            if(ContainsKey(queue))
                throw new InvalidOperationException("Duplicate queue consumer")
                    .AndFactIs("queue", queue);

            Add(queue, rabbitConsumerProvider);
        }
    }
}