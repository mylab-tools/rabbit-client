using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class PublicationAddressBehavior
    {
        private readonly ITestOutputHelper _output;

        /// <summary>
        /// Initializes a new instance of <see cref="PublicationAddressBehavior"/>
        /// </summary>
        public PublicationAddressBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldSerializeWithExchange()
        {
            //Arrange
            var addr = new PublicationAddress("exchange-type", "exchange-name", "routing-key");

            //Act
            var str = addr.ToString();
            _output.WriteLine(str);

            //Assert
            Assert.Equal("exchange-type://exchange-name/routing-key", str);
        }

        [Fact]
        public void ShouldSerializeWithoutExchange()
        {
            //Arrange
            var addr = new PublicationAddress(null, null, "routing-key");

            //Act
            var str = addr.ToString();
            _output.WriteLine(str);

            //Assert
            Assert.Equal(":///routing-key", str);
        }

        [Fact]
        public void ShouldDeserializeWithExchange()
        {
            //Act
            var addr = PublicationAddress.Parse("exchange-type://exchange-name/routing-key");

            //Assert
            Assert.Equal("exchange-type", addr.ExchangeType);
            Assert.Equal("exchange-name", addr.ExchangeName);
            Assert.Equal("routing-key", addr.RoutingKey);
        }

        [Fact]
        public void ShouldDeserializeWithoutExchange()
        {
            //Act
            var addr = PublicationAddress.Parse(":///routing-key");

            //Assert
            Assert.Null(addr);
        }

        [Fact]
        public void ShouldDeserializeWithoutExchangeName()
        {
            //Act
            var addr = PublicationAddress.Parse("default:///routing-key");

            //Assert
            Assert.Equal("default", addr.ExchangeType);
            Assert.Equal("", addr.ExchangeName);
            Assert.Equal("routing-key", addr.RoutingKey);
        }
    }
}
