using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLab.Mq;

namespace TestServer
{
    public class TestSimpleMqLogic : IMqConsumerLogic<TestMqMsg>
    {
        public static readonly SingleMessageTestBox Box = new SingleMessageTestBox();

        public Task Consume(TestMqMsg message)
        {
            Box.AckMsg = message;
            return Task.CompletedTask;
        }
    }

    public class TestSimpleMqLogicWithReject : IMqConsumerLogic<TestMqMsg>
    {
        public static readonly SingleMessageTestBox Box = new SingleMessageTestBox();

        public Task Consume(TestMqMsg message)
        {
            if (Box.RejectedMsg == null)
            {
                Box.RejectedMsg = message;
                throw new Exception("Reject");
            }

            Box.AckMsg = message;
            return Task.CompletedTask;
        }
    }

    public class TestBatchMqLogic : IMqBatchConsumerLogic<TestMqMsg>
    {
        public static readonly BatchMessageTestBox Box = new BatchMessageTestBox();

        public Task Consume(IEnumerable<TestMqMsg> messages)
        {
            Box.AckMsgs = messages.ToArray();
            return Task.CompletedTask;
        }
    }

    public class TestBatchMqLogicWithReject : IMqBatchConsumerLogic<TestMqMsg>
    {
        public static readonly BatchMessageTestBox Box = new BatchMessageTestBox();

        public Task Consume(IEnumerable<TestMqMsg> messages)
        {
            if (Box.RejectedMsgs == null)
            {
                Box.RejectedMsgs = messages.ToArray();
                throw new Exception("Reject");
            }

            Box.AckMsgs = messages.ToArray();
            return Task.CompletedTask;
        }
    }

    public class SingleMessageTestBox
    {
        public TestMqMsg RejectedMsg { get; set; }
        public TestMqMsg AckMsg { get; set; }
    }

    public class BatchMessageTestBox
    {
        public TestMqMsg[] RejectedMsgs { get; set; }
        public TestMqMsg[] AckMsgs { get; set; }
    }
}
