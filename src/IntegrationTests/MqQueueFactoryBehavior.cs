using System;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;
using Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class MqQueueFactoryBehavior
    {
        private readonly ITestOutputHelper _output;

        public MqQueueFactoryBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldCreateQueue()
        {
            //Arrange
            var queueName = Guid.NewGuid().ToString("N");
            var queueFactory = CreateQueueFactory();
            
            //Act
            using var queue = queueFactory.CreateWithName(queueName);

            _output.WriteLine("Queue name: " + queue.Name);

            //Assert
            Assert.True(queue.IsExists());
            Assert.Equal(queueName, queue.Name);
        }

        [Fact]
        public void ShouldCreateQueueWithId()
        {
            //Arrange
            var queueId = Guid.NewGuid().ToString("N");
            var queueFactory = CreateQueueFactory("prefix:");

            //Act
            using var queue = queueFactory.CreateWithId(queueId);

            _output.WriteLine("Queue name: " + queue.Name);

            //Assert
            Assert.True(queue.IsExists());
            Assert.Equal("prefix:" + queueId, queue.Name);
        }

        [Fact]
        public void ShouldCreateQueueWithRandomId()
        {
            //Arrange
            var queueFactory = CreateQueueFactory("prefix:");

            //Act
            using var queue = queueFactory.CreateWithRandomId();

            _output.WriteLine("Queue name: " + queue.Name);

            //Assert
            Assert.True(queue.IsExists());
        }
        
        MqQueueFactory CreateQueueFactory(string namePrefix = null)
        {
            var connProvider = new DefaultMqConnectionProvider(TestMqTools.Load());
            return new MqQueueFactory(new DefaultMqChannelProvider(connProvider))
            {
                Prefix = namePrefix,
                AutoDelete = true
            };
        }
    }
}
