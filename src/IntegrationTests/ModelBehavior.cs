using System;
using MyLab.RabbitClient.Model;
using Xunit;

namespace IntegrationTests
{
    public class ModelBehavior
    {
        [Fact]
        public void ShouldDeliverWithQueue()
        {
            //Arrange
            var queueFactory = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix ="test-"
            };
            var queue = queueFactory.CreateWithRandomId();

            //Act
            queue.Publish("foo");
            var msg = queue.Listen<string>();

            //Assert
            Assert.Equal("foo", msg.Content);
        }

        [Fact]
        public void ShouldDeliverWithExchange()
        {
            //Arrange
            var exchangeFactory = new RabbitExchangeFactory(RabbitExchangeType.Fanout, TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            };

            var exchange = exchangeFactory.CreateWithRandomId();

            var queueFactory = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            };
            var queue = queueFactory.CreateWithRandomId();
            queue.BindToExchange(exchange);

            //Act
            exchange.Publish("foo");
            var msg = queue.Listen<string>();

            //Assert
            Assert.Equal("foo", msg.Content);
        }

        [Fact]
        public void ShouldDeliverWithExchangeRouting()
        {
            //Arrange
            var exchangeFactory = new RabbitExchangeFactory(RabbitExchangeType.Direct, TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            };

            var exchange = exchangeFactory.CreateWithRandomId();

            var queueFactory = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            };
            var queue = queueFactory.CreateWithRandomId();
            queue.BindToExchange(exchange, "right-route");

            //Act
            exchange.Publish("foo", "right-route");
            var msg = queue.Listen<string>();

            //Assert
            Assert.Equal("foo", msg.Content);
        }

        [Fact]
        public void ShouldNotDeliverWithWrongExchangeRouting()
        {
            //Arrange
            var exchangeFactory = new RabbitExchangeFactory(RabbitExchangeType.Direct, TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            };

            var exchange = exchangeFactory.CreateWithRandomId();

            var queueFactory = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            };
            var queue = queueFactory.CreateWithRandomId();
            queue.BindToExchange(exchange, "right-route");

            //Act
            exchange.Publish("foo", "wrong-route");

            //Assert
            Assert.Throws<TimeoutException>(() =>
            {
                queue.Listen<string>();
            });
        }
    }
}
