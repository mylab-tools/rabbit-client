using System;
using MyLab.Mq.Communication;

namespace MyLab.Mq.MqObjects
{
    /// <summary>
    /// Creates queue
    /// </summary>
    public class MqQueueFactory
    {
        private readonly IMqConnectionProvider _connectionProvider;

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
        /// Initializes a new instance of <see cref="MqQueueFactory"/>
        /// </summary>
        public MqQueueFactory(IMqConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        }

        /// <summary>
        /// Create queue with name = {Pattern}:{Guid.NewGuid().ToString("N")}
        /// </summary>
        public MqQueue CreateWithRandomId()
        {
            return CreateWithName(Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Create queue with name = {Pattern}:{id}
        /// </summary>
        public MqQueue CreateWithId(string id)
        {
            return CreateWithName((Prefix ?? string.Empty) + id);
        }

        /// <summary>
        /// Create queue with name = {name}
        /// </summary>
        public MqQueue CreateWithName(string name)
        {
            using var channelProvider = new MqChannelProvider(_connectionProvider);

            channelProvider.Provide().QueueDeclare(name, Durable, Exclusive, AutoDelete, null);

            return new MqQueue(name, _connectionProvider);
        }
    }
}
