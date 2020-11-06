using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyLab.Mq.PubSub;
using RabbitMQ.Client;

namespace MyLab.Mq.Test
{
    class DefaultInputMessageEmulator : IInputMessageEmulator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, MqConsumer> _consumers;

        public DefaultInputMessageEmulator(
            IMqInitialConsumerRegistry initialConsumerRegistry,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _consumers = new Dictionary<string, MqConsumer>(initialConsumerRegistry.GetConsumers());
        }

        public async Task<FakeMessageQueueProcResult> Queue(object message, string queue, IBasicProperties messageProps = null)
        {
            if (!_consumers.TryGetValue(queue, out var consumer))
            {
                return null;
            }

            using var scope = _serviceProvider.CreateScope();
            

            var msgAccessorCore = scope.ServiceProvider.GetService<IMqMessageAccessorCore>();
            msgAccessorCore.SetScopedMessage(message, messageProps);

            var ctx = new FakeConsumingContext(message, _serviceProvider);

            try
            {
                await consumer.Consume(ctx);
            }
            catch 
            {
            }

            return ctx.Result;
        }
    }
}
