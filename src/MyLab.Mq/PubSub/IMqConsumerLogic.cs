using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLab.Mq.PubSub
{
    /// <summary>
    /// Represent messages queue consumer
    /// </summary>
    public interface IMqConsumerLogic<TMsgPayload>
    {
        Task Consume(MqMessage<TMsgPayload> message);
    }

    /// <summary>
    /// Represent batch messages queue consumer
    /// </summary>
    public interface IMqBatchConsumerLogic<TMsgPayload>
    {
        Task Consume(IEnumerable<MqMessage<TMsgPayload>> messages);
    }
}
