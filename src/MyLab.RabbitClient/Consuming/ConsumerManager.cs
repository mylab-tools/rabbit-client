
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Connection;
using RabbitMQ.Client.Exceptions;

namespace MyLab.RabbitClient.Consuming
{
    class ConsumerManager : IConsumerManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitChannelProvider _channelProvider;
        private readonly ConsumerRegistrarSource _consumerRegistrarSource;
        private readonly IDslLogger _log;

        private readonly RabbitChannelExceptionReceiver _exceptionReceiver;
        private readonly RabbitChannelMessageReceiver _messageReceiver;

        private readonly ConsumerRegistry _consumerRegistry = new ConsumerRegistry();

        private readonly IList<IDisposable> _listenDisposers = new List<IDisposable>();

        public ConsumerManager(
            IServiceProvider serviceProvider,
            IRabbitChannelProvider channelProvider,
            IOptions<ConsumerRegistrarSource> consumerRegistrarSource,
            ILogger<ConsumerManager> logger = null)
        {
            _serviceProvider = serviceProvider;
            _channelProvider = channelProvider;
            _consumerRegistrarSource = consumerRegistrarSource.Value;
            _log = logger?.Dsl();

            _exceptionReceiver = new RabbitChannelExceptionReceiver(logger);

            var consumingLogic = new ConsumingLogic(_consumerRegistry, serviceProvider)
            {
                Log = _log
            };

            _messageReceiver = new RabbitChannelMessageReceiver(consumingLogic)
            {
                Log = _log
            };
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _consumerRegistry.RegisterConsumersFromSource(_consumerRegistrarSource, _serviceProvider);

                if (_consumerRegistry.Count != 0)
                {
                    await ConnectToQueuesAsync(cancellationToken);
                }
                else
                {
                    _log?.Warning("No consumer registered. No connection will be created").Write();
                }
            }
            catch (Exception e)
            {
                _log?.Error("Consuming starting error", e)
                    .Write();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
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
                _log?.Error("Consuming stopping error", e)
                    .Write();
            }

            return Task.CompletedTask;
        }

        private async Task ConnectToQueuesAsync(CancellationToken cancellationToken)
        {
            var qList = _consumerRegistry.ToArray();

            foreach (var queue in qList)
            {
                var channel = await ProvideChannelAsync(cancellationToken);

                var exceptionHandlingDisposer = _exceptionReceiver.StartListen(channel.Channel);
                var messageHandlingDisposer = _messageReceiver.StartListen(queue.Key, channel.Channel);

                _listenDisposers.Add(exceptionHandlingDisposer);
                _listenDisposers.Add(messageHandlingDisposer);
                _listenDisposers.Add(channel);
            }
        }

        private async Task<RabbitChannelUsing> ProvideChannelAsync(CancellationToken cancellationToken)
        {
            var hasBrokerUnreachableException = false;
            RabbitChannelUsing resChannel = null;

            do
            {
                try
                {
                    resChannel = _channelProvider.Provide();
                }
                catch (BrokerUnreachableException )
                {
                    _log?.Warning("Broker is not yet accessible. Retry after 5 sec...").Write();
                    hasBrokerUnreachableException = true;

                    await Task.Delay(5000, cancellationToken);
                }
            } while (hasBrokerUnreachableException && !cancellationToken.IsCancellationRequested);

            return resChannel;
        }
    }
}