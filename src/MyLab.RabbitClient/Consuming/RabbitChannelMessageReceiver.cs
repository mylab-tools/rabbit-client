using System;
using System.Threading.Tasks;
using MyLab.Log.Dsl;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Consuming
{
    class RabbitChannelMessageReceiver
    {
        private readonly ConsumingLogic _consumingLogic;

        public IDslLogger Log { get; set; }

        public RabbitChannelMessageReceiver(ConsumingLogic consumingLogic)
        {
            _consumingLogic = consumingLogic;
        }

        public IDisposable StartListen(string queue, IModel channel)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += ProcessReceivedAsync;

            channel.BasicConsume(queue: queue, consumerTag: queue, consumer: consumer);

            return new Disposer(channel, queue, consumer, ProcessReceivedAsync);
        }

        private async Task ProcessReceivedAsync(object sender, BasicDeliverEventArgs args)
        {
            if (!(sender is IAsyncBasicConsumer asyncBasicConsumer))
            {
                Log?.Error("Unexpected consumer type")
                    .AndFactIs("expected", typeof(IAsyncBasicConsumer).FullName)
                    .AndFactIs("actual", sender?.GetType().FullName ?? "[null]")
                    .Write();
                return;
            }

            var channel = asyncBasicConsumer.Model;

            await _consumingLogic.ConsumeMessageAsync(args, new DefaultConsumingLogicStrategy(channel));
        }

        class Disposer : IDisposable
        {
            private readonly IModel _channel;
            private readonly string _queue;
            private readonly AsyncEventingBasicConsumer _consumer;
            private readonly AsyncEventHandler<BasicDeliverEventArgs> _handler;

            public Disposer(IModel channel, string queue, AsyncEventingBasicConsumer consumer, AsyncEventHandler<BasicDeliverEventArgs> handler)
            {
                _channel = channel;
                _queue = queue;
                _consumer = consumer;
                _handler = handler;
            }

            public void Dispose()
            {
                _channel.BasicCancelNoWait(_queue);
                _consumer.Received -= _handler;
            }
        }
    }
}