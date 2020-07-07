using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace MyLab.Mq
{
    /// <summary>
    /// Specifies emulator of queue with input messages
    /// </summary>
    public interface IInputMessageEmulator
    {
        public Task<FakeMessageQueueProcResult> Queue(object message, string queue, IBasicProperties messageProps = null);
    }

    /// <summary>
    /// Contains fake queue message processing result
    /// </summary>
    public class FakeMessageQueueProcResult
    {
        /// <summary>
        /// Is there was acknowledge
        /// </summary>
        public bool Acked { get; set; }

        /// <summary>
        /// Is there was rejected
        /// </summary>
        public bool Rejected { get; set; }

        /// <summary>
        /// Exception which is reason of rejection
        /// </summary>
        public Exception RejectionException { get; set; }

        /// <summary>
        /// Requeue flag value
        /// </summary>
        public bool RequeueFlag { get; set; }
    }

    class DefaultInputMessageEmulator : IInputMessageEmulator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, MqConsumer> _consumers;

        public DefaultInputMessageEmulator(
            IMqConsumerRegistry consumerRegistry,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _consumers = new Dictionary<string, MqConsumer>(consumerRegistry.GetConsumers());
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
