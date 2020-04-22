using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        public abstract Task Consume(ConsumingContext consumingContext);


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
        where TLogic : IMqConsumerLogic<TMsgPayload>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MqConsumer{TMsg, TLogic}"/>
        /// </summary>
        public MqConsumer(string queue)
            :base(queue, 1)
        {
        }


        /// <inheritdoc />
        public override async Task Consume(ConsumingContext consumingContext)
        {
            try
            {
                var msgObj = consumingContext.GetMessage<TMsgPayload>();
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
    /// <typeparam name="TMsgPayload">message payload type</typeparam>
    /// <typeparam name="TLogic">consuming logic type</typeparam>
    public class MqBatchConsumer<TMsgPayload, TLogic> : MqConsumer
        where TLogic : IMqBatchConsumerLogic<TMsgPayload>
    {
        readonly List<MqMessage<TMsgPayload>> _messages = new List<MqMessage<TMsgPayload>>();

        /// <summary>
        /// Initializes a new instance of <see cref="MqBatchConsumer{TMsg, TLogic}"/>
        /// </summary>
        public MqBatchConsumer(string queue, int batchSize)
            : base(queue, batchSize)
        {

        }

        /// <inheritdoc />
        public override async Task Consume(ConsumingContext consumingContext)
        {
            _messages.Add(consumingContext.GetMessage<TMsgPayload>());

            if (_messages.Count >= BatchSize)
            {
                var msgCache = _messages.ToArray();

                try
                {
                    var msgs = msgCache.ToArray();
                    await consumingContext.CreateLogic<TLogic>().Consume(msgs);

                    consumingContext.Ack();
                }
                catch (Exception e)
                {
                    consumingContext.RejectOnError(e);
                }
                finally
                {
                    _messages.RemoveAll(m => msgCache.Contains(m));
                }
            }
        }
    }
}
