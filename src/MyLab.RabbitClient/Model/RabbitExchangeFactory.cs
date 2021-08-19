using System;
using MyLab.RabbitClient.Connection;

namespace MyLab.RabbitClient.Model
{
    /// <summary>
    /// Creates Rabbit exchanges
    /// </summary>
    public class RabbitExchangeFactory
    {
        private readonly IRabbitChannelProvider _channelProvider;

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
        public RabbitExchangeType ExchangeType { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitExchangeFactory"/>
        /// </summary>
        public RabbitExchangeFactory(RabbitExchangeType exchangeType, IRabbitChannelProvider channelProvider)
        {
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
            ExchangeType = exchangeType;
        }

        /// <summary>
        /// Creates exchange with name = {Prefix}:{Guid.NewGuid().ToString("N")}
        /// </summary>
        public RabbitExchange CreateWithRandomId()
        {
            return CreateWithId(Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// Creates exchange with name = {Prefix}:{id}
        /// </summary>
        public RabbitExchange CreateWithId(string id)
        {
            return CreateWithName((Prefix ?? string.Empty) + id);
        }

        /// <summary>
        /// Creates exchange  with name = {name}
        /// </summary>
        public RabbitExchange CreateWithName(string name)
        {
            _channelProvider.Use(ch =>
                    ch.ExchangeDeclare(name, ExchangeType.ToLiteral(), Durable, AutoDelete, null)
                );

            return new RabbitExchange(name, _channelProvider);
        }
    }
}