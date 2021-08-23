using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LoadTest.Common;
using Microsoft.Extensions.Hosting;
using MyLab.RabbitClient.Publishing;

namespace LoadTest.Publisher
{
    class TestService : BackgroundService
    {
        private readonly IRabbitPublisher _pub;
        
        public TestService(IRabbitPublisher pub)
        {
            _pub = pub;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //int index = 0;
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    var model = new Model
            //    {
            //        Id = 0 + "-" + index,
            //        Value = index.ToString()
            //    };

            //    _pub.IntoDefault().SendJson(model).Publish();

            //    index++;
            //}

            ThreadPool.SetMinThreads(100, 100);

            Task[] tasks = Enumerable.Repeat("", 100).Select((s, i) => new Task(() => PerformTask(i), stoppingToken)).ToArray();

            foreach (var task in tasks)
                task.Start();

            Task.WaitAll(tasks, stoppingToken);

            return Task.CompletedTask;
        }

        private void PerformTask(in int i)
        {
            int index = 0;
            while (index < 10000)
            {
                var model = new Model
                {
                    Id = i + "-" + index,
                    Value = index.ToString()
                };

                _pub.IntoDefault().SendJson(model).Publish();

                index++;
            }
        }
    }
}