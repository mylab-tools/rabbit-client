using System;
using RabbitMQ.Client;

namespace Tests.Common
{
    public static class TestQueue
    {
        public static ConnectionFactory CreateConnectionFactory(bool async = false)
        {
            var mqTestServer = Environment.GetEnvironmentVariable("MYLAB_TEST_MQ");
            if (string.IsNullOrEmpty(mqTestServer))
                throw new InvalidOperationException("Environment variable 'MYLAB_TEST_MQ' is not set");

            var mqTestUser = Environment.GetEnvironmentVariable("MYLAB_TEST_MQ_USER");
            if (string.IsNullOrEmpty(mqTestUser))
                throw new InvalidOperationException("Environment variable 'MYLAB_TEST_MQ_USER' is not set");

            var mqTestPass = Environment.GetEnvironmentVariable("MYLAB_TEST_MQ_PASS");
            if (string.IsNullOrEmpty(mqTestPass))
                throw new InvalidOperationException("Environment variable 'MYLAB_TEST_MQ_PASS' is not set");

            return new ConnectionFactory
            {
                Endpoint = AmqpTcpEndpoint.Parse(mqTestServer),
                UserName = mqTestUser,
                Password = mqTestPass,
                DispatchConsumersAsync = async
            };
        }

        public static QueueTestCtx CreateWithId(string queueId)
        {
            return Create("mylab:mq-app:test:" + queueId);
        }
        public static QueueTestCtx Create(string queueName = null)
        {
            var factory = CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            string resName = queueName ?? ("mylab:mq-app:test:" + Guid.NewGuid().ToString("N"));

            channel.QueueDeclare(resName, autoDelete: true, exclusive: false);

            return new QueueTestCtx(resName);
        }
    }
}
