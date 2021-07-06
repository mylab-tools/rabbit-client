using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;
using MyLab.Mq.StatusProvider;
using MyLab.Mq.Test;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Contains dependencies for consuming
    /// </summary>
    public interface IConsumingContext
    {
        /// <summary>
        /// Creates consuming logic
        /// </summary>
        T CreateLogic<T>();

        /// <summary>
        /// Gets delivered message
        /// </summary>
        MqMessage<T> GetMessage<T>();

        /// <summary>
        /// Acknowledgement delivery 
        /// </summary>
        void Ack();

        /// <summary>
        /// Reject delivery 
        /// </summary>
        void RejectOnError(Exception exception, bool requeue);

        /// <summary>
        /// Provides logger
        /// </summary>
        IDslLogger GetLogger<T>();
    }

    
    class DefaultConsumingContext : IConsumingContext
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
        /// Initializes a new instance of <see cref="DefaultConsumingContext"/>
        /// </summary>
        public DefaultConsumingContext(
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

        public IDslLogger GetLogger<T>()
        {
            return _serviceProvider
                .GetService<ILoggerFactory>()
                .CreateLogger(typeof(T))
                .Dsl();
        }
    }

    class FakeConsumingContext : IConsumingContext
    {
        private readonly object _messagePayload;
        private readonly IServiceProvider _serviceProvider;

        public FakeMessageQueueProcResult Result { get; private set; }

        public FakeConsumingContext(object messagePayload, IServiceProvider serviceProvider)
        {
            _messagePayload = messagePayload;
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public T CreateLogic<T>()
        {
            return (T)ActivatorUtilities.CreateInstance(
                _serviceProvider,
                typeof(T));
        }

        public MqMessage<T> GetMessage<T>()
        {
            return new MqMessage<T>((T)_messagePayload);
        }

        public void Ack()
        {
            Result = new FakeMessageQueueProcResult
            {
                Acked = true
            };
        }

        public void RejectOnError(Exception exception, bool requeue)
        {
            Result = new FakeMessageQueueProcResult
            {
                Rejected = true,
                RejectionException = exception,
                RequeueFlag = requeue
            };
        }

        public IDslLogger GetLogger<T>()
        {
            return _serviceProvider
                .GetService<ILoggerFactory>()
                .CreateLogger(typeof(T))
                .Dsl();
        }
    }
}