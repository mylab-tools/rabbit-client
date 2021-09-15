using System;
using MyLab.Log.Dsl;
using RabbitMQ.Client;

namespace MyLab.RabbitClient.Connection
{
    class RabbitConnector
    {
        private readonly RabbitOptions _options;
        private readonly ConnectionFactory _connectionFactory;

        public IDslLogger Log { get; set; }

        public RabbitConnector(RabbitOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _connectionFactory = new ConnectionFactory
            {
                HostName = options.Host,
                VirtualHost = options.VHost ?? "/",
                Port = options.Port,
                UserName = options.User,
                Password = options.Password,
                DispatchConsumersAsync = true
            };
        }

        public IConnection Connect()
        {
            var c = _connectionFactory.CreateConnection();

            c.ConnectionShutdown += ConnectionOnConnectionShutdown;

            Log?.Action("New Rabbit connection established")
                .AndFactIs("host", _options.Host)
                .AndFactIs("port", _options.Port)
                .AndFactIs("vhost", _options.VHost ?? "[default]")
                .AndFactIs("user", _options.User)
                .AndFactIs("pass", string.IsNullOrEmpty(_options.Password) ? "[empty]" : "*****")
                .Write();

            return c;
        }

        private void ConnectionOnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Log?.Action("Rabbit connection shutdown")
                .AndFactIs("args", e)
                .Write();
        }
    }
}