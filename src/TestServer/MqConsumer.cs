using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLab.Mq;
using Tests.Common;

namespace TestServer
{
    public class TestMqMsg
    {
        public string Content { get; set; }
    }

    public class TestSimpleMqConsumer<TLogic> : MqConsumer<TestMqMsg, TLogic>
        where TLogic : IMqConsumerLogic<TestMqMsg>
    {
        private readonly QueueTestCtx _ctx;

        TestSimpleMqConsumer(QueueTestCtx ctx) 
            : base(ctx.QueueName)
        {
            _ctx = ctx;
            StatusIgnore = true;
        }

        public static TestSimpleMqConsumer<TLogic> Create(string queueId)
        {
            return new TestSimpleMqConsumer<TLogic>(TestQueue.CreateWithId(queueId));
        }

        public override void Dispose()
        {
            _ctx.Dispose();
        }
    }

    public class TestBatchMqConsumer<TLogic> : MqBatchConsumer<TestMqMsg, TLogic>
        where TLogic : IMqBatchConsumerLogic<TestMqMsg>
    {
        private readonly QueueTestCtx _ctx;

        TestBatchMqConsumer(QueueTestCtx ctx)
            : base(ctx.QueueName, 2)
        {
            _ctx = ctx;
            StatusIgnore = true;
        }

        public static TestBatchMqConsumer<TLogic> Create(string queueId)
        {
            return new TestBatchMqConsumer<TLogic>(TestQueue.CreateWithId(queueId));
        }

        public override void Dispose()
        {
            _ctx.Dispose();
        }
    }
}