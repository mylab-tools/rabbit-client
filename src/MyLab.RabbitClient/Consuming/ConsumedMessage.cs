using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Consuming
{
    /// <summary>
    /// Contains consumer message data
    /// </summary>
    public class ConsumedMessage<TContent>
    {
        private readonly BasicDeliverEventArgs _args;
        
        /// <summary>
        /// The queue which from message was received
        /// </summary>
        public string Queue { get; }

        /// <summary>
        /// The content header of the message
        /// </summary>
        public IBasicProperties BasicProperties { get; }

        /// <summary>
        /// Deserialized message content
        /// </summary>
        public TContent Content { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ConsumedMessage{TContent}"/>
        /// </summary>
        public ConsumedMessage(TContent content, BasicDeliverEventArgs args)
        {
            Content = content;
            _args = args;
            
            Queue = args.ConsumerTag;
            BasicProperties = args.BasicProperties;
        }
    }
}
