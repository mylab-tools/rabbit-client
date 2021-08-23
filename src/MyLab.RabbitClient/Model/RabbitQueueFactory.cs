using System;
using System.Collections.Generic;
using MyLab.RabbitClient.Connection;

namespace MyLab.RabbitClient.Model
{
    /// <summary>
    /// Creates queue
    /// </summary>
    public class RabbitQueueFactory
    {
        private readonly IRabbitChannelProvider _channelProvider;

        /// <summary>
        /// Prefix for queue name 
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Queue that has had at least one consumer is deleted when last consumer unsubscribes
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// The queue will survive a broker restart
        /// </summary>
        public bool Durable { get; set; } = false;

        /// <summary>
        /// Used by only one connection and the queue will be deleted when that connection closes
        /// </summary>
        public bool Exclusive { get; set; } = false;

        /// <summary>
        /// Specifies a dead letter exchange
        /// </summary>
        public string DeadLetterExchange { get; set; }

        /// <summary>
        /// Specifies a dead letter routing key
        /// </summary>
        public string DeadLetterRoutingKey { get; set; }

        /// <summary>
        /// Create queue with name = {Prefix}:{Guid.NewGuid().ToString("N")}
        /// </summary>
        public RabbitQueue CreateWithRandomId()
        {
            return CreateWithId(Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Create queue with name = {Prefix}:{id}
        /// </summary>
        public RabbitQueue CreateWithId(string id)
        {
            return CreateWithName((Prefix ?? string.Empty) + id);
        }

        /// <summary>
        /// Create queue with name = {name}
        /// </summary>
        public RabbitQueue CreateWithName(string name)
        {
            var args = new Dictionary<string, object>();

            if (DeadLetterExchange != null)
            {
                args.Add("x-dead-letter-exchange", DeadLetterExchange);
                if (DeadLetterRoutingKey != null)
                    args.Add("x-dead-letter-routing-key", DeadLetterRoutingKey);
            }
            
            _channelProvider.Use(ch => ch.QueueDeclare(name, Durable, Exclusive, AutoDelete, args));

            return new RabbitQueue(name, _channelProvider);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitQueueFactory"/>
        /// </summary>
        public RabbitQueueFactory(IRabbitChannelProvider channelProvider)
        {
            _channelProvider = channelProvider;
        }
    }
}
