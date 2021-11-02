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
            IDslLogger<ConsumerHost> logger = null)
        {
            _consumerHost = consumerHost ?? throw new ArgumentNullException(nameof(consumerHost));
            _log = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _consumerHost.Start();
            }
            catch (Exception e)
            {
                _log.Error("Error when starting consuming", e).Write();
            }

            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _consumerHost.Stop();
            }
            catch (Exception e)
            {
                _log.Error("Error when stopping consuming", e).Write();
            }

            return Task.CompletedTask;
        }
    }
}
