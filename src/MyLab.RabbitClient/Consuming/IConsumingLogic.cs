using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.Log.Dsl;
using RabbitMQ.Client.Events;

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

                strategy.Ack(args.DeliveryTag);
            }
            catch (Exception e)
            {
                Log?.Error(e)
                    .AndFactIs("queue", queue)
                    .AndLabel(LogLabels.ConsumingError)
                    .Write();

                strategy.Nack(args.DeliveryTag);

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
