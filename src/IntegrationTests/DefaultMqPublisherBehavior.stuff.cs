using MyLab.Mq;
using MyLab.StatusProvider;
using Tests.Common;

namespace IntegrationTests
{
    public partial class DefaultMqPublisherBehavior
    {
        private const string BoundQueue = "mylab:mq:test:DefaultMqPublisherBehavior";

        private MqQueue CreateTestQueue() => TestQueueFactory.Default.CreateWithName(BoundQueue);

        IMqPublisher CreateTestPublisher(IAppStatusService appStatusService = null) => new DefaultMqPublisher(
            new DefaultMqConnectionProvider(TestMqOptions.Load()),
            null, appStatusService);

        [Mq(Routing = BoundQueue)]
        private class Msg
        {
            public string Value { get; set; }
        }
    }
}