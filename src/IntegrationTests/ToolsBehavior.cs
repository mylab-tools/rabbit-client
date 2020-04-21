using System;
using Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class ToolsBehavior : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly QueueTestCtx _queueCtx;

        public ToolsBehavior(ITestOutputHelper output)
        {
            _output = output;
            _queueCtx = TestQueue.Create();    
        }

        [Fact]
        public void ShouldSendAndReceiveMessage()
        {
            //Arrange
            _queueCtx.CreateSender().Queue("foo");

            //Act
            var received = _queueCtx.CreateListener().Listen<string>();

            //Assert
            Assert.Equal("foo", received.Payload);
        }

        public void Dispose()
        {
            _queueCtx.Dispose();
        }
    }
}
