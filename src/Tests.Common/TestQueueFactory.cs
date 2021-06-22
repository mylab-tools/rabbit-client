using MyLab.Mq.MqObjects;

namespace Tests.Common
{

    public class TestQueueFactory : MqQueueFactory
    {
        public static readonly TestQueueFactory Default = new TestQueueFactory();

        public TestQueueFactory()
            :base(TestMqTools.ChannelProvider)
        {
            Prefix = "mylab:mq:test:";
            AutoDelete = true;
        }
    }
}
