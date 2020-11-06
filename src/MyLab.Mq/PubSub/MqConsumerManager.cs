using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.LogDsl;
using MyLab.Mq.Communication;
using MyLab.Mq.StatusProvider;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq.PubSub
{
    class MqConsumerManager : IHostedService, IDisposable
    {
        private readonly IMqConnectionProvider _connectionProvider;
        private readonly IMqConsumerRegistry _consumerRegistry;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMqStatusService _mqStatusService;
        private readonly DslLogger _logger;

        private IDictionary<string, MqConsumer> _consumers;
        private IModel _curChannel;

        public MqConsumerManager(
            IMqConnectionProvider connectionProvider, 
            IMqConsumerRegistry consumerRegistry,
            IServiceProvider serviceProvider,
            IMqStatusService mqStatusService,
            ILogger<MqConsumerManager> logger = null)
        {
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
            _consumerRegistry = consumerRegistry ?? throw new ArgumentNullException(nameof(consumerRegistry));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _mqStatusService = mqStatusService;
            _logger = logger?.Dsl();
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
                _mqStatusService.QueueConnected(logicConsumer.Queue);
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
                    .AndFactIs("consumer-tag", args.ConsumerTag)
                    .Write();

                return;
            }

            _mqStatusService.MessageReceived(args.ConsumerTag);

            using var scope = _serviceProvider.CreateScope();

            var msgAccessorCore = scope.ServiceProvider.GetService<IMqMessageAccessorCore>();
            msgAccessorCore.SetScopedMessage(args.Body, args.BasicProperties);

            var msgAccessor = scope.ServiceProvider.GetService<IMqMessageAccessor>();

            var ctx = new DefaultConsumingContext(
                args.ConsumerTag,
                args,
                scope.ServiceProvider,
                _curChannel,
                _mqStatusService,
                msgAccessor);

            try
            {
                await consumer.Consume(ctx);
            }
            catch (Exception e)
            {
                _logger.Error(e).Write();
            }
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