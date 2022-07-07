using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;

namespace MyLab.RabbitClient.Consuming
{
    class ConsumerHost : IHostedService
    {
        private readonly IConsumerManager _consumerHost;
        private readonly IDslLogger _log;

        public ConsumerHost(
            IConsumerManager consumerHost,
            ILogger<ConsumerHost> logger = null)
        {
            _consumerHost = consumerHost ?? throw new ArgumentNullException(nameof(consumerHost));
            _log = logger?.Dsl();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await  _consumerHost.StartAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _log?.Error("Error when starting consuming", e).Write();
            }
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _consumerHost.StopAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _log?.Error("Error when stopping consuming", e).Write();
            }
        }
    }
}
