using MyLab.Mq;
using RabbitMQ.Client;
using Tests.Common;

namespace TestServer
{
    class TestConnectionProvider : IMqConnectionProvider
    {
        private readonly IConnection _connection;

        /// <summary>
        /// Initializes a new instance of <see cref="TestConnectionProvider"/>
        /// </summary>
        public TestConnectionProvider()
        {
            _connection = TestQueue.CreateConnectionFactory().CreateConnection();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public IConnection Provide()
        {
            return _connection;
        }
    }
}