using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;

namespace MyLab.RabbitClient.Connection
{
    class RabbitConnectionStarter : IHostedService
    {
        private readonly IBackgroundRabbitConnectionManager _connectionManager;
        private readonly IDslLogger _log;

        public RabbitConnectionStarter(
            IBackgroundRabbitConnectionManager connectionManager,
            ILogger<RabbitConnectionStarter> logger = null)
        {
            _connectionManager = connectionManager;
            _log = logger.Dsl();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _log?.Action("Try to establish initial connect").Write();

            return _connectionManager.ConnectAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
