﻿using System;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Consuming;
using MyLab.RabbitClient.Publishing;
using RabbitMQ.Client.Events;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class EmulationBehavior
    {
        private readonly ITestOutputHelper _output;

        public EmulationBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldUseEmulation()
        {
            //Arrange
            BasicDeliverEventArgs receivedMsg = null;

            var consumerMock = new Mock<IRabbitConsumer>();
            consumerMock
                .Setup(r => r.ConsumeAsync(It.IsAny<BasicDeliverEventArgs>()))
                .Callback((BasicDeliverEventArgs args) =>
                {
                    receivedMsg = args;
                })
                .Returns(Task.CompletedTask);


            var serviceProvider = new ServiceCollection()
                .AddLogging(l => l.AddFilter(_ => true).AddXUnit(_output))
                .AddRabbit()
                .ConfigureRabbit(options => options.DefaultPub = 
                    new PublishOptions
                    {
                        RoutingKey = "foo-queue"
                    }
                )
                .AddRabbitConsumer("foo-queue", consumerMock.Object)
                .AddRabbitEmulation()
                .BuildServiceProvider();

            var publisher = serviceProvider.GetService<IRabbitPublisher>();

            var messageId = Guid.NewGuid().ToString();

            //Act
            publisher
                ?.IntoDefault()
                .AndProperty(properties => properties.MessageId = messageId)
                .SetStringContent("data")
                .Publish();

            //Assert
            Assert.NotNull(receivedMsg);
            Assert.Equal(messageId, receivedMsg.BasicProperties.MessageId);
            Assert.Equal("data", Encoding.UTF8.GetString(receivedMsg.Body.ToArray()));
        }

        [Fact]
        public void ShouldUseConsumingLogicStrategyWithAck()
        {
            //Arrange
            BasicDeliverEventArgs receivedMsg = null;

            var consumerMock = new Mock<IRabbitConsumer>();
            consumerMock
                .Setup(r => r.ConsumeAsync(It.IsAny<BasicDeliverEventArgs>()))
                .Callback((BasicDeliverEventArgs args) =>
                {
                    receivedMsg = args;
                })
                .Returns(Task.CompletedTask);

            var consLogicStrategyMock = new Mock<IConsumingLogicStrategy>();

            var serviceProvider = new ServiceCollection()
                .AddLogging(l => l.AddFilter(_ => true).AddXUnit(_output))
                .AddRabbit()
                .ConfigureRabbit(options => options.DefaultPub =
                    new PublishOptions
                    {
                        RoutingKey = "foo-queue"
                    }
                )
                .AddRabbitConsumer("foo-queue", consumerMock.Object)
                .AddRabbitEmulation(consLogicStrategyMock.Object)
                .BuildServiceProvider();

            var publisher = serviceProvider.GetService<IRabbitPublisher>();

            var messageId = Guid.NewGuid().ToString();

            //Act
            publisher
                ?.IntoDefault()
                .AndProperty(properties => properties.MessageId = messageId)
                .SetStringContent("data")
                .Publish();

            //Assert
            consLogicStrategyMock.Verify(s => s.Ack(It.IsAny<ulong>()));

            Assert.NotNull(receivedMsg);
            Assert.Equal(messageId, receivedMsg.BasicProperties.MessageId);
        }

        [Fact]
        public void ShouldUseConsumingLogicStrategyWithNack()
        {
            //Arrange
            var consumerMock = new Mock<IRabbitConsumer>();
            consumerMock
                .Setup(r => r.ConsumeAsync(It.IsAny<BasicDeliverEventArgs>()))
                .Callback((BasicDeliverEventArgs _) => throw new Exception("Test interrupt"))
                .Returns(Task.CompletedTask);

            var consLogicStrategyMock = new Mock<IConsumingLogicStrategy>();

            var serviceProvider = new ServiceCollection()
                .AddLogging(l => l.AddFilter(_ => true).AddXUnit(_output))
                .AddRabbit()
                .ConfigureRabbit(options => options.DefaultPub =
                    new PublishOptions
                    {
                        RoutingKey = "foo-queue"
                    }
                )
                .AddRabbitConsumer("foo-queue", consumerMock.Object)
                .AddRabbitEmulation(consLogicStrategyMock.Object)
                .BuildServiceProvider();

            var publisher = serviceProvider.GetService<IRabbitPublisher>();
            
            //Act
            publisher
                ?.IntoDefault()
                .SetStringContent("data")
                .Publish();

            //Assert
            consLogicStrategyMock.Verify(s => s.Nack(It.IsAny<ulong>()));
        }

        [Fact]
        public void ShouldRemoveRealConnectionServices()
        {
            //Arrange
            var consumerMock = new Mock<IRabbitConsumer>();

            var services = new ServiceCollection()
                .AddLogging(l => l.AddFilter(_ => true).AddXUnit(_output))
                .AddRabbit()
                .ConfigureRabbit(options => options.DefaultPub =
                    new PublishOptions
                    {
                        RoutingKey = "foo-queue"
                    }
                )
                .AddRabbitConsumer("foo-queue", consumerMock.Object);

            //Act
            var emulationModeServices = services.AddRabbitEmulation();

            //Assert
            Assert.DoesNotContain(emulationModeServices, d => CompareDescriptors(d, AppIntegration.PublisherSrvDesc));
            Assert.DoesNotContain(emulationModeServices, d => CompareDescriptors(d, AppIntegration.ChannelProviderSrvDesc));
            Assert.DoesNotContain(emulationModeServices, d => CompareDescriptors(d, AppIntegration.ConsumerHostSrvDesc));
            Assert.DoesNotContain(emulationModeServices, d => CompareDescriptors(d, AppIntegration.ConsumerManagerSrvDesc));
            Assert.DoesNotContain(emulationModeServices, d => CompareDescriptors(d, AppIntegration.LazyConnProviderSrvDesc));
            Assert.DoesNotContain(emulationModeServices, d => CompareDescriptors(d, AppIntegration.BgConnManagerSrvDesc));
            Assert.DoesNotContain(emulationModeServices, d => CompareDescriptors(d, AppIntegration.BgConnProviderSrvDesc));
            Assert.DoesNotContain(emulationModeServices, d => CompareDescriptors(d, AppIntegration.BgConnStarterSrvDesc));
        }

        bool CompareDescriptors(ServiceDescriptor desc1, ServiceDescriptor desc2)
        {
            if (desc1 == desc2) return true;
            if(desc1 == null || desc2 == null) return false;

            return desc1.ServiceType == desc2.ServiceType &&
                   desc1.ImplementationType == desc2.ImplementationType &&
                   desc1.Lifetime == desc2.Lifetime;
        }
    }
}
