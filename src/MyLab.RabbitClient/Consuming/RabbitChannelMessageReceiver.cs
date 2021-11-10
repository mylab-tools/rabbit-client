using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.Log;
using MyLab.Log.Dsl;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.RabbitClient.Consuming
{
    class RabbitChannelMessageReceiver
    {
        private readonly IRabbitConsumerRegistry _consumerRegistry;
        private readonly IServiceProvider _serviceProvider;

        public IDslLogger Log { get; set; }

        public RabbitChannelMessageReceiver(
            IRabbitConsumerRegistry consumerRegistry,
            IServiceProvider serviceProvider)
        {
            _consumerRegistry = consumerRegistry;
            _serviceProvider = serviceProvider;
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
            var queue = args.ConsumerTag;
            
            List<IDisposable> cContexts = null;

            try
            {
                Log?.Debug("Message received")
                    .AndFactIs("consumer-tag", args.ConsumerTag)
                    .AndFactIs("delivery-tag", args.DeliveryTag)
                    .AndFactIs("RoutingKey", args.RoutingKey)
                    .AndFactIs("Exchange", args.Exchange)
                    .Write();

                if (!_consumerRegistry.TryGetValue(queue, out var consumerProvider))
                    throw new InvalidOperationException("Consumer not found");

                cContexts = SetLogContexts(args);

                using var scope = _serviceProvider.CreateScope();

                var consumer = consumerProvider.Provide(scope.ServiceProvider);
                await consumer.ConsumeAsync(args);

                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception e)
            {
                Log.Error(e)
                    .AndFactIs("queue", queue)
                    .AndLabel(LogLabels.ConsumingError)
                    .Write();

                channel.BasicNack(args.DeliveryTag, false, false);
            }
            finally
            {
                if (cContexts != null)
                {
                    ResetLogContexts(cContexts);
                }
            }
        }

        void ResetLogContexts(List<IDisposable> contexts)
        {
            foreach (var context in contexts)
            {
                try
                {
                    context.Dispose();
                }
                catch (Exception e)
                {
                    Log?.Error("Consuming context releasing error", e).Write();
                }
            }
        }

        private List<IDisposable> SetLogContexts(BasicDeliverEventArgs args)
        {
            var cContexts = new List<IDisposable>();

            var consumingContexts = _serviceProvider.GetServices<IConsumingContext>();
            if (consumingContexts != null)
            {
                var gotContexts = consumingContexts
                    .Select(c => c.Set(args))
                    .Where(c => c != null);

                cContexts.AddRange(gotContexts);
            }

            return cContexts;
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