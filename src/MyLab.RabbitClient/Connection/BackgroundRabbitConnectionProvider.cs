using System;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    class BackgroundRabbitConnectionProvider : IRabbitConnectionProvider
    {
        private readonly IBackgroundRabbitConnectionManager _connectionManager;

        public BackgroundRabbitConnectionProvider(IBackgroundRabbitConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
            _connectionManager.Connected += (sender, args) => OnReconnected();

        }

        public event EventHandler Reconnected;

        public IConnection Provide()
        {
            var resultConnection = _connectionManager.ProvideConnection();

            return resultConnection ?? throw new RabbitNotConnectedException();
        }

        protected virtual void OnReconnected()
        {
            Reconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}