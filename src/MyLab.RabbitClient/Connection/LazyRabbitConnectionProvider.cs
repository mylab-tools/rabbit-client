using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    /// <summary>
    /// Manages and provides Rabbit connection
    /// </summary>
    class LazyRabbitConnectionProvider : IRabbitConnectionProvider, IDisposable
    {
        private readonly RabbitConnector _connector;
        private readonly object _lock = new object();

        private IConnection _currConnection;

        public LazyRabbitConnectionProvider(
            IOptions<RabbitOptions> options,
            ILogger<LazyRabbitConnectionProvider> logger = null)
            : this(options.Value, logger)
        {
        }

        public LazyRabbitConnectionProvider(
            RabbitOptions options,
            ILogger<LazyRabbitConnectionProvider> logger = null)
        {
            _connector = new RabbitConnector(options)
            {
                Log = logger?.Dsl()
            };
        }

        /// <inheritdoc />
        public event EventHandler Reconnected;

        /// <inheritdoc />
        public IConnection Provide()
        {
            lock (_lock)
            {
                if (_currConnection == null || !_currConnection.IsOpen)
                {
                    _currConnection = _connector.Connect();

                    OnConnected();
                }
            }

            return _currConnection;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _currConnection?.Close();
            _currConnection?.Dispose();
            _currConnection = null;
        }

        void OnConnected()
        {
            Reconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}