using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.LogDsl;
using MyLab.StatusProvider;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq
{
    class DefaultMqConsumerManager : IHostedService, IDisposable
    {
        private readonly IMqConnectionProvider _connectionProvider;
        private readonly IMqConsumerRegistry _consumerRegistry;
        private readonly IAppStatusService _statusService;
        private readonly IServiceProvider _serviceProvider;
        private readonly DslLogger _logger;

        private IDictionary<string, MqConsumer> _consumers;
        private IModel _curChannel;

        public DefaultMqConsumerManager(
            IMqConnectionProvider connectionProvider, 
            IMqConsumerRegistry consumerRegistry,
            IAppStatusService statusService,
            IServiceProvider serviceProvider,
            ILogger<DefaultMqConsumerManager> logger)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _consumerRegistry = consumerRegistry ?? throw new ArgumentNullException(nameof(consumerRegistry));
            _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger.Dsl();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var cp = _connectionProvider.Provide();
            _curChannel = cp.CreateModel();
            _curChannel.CallbackException += ChannelExceptionReceived;

            var systemConsumer = new AsyncEventingBasicConsumer(_curChannel);

            systemConsumer.Received += ConsumerReceivedAsync;

            _consumers = new Dictionary<string, MqConsumer>(_consumerRegistry.GetConsumers());

            foreach (var logicConsumer in _consumers.Values)
            {
                _curChannel.BasicConsume(
                    queue: logicConsumer.Queue,
                    consumerTag: logicConsumer.Queue,
                    consumer: systemConsumer);
            }

            return Task.CompletedTask;
        }

        private void ChannelExceptionReceived(object sender, CallbackExceptionEventArgs e)
        {
            _logger
                .Error(e.Exception)
                .Write();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ClosesAll();

            return Task.CompletedTask;
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

            var ctx = new ConsumingContext(args, _serviceProvider, _curChannel, _statusService);

            await consumer.Consume(ctx);
        }

        public void Dispose()
        {
            ClosesAll();
        }

        void ClosesAll()
        {
            _curChannel?.Dispose();
            _curChannel = null;

            if (_consumers != null)
            {
                foreach (var mqConsumer in _consumers.Values)
                {
                    mqConsumer.Dispose();
                }
                _consumers.Clear();
                _consumers = null;
            }
        }
    }
}