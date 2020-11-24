using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MyLab.LogDsl;
using MyLab.Mq.Communication;
using MyLab.Mq.StatusProvider;

namespace MyLab.Mq.PubSub
{
    class MqConsumerHost : IMqConsumerHost, IMqConsumerRegistrar, IDisposable
    {
        private readonly IMqChannelProvider _channelProvider;
        private readonly IMqInitialConsumerRegistry _initialConsumerRegistry;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMqStatusService _mqStatusService;
        private readonly DslLogger _logger;
        
        private readonly IDictionary<string, MqConsumer> _runtimeConsumerRegister = new Dictionary<string, MqConsumer>();
        private readonly IDictionary<string, MqConsumer> _runConsumers = new Dictionary<string, MqConsumer>();

        private MqConsumerHostState _state = MqConsumerHostState.Stopped;
        private readonly ChannelCallbackExceptionLogger _channelCallbackExceptionLogger;
        private readonly ChannelMessageReceivingController _channelMessageReceivingController;

        public MqConsumerHost(IMqChannelProvider channelProvider,
            IMqInitialConsumerRegistry initialConsumerRegistry,
            IServiceProvider serviceProvider,
            IMqStatusService mqStatusService,
            ILogger<MqConsumerHost> logger = null)
        {

            var messageProcessor = new QueueMessageProcessor(mqStatusService, serviceProvider, _runConsumers);
            _channelMessageReceivingController = new ChannelMessageReceivingController(messageProcessor);
            _channelCallbackExceptionLogger = new ChannelCallbackExceptionLogger(logger);
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
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

            _runtimeConsumerRegister.Remove(queueName);
        }

        public void Dispose()
        {
            StopCore();
        }

        void StartConsumer(MqConsumer consumer)
        {
            var channel = _channelProvider.Provide(consumer.BatchSize);

            _channelCallbackExceptionLogger.Register(channel, consumer.Queue);
            _channelMessageReceivingController.Register(channel, consumer.Queue);

            _mqStatusService.QueueConnected(consumer.Queue);
            _runConsumers.Add(consumer.Queue, consumer);
        }

        public void StopCore()
        {
            _state = MqConsumerHostState.StopRunning;

            try
            {
                _channelCallbackExceptionLogger.Clear();
                _channelMessageReceivingController.Clear();
                _mqStatusService.AllQueueDisconnected();

                var runConsumers = _runConsumers.Values.ToArray();
                _runConsumers.Clear();

                foreach (var runConsumer in runConsumers)
                {
                    runConsumer.Dispose();
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
            _channelMessageReceivingController.Unregister(queueName);
            _channelCallbackExceptionLogger.Unregister(queueName);
            _mqStatusService.QueueDisconnected(queueName);

            if (_runConsumers.TryGetValue(queueName, out var runConsumer))
            {
                runConsumer.Dispose();
            }

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
