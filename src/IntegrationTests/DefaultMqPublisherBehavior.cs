using System;
using Moq;
using MyLab.Mq;
using MyLab.StatusProvider;
using Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class DefaultMqPublisherBehavior : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly QueueTestCtx _queueCtx;
        private readonly TestMqConsumer _listener;

        private const string BoundQueue = "mylab:mq-app:test:bound";

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultMqPublisherBehavior"/>
        /// </summary>
        public DefaultMqPublisherBehavior(ITestOutputHelper output)
        {
            _output = output;
            _queueCtx = TestQueue.Create(BoundQueue);
            _listener = _queueCtx.CreateListener();
        }

        [Fact]
        public void ShouldFailIfPublishTargetNotDefined()
        {
            //Arrange
            var publisher = new DefaultMqPublisher(
                new DefaultMqConnectionProvider(TestQueue.CreateConnectionFactory()), 
                null);
            var outgoingMessage = new OutgoingMqEnvelop<string>
            {
                PublishTarget = new PublishTarget
                {
                    Routing = null,
                    Exchange = null
                },
                Message = new MqMessage<string>("Foo")
            };

            //Act & Assert
            Assert.Throws<InvalidOperationException>(() => publisher.Publish(outgoingMessage));
        }

        [Fact]
        public void ShouldSendMessageWhenPublishTargetSpecified()
        {
            //Arrange
            var publisher = new DefaultMqPublisher(
                new DefaultMqConnectionProvider(TestQueue.CreateConnectionFactory()), 
                null);
            var outgoingMessage = new OutgoingMqEnvelop<string>
            {
                PublishTarget = new PublishTarget
                {
                    Routing = BoundQueue,
                    Exchange = null
                },
                Message = new MqMessage<string>("Foo")
            };

            //Act
            publisher.Publish(outgoingMessage);

            var incoming = _listener.Listen<string>();

            //Assert
            Assert.NotNull(incoming.Payload);
            Assert.Equal("Foo", incoming.Payload);
        }

        [Fact]
        public void ShouldSendMessageWhenPublishTargetSpecifiedByPayloadType()
        {
            //Arrange
            var publisher = new DefaultMqPublisher(
                new DefaultMqConnectionProvider(TestQueue.CreateConnectionFactory()), 
                null);
            var outgoingMessage = new OutgoingMqEnvelop<Msg>
            {
                PublishTarget = new PublishTarget
                {
                    Routing = null,
                    Exchange = null
                },
                Message = new MqMessage<Msg>(new Msg{Value = "Foo"})
            };

            //Act
            publisher.Publish(outgoingMessage);

            var incoming = _listener.Listen<Msg>();

            //Assert
            Assert.NotNull(incoming.Payload);
            Assert.Equal("Foo", incoming.Payload.Value);
        }

        [Fact]
        public void ShouldSendData()
        {
            //Arrange
            var statusServiceMock = new Mock<IAppStatusService>();
            statusServiceMock.Setup(service => service.GetStatus())
                .Returns(new ApplicationStatus
                {
                    Name = "FooApp"
                });
            var publisher = new DefaultMqPublisher(
                new DefaultMqConnectionProvider(TestQueue.CreateConnectionFactory()), 
                null,
                statusServiceMock.Object);

            var correlationId = Guid.NewGuid();
            var messageId = Guid.NewGuid();
            var mgsPayload = new Msg
            {
                Value = "Foo"
            };
            var outgoingMsg = new OutgoingMqEnvelop<Msg>
            {
                PublishTarget = new PublishTarget
                {
                    Routing = BoundQueue
                },
                Message = new MqMessage<Msg>(mgsPayload)
                {
                    ReplyTo = "foo-queue",
                    CorrelationId = correlationId,
                    MessageId = messageId,
                    Headers = new[]
                    {
                        new MqHeader {Name = "FooHeader", Value = "FooValue"},
                    }
                }
            };

            //Act
            publisher.Publish(outgoingMsg);

            var incoming = _listener.Listen<Msg>();

            //Assert
            Assert.NotNull(incoming.ReplyTo);
            Assert.Equal("foo-queue", incoming.ReplyTo);
            Assert.Equal(correlationId, incoming.CorrelationId);
            Assert.Equal(messageId, incoming.MessageId);
            Assert.NotNull(incoming.Payload);
            Assert.Equal("Foo", incoming.Payload.Value);
            Assert.NotNull(incoming.Headers);
            Assert.Contains(incoming.Headers, header => header.Name == "FooHeader" && header.Value == "FooValue");

        }

        public void Dispose()
        {
            _queueCtx.Dispose();
        }

        [Mq(Routing = BoundQueue)]
        class Msg
        {
            public string Value { get; set; }
        }
    }
}
