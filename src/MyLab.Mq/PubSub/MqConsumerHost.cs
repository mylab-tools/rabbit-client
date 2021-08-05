using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;
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
        private readonly IEnabledIndicatorService _enabledIndicatorService;
        private readonly IDslLogger _logger;
        
        private readonly IDictionary<string, MqConsumer> _runtimeConsumerRegister = new Dictionary<string, MqConsumer>();
        private readonly IDictionary<string, MqConsumer> _runConsumers = new Dictionary<string, MqConsumer>();

        private MqConsumerHostState _state = MqConsumerHostState.Stopped;
        private readonly ChannelCallbackExceptionLogger _channelCallbackExceptionLogger;
        private readonly ChannelMessageReceivingController _channelMessageReceivingController;

        public MqConsumerHost(IMqChannelProvider channelProvider,
            IMqInitialConsumerRegistry initialConsumerRegistry,
            IServiceProvider serviceProvider,
            IMqStatusService mqStatusService,
            IEnabledIndicatorService enabledIndicatorService = null,
            ILogger<MqConsumerHost> logger = null)
        {
            _logger = logger?.Dsl();
            var messageProcessor = new QueueMessageProcessor(mqStatusService, serviceProvider, _runConsumers)
            {
                Logger = _logger
            };
            _channelMessageReceivingController = new ChannelMessageReceivingController(messageProcessor);
            _channelCallbackExceptionLogger = new ChannelCallbackExceptionLogger(logger);
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
            _initialConsumerRegistry = initialConsumerRegistry ?? throw new ArgumentNullException(nameof(initialConsumerRegistry));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _mqStatusService = mqStatusService;
            _enabledIndicatorService = enabledIndicatorService;
        }

        public void Start()
        {
            if (_enabledIndicatorService != null && !_enabledIndicatorService.ShouldBeEnabled())
            {
                _logger
                    .Warning("Enabled indicator service indicate `false`. Consuming is not started.")
                    .Write();
                return;
            }

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

            _logger.Action("Consuming started").Write();
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

            _logger.Action("Consuming stopped").Write();
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

            _logger.Action("Consumer enabled")
                .AndFactIs("queue", consumer.Queue)
                .Write();
        }

        public void StopCore()
        {
            _state = MqConsumerHostState.StopRunning;

            try
            {
                _channelCallbackExceptionLogger.Clear();
                _channelMessageReceivingController.Clear();

                try
                {
                    _mqStatusService.AllQueueDisconnected();
                }
                catch (ObjectDisposedException)
                {
                    //Ignore it
                }

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

            _logger.Action("Consumer disabled")
                .AndFactIs("queue", queueName)
                .Write();
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
