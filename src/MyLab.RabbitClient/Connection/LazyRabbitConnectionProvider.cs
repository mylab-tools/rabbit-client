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
    public class LazyRabbitConnectionProvider : IRabbitConnectionProvider, IDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly object _lock = new object();

        private IConnection _currConnection;
        private readonly IDslLogger _log;
        private readonly RabbitOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="LazyRabbitConnectionProvider"/>
        /// </summary>
        public LazyRabbitConnectionProvider(
            IOptions<RabbitOptions> options,
            ILogger<LazyRabbitConnectionProvider> logger = null)
            : this(options.Value, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LazyRabbitConnectionProvider"/>
        /// </summary>
        public LazyRabbitConnectionProvider(
            RabbitOptions options,
            ILogger<LazyRabbitConnectionProvider> logger = null)
            : this(OptionToConnectionFactory(options))
        {
            _log = logger?.Dsl();
            _options = options;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LazyRabbitConnectionProvider"/>
        /// </summary>
        public LazyRabbitConnectionProvider(ConnectionFactory connectionFactory)
        {
            _factory = connectionFactory;
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
                    _currConnection = _factory.CreateConnection();

                    _log?.Action("New Rabbit connection created")
                        .AndFactIs("host", _options.Host)
                        .AndFactIs("port", _options.Port)
                        .AndFactIs("vhost", _options.VHost ?? "[default]")
                        .AndFactIs("user", _options.User)
                        .AndFactIs("pass", string.IsNullOrEmpty(_options.Password) ? "[empty]" : "*****")
                        .Write();

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

        static ConnectionFactory OptionToConnectionFactory(RabbitOptions options)
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

        void OnConnected()
        {
            Reconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}