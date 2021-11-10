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
    public partial class ConsumingBehavior
    {
        [Fact]
        public async Task ShouldReceiveMessage()
        {
            //Arrange

            var testEntity = new TestEntity
            {
                Id = 1,
                Value = "foo"
            };

            var queue = CreateQueue();
            var consumer = new TestConsumer();
            var host = CreateHost(queue.Name, consumer);

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
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

            var queue = CreateQueue();
            var consumer = new TestConsumer();
            var host = CreateHost(queue.Name, consumer);

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
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

            var queue = CreateQueue();
            var consumer = new TestConsumer();
            var host = CreateHost(queue.Name, consumer, srv =>
                srv.AddRabbitConsumingContext<AddFoobarHeaderConsumingCtx>());

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
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

            var queue = CreateQueue();
            var consumer = new TestConsumer();
            var logErrorCatcher = new LogErrorCatcher();
            var logErrorCatcherProvider = new LogErrorCatcherProvider(logErrorCatcher);
            var host = CreateHost(queue.Name, consumer, 
                srv => srv.AddRabbitConsumingContext<AddFoobarHeaderConsumingCtx>(),
                log => log.AddProvider(logErrorCatcherProvider));

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
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
    }
}
