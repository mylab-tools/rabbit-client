using System.Text;
using System.Threading.Tasks;
using MyLab.MqApp;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace IntegrationTests.Tools
{
    static class TestMqSender
    {
        public static void Queue(object message)
        {
            var factory = TestQueue.CreateConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            //var msgDesc = MqMsgModelDesc.GetFromModel(message);

            string messageStr = JsonConvert.SerializeObject(message);
            var messageBin = Encoding.UTF8.GetBytes(messageStr);

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey:TestQueue.Name,
                body: messageBin);
        }
    }
}
