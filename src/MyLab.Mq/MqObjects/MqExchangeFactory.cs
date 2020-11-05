using System;
using MyLab.Mq.Communication;

namespace MyLab.Mq.MqObjects
{
    /// <summary>
    /// Creates MQ exchanges
    /// </summary>
    public class MqExchangeFactory
    {
        private readonly IMqConnectionProvider _connectionProvider;

        /// <summary>
        /// Prefix for exchange name 
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
        /// Exchange type
        /// </summary>
        public MqExchangeType ExchangeType { get; }
        
        /// <summary>
        /// Initializes a new instance of <see cref="MqQueueFactory"/>
        /// </summary>
        public MqExchangeFactory(MqExchangeType exchangeType, IMqConnectionProvider connectionProvider)
        {
            ExchangeType = exchangeType;
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        }

        /// <summary>
        /// Creates exchange with name = {Pattern}:{Guid.NewGuid().ToString("N")}
        /// </summary>
        public MqExchange CreateWithRandomId()
        {
            return CreateWithName(Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Creates exchange with name = {Pattern}:{id}
        /// </summary>
        public MqExchange CreateWithId(string id)
        {
            return CreateWithName((Prefix ?? string.Empty) + id);
        }

        /// <summary>
        /// Creates exchange  with name = {name}
        /// </summary>
        public MqExchange CreateWithName(string name)
        {
            using var channelProvider = new MqChannelProvider(_connectionProvider);

            var channel = channelProvider.Provide();

            channel.ExchangeDeclare(name, ExchangeType.ToLiteral(), Durable, AutoDelete, null);

            return new MqExchange(name, _connectionProvider);
        }
    }
}
