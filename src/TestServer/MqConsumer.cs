using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLab.Mq;
using Tests.Common;

namespace TestServer
{
    public class TestMqMsg
    {

    }

    class TestSimpleMqConsumer : MqConsumer<TestMqMsg, TestSimpleMqLogic>
    {
        private readonly QueueTestCtx _ctx;

        TestSimpleMqConsumer(QueueTestCtx ctx) 
            : base(ctx.QueueName)
        {
            _ctx = ctx;
        }

        public static TestSimpleMqConsumer Create(string queueId)
        {
            return new TestSimpleMqConsumer(TestQueue.CreateWithId(queueId));
        }

        public override void Dispose()
        {
            _ctx.Dispose();
        }
    }

    public class TestSimpleMqLogic : IMqConsumerLogic<TestMqMsg>
    {
        public static TestMqMsg LastMsg { get; private set; }

        public Task Consume(TestMqMsg message)
        {
            LastMsg = message;
            return Task.CompletedTask;
        }
    }

    class TestBatchMqConsumer : MqBatchConsumer<TestMqMsg, TestBatchMqLogic>
    {
        private readonly QueueTestCtx _ctx;

        TestBatchMqConsumer(QueueTestCtx ctx)
            : base(ctx.QueueName, 2)
        {
            _ctx = ctx;
        }

        public static TestBatchMqConsumer Create(string queueId)
        {
            return new TestBatchMqConsumer(TestQueue.CreateWithId(queueId));
        }

        public override void Dispose()
        {
            _ctx.Dispose();
        }
    }

    public class TestBatchMqLogic : IMqBatchConsumerLogic<TestMqMsg>
    {
        public static TestMqMsg[] LastMsgs { get; private set; }
        
        public Task Consume(IEnumerable<TestMqMsg> messages)
        {
            LastMsgs = messages.ToArray();
            return Task.CompletedTask;
        }
    }
}