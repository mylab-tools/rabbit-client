using System;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;
using Tests.Common;
using Xunit;

namespace IntegrationTests
{
    public class MqQueueBehavior
    {
        [Fact]
        public void ShouldFailIfQueueNotExists()
        {
            //Arrange
            var queueName = Guid.NewGuid().ToString("N");
            var connProvider = new DefaultMqConnectionProvider(TestMqOptions.Load());
            var queue = new MqQueue(queueName, connProvider);

            //Act
            var exists = queue.IsExists();

            //Assert
            Assert.False(exists);
        }

        [Fact]
        public void ShouldSendAndReceiveMessage()
        {
            //Arrange
            var queue = TestQueueFactory.Default.CreateWithRandomId();

            //Act
            queue.Publish("foo");
            var incomingMsg = queue.Listen<string>();

            //Assert
            Assert.Equal("foo", incomingMsg.Payload);
        }

        [Fact]
        public void ShouldBindToExchange()
        {
            //Arrange
            var queue = TestQueueFactory.Default.CreateWithRandomId();
            var exchange = TestExchangeFactory.Fanaut.CreateWithRandomId();

            //Act
            queue.BindToExchange(exchange);

            exchange.Publish("bar-message");
            var incomingMessage = queue.Listen<string>();

            //Assert
            Assert.Equal("bar-message", incomingMessage.Payload);
        }

        [Fact]
        public void ShouldBindToExchangeWithRouting()
        {
            //Arrange
            var queue = TestQueueFactory.Default.CreateWithRandomId();
            var exchange = TestExchangeFactory.Direct.CreateWithRandomId();

            //Act
            queue.BindToExchange(exchange, "foo-routing");

            exchange.Publish("bar-message", "foo-routing");
            var incomingMessage = queue.Listen<string>();

            //Assert
            Assert.Equal("bar-message", incomingMessage.Payload);
        }

        [Fact]
        public void ShouldNotReceiveMessageWhenWrongRouting()
        {
            //Arrange
            var queue = TestQueueFactory.Default.CreateWithRandomId();
            var exchange = TestExchangeFactory.Direct.CreateWithRandomId();
            queue.BindToExchange(exchange, "foo-routing");

            //Act
            exchange.Publish("bar-message", "wrong-routing");

            //Assert
            Assert.Throws<TimeoutException>(() => queue.Listen<string>());
        }
    }
}