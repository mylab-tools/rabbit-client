using System;
using RabbitMQ.Client;

namespace IntegrationTests.Tools
{
    static class TestQueue
    {
        public static readonly string Name = "mylab:mq-app:test";
        
        public static ConnectionFactory CreateConnectionFactory()
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
                Password = mqTestPass
            };
        }

        public static void Create()
        {
            var factory = CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(Name, autoDelete:false, exclusive:false);
        }

        public static void Delete()
        {
            var factory = CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDelete(Name);
        }
    }
}