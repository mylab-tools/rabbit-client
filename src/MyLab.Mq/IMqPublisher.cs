using System;

namespace MyLab.Mq
{
    /// <summary>
    /// Defines a Message Queue publisher
    /// </summary>
    public interface IMqPublisher 
    {
        /// <summary>
        /// Publish message into queue
        /// </summary>
        void Publish<T>(OutgoingMqEnvelop<T> envelop) where T : class;
    }

    /// <summary>
    /// Extension methods for <see cref="IMqPublisher"/>
    /// </summary>
    public static class MqPublisherExtensions
    {
        public static void Publish<T>(this IMqPublisher publisher, T msg) 
            where T : class
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (msg == null) throw new ArgumentNullException(nameof(msg));

            publisher.Publish(new OutgoingMqEnvelop<T>
            {
                Message = new MqMessage<T>(msg)
            });
        }

        public static void PublishToQueue<T>(this IMqPublisher publisher, T msg, string queueName) 
            where T : class
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (msg == null) throw new ArgumentNullException(nameof(msg));

            publisher.Publish(new OutgoingMqEnvelop<T>
            {
                PublishTarget = new PublishTarget { Routing = queueName },
                Message = new MqMessage<T>(msg)
            });
        }

        public static void PublishToExchange<T>(this IMqPublisher publisher, T msg, string exchange, string routingKey = null) where T : class
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (msg == null) throw new ArgumentNullException(nameof(msg));

            publisher.Publish(new OutgoingMqEnvelop<T>
            {
                PublishTarget = new PublishTarget
                {
                    Routing = routingKey,
                    Exchange = exchange
                },
                Message = new MqMessage<T>(msg)
            });
        }
    }
}