using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;

namespace Tests.Common
{
    public class TestExchangeFactory : MqExchangeFactory
    {
        public static readonly TestExchangeFactory Fanaut = new TestExchangeFactory(MqExchangeType.Fanout);
        public static readonly TestExchangeFactory Direct = new TestExchangeFactory(MqExchangeType.Direct);

        public TestExchangeFactory(MqExchangeType exchangeType)
            : base(exchangeType, new DefaultMqConnectionProvider(TestMqOptions.Load()))
        {
            Prefix = "mylab:mq:test:";
            AutoDelete = true;
        }
    }
}
