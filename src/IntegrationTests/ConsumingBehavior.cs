using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Consuming;
using MyLab.RabbitClient.Model;
using RabbitMQ.Client.Events;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class ConsumingBehavior
    {
        private readonly ITestOutputHelper _output;

        public ConsumingBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ShouldReceiveMessage()
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
                Prefix = "test"
            }.CreateWithRandomId();

            var consumer = new TestConsumer();

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(srv => srv
                    .AddRabbit()
                    .ConfigureRabbit(TestTools.OptionsConfigureAct)
                    .AddRabbitConsumer(queue.Name, consumer)
                    .AddLogging(l=> l.AddXUnit(_output).AddFilter(l => true)))
                .Build();

            await host.StartAsync(cancellationToken);

            //Act
            queue.Publish(testEntity);

            await Task.Delay(500);

            cancellationSource.Cancel();

            await host.StopAsync();

            host.Dispose();

            var gotMsg = consumer.LastMessages.Dequeue();

            //Assert
            Assert.NotNull(gotMsg);
            Assert.Equal(1, gotMsg.Content.Id);
            Assert.Equal("foo", gotMsg.Content.Value);
        }

        [Fact]
        public async Task ShouldReceiveMessageOneAtTime()
        {
            //Arrange

            var testEntity1 = new TestEntity
            {
                Id = 1,
                Value = "foo"
            };

            var testEntity2 = new TestEntity
            {
                Id = 2,
                Value = "bar"
            };

            var queue = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test"
            }.CreateWithRandomId();

            var consumer = new TestConsumer();

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(srv => srv
                    .ConfigureRabbit(TestTools.OptionsConfigureAct)
                    .AddRabbit()
                    .AddRabbitConsumer(queue.Name, consumer)
                    .AddLogging(l => l.AddXUnit(_output).AddFilter(l => true)))
                .Build();

            await host.StartAsync(cancellationToken);

            //Act
            queue.Publish(testEntity1);
            queue.Publish(testEntity2);

            await Task.Delay(500);

            cancellationSource.Cancel();
            
            host.Dispose();

            var msg1 = consumer.LastMessages.Dequeue();
            var msg2 = consumer.LastMessages.Dequeue();

            //Assert
            Assert.NotNull(msg1);
            Assert.Equal(1, msg1.Content.Id);
            Assert.Equal("foo", msg1.Content.Value);
            Assert.NotNull(msg2);
            Assert.Equal(2, msg2.Content.Id);
            Assert.Equal("bar", msg2.Content.Value);
        }

        [Fact]
        public async Task ShouldUseConsumingCtx()
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
                Prefix = "test"
            }.CreateWithRandomId();

            var consumer = new TestConsumer();

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(srv => srv
                    .AddRabbit()
                    .ConfigureRabbit(TestTools.OptionsConfigureAct)
                    .AddRabbitConsumer(queue.Name, consumer)
                    .AddRabbitConsumingContext<AddFoobarHeaderConsumingCtx>()
                    .AddLogging(l => l.AddXUnit(_output).AddFilter(l => true)))
                .Build();

            await host.StartAsync(cancellationToken);

            //Act
            queue.Publish(testEntity);

            await Task.Delay(500);

            cancellationSource.Cancel();

            await host.StopAsync();

            host.Dispose();

            var gotMsg = consumer.LastMessages.Dequeue();

            //Assert
            Assert.True(gotMsg.BasicProperties.Headers.TryGetValue("foo", out var barValue));
            Assert.Equal("bar", barValue);
        }

        [Fact]
        public async Task ShouldUseNullConsumingCtx()
        {
            //Arrange
            var testEntity = new TestEntity
            {
                Id = 10
            };

            var queue = new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test"
            }.CreateWithRandomId();

            var consumer = new TestConsumer();

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            var logErrorCatcher = new LogErrorCatcher();
            var logErrorCatcherProvider = new LogErrorCatcherProvider(logErrorCatcher);

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(srv => srv
                    .AddRabbit()
                    .ConfigureRabbit(TestTools.OptionsConfigureAct)
                    .AddRabbitConsumer(queue.Name, consumer)
                    .AddRabbitConsumingContext<NullConsumingCtx>()
                    .AddLogging(l => l
                        .AddXUnit(_output)
                        .AddFilter(l => true)
                        .AddProvider(logErrorCatcherProvider)))
                .Build();

            await host.StartAsync(cancellationToken);

            //Act
            queue.Publish(testEntity);

            await Task.Delay(500);

            cancellationSource.Cancel();

            await host.StopAsync();

            host.Dispose();

            var gotMsg = consumer.LastMessages.Dequeue();

            //Assert
            Assert.Null(logErrorCatcher.LastError);
            Assert.NotNull(gotMsg);
            Assert.Equal(10, gotMsg.Content.Id);
        }

        class AddFoobarHeaderConsumingCtx : IConsumingContext
        {
            public IDisposable Set(BasicDeliverEventArgs deliverEventArgs)
            {
                deliverEventArgs.BasicProperties.Headers = new Dictionary<string, object>
                {
                    {"foo", "bar"}
                };

                return new EmptyCtx();
            }

            class EmptyCtx : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }

        class NullConsumingCtx : IConsumingContext
        {
            public IDisposable Set(BasicDeliverEventArgs deliverEventArgs)
            {
                return null;
            }
        }

        class TestConsumer : RabbitConsumer<TestEntity>
        {
            public Queue<ConsumedMessage<TestEntity>> LastMessages { get; } = new Queue<ConsumedMessage<TestEntity>>();

            protected override Task ConsumeMessageAsync(ConsumedMessage<TestEntity> consumedMessage)
            {
                LastMessages.Enqueue(consumedMessage);

                return Task.CompletedTask;
            }
        }

        class TestEntity
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }
    }
}
