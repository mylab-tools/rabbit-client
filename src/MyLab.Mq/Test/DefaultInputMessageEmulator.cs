using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.LogDsl;
using MyLab.Mq.PubSub;
using RabbitMQ.Client;

namespace MyLab.Mq.Test
{
    class DefaultInputMessageEmulator : IInputMessageEmulator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, MqConsumer> _consumers;
        private readonly DslLogger _log;

        public DefaultInputMessageEmulator(
            IMqInitialConsumerRegistry initialConsumerRegistry,
            IServiceProvider serviceProvider,
            ILogger<DefaultInputMessageEmulator> logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _consumers = initialConsumerRegistry
                .GetConsumers(_serviceProvider)
                .ToDictionary(c => c.Queue, c => c);
            _log = logger?.Dsl();
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
            catch (Exception e)
            {
                _log?.Error(e).Write();
            }

            return ctx.Result;
        }
    }
}
