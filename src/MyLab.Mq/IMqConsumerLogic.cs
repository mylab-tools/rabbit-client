using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLab.Mq
{
    /// <summary>
    /// Represent messages queue consumer
    /// </summary>
    public interface IMqConsumerLogic<in TMsg>
    {
        Task Consume(TMsg message);
    }

    /// <summary>
    /// Represent batch messages queue consumer
    /// </summary>
    public interface IMqBatchConsumerLogic<in TMsg>
    {
        Task Consume(IEnumerable<TMsg> messages);
    }
}
