using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Model;
using MyLab.RabbitClient.Publishing;
using RabbitMQ.Client;
using Xunit;

namespace IntegrationTests
{
    public class PublishingBehavior
    {
        [Fact]
        public void ShouldPublish()
        {
            //Arrange
            var testEntity = new TestEntity
            {
                Id = 1,
                Value = "foo"
            };

            var queue = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            }.CreateWithRandomId();

            var sp = new ServiceCollection()
                .AddRabbit()
                .ConfigureRabbit(TestTools.OptionsConfigureAct)
                .BuildServiceProvider();

            var publisher = sp.GetService<IRabbitPublisher>();

            //Act
            publisher
                .IntoQueue(queue.Name)
                .SendJson(testEntity)
                .Publish();

            var gotMsg = queue.Listen<TestEntity>(TimeSpan.FromSeconds(1));

            //Assert
            Assert.Equal(1, gotMsg.Content.Id);
            Assert.Equal("foo", gotMsg.Content.Value);
        }

        [Fact]
        public void ShouldUsePublishingMessageProcessors()
        {
            //Arrange
            var testEntity = new TestEntity
            {
                Id = 1,
                Value = "foo"
            };

            var queue = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            }.CreateWithRandomId();

            var sp = new ServiceCollection()
                .AddRabbit()
                .ConfigureRabbit(TestTools.OptionsConfigureAct)
                .AddRabbitPublishingContext<AddFoobarHeaderPubCtx>()
                .BuildServiceProvider();

            var publisher = sp.GetService<IRabbitPublisher>();

            //Act
            publisher
                .IntoQueue(queue.Name)
                .SendJson(testEntity)
                .Publish();

            var gotMsg = queue.Listen<TestEntity>(TimeSpan.FromSeconds(1));

            //Assert
            Assert.True(gotMsg.BasicProperties.Headers.TryGetValue("foo", out var barValue));
            Assert.Equal("bar", Encoding.UTF8.GetString((byte[])barValue));
        }

        class TestEntity
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }

        class AddFoobarHeaderPubCtx : IPublishingContext
        {
            public IDisposable Set(RabbitPublishingMessage publishingMessage)
            {
                publishingMessage.BasicProperties.Headers.Add("foo", "bar");

                return EmptyCtx.Instance;
            }
        }
    }
}
