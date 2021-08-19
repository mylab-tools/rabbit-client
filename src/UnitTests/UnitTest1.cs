using System;
using System.Net.Http.Headers;
using RabbitMQ.Client;
using Xunit;

namespace UnitTests
{
    public class BasicPropertiesResearch
    {
        [Fact]
        public void ShouldNotFillHeadersWhenInitProperties()
        {
            //Arrange
            var connectionFactory = new ConnectionFactory();
            IConnection connection = connectionFactory.CreateConnection();
            IModel channel = connection.CreateModel();
            IBasicProperties basicProperties = channel.CreateBasicProperties();

            //Act
            basicProperties.AppId = "foo";

            //Assert
            Assert.Null(basicProperties.Headers);
        }
    }
}
