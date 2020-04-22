using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MyLab.StatusProvider;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq
{
    /// <summary>
    /// Contains dependencies for consuming
    /// </summary>
    public class ConsumingContext
    {
        private readonly string _queue;
        private readonly BasicDeliverEventArgs _args;
        private readonly IServiceProvider _serviceProvider;
        private readonly IModel _channel;
        private readonly IMqStatusService _statusService;

        /// <summary>
        /// Gets delivery identifier
        /// </summary>
        public ulong DeliveryTag { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ConsumingContext"/>
        /// </summary>
        public ConsumingContext(
            string queue,
            BasicDeliverEventArgs args,
            IServiceProvider serviceProvider,
            IModel channel,
            IMqStatusService statusService)
        {
            DeliveryTag = args.DeliveryTag;
            _queue = queue;
            _args = args;
            _serviceProvider = serviceProvider;
            _channel = channel;
            _statusService = statusService;
        }

        /// <summary>
        /// Creates consuming logic
        /// </summary>
        public T CreateLogic<T>()
        {
            return (T)ActivatorUtilities.CreateInstance(
                _serviceProvider,
                typeof(T));
        }

        /// <summary>
        /// Gets delivered message
        /// </summary>
        public MqMessage<T> GetMessage<T>()
        {
            var bodyStr = Encoding.UTF8.GetString(_args.Body.ToArray());
            var payload = JsonConvert.DeserializeObject<T>(bodyStr);

            var props = _args.BasicProperties;
            var msg= new MqMessage<T>(payload)
            {
                ReplyTo = props.ReplyTo
            };

            if(Guid.TryParse(props.CorrelationId, out var correlationId))
                msg.CorrelationId = correlationId;
            if (Guid.TryParse(props.MessageId, out var messageId))
                msg.MessageId = messageId;
            if (props.Headers != null)
            {
                msg.Headers = props.Headers
                    .Select(h => new MqHeader
                    {
                        Name = h.Key,
                        Value = h.Value.ToString()
                    })
                    .ToArray();
            }

            return msg;
        }

        /// <summary>
        /// Acknowledgement delivery 
        /// </summary>
        public void Ack()
        {
            _channel.BasicAck(DeliveryTag, true);
            _statusService.MessageProcessed(_queue);
        }

        /// <summary>
        /// Reject delivery 
        /// </summary>
        public void RejectOnError(Exception exception)
        {
            _channel.BasicNack(DeliveryTag, true, true);
            _statusService.ConsumingError(_queue, exception);
        }
    }
}