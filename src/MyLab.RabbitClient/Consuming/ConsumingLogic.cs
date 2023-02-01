using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.Log.Dsl;
using RabbitMQ.Client.Events;
using YamlDotNet.Serialization.NodeDeserializers;

namespace MyLab.RabbitClient.Consuming
{

    class ConsumingLogic 
    {
        private readonly IRabbitConsumerRegistry _consumerRegistry;
        private readonly IServiceProvider _serviceProvider;

        public IDslLogger Log { get; set; }

        public ConsumingLogic(IRabbitConsumerRegistry consumerRegistry, IServiceProvider serviceProvider)
        {
            _consumerRegistry = consumerRegistry;
            _serviceProvider = serviceProvider;
        }

        public async Task ConsumeMessageAsync(BasicDeliverEventArgs args, IConsumingLogicStrategy strategy)
        {
            var queue = args.ConsumerTag;

            List<IConsumingContextInstance> cContexts = null;

            try
            {
                Log?.Debug("Message received")
                    .AndFactIs("delivery-args", args)
                    .Write();

                if (!_consumerRegistry.TryGetValue(queue, out var consumerProvider))
                    throw new InvalidOperationException("Consumer not found");

                cContexts = SetLogContexts(args);
                
                using var scope = _serviceProvider.CreateScope();

                var consumer = consumerProvider.Provide(scope.ServiceProvider);
                await consumer.ConsumeAsync(args);

                if (consumer is IDisposable disposableConsumer)
                {
                    disposableConsumer.Dispose();
                }
                if (consumer is IAsyncDisposable asyncDisposableConsumer)
                {
                    await asyncDisposableConsumer.DisposeAsync();
                }

                strategy.Ack(args.DeliveryTag);

                Log?.Debug("Ack")
                    .AndFactIs("delivery-tag", args.DeliveryTag)
                    .Write();
            }
            catch (Exception e)
            {
                Log?.Error(e)
                    .AndFactIs("queue", queue)
                    .AndLabel(LogLabels.ConsumingError)
                    .Write();

                strategy.Nack(args.DeliveryTag);

                Log?.Debug("Nack")
                    .AndFactIs("delivery-tag", args.DeliveryTag)
                    .Write();

                if (cContexts != null)
                {
                    NotifyContextError(cContexts, e);
                }
            }
            finally
            {
                if (cContexts != null)
                {
                    ResetLogContexts(cContexts);
                }
            }
        }

        private List<IConsumingContextInstance> SetLogContexts(BasicDeliverEventArgs args)
        {
            var cContexts = new List<IConsumingContextInstance>();

            var consumingContexts = _serviceProvider.GetServices<IConsumingContext>();

            var gotContexts = consumingContexts
                .Select(c => c.Set(args))
                .Where(c => c != null);

            cContexts.AddRange(gotContexts);

            return cContexts;
        }

        void NotifyContextError(List<IConsumingContextInstance> contexts, Exception unhandledException)
        {
            foreach (var context in contexts)
            {
                try
                {
                    context.NotifyUnhandledException(unhandledException);
                }
                catch (Exception e)
                {
                    Log?.Error("Consuming context error notification exception", e).Write();
                }
            }
        }

        void ResetLogContexts(List<IConsumingContextInstance> contexts)
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
    }
}
