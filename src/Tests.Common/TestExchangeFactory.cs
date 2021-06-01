using MyLab.Mq.MqObjects;

namespace Tests.Common
{
    public class TestExchangeFactory : MqExchangeFactory
    {
        public static readonly TestExchangeFactory Fanout = new TestExchangeFactory(MqExchangeType.Fanout);
        public static readonly TestExchangeFactory Direct = new TestExchangeFactory(MqExchangeType.Direct);

        public TestExchangeFactory(MqExchangeType exchangeType)
            : base(exchangeType, TestMqTools.ChannelProvider)
        {
            Prefix = "mylab:mq:test:";
            AutoDelete = true;
        }
    }
}
