using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.LogDsl;
using MyLab.Mq.Communication;
using MyLab.Mq.StatusProvider;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyLab.Mq.PubSub
{
    class MqConsumerHost : IMqConsumerHost, IMqConsumerRegistrar, IDisposable
    {
        private readonly IMqInitialConsumerRegistry _initialConsumerRegistry;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMqStatusService _mqStatusService;
        private readonly DslLogger _logger;
        
        private readonly IDictionary<string, MqConsumer> _runtimeConsumerRegister = new Dictionary<string, MqConsumer>();
        private readonly IDictionary<string, MqConsumer> _runConsumers = new Dictionary<string, MqConsumer>();

        private readonly Lazy<IModel> _channel;
        private AsyncEventingBasicConsumer _systemConsumer;
        private MqConsumerHostState _state = MqConsumerHostState.Stopped;

        public MqConsumerHost(IMqConnectionProvider connectionProvider,
            IMqInitialConsumerRegistry initialConsumerRegistry,
            IServiceProvider serviceProvider,
            IMqStatusService mqStatusService,
            ILogger<MqConsumerHost> logger = null)
        {
            if (connectionProvider == null) 
                throw new ArgumentNullException(nameof(connectionProvider));

            _channel = new Lazy<IModel>(() =>
            {
                var ch = connectionProvider.Provide().CreateModel();
                ch.CallbackException += ChannelExceptionReceived;

                _systemConsumer = new AsyncEventingBasicConsumer(ch);
                _systemConsumer.Received += ConsumerReceivedAsync;

                return ch;
            });

            _initialConsumerRegistry = initialConsumerRegistry ?? throw new ArgumentNullException(nameof(initialConsumerRegistry));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _mqStatusService = mqStatusService;
            _logger = logger?.Dsl();
        }

        public void Start()
        {
            if (_state != MqConsumerHostState.Stopped)
            {
                _logger
                    .Warning($"An attempt to start consumer host in unsuitable state was detected")
                    .AndFactIs("state", _state)
                    .Write();
            }

            _state = MqConsumerHostState.StartRunning;

            try
            {
                var initialConsumers = _initialConsumerRegistry.GetConsumers(_serviceProvider);
                foreach (var logicConsumer in initialConsumers)
                {
                    StartConsumer(logicConsumer);
                }

                foreach (var logicConsumer in _runtimeConsumerRegister.Values)
                {
                    StartConsumer(logicConsumer);
                }
            }
            catch (Exception e)
            {
                _state = MqConsumerHostState.Undefined;
                _logger.Error(e).Write();
            }

            _state = MqConsumerHostState.Running;
        }

        public void Stop()
        {
            if (_state != MqConsumerHostState.Stopped && _state != MqConsumerHostState.Undefined)
            {
                _logger
                    .Warning($"An attempt to stop consumer host in unsuitable state was detected")
                    .AndFactIs("state", _state)
                    .Write();
            }

            StopCore();
        }

        public IDisposable AddConsumer(MqConsumer consumer)
        {
            if(_runConsumers.ContainsKey(consumer.Queue))
                throw new InvalidOperationException("The consumer for the same queue already registered");

            _runtimeConsumerRegister.Add(consumer.Queue, consumer);

            if(_state == MqConsumerHostState.Running)
                StartConsumer(consumer);

            return new ConsumerUnregistrar(consumer.Queue, this);
        }

        public void RemoveConsumer(string queueName)
        {
            if (_runConsumers.ContainsKey(queueName))
                StopConsumer(queueName);

            _runConsumers.Remove(queueName);
            _runtimeConsumerRegister.Remove(queueName);
        }

        public void Dispose()
        {
            StopCore();
            _channel.Value?.Dispose();

            if (_runConsumers != null)
            {
                foreach (var mqConsumer in _runConsumers.Values)
                {
                    mqConsumer.Dispose();
                }
                _runConsumers.Clear();
            }
        }

        void StartConsumer(MqConsumer consumer)
        {
            _channel.Value.BasicConsume(
                queue: consumer.Queue,
                consumerTag: consumer.Queue,
                consumer: _systemConsumer);
            _mqStatusService.QueueConnected(consumer.Queue);

            _runConsumers.Add(consumer.Queue, consumer);
        }

        private void ChannelExceptionReceived(object sender, CallbackExceptionEventArgs e)
        {
            _logger
                .Error(e.Exception)
                .Write();
        }

        private async Task ConsumerReceivedAsync(object sender, BasicDeliverEventArgs args)
        {
            if (!_runConsumers.TryGetValue(args.ConsumerTag, out var consumer))
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
                _channel.Value,
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

        public void StopCore()
        {
            _state = MqConsumerHostState.StopRunning;

            try
            {
                foreach (var logicConsumer in _runConsumers.Values)
                {
                    StopConsumer(logicConsumer.Queue);
                }
            }
            catch (Exception e)
            {
                _state = MqConsumerHostState.Undefined;
                _logger.Error(e).Write();
            }

            _state = MqConsumerHostState.Stopped;
        }

        void StopConsumer(string queueName)
        {
            _channel.Value.BasicCancelNoWait(queueName);
            _mqStatusService.QueueDisconnected(queueName);

            _runConsumers.Remove(queueName);
        }

        class ConsumerUnregistrar : IDisposable
        {
            private readonly string _queueName;
            private readonly IMqConsumerRegistrar _registrar;

            public ConsumerUnregistrar(string queueName, IMqConsumerRegistrar registrar)
            {
                _queueName = queueName;
                _registrar = registrar;
            }

            public void Dispose()
            {
                _registrar.RemoveConsumer(_queueName);
            }
        }
    }
}
