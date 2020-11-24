using System;
using Microsoft.AspNetCore.Mvc.Formatters;
using MyLab.Mq;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;
using Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class MqQueueBehavior
    {
        private readonly ITestOutputHelper _output;

        public MqQueueBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldFailIfQueueNotExists()
        {
            //Arrange
            var queueName = Guid.NewGuid().ToString("N");
            var connProvider = new DefaultMqConnectionProvider(TestMqOptions.Load());
            var chProvider = new MqChannelProvider(connProvider);
            var queue = new MqQueue(queueName, chProvider);

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
            var incomingMsg = queue.ListenAutoAck<string>();

            //Assert
            Assert.Equal("foo", incomingMsg.Payload);
        }

        [Fact]
        public void ShouldBindToExchange()
        {
            //Arrange
            var queue = TestQueueFactory.Default.CreateWithRandomId();
            var exchange = TestExchangeFactory.Fanout.CreateWithRandomId();

            //Act
            queue.BindToExchange(exchange);

            exchange.Publish("bar-message");
            var incomingMessage = queue.ListenAutoAck<string>();

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
            var incomingMessage = queue.ListenAutoAck<string>();

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
            Assert.Throws<TimeoutException>(() => queue.ListenAutoAck<string>());
        }

        [Fact]
        public void ShouldDefineDeadLetterExchange()
        {
            //Arrange
            var deadLetterExchange = TestExchangeFactory.Fanout.CreateWithRandomId();
            var deadLetterQueue = TestQueueFactory.Default.CreateWithRandomId();
            deadLetterQueue.BindToExchange(deadLetterExchange);

            var queueFactory = new TestQueueFactory
            {
                DeadLetterExchange = deadLetterExchange.Name,
                AutoDelete = false
            };
            var queue = queueFactory.CreateWithRandomId();

            MqMessageRead<string> deadMsg;

            try
            {
                //Act
                queue.Publish("foo");

                var msg = queue.Listen<string>();
                msg.Nack();

                deadMsg = deadLetterQueue.Listen<string>();
                deadMsg.Ack();
            }
            finally
            {
                queue.Dispose();
            }

            //Assert
            Assert.NotNull(deadMsg);
            Assert.Equal("foo", deadMsg.Message.Payload);
        }

        [Fact]
        public void ShouldDefineDeadLetterWithRouting()
        {
            //Arrange
            var deadLetterExchange = TestExchangeFactory.Fanout.CreateWithRandomId();
            var deadLetterQueue = TestQueueFactory.Default.CreateWithRandomId();
            deadLetterQueue.BindToExchange(deadLetterExchange, "bar");

            var queueFactory = new TestQueueFactory
            {
                DeadLetterExchange = deadLetterExchange.Name,
                DeadLetterRoutingKey = "bar",
                AutoDelete = false
            };
            var queue = queueFactory.CreateWithRandomId();
            MqMessageRead<string> deadMsg;
            try
            {
                //Act
                queue.Publish("foo");

                var msg = queue.Listen<string>();
                msg.Nack();

                deadMsg = deadLetterQueue.Listen<string>();
                deadMsg.Ack();
            }
            finally
            {
                queue.Dispose();
            }

            //Assert
            Assert.NotNull(deadMsg);
            Assert.Equal("foo", deadMsg.Message.Payload);
        }

        [Fact]
        public void ShouldNotDeliveryWhenDeadLetterRoutingIsWrong()
        {
            //Arrange
            var deadLetterExchange = TestExchangeFactory.Direct.CreateWithRandomId();
            var deadLetterQueue = TestQueueFactory.Default.CreateWithRandomId();
            deadLetterQueue.BindToExchange(deadLetterExchange, "bar");

            var queueFactory = new TestQueueFactory
            {
                DeadLetterExchange = deadLetterExchange.Name,
                DeadLetterRoutingKey = "wrong"
            };
            var queue = queueFactory.CreateWithRandomId();

            //Act
            queue.Publish("foo");   

            var msg = queue.Listen<string>();
            msg.Nack();

            MqMessage<string> deadMsg = null;
            TimeoutException timeoutException = null;

            try
            {
                deadMsg = deadLetterQueue.ListenAutoAck<string>();
            }
            catch (TimeoutException e)
            {
                timeoutException = e;
            }

            if(timeoutException == null)
                _output.WriteLine("Dead message: " + (deadMsg?.Payload ?? "[null]"));

            //Assert
            Assert.NotNull(timeoutException);
        }

        [Fact]
        public void ShouldReceiveSeveralMessages()
        {
            //Arrange
            var queueFactory = new TestQueueFactory
            {
                AutoDelete = false
            };
            var q = queueFactory.CreateWithRandomId();

            MqMessageRead<string> msg1;
            MqMessageRead<string> msg2;

            try
            {
                q.Publish("foo");
                q.Publish("bar");

                //Act
                msg1 = q.Listen<string>();
                _output.WriteLine("Msg1: " + msg1.Message.Payload);
                msg1.Ack();

                msg2 = q.Listen<string>();
                _output.WriteLine("Msg2: " + msg2.Message.Payload);
                msg2.Ack();

            }
            finally
            {
                q.Dispose();
            }

            ////Assert
            Assert.Equal("foo", msg1.Message.Payload);
            Assert.Equal("bar", msg2.Message.Payload);
        }
    }
}