using System;
using System.Text;
using MyLab.RabbitClient.Connection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MyLab.RabbitClient.Model
{
    /// <summary>
    /// Provides abilities to work with Rabbit exchange
    /// </summary>
    public class RabbitExchange 
    {
        private readonly IRabbitChannelProvider _channelProvider;

        /// <summary>
        /// Exchange name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitExchange"/>
        /// </summary>
        public RabbitExchange(string name, IRabbitChannelProvider channelProvider)
        {
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
            Name = name;
        }

        /// <summary>
        /// Publish object as JSON message content
        /// </summary>
        public void Publish(object message, string routingKey = null)
        {
            string messageStr = JsonConvert.SerializeObject(message);
            var messageBin = Encoding.UTF8.GetBytes(messageStr);

            _channelProvider.Use(ch =>
                ch.BasicPublish(
                    exchange: Name,
                    routingKey: routingKey ?? "",
                    body: messageBin)
                );
        }

        /// <summary>
        /// Gets 'true' if queue exists 
        /// </summary>
        public bool IsExists()
        {
            try
            {
                _channelProvider.Use(ch => ch.ExchangeDeclarePassive(Name));
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyText.Contains("no exchange"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove exchange
        /// </summary>
        public void Remove()
        {
            _channelProvider.Use(ch => ch.ExchangeDeleteNoWait(Name));
        }
    }
}