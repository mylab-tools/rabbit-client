using System;
using MyLab.Mq;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;
using RabbitMQ.Client.Exceptions;
using Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class QueueFactoryBehavior
    {
        private readonly ITestOutputHelper _output;

        public QueueFactoryBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldCreateQueue()
        {
            //Arrange
            var queueName = Guid.NewGuid().ToString("N");
            var connProvider = new DefaultMqConnectionProvider(TestMqOptions.Load());
            var queueFactory = new MqQueueFactory(connProvider)
            {
                AutoDelete = true
            };
            
            //Act
            var queue = queueFactory.CreateWithName(queueName);

            _output.WriteLine("Queue name: " + queue.Name);

            //Assert
            Assert.True(QueueExists(queue, connProvider));
            Assert.Equal(queueName, queue.Name);
        }

        [Fact]
        public void ShouldCreateQueueWithId()
        {
            //Arrange
            var queueId = Guid.NewGuid().ToString("N");
            var connProvider = new DefaultMqConnectionProvider(TestMqOptions.Load());
            var queueFactory = new MqQueueFactory(connProvider)
            {
                Prefix = "prefix:",
                AutoDelete = true
            };

            //Act
            var queue = queueFactory.CreateWithId(queueId);

            _output.WriteLine("Queue name: " + queue.Name);

            //Assert
            Assert.True(QueueExists(queue, connProvider));
            Assert.Equal("prefix:" + queueId, queue.Name);
        }

        [Fact]
        public void ShouldCreateQueueWithRandomId()
        {
            //Arrange
            var connProvider = new DefaultMqConnectionProvider(TestMqOptions.Load());
            var queueFactory = new MqQueueFactory(connProvider)
            {
                Prefix = "prefix:",
                AutoDelete = true
            };

            //Act
            var queue = queueFactory.CreateWithRandomId();

            _output.WriteLine("Queue name: " + queue.Name);

            //Assert
            Assert.True(QueueExists(queue, connProvider));
        }

        bool QueueExists(MqQueue queue, IMqConnectionProvider connectionProvider)
        {
            using var channel = connectionProvider.Provide().CreateModel();

            try
            {
                channel.QueueDeclarePassive(queue.Name);
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyText.Contains("no queue"))
            {
                return false;
            }

            return true;
        }
    }
}
