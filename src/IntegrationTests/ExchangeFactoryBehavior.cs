using System;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;
using Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class ExchangeFactoryBehavior
    {
        private readonly ITestOutputHelper _output;

        public ExchangeFactoryBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldCreateExchange()
        {
            //Arrange
            var exchangeName = Guid.NewGuid().ToString("N");
            var exchangeFactory = CreateExchangeFactory();
            
            //Act
            using var exchange = exchangeFactory.CreateWithName(exchangeName);

            _output.WriteLine("Exchange name: " + exchange.Name);

            //Assert
            Assert.True(exchange.IsExists());
            Assert.Equal(exchangeName, exchange.Name);
        }

        [Fact]
        public void ShouldCreateExchangeWithId()
        {
            //Arrange
            var exchangeId = Guid.NewGuid().ToString("N");
            var exchangeFactory = CreateExchangeFactory("prefix:");

            //Act
            using var exchange = exchangeFactory.CreateWithId(exchangeId);

            _output.WriteLine("Exchange name: " + exchange.Name);

            //Assert
            Assert.True(exchange.IsExists());
            Assert.Equal("prefix:" + exchangeId, exchange.Name);
        }

        [Fact]
        public void ShouldCreateExchangeWithRandomId()
        {
            //Arrange
            var exchangeFactory = CreateExchangeFactory("prefix:");

            //Act
            using var exchange = exchangeFactory.CreateWithRandomId();

            _output.WriteLine("Exchange name: " + exchange.Name);

            //Assert
            Assert.True(exchange.IsExists());
        }

        MqExchangeFactory CreateExchangeFactory(string namePrefix = null)
        {
            var connProvider = new DefaultMqConnectionProvider(TestMqOptions.Load());
            return new MqExchangeFactory(MqExchangeType.Fanout, connProvider)
            {
                Prefix = namePrefix,
                AutoDelete = true
            };
        }
    }
}
