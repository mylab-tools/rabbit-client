using System;
using RabbitMQ.Client;

namespace IntegrationTests.Tools
{
    class QueueTestCtx : IDisposable
    {
        private readonly string _name;

        public QueueTestCtx(string name)
        {
            _name = name;
        }

        public TestMqConsumer CreateListener()
        {
            return new TestMqConsumer(_name);
        }

        public TestMqSender CreateSender()
        {
            return new TestMqSender(_name);
        }

        public void Dispose()
        {
            var factory = TestQueue.CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDelete(_name);
        }
    }
}