using RabbitMQ.Client;
using Xunit;

namespace IntegrationTests
{
    public class BasicPropertiesResearch
    {
        [Fact]
        public void ShouldNotFillHeadersWhenInitProperties()
        {
            //Arrange
            IBasicProperties basicProperties = null;
            TestTools.ChannelProvider.Use(ch => basicProperties = ch.CreateBasicProperties());

            //Act
            basicProperties.AppId = "foo";

            //Assert
            Assert.Null(basicProperties.Headers);
        }
    }
}
