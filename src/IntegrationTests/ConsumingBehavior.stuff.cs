using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyLab.Log.Dsl;
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

        private class CatchUnhandledExceptionCtx : IConsumingContext
        {
            public IConsumingContextInstance Set(BasicDeliverEventArgs deliverEventArgs)
            {
                return new CatchUnhandledExceptionCtxInstance();
            }
        }

        class CatchUnhandledExceptionCtxInstance : IConsumingContextInstance
        {
            public static Exception LastException { get; set; }

            public void Dispose()
            {

            }

            public void NotifyUnhandledException(Exception exception)
            {
                LastException = exception;
            }
        }

        private class AddHeaderConsumingCtx : IConsumingContext
        {
            public string Key { get; }
            public string Value { get; }

            public AddHeaderConsumingCtx()
                :this("foo", "bar")
            {

            }

            public AddHeaderConsumingCtx(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public IConsumingContextInstance Set(BasicDeliverEventArgs deliverEventArgs)
            {
                deliverEventArgs.BasicProperties.Headers = new Dictionary<string, object>
                {
                    {Key, Value}
                };

                return null;
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

        IHost CreateHost(string queueName, IRabbitConsumer consumer, Action<IServiceCollection> srvAct = null, Action<ILoggingBuilder> logAct = null)
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
                            l.AddXUnit(_output)
                                .AddFilter(l => true);
                            logAct?.Invoke(l);
                        });

                    srvAct?.Invoke(srv);
                })
                .Build();
        }

        private class NullConsumingCtx : IConsumingContext
        {
            public IConsumingContextInstance Set(BasicDeliverEventArgs deliverEventArgs)
            {
                return null;
            }
        }

        private class BrokenTestConsumer : RabbitConsumer<TestEntity>
        {
            private readonly Exception _exceptionForThrow;

            public BrokenTestConsumer(Exception exceptionForThrow)
            {
                _exceptionForThrow = exceptionForThrow;
            }

            protected override Task ConsumeMessageAsync(ConsumedMessage<TestEntity> consumedMessage)
            {
                throw _exceptionForThrow;
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

        private class BrokenConsumer : RabbitConsumer<TestEntity>
        {
            protected override Task ConsumeMessageAsync(ConsumedMessage<TestEntity> consumedMessage)
            {
                throw new Exception("Test exception");
            }
        }

        private class TestEntity
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }
    }
}