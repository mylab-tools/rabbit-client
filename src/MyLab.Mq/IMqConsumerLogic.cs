using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLab.Mq
{
    /// <summary>
    /// Represent message queue consumer
    /// </summary>
    public interface IMqConsumerLogic<in TMsg>
    {
        Task Consume(TMsg message);
    }

    /// <summary>
    /// Represent batch message queue consumer
    /// </summary>
    public interface IMqBatchConsumerLogic<in TMsg>
    {
        Task Consume(IEnumerable<TMsg> message);
    }
}
