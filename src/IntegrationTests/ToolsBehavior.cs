using System;
using IntegrationTests.Tools;
using Xunit;

namespace IntegrationTests
{
    public class ToolsBehavior : IDisposable
    {
        public ToolsBehavior()
        {
            TestQueue.Create();    
        }

        [Fact]
        public void ShouldSendAndReceiveMessage()
        {
            //Arrange
            TestMqSender.Queue("foo");

            //Act
            var received = TestMqConsumer.Listen<string>();

            //Assert
            Assert.Equal("foo", received);
        }

        public void Dispose()
        {
            TestQueue.Delete();
        }
    }
}
