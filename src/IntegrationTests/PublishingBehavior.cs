using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Model;
using MyLab.RabbitClient.Publishing;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class PublishingBehavior
    {
        private readonly ITestOutputHelper _output;

        public PublishingBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

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
                .AddLogging(l => l.AddFilter(lvl => true).AddXUnit(_output))
                .BuildServiceProvider();

            var publisher = sp.GetService<IRabbitPublisher>();

            //Act
            publisher
                .IntoQueue(queue.Name)
                .SetJsonContent(testEntity)
                .Publish();

            var gotMsg = queue.Listen<TestEntity>(TimeSpan.FromSeconds(1));

            //Assert
            Assert.Equal(1, gotMsg.Content.Id);
            Assert.Equal("foo", gotMsg.Content.Value);
        }

        [Fact]
        public void ShouldUsePublishingCtx()
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
                .AddLogging(l => l.AddFilter(lvl => true).AddXUnit(_output))
                .BuildServiceProvider();

            var publisher = sp.GetService<IRabbitPublisher>();

            //Act
            publisher
                .IntoQueue(queue.Name)
                .SetJsonContent(testEntity)
                .Publish();

            var gotMsg = queue.Listen<TestEntity>(TimeSpan.FromSeconds(1));

            //Assert
            Assert.True(gotMsg.BasicProperties.Headers.TryGetValue("foo", out var barValue));
            Assert.Equal("bar", Encoding.UTF8.GetString((byte[])barValue));
        }

        [Fact]
        public void ShouldIgnoreNullPublishingCtx()
        {
            //Arrange
            var testEntity = new TestEntity
            {
                Id = 10
            };

            var queue = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test-"
            }.CreateWithRandomId();

            var logErrorCatcher = new LogErrorCatcher();
            var logErrorCatcherProvider = new LogErrorCatcherProvider(logErrorCatcher);

            var sp = new ServiceCollection()
                .AddRabbit()
                .ConfigureRabbit(TestTools.OptionsConfigureAct)
                .AddRabbitPublishingContext<NullPubCtx>()
                .AddLogging(l => l                
                    .AddFilter(lvl => true)                
                    .AddXUnit(_output)
                    .AddProvider(logErrorCatcherProvider))
                .BuildServiceProvider();

            var publisher = sp.GetService<IRabbitPublisher>();

            //Act
            publisher
                .IntoQueue(queue.Name)
                .SetJsonContent(testEntity)
                .Publish();

            var gotMsg = queue.Listen<TestEntity>(TimeSpan.FromSeconds(1));

            //Assert
            Assert.Null(logErrorCatcher.LastError);
            Assert.NotNull(gotMsg);
            Assert.Equal(10, gotMsg.Content.Id);
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

                return new EmptyCtx();
            }

            class EmptyCtx : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }

        class NullPubCtx : IPublishingContext
        {
            public IDisposable Set(RabbitPublishingMessage publishingMessage)
            {
                return null;
            }
        }
    }
}
