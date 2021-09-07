using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
                    .ConfigureRabbitClient(TestTools.OptionsConfigureAct)
                    .AddRabbitConsumer(queue.Name, consumer)
                    .AddLogging(l=> l.AddXUnit(_output).AddFilter(l => true)))
                .Build();

            //await host.RunAsync(cancellationToken);
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
            Assert.Equal(1, gotMsg.Id);
            Assert.Equal("foo", gotMsg.Value);
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
                    .ConfigureRabbitClient(TestTools.OptionsConfigureAct)
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
            Assert.Equal(1, msg1.Id);
            Assert.Equal("foo", msg1.Value);
            Assert.NotNull(msg2);
            Assert.Equal(2, msg2.Id);
            Assert.Equal("bar", msg2.Value);
        }

        class TestConsumer : RabbitConsumer<TestEntity>
        {
            public Queue<TestEntity> LastMessages { get; } = new Queue<TestEntity>();

            protected override Task ConsumeMessageAsync(ConsumedMessage<TestEntity> consumedMessage)
            {
                LastMessages.Enqueue(consumedMessage.Content);

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
