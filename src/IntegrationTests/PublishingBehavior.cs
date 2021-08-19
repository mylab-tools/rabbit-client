using System;
using Microsoft.Extensions.DependencyInjection;
using MyLab.RabbitClient.Model;
using MyLab.RabbitClient.Publishing;
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
                .AddRabbitPublisher()
                .ConfigureRabbitClient(TestTools.OptionsConfigureAct)
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

        class TestEntity
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }
    }
}
