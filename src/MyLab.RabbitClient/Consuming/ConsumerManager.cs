
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Connection;

namespace MyLab.RabbitClient.Consuming
{
    class ConsumerManager : IConsumerManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitConnectionProvider _connectionProvider;
        private readonly ConsumerRegistrarSource _consumerRegistrarSource;
        private readonly IDslLogger _log;

        private readonly RabbitChannelExceptionReceiver _exceptionReceiver;
        private readonly RabbitChannelMessageReceiver _messageReceiver;

        private readonly ConsumerRegistry _consumerRegistry = new ConsumerRegistry();

        private readonly IList<IDisposable> _listenDisposers = new List<IDisposable>();

        public ConsumerManager(
            IServiceProvider serviceProvider,
            IRabbitConnectionProvider connectionProvider,
            IOptions<ConsumerRegistrarSource> consumerRegistrarSource,
            ILogger<ConsumerManager> logger = null)
        {
            _serviceProvider = serviceProvider;
            _connectionProvider = connectionProvider;
            _consumerRegistrarSource = consumerRegistrarSource.Value;
            _log = logger?.Dsl();

            _exceptionReceiver = new RabbitChannelExceptionReceiver(_log);
            _messageReceiver = new RabbitChannelMessageReceiver(_consumerRegistry, _serviceProvider)
            {
                Log = _log
            };
        }
        public void Start()
        {
            try
            {
                RegisterConsumers();
                ConnectToQueues();
            }
            catch (Exception e)
            {
                _log.Error("Consuming starting error", e)
                    .Write();
            }
        }

        public void Stop()
        {
            try
            {
                _consumerRegistry.Clear();

                foreach (var disposer in _listenDisposers)
                    disposer.Dispose();
                _listenDisposers.Clear();
            }
            catch (Exception e)
            {
                _log.Error("Consuming stopping error", e)
                    .Write();
            }
        }

        private void RegisterConsumers()
        {
            foreach (var consumerRegistrar in _consumerRegistrarSource)
            {
                consumerRegistrar.Register(_consumerRegistry, _serviceProvider);
            }
        }

        private void ConnectToQueues()
        {
            var qList = _consumerRegistry.ToArray();

            var connection = _connectionProvider.Provide();

            foreach (var queue in qList)
            {
                var channel = connection.CreateModel();

                var exceptionHandlingDisposer = _exceptionReceiver.StartListen(channel);
                var messageHandlingDisposer = _messageReceiver.StartListen(queue.Key, channel);

                _listenDisposers.Add(exceptionHandlingDisposer);
                _listenDisposers.Add(messageHandlingDisposer);
            }
        }
    }
}