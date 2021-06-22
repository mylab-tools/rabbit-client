using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.Log.Dsl;
using MyLab.Mq.StatusProvider;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq.PubSub
{
    class QueueMessageProcessor
    {
        private readonly IMqStatusService _statusService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, MqConsumer> _consumerTagToConsumerMap;

        public IDslLogger Logger { get; set; }

        public QueueMessageProcessor(
            IMqStatusService statusService, 
            IServiceProvider serviceProvider,
            IDictionary<string, MqConsumer> consumerTagToConsumerMap)
        {
            _statusService = statusService;
            _serviceProvider = serviceProvider;
            _consumerTagToConsumerMap = consumerTagToConsumerMap;
        }

        public async Task ConsumerReceivedAsync(object sender, BasicDeliverEventArgs args)
        {
            if (!_consumerTagToConsumerMap.TryGetValue(args.ConsumerTag, out var consumer))
            {
                Logger?.Error("Consumer not found")
                    .AndFactIs("consumer-tag", args.ConsumerTag)
                    .Write();

                return;
            }

            _statusService.MessageReceived(args.ConsumerTag);

            using var scope = _serviceProvider.CreateScope();

            var msgAccessorCore = scope.ServiceProvider.GetService<IMqMessageAccessorCore>();
            msgAccessorCore.SetScopedMessage(args.Body, args.BasicProperties);

            var msgAccessor = scope.ServiceProvider.GetService<IMqMessageAccessor>();

            if (!(sender is IAsyncBasicConsumer asyncBasicConsumer))
            {
                throw new InvalidOperationException($"Expected '{typeof(IAsyncBasicConsumer).FullName}' but actual '{sender?.GetType().FullName}'");
            }

            var ctx = new DefaultConsumingContext(
                args.ConsumerTag,
                args,
                scope.ServiceProvider,
                asyncBasicConsumer.Model,
                _statusService,
                msgAccessor);

            try
            {
                await consumer.Consume(ctx);
            }
            catch (Exception e)
            {
                Logger?.Error(e).Write();
            }
        }
    }
}