using System;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MyLab.Mq.Communication
{
    /// <summary>
    /// Default implementation for <see cref="IMqConnectionProvider"/>
    /// </summary>
    public class DefaultMqConnectionProvider : IMqConnectionProvider
    {
        private readonly ConnectionFactory _factory;
        private readonly object _lock = new object();

        private IConnection _currConnection;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConnectionProvider"/>
        /// </summary>
        public DefaultMqConnectionProvider(IOptions<MqOptions> options)
            : this(options.Value)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConnectionProvider"/>
        /// </summary>
        public DefaultMqConnectionProvider(MqOptions options)
            : this(OptionToConnectionFactory(options))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConnectionProvider"/>
        /// </summary>
        public DefaultMqConnectionProvider(ConnectionFactory connectionFactory)
        {
            _factory = connectionFactory;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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
                VirtualHost = options.VHost ?? "/",
                Port = options.Port,
                UserName = options.User,
                Password = options.Password,
                DispatchConsumersAsync = true
            };
        }
    }
}