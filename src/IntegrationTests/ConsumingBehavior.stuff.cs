using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.RabbitClient.Consuming;
using MyLab.RabbitClient.Model;
using RabbitMQ.Client.Events;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public partial class ConsumingBehavior
    {
        private readonly ITestOutputHelper _output;

        public ConsumingBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        private class AddFoobarHeaderConsumingCtx : IConsumingContext
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

        RabbitQueue CreateQueue()
        {
            return new RabbitQueueFactory(TestTools.ChannelProvider)
            {
                AutoDelete = true,
                Prefix = "test"
            }.CreateWithRandomId();
        }

        IHost CreateHost(string queueName, TestConsumer consumer, Action<IServiceCollection> srvAct = null, Action<ILoggingBuilder> logAct = null)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(srv =>
                {
                    srv
                        .AddRabbit()
                        .ConfigureRabbit(TestTools.OptionsConfigureAct)
                        .AddRabbitConsumer(queueName, consumer)
                        .AddLogging(l =>
                        {
                            l.AddXUnit(_output).AddFilter(l => true);
                            logAct?.Invoke(l);
                        });

                    srvAct?.Invoke(srv);
                })
                .Build();
        }

        private class NullConsumingCtx : IConsumingContext
        {
            public IDisposable Set(BasicDeliverEventArgs deliverEventArgs)
            {
                return null;
            }
        }

        private class TestConsumer : RabbitConsumer<TestEntity>
        {
            public Queue<ConsumedMessage<TestEntity>> LastMessages { get; } = new Queue<ConsumedMessage<TestEntity>>();

            protected override Task ConsumeMessageAsync(ConsumedMessage<TestEntity> consumedMessage)
            {
                LastMessages.Enqueue(consumedMessage);

                return Task.CompletedTask;
            }
        }

        private class TestEntity
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }
    }
}