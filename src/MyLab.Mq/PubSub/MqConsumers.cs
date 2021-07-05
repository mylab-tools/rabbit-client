using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyLab.Mq.PubSub
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
        public ushort BatchSize { get; }

        /// <summary>
        /// Determines requeue behavior when message processing error
        /// </summary>
        public bool RequeueWhenError { get; set; } = false;
        
        /// <summary>
        /// Initializes a new instance of <see cref="MqConsumer"/>
        /// </summary>
        protected MqConsumer(string queue, ushort batchSize)
        {
            Queue = queue;
            BatchSize = batchSize;
        }

        /// <summary>
        /// Override to implement message consuming
        /// </summary>
        public abstract Task Consume(IConsumingContext consumingContext);


        /// <inheritdoc />
        public virtual void Dispose()
        {
        }
    }

    /// <summary>
    /// Describes simple MQ consumer
    /// </summary>
    /// <typeparam name="TMsgPayload">message payload type</typeparam>
    /// <typeparam name="TLogic">consuming logic type</typeparam>
    public class MqConsumer<TMsgPayload, TLogic> : MqConsumer
        where TLogic : class, IMqConsumerLogic<TMsgPayload>
    {
        private readonly TLogic _singletonLogic;

        /// <summary>
        /// Initializes a new instance of <see cref="MqConsumer{TMsg, TLogic}"/>
        /// </summary>
        public MqConsumer(string queue, TLogic singletonLogic = null)
            :base(queue, 1)
        {
            _singletonLogic = singletonLogic;
        }

        /// <inheritdoc />
        public override async Task Consume(IConsumingContext consumingContext)
        {
            try
            {
                var msgObj = consumingContext.GetMessage<TMsgPayload>();
                var logic = _singletonLogic ?? consumingContext.CreateLogic<TLogic>();

                await logic.Consume(msgObj);

                consumingContext.Ack();
            }
            catch (Exception e)
            {
                consumingContext.RejectOnError(e, RequeueWhenError);
                throw;
            }
        }
    }

    /// <summary>
    /// Describes MQ consumer which consumes a batch of messages
    /// </summary>
    /// <typeparam name="TMsgPayload">message payload type</typeparam>
    /// <typeparam name="TLogic">consuming logic type</typeparam>
    public class MqBatchConsumer<TMsgPayload, TLogic> : MqConsumer
        where TLogic : class, IMqBatchConsumerLogic<TMsgPayload>
    {
        private Task _monitorTask;
        private DateTime _lastMsgTime = DateTime.MinValue;
        private IConsumingContext _lastConsumingContext;

        private readonly TLogic _singletonLogic;
        readonly List<MqMessage<TMsgPayload>> _messages = new List<MqMessage<TMsgPayload>>();

        /// <summary>
        /// Determines time span after which an incomplete batch is processed
        /// </summary>
        /// <remarks>5 sec by default</remarks>
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Initializes a new instance of <see cref="MqBatchConsumer{TMsg, TLogic}"/>
        /// </summary>
        public MqBatchConsumer(string queue, ushort batchSize, TLogic singletonLogic = null)
            : base(queue, batchSize)
        {
            _singletonLogic = singletonLogic;
        }

        /// <inheritdoc />
        public override async Task Consume(IConsumingContext consumingContext)
        {
            _lastConsumingContext = consumingContext;
            _lastMsgTime = DateTime.Now;
            
            _messages.Add(consumingContext.GetMessage<TMsgPayload>());

            if (_messages.Count >= BatchSize)
            {
                await PerformConsumingAsync(consumingContext);
            }

            _monitorTask ??= Task.Run(Async);
        }

        private async Task PerformConsumingAsync(IConsumingContext consumingContext)
        {
            var msgCache = _messages.ToArray();

            try
            {
                var msgs = msgCache.ToArray();
                var logic = _singletonLogic ?? consumingContext.CreateLogic<TLogic>();

                await logic.Consume(msgs);

                consumingContext.Ack();
            }
            catch (Exception e)
            {
                consumingContext.RejectOnError(e, RequeueWhenError);
            }
            finally
            {
                _messages.RemoveAll(m => msgCache.Contains(m));
            }
        }

        private async Task Async()
        {
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                if (DateTime.Now  - _lastMsgTime >= BatchTimeout && _messages.Count != 0)
                {
                    await PerformConsumingAsync(_lastConsumingContext);
                }
            } while (true);
        }
    }
}
