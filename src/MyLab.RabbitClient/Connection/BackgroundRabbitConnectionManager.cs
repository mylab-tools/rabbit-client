using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    class BackgroundRabbitConnectionManager : IBackgroundRabbitConnectionManager, IDisposable
    {
        private readonly RabbitConnector _connector;
        private IDslLogger _log;
        private IConnection _connection;
        private readonly TimeSpan _retryDelay;

        public BackgroundRabbitConnectionManager(
            IOptions<RabbitOptions> options,
            ILogger<LazyRabbitConnectionProvider> logger = null)
            : this(options.Value, logger)
        {
        }

        public BackgroundRabbitConnectionManager(
            RabbitOptions options,
            ILogger<LazyRabbitConnectionProvider> logger = null)
        {
            _log = logger?.Dsl();
            _connector = new RabbitConnector(options)
            {
                Log = _log
            };

            _retryDelay = TimeSpan.FromSeconds(options.BackgroundRetryPeriodSec);
        }

        public event EventHandler Connected;
        public IConnection ProvideConnection()
        {
            if (_connection == null || !_connection.IsOpen)
                return null;

            return _connection;
        }

        public void Connect()
        {
            bool hasError = false;
            do
            {
                try
                {
                    Thread.Sleep(_retryDelay);
                    _log?.Action("Connection retrying")
                        .Write();
                    _connection = _connector.Connect();
                    _log?.Action("Connection established")
                        .Write();
                }
                catch (Exception e)
                {
                    hasError = true;

                    _log?.Error("Retry connection error", e)
                        .Write();
                }

            } while (hasError);

            _connection.ConnectionShutdown += ConnectionOnConnectionShutdown;

            OnConnected();
        }

        private void ConnectionOnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _connection.ConnectionShutdown -= ConnectionOnConnectionShutdown;

            if (e.Initiator == ShutdownInitiator.Peer)
            {
                _log.Action("Connection retry")
                    .Write();

                Connect();
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        protected virtual void OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }
    }
}