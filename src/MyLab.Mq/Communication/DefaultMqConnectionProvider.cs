using System;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyLab.Log.Dsl;
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
        private IDslLogger _log;
        private MqOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConnectionProvider"/>
        /// </summary>
        public DefaultMqConnectionProvider(
            IOptions<MqOptions> options,
            ILogger<DefaultMqConnectionProvider> logger = null)
            : this(options.Value, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqConnectionProvider"/>
        /// </summary>
        public DefaultMqConnectionProvider(
            MqOptions options,
            ILogger<DefaultMqConnectionProvider> logger = null)
            : this(OptionToConnectionFactory(options))
        {
            _log = logger?.Dsl();
            _options = options;
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

                    _log?.Action("New MQ connection created")
                        .AndFactIs("host", _options.Host)
                        .AndFactIs("port", _options.Port)
                        .AndFactIs("vhost", _options.VHost ?? "[default]")
                        .AndFactIs("user", _options.User)
                        .AndFactIs("pass", string.IsNullOrEmpty(_options.Password) ? "[empty]" : "*****")
                        .Write();
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