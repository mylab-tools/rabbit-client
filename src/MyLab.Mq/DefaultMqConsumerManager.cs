using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyLab.LogDsl;
using MyLab.StatusProvider;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq
{
    class DefaultMqConsumerManager : IDisposable
    {
        private readonly IAppStatusService _statusService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, MqConsumer> _consumers;
        private readonly DslLogger _logger;
        private readonly IModel _curChannel;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConsumerManager"/>
        /// </summary>
        public DefaultMqConsumerManager(
            IMqConnectionProvider connectionProvider, 
            IMqConsumerRegistry consumerRegistry,
            IAppStatusService statusService,
            IServiceProvider serviceProvider,
            ILogger<DefaultMqConsumerManager> logger)
        {
            if (connectionProvider == null) throw new ArgumentNullException(nameof(connectionProvider));
            if (consumerRegistry == null) throw new ArgumentNullException(nameof(consumerRegistry));
            _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger.Dsl();

            var cp = connectionProvider.Provide();
            _curChannel = cp.CreateModel();
            var systemConsumer = new AsyncEventingBasicConsumer(_curChannel);

            systemConsumer.Received += ConsumerReceivedAsync;

            _consumers = new Dictionary<string, MqConsumer>(consumerRegistry.GetConsumers());

            foreach (var logicConsumer in _consumers.Values)
            {
                _curChannel.BasicConsume(
                    logicConsumer.Queue, 
                    false, 
                    logicConsumer.Queue, 
                    true, 
                    false, 
                    null, 
                    systemConsumer);
            }
        }

        private async Task ConsumerReceivedAsync(object sender, BasicDeliverEventArgs args)
        {
            if (!_consumers.TryGetValue(args.ConsumerTag, out var consumer))
            {
                _logger.Error("Consumer not found")
                    .AndFactIs("Consumer tag", args.ConsumerTag)
                    .Write();

                return;
            }

            _statusService.IncomingMqMessageReceived(args.ConsumerTag);

            var ctx = new ConsumingContext(args.DeliveryTag, _serviceProvider, _curChannel, _statusService);

            await consumer.Consume(args.Body, ctx);
        }

        public void Dispose()
        {
            _curChannel?.Dispose();

            if (_consumers != null)
            {
                foreach (var mqConsumer in _consumers.Values)
                {
                    mqConsumer.Dispose();
                }
            }
        }
    }
}