using System;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    class DefaultMqConnectionProvider : IMqConnectionProvider
    {
        private readonly ConnectionFactory _factory;
        private readonly object _lock = new object();

        private IConnection _currConnection;

        public DefaultMqConnectionProvider(IOptions<MqOptions> options)
            : this(options.Value)
        {
        }

        public DefaultMqConnectionProvider(MqOptions options)
            : this(OptionToConnectionFactory(options))
        {
        }

        public DefaultMqConnectionProvider(ConnectionFactory connectionFactory)
        {
            _factory = connectionFactory;
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

        static ConnectionFactory OptionToConnectionFactory(MqOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            return new ConnectionFactory
            {
                HostName = options.Host,
                VirtualHost = options.VHost,
                Port = options.Port,
                UserName = options.User,
                Password = options.Password,
                DispatchConsumersAsync = true 
            };
        }
    }
}