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

        public Task Consume(MqMessage<TestMqMsg> message)
        {
            Box.AckMsg = message;
            return Task.CompletedTask;
        }
    }

    public class TestSimpleMqLogicWithReject : IMqConsumerLogic<TestMqMsg>
    {
        public static readonly SingleMessageTestBox Box = new SingleMessageTestBox();

        public Task Consume(MqMessage<TestMqMsg> message)
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

        public Task Consume(IEnumerable<MqMessage<TestMqMsg>> messages)
        {
            Box.AckMsgs = messages.ToArray();
            return Task.CompletedTask;
        }
    }

    public class TestBatchMqLogicWithReject : IMqBatchConsumerLogic<TestMqMsg>
    {
        public static readonly BatchMessageTestBox Box = new BatchMessageTestBox();

        public Task Consume(IEnumerable<MqMessage<TestMqMsg>> messages)
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
        public MqMessage<TestMqMsg> RejectedMsg { get; set; }
        public MqMessage<TestMqMsg> AckMsg { get; set; }
    }

    public class BatchMessageTestBox
    {
        public MqMessage<TestMqMsg>[] RejectedMsgs { get; set; }
        public MqMessage<TestMqMsg>[] AckMsgs { get; set; }
    }

    public class MqLogicWithScopedDependency : IMqConsumerLogic<TestMqMsg>
    {
        private readonly ScopedService _scopedService;
        public static string BuffVal { get; set; }

        public MqLogicWithScopedDependency(ScopedService scopedService)
        {
            _scopedService = scopedService;
        }
        public Task Consume(MqMessage<TestMqMsg> message)
        {
            BuffVal = _scopedService.Get<TestMqMsg>().Payload.Content;

            return Task.CompletedTask;
        }
    }
}
