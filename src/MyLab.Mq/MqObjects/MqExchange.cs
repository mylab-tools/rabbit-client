using System;
using System.Text;
using MyLab.Mq.Communication;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MyLab.Mq.MqObjects
{
    /// <summary>
    /// Provides abilities to work with MQ exchange
    /// </summary>
    public class MqExchange : IDisposable
    {
        private readonly IMqChannelProvider _channelProvider;

        /// <summary>
        /// Exchange name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqExchange"/>
        /// </summary>
        public MqExchange(string name, IMqChannelProvider channelProvider)
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

            _channelProvider.Provide().BasicPublish(
                exchange: Name,
                routingKey: routingKey ?? "",
                body: messageBin);
        }

        /// <summary>
        /// Gets 'true' if queue exists 
        /// </summary>
        public bool IsExists()
        {
            try
            {
                _channelProvider.Provide().ExchangeDeclarePassive(Name);
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
        public void Dispose()
        {
            _channelProvider.Provide().ExchangeDeleteNoWait(Name);
        }
    }
}