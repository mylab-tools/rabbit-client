using MyLab.Mq;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;
using MyLab.Mq.PubSub;
using MyLab.StatusProvider;
using Tests.Common;

namespace IntegrationTests
{
    public partial class DefaultMqPublisherBehavior
    {
        private const string BoundQueue = "mylab:mq:test:DefaultMqPublisherBehavior";

        private MqQueue CreateTestQueue() => TestQueueFactory.Default.CreateWithName(BoundQueue);

        IMqPublisher CreateTestPublisher(IAppStatusService appStatusService = null) => new DefaultMqPublisher(
            new MqChannelProvider(new DefaultMqConnectionProvider(TestMqOptions.Load())),
            null, appStatusService);

        [Mq(Routing = BoundQueue)]
        private class Msg
        {
            public string Value { get; set; }
        }
    }
}