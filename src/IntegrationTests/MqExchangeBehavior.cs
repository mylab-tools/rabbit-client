using System;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;
using Tests.Common;
using Xunit;

namespace IntegrationTests
{
    public class MqExchangeBehavior
    {
        [Fact]
        public void ShouldFailIfExchangeNotExists()
        {
            //Arrange
            var exchangeName = Guid.NewGuid().ToString("N");
            var connProvider = new DefaultMqConnectionProvider(TestMqTools.Load());
            var chProvider = new DefaultMqChannelProvider(connProvider);
            var exchange = new MqExchange(exchangeName, chProvider);

            //Act
            var exists = exchange.IsExists();

            //Assert
            Assert.False(exists);
        }
    }
}