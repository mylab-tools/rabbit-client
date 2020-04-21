using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.StatusProvider;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    /// <summary>
    /// The base for MQ consumer
    /// </summary>
    public abstract class MqConsumer : IDisposable
    {
        /// <summary>
        /// Source queue name
        /// </summary>
        public string Queue { get; }
        /// <summary>
        /// Determines number of retrieved messages to process
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqConsumer"/>
        /// </summary>
        protected MqConsumer(string queue, int batchSize)
        {
            Queue = queue;
            BatchSize = batchSize;
        }

        /// <summary>
        /// Override to implement message consuming
        /// </summary>
        public abstract Task Consume(ReadOnlyMemory<byte> messageBin, ConsumingContext consumingContext);


        /// <inheritdoc />
        public virtual void Dispose()
        {
        }
    }

    /// <summary>
    /// Describes simple MQ consumer
    /// </summary>
    /// <typeparam name="TMsg">message type</typeparam>
    /// <typeparam name="TLogic">consuming logic type</typeparam>
    public class MqConsumer<TMsg, TLogic> : MqConsumer
        where TLogic : IMqConsumerLogic<TMsg>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MqConsumer{TMsg, TLogic}"/>
        /// </summary>
        public MqConsumer(string queue)
            :base(queue, 1)
        {
        }


        /// <inheritdoc />
        public override async Task Consume(ReadOnlyMemory<byte> messageBin, ConsumingContext consumingContext)
        {
            try
            {
                var msgObj = JsonConvert.DeserializeObject<TMsg>(Encoding.UTF8.GetString(messageBin.ToArray()));
                await consumingContext.CreateLogic<TLogic>().Consume(msgObj);

                consumingContext.Ack();
            }
            catch (Exception e)
            {
                consumingContext.RejectOnError(e);
            }
        }
    }

    /// <summary>
    /// Describes MQ consumer which consumes a batch of messages
    /// </summary>
    /// <typeparam name="TMsg">message type</typeparam>
    /// <typeparam name="TLogic">consuming logic type</typeparam>
    public class MqBatchConsumer<TMsg, TLogic> : MqConsumer
        where TLogic : IMqBatchConsumerLogic<TMsg>
    {
        readonly List<ReadOnlyMemory<byte>> _messages = new List<ReadOnlyMemory<byte>>();

        /// <summary>
        /// Initializes a new instance of <see cref="MqBatchConsumer{TMsg, TLogic}"/>
        /// </summary>
        public MqBatchConsumer(string queue, int batchSize)
            : base(queue, batchSize)
        {

        }

        /// <inheritdoc />
        public override async Task Consume(ReadOnlyMemory<byte> messageBin, ConsumingContext consumingContext)
        {
            _messages.Add(messageBin);

            if (_messages.Count >= BatchSize)
            {
                try
                {
                    var msgs = _messages
                        .Select(m => JsonConvert.DeserializeObject<TMsg>(Encoding.UTF8.GetString(m.ToArray())))
                        .ToArray();
                    await consumingContext.CreateLogic<TLogic>().Consume(msgs);

                    consumingContext.Ack();
                }
                catch (Exception e)
                {
                    consumingContext.RejectOnError(e);
                }
                finally
                {
                    _messages.Clear();
                }
            }
        }
    }

    /// <summary>
    /// Contains dependencies for consuming
    /// </summary>
    public class ConsumingContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IModel _channel;
        private readonly IAppStatusService _statusService;

        /// <summary>
        /// Gets delivery identifier
        /// </summary>
        public ulong DeliveryTag { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ConsumingContext"/>
        /// </summary>
        public ConsumingContext(
            ulong deliveryTag,
            IServiceProvider serviceProvider,
            IModel channel,
            IAppStatusService statusService)
        {
            DeliveryTag = deliveryTag;
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
        /// Acknowledgement delivery 
        /// </summary>
        public void Ack()
        {
            _channel.BasicAck(DeliveryTag, true);
            _statusService.IncomingMqMessageProcessed();
        }

        /// <summary>
        /// Reject delivery 
        /// </summary>
        public void RejectOnError(Exception exception)
        {
            _channel.BasicNack(DeliveryTag, true, true);
            _statusService.IncomingMqMessageError(exception);
        }
    }
}
