using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    /// <summary>
    /// Contains MQ message data
    /// </summary>
    /// <typeparam name="T">payload type</typeparam>
    public class MqMessage<T>
    {
        /// <summary>
        /// Message identifier. <see cref="Guid.NewGuid"/> by default
        /// </summary>
        public Guid MessageId { get; set; } = Guid.NewGuid();
        /// <summary>
        /// Message correlated to this one
        /// </summary>
        public Guid CorrelationId { get; set; }
        /// <summary>
        /// Gets response publish parameters
        /// </summary>
        public string ReplyTo { get; set; }
        /// <summary>
        /// Headers
        /// </summary>
        public MqHeader[] Headers { get; set; }
        /// <summary>
        /// Message payload
        /// </summary>
        public T Payload { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqMessage{T}"/>
        /// </summary>
        public MqMessage(T payload)
        {
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }
    }

    /// <summary>
    /// Represent MQ message header
    /// </summary>
    public class MqHeader
    {
        /// <summary>
        /// Header name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Header value
        /// </summary>
        public string Value { get; set; }

        public static MqHeader Create(KeyValuePair<string, object> headerItem)
        {
            string strVal = null; 

            var val = headerItem.Value;

            if (val != null)
            {
                if(val is byte[] binVal)
                    strVal = Encoding.UTF8.GetString(binVal);
                else 
                    strVal = val.ToString();
            }

            return new MqHeader
            {
                Name = headerItem.Key,
                Value = strVal
            };
        }
    }

    /// <summary>
    /// Defines publish parameters
    /// </summary>
    public class PublishTarget
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        public string Exchange { get; set; }
        /// <summary>
        /// Queue name or routing key
        /// </summary>
        public string Routing { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="PublishTarget"/>
        /// </summary>
        public PublishTarget()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PublishTarget"/>
        /// </summary>
        public PublishTarget(PublicationAddress publicationAddress)
        {
            Exchange = publicationAddress.ExchangeName;
            Routing = publicationAddress.RoutingKey;
        }

        public PublicationAddress ToPubAddr()
        {
            return new PublicationAddress("undefined", Exchange, Routing);
        }
    }

    /// <summary>
    /// Contains outgoing message and publish parameters
    /// </summary>
    /// <typeparam name="T">Message payload type</typeparam>
    public class OutgoingMqEnvelop<T>
    {
        /// <summary>
        /// Gets publish parameters
        /// </summary>
        public PublishTarget PublishTarget { get; set; }
        /// <summary>
        /// Expiration time stamp
        /// </summary>
        public TimeSpan Expiration { get; set; }
        /// <summary>
        /// Outgoing message
        /// </summary>
        public MqMessage<T> Message { get; set; }
    }
}
