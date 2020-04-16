using System;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    class MqConnectionProvider : IDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly object _lock = new object();

        private IConnection _currConnection;

        public MqConnectionProvider(ConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public IConnection Provide()
        {
            lock (_lock)
            {
                if (_currConnection == null || !_currConnection.IsOpen)
                {
                    _currConnection = _factory.CreateConnection();
                }
            }

            return _currConnection;
        }

        public void Dispose()
        {
            _currConnection?.Dispose();
            _currConnection = null;
        }
    }
}