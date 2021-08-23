using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoadTest.Common;
using MyLab.RabbitClient.Consuming;

namespace LoadTest.Listener
{
    class TestConsumer : RabbitConsumer<Model>
    {
        private static ulong _counter = 0;
        private static ulong _lastCounter = 0;
        private static DateTime _lastCheckDt = DateTime.Now;

        protected override Task ConsumeMessageAsync(ConsumedMessage<Model> consumedMessage)
        {
            _counter++;

            var now = DateTime.Now;
            var elapsed = now - _lastCheckDt;

            if (elapsed.TotalSeconds > 2)
            {
                var cPos = Console.GetCursorPosition();
                var speed = (_counter - _lastCounter) / elapsed.TotalSeconds;
                
                Console.WriteLine($"Consuming speed: {speed} msg/sec");

                Console.SetCursorPosition(cPos.Left, cPos.Top);

                _lastCounter = _counter;
                _lastCheckDt = DateTime.Now;
            }

            return Task.CompletedTask;
        }

        class SecondReport
        {
            public TimeSpan Time { get; set; }
            public ulong Count { get; set; }
        }
    }
}