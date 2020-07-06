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
        private readonly IServiceProvider _serviceProvider;
        private readonly IModel _channel;
        private readonly IMqStatusService _statusService;
        private readonly IMqMessageAccessor _mqMessageAccessor;

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
            IMqStatusService statusService,
            IMqMessageAccessor mqMessageAccessor)
        {
            DeliveryTag = args.DeliveryTag;
            _queue = queue;
            _serviceProvider = serviceProvider;
            _channel = channel;
            _statusService = statusService;
            _mqMessageAccessor = mqMessageAccessor;
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
            return _mqMessageAccessor.GetScopedMqMessage<T>();
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
        public void RejectOnError(Exception exception, bool requeue)
        {
            _channel.BasicNack(DeliveryTag, true, requeue);
            _statusService.ConsumingError(_queue, exception);
        }
    }
}