using System;
using RabbitMQ.Client;

namespace Tests.Common
{
    public class QueueTestCtx : IDisposable
    {
        public string QueueName { get; }

        public QueueTestCtx(string queueName)
        {
            QueueName = queueName;
        }

        public TestMqConsumer CreateListener()
        {
            return new TestMqConsumer(QueueName);
        }

        public TestMqSender CreateSender()
        {
            return new TestMqSender(QueueName);
        }

        public void Dispose()
        {
            var factory = TestQueue.CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDelete(QueueName);
        }
    }
}