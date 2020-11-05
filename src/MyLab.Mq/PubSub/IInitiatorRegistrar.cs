using Microsoft.Extensions.DependencyInjection;
using MyLab.Mq.Test;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Registers MQ processing initiator
    /// </summary>
    public interface IInitiatorRegistrar
    {
        IServiceCollection Register(IServiceCollection serviceCollection);
    }

    public class DefaultQueueListenerRegistrar : IInitiatorRegistrar
    {
        public IServiceCollection Register(IServiceCollection serviceCollection)
        {
            return serviceCollection.AddHostedService<DefaultMqConsumerManager>();
        }
    }

    public class InputMessageEmulatorRegistrar : IInitiatorRegistrar
    {
        public IServiceCollection Register(IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSingleton<IInputMessageEmulator, DefaultInputMessageEmulator>();
        }
    }
}

