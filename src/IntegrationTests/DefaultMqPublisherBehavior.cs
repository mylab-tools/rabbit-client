using System;
using Moq;
using MyLab.Mq;
using MyLab.StatusProvider;
using Xunit;

namespace IntegrationTests
{
    public partial class DefaultMqPublisherBehavior 
    {
        [Fact]
        public void ShouldFailIfPublishTargetNotDefined()
        {
            //Arrange
            var publisher = CreateTestPublisher();

            var outgoingMessage = new OutgoingMqEnvelop<string>
            {
                Message = new MqMessage<string>("Foo")
            };

            //Act & Assert
            Assert.Throws<InvalidOperationException>(() => publisher.Publish(outgoingMessage));
        }

        [Fact]
        public void ShouldSendMessageWhenPublishTargetSpecified()
        {
            //Arrange
            using var queue = CreateTestQueue();
            var publisher = CreateTestPublisher();

            var outgoingMessage = new OutgoingMqEnvelop<string>
            {
                PublishTarget = new PublishTarget
                {
                    Routing = queue.Name,
                    Exchange = null
                },
                Message = new MqMessage<string>("Foo")
            };

            //Act
            publisher.Publish(outgoingMessage);

            var incoming = queue.ListenAutoAck<string>();

            //Assert
            Assert.NotNull(incoming.Payload);
            Assert.Equal("Foo", incoming.Payload);
        }

        [Fact]
        public void ShouldSendMessageWhenPublishTargetSpecifiedByPayloadType()
        {
            //Arrange
            using var queue = CreateTestQueue();
            var publisher = CreateTestPublisher();

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

            var incoming = queue.ListenAutoAck<Msg>();

            //Assert
            Assert.NotNull(incoming.Payload);
            Assert.Equal("Foo", incoming.Payload.Value);
        }

        [Fact]
        public void ShouldSendData()
        {
            //Arrange
            using var queue = CreateTestQueue();

            var statusServiceMock = new Mock<IAppStatusService>();
            statusServiceMock.Setup(service => service.GetStatus())
                .Returns(new ApplicationStatus
                {
                    Name = "FooApp"
                });
            var publisher = CreateTestPublisher(statusServiceMock.Object);

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
                    Routing = queue.Name
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

            var incoming = queue.ListenAutoAck<Msg>();

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
    }
}
