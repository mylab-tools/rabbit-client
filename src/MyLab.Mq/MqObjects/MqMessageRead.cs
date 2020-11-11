using System;
using RabbitMQ.Client;

namespace MyLab.Mq.MqObjects
{
    /// <summary>
    /// Read message
    /// </summary>
    /// <typeparam name="T">payload type</typeparam>
    public class MqMessageRead<T>
    {
        private readonly IModel _model;
        private readonly ulong _deliveryTag;

        /// <summary>
        /// Message
        /// </summary>
        public MqMessage<T> Message { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqMessageRead{T}"/>
        /// </summary>
        public MqMessageRead(IModel model, ulong deliveryTag, MqMessage<T> message)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _deliveryTag = deliveryTag;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// Ack message
        /// </summary>
        public void Ack()
        {
            _model.BasicAck(_deliveryTag, true);
        }

        /// <summary>
        /// Nack message
        /// </summary>
        public void Nack()
        {
            _model.BasicNack(_deliveryTag, true, false);
        }
    }
}