using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.StatusProvider;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    public abstract class MqConsumer
    {
        public string Queue { get; }
        public int BatchSize { get; }

        protected MqConsumer(string queue, int batchSize)
        {
            Queue = queue;
            BatchSize = batchSize;
        }

        public abstract Task Consume(byte[] messageBin, ConsumingContext consumingContext);
        
    }

    class MqConsumer<TMsg, TLogic> : MqConsumer
        where TLogic : IMqConsumerLogic<TMsg>
    {
        public MqConsumer(string queue)
            :base(queue, 1)
        {
        }

        public override async Task Consume(byte[] messageBin, ConsumingContext consumingContext)
        {
            try
            {
                var msgObj = JsonConvert.DeserializeObject<TMsg>(Encoding.UTF8.GetString(messageBin));
                await consumingContext.CreateLogic<TLogic>().Consume(msgObj);

                consumingContext.Ack();
            }
            catch (Exception e)
            {
                consumingContext.RejectOnError(e);
            }
        }
    }

    class MqBatchConsumer<TMsg, TLogic> : MqConsumer
        where TLogic : IMqBatchConsumerLogic<TMsg>
    {
        readonly List<byte[]> _messages = new List<byte[]>();

        public MqBatchConsumer(string queue, int batchSize)
            : base(queue, batchSize)
        {

        }

        public override async Task Consume(byte[] messageBin, ConsumingContext consumingContext)
        {
            _messages.Add(messageBin);

            if (_messages.Count >= BatchSize)
            {
                try
                {
                    var msgs = _messages
                        .Select(m => JsonConvert.DeserializeObject<TMsg>(Encoding.UTF8.GetString(m)))
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

    public class ConsumingContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IModel _channel;
        private readonly IAppStatusService _statusService;

        public ulong DeliveryTag { get; }

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

        public T CreateLogic<T>()
        {
            return (T)ActivatorUtilities.CreateInstance(
                _serviceProvider,
                typeof(T));
        }

        public void Ack()
        {
            _channel.BasicAck(DeliveryTag, true);
            _statusService.IncomingMqMessageProcessed();
        }

        public void RejectOnError(Exception exception)
        {
            _channel.BasicNack(DeliveryTag, true, true);
            _statusService.IncomingMqMessageError(exception);
        }
    }
}
