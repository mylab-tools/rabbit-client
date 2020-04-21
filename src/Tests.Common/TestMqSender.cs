using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Tests.Common
{
    public class TestMqSender
    {
        private readonly string _queue;

        public TestMqSender(string queue)
        {
            _queue = queue;
        }

        public void Queue(object message)
        {
            var factory = TestQueue.CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            //var msgDesc = MqMsgModelDesc.GetFromModel(message);

            string messageStr = JsonConvert.SerializeObject(message);
            var messageBin = Encoding.UTF8.GetBytes(messageStr);

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: _queue,
                body: messageBin);
        }
    }
}
