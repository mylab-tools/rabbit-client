using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLab.Mq.MqObjects;
using MyLab.Mq.PubSub;
using Tests.Common;
using TestServer;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public partial class ConsumingBehavior
    {
        private readonly WebApplicationFactory<Startup> _appFactory;
        private readonly ITestOutputHelper _output;

        public ConsumingBehavior(WebApplicationFactory<Startup> appFactory, ITestOutputHelper output)
        {
            _appFactory = appFactory;
            _output = output;
        }

        private async Task PrintStatus(HttpClient client)
        {
            var resp = await client.GetAsync("status");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine("");
            if (!resp.IsSuccessStatusCode)
            {
                _output.WriteLine("Get status error: " + resp.StatusCode);
            }
            else
            {
                _output.WriteLine("STATUS: ");
            }

            _output.WriteLine(respStr);
        }

        private MqQueue CreateTestQueue() => TestQueueFactory.Default.CreateWithRandomId();

        private HttpClient CreateTestClientWithSingleConsumer<T>(MqQueue queue, ITestOutputHelper output)
            where T : class, IMqConsumerLogic<TestMqMsg>
        {
            return CreateTestClient(new MqConsumer<TestMqMsg, T>(queue.Name), output);
        }

        private HttpClient CreateTestClientWithBatchConsumer<T>(MqQueue queue, ITestOutputHelper output, ushort size = 2)
            where T : class, IMqBatchConsumerLogic<TestMqMsg>
        {
            return CreateTestClient(new MqBatchConsumer<TestMqMsg, T>(queue.Name, size)
            {
                BatchTimeout = TimeSpan.FromSeconds(2)
            }, output);
        }

        private HttpClient CreateTestClient(MqConsumer consumer, ITestOutputHelper output)
        {
            return _appFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddLogging(b => b
                        .SetMinimumLevel(LogLevel.Trace)
                        .AddFilter(level => level >= LogLevel.Debug)
                        .AddXUnit(output));
                    services.AddMqConsuming(registrar =>
                    {
                        registrar.RegisterConsumer(consumer);
                    })
                        .ConfigureMq(TestMqTools.ConfigureAction);
                });
            }).CreateClient();
        }

        private async Task PublishMessages(MqQueue queue, params string[] msgs)
        {
            foreach (var msg in msgs)
            {
                queue.Publish(new TestMqMsg {Content = msg});
            }

            await Task.Delay(500);
        }
    }
}