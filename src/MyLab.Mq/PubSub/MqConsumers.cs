using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLab.Log;
using MyLab.Log.Dsl;
using Newtonsoft.Json;

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
                MqMessage<TMsgPayload> msgObj;
                try
                {
                    msgObj = consumingContext.GetMessage<TMsgPayload>();
                }
                catch (JsonMessageSerializationException e)
                {
                    e.AndFactIs("dump", e.Content);
                    throw;
                }

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
        //private IConsumingContext _lastConsumingContext;
        private IDslLogger _lastLogger;

        private readonly TLogic _singletonLogic;
        private TLogic _lastLogic;
        readonly List<MqMessage<TMsgPayload>> _messages = new List<MqMessage<TMsgPayload>>();
        private IConsumingContext _lastConsumingContext;


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
            _lastMsgTime = DateTime.Now;

            var consumedMessage = consumingContext.GetMessage<TMsgPayload>();

            var logger = consumingContext.GetLogger<MqBatchConsumer<TMsgPayload, TLogic>>();
            _lastLogger = logger;
            _lastLogic = _singletonLogic ?? consumingContext.CreateLogic<TLogic>();
            _lastConsumingContext = consumingContext;

            logger.Debug("Input mq message")
                .AndFactIs("queue", Queue)
                .AndFactIs("msg-id", consumedMessage.MessageId)
                .AndLabel("batch-consumer")
                .Write();

            _messages.Add(consumedMessage);

            if (_messages.Count >= BatchSize)
            {
                logger.Debug("Perform consuming")
                    .AndFactIs("queue", Queue)
                    .AndFactIs("msg-count", _messages.Count)
                    .AndLabel("sync-mq-batch-processing")
                    .Write();

                await PerformConsumingAsync();
            }

            if (_monitorTask != null)
            {
                ExceptionDto exDto = null;

                if (_monitorTask.Exception != null)
                    exDto = ExceptionDto.Create(_monitorTask.Exception);

                logger.Debug("Monitor task state")
                    .AndFactIs("Status", _monitorTask.Status)
                    .AndFactIs("IsCompleted", _monitorTask.IsCompleted)
                    .AndFactIs("IsCompletedSuccessfully", _monitorTask.IsCompletedSuccessfully)
                    .AndFactIs("IsFaulted", _monitorTask.IsFaulted)
                    .AndFactIs("IsCanceled", _monitorTask.IsCanceled)
                    .AndFactIs("Exception", exDto ?? (object)"no-exception")
                    .Write();
            }

            _monitorTask ??= Task.Run(Async);

            //try
            //{
            //    logger.Debug("Monitor task state")
            //        .AndFactIs("task", _monitorTask)
            //        .Write();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //    throw;
            //}
        }

        private async Task PerformConsumingAsync()
        {
            var msgCache = _messages.ToArray();

            try
            {
                var msgs = msgCache.ToArray();

                await _lastLogic.Consume(msgs);

                _lastConsumingContext.Ack();
            }
            catch (Exception e)
            {
                _lastConsumingContext.RejectOnError(e, RequeueWhenError);
            }
            finally
            {
                _messages.RemoveAll(m => msgCache.Contains(m));
            }
        }

        private async Task Async()
        {
            _lastLogger.Debug("Start monitor task").Write();

            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                var lastMsgTimeDelta = DateTime.Now - _lastMsgTime;

                if (lastMsgTimeDelta >= BatchTimeout && _messages.Count != 0)
                {
                    ApplyContext(
                            _lastLogger.Debug("Hit mq batch processing"),
                            lastMsgTimeDelta
                        )
                        .Write();

                    try
                    {
                        await PerformConsumingAsync();
                    }
                    catch (Exception e)
                    {
                        ApplyContext(
                                _lastLogger.Error("Mq batch processing error", e),
                                lastMsgTimeDelta
                            )
                            .Write();
                    }
                }
                else
                {
                    ApplyContext(
                            _lastLogger.Debug("Pass mq batch processing"),
                            lastMsgTimeDelta
                        )
                        .Write();
                }

            } while (true);

            DslExpression ApplyContext(DslExpression dslExpression, TimeSpan lastMsgTimeDelta)
            {
                return dslExpression
                    .AndLabel("async-mq-batch-processing")
                    .AndFactIs("last-msg-time", _lastMsgTime.ToString("T"))
                    .AndFactIs("last-msg-delta", lastMsgTimeDelta.ToString("T"))
                    .AndFactIs("queue", Queue)
                    .AndFactIs("msg-count", _messages.Count);
            }
        }
    }
}
