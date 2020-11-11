using System.Threading.Tasks;
using RabbitMQ.Client;

namespace MyLab.Mq.Test
{
    /// <summary>
    /// Specifies emulator of queue with input messages
    /// </summary>
    public interface IInputMessageEmulator
    {
        /// <summary>
        /// Emulates queueing of message 
        /// </summary>
        public Task<FakeMessageQueueProcResult> Queue(object message, string queue, IBasicProperties messageProps = null);
    }
}