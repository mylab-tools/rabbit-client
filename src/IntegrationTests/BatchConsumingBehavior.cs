using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MyLab.Mq;
using Newtonsoft.Json;
using Tests.Common;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class BatchConsumingBehavior : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _appFactory;
        private readonly ITestOutputHelper _output;

        public BatchConsumingBehavior(WebApplicationFactory<Startup> appFactory, ITestOutputHelper output)
        {
            _appFactory = appFactory;
            _output = output;
        }

        [Fact]
        public async Task ShouldConsumeSimpleMessage()
        {
            //Arrange
            var queueId = Guid.NewGuid().ToString("N");
            var client = _appFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddMqConsuming(registrar =>
                    {
                        registrar.RegisterConsumer(TestBatchMqConsumer<TestBatchMqLogic>.Create(queueId));
                    })
                        .AddSingleton<IMqConnectionProvider, TestConnectionProvider>();
                });
            }).CreateClient();

            using var queueCtx = TestQueue.CreateWithId(queueId);
            var sender = queueCtx.CreateSender();

            //Act
            sender.Queue(new TestMqMsg { Content = "foo" });
            sender.Queue(new TestMqMsg { Content = "bar" });
            await Task.Delay(500);

            var resp = await client.GetAsync("test/batch");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            var testBox = JsonConvert.DeserializeObject<BatchMessageTestBox>(respStr);
            
            //Assert
            Assert.Null(testBox.RejectedMsgs);
            Assert.NotNull(testBox.AckMsgs);
            Assert.Equal(2, testBox.AckMsgs.Length);
            Assert.Contains(testBox.AckMsgs, m => m.Content == "foo");
            Assert.Contains(testBox.AckMsgs, m => m.Content == "bar");
        }

        [Fact]
        public async Task ShouldRejectMessage()
        {
            //Arrange
            var queueId = Guid.NewGuid().ToString("N");
            var client = _appFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddMqConsuming(registrar =>
                        {
                            registrar.RegisterConsumer(TestBatchMqConsumer<TestBatchMqLogicWithReject>.Create(queueId));
                        })
                        .AddSingleton<IMqConnectionProvider, TestConnectionProvider>();
                });
            }).CreateClient();

            using var queueCtx = TestQueue.CreateWithId(queueId);
            var sender = queueCtx.CreateSender();

            //Act
            sender.Queue(new TestMqMsg { Content = "foo" });
            sender.Queue(new TestMqMsg { Content = "bar" });
            await Task.Delay(500);

            var resp = await client.GetAsync("test/batch-with-reject");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            var testBox = JsonConvert.DeserializeObject<BatchMessageTestBox>(respStr);

            //Assert
            Assert.NotNull(testBox.AckMsgs);
            Assert.Equal(2, testBox.AckMsgs.Length);
            Assert.Contains(testBox.AckMsgs, m => m.Content == "foo");
            Assert.Contains(testBox.AckMsgs, m => m.Content == "bar");
            Assert.NotNull(testBox.RejectedMsgs);
            Assert.Equal(2, testBox.RejectedMsgs.Length);
            Assert.Contains(testBox.RejectedMsgs, m => m.Content == "foo");
            Assert.Contains(testBox.RejectedMsgs, m => m.Content == "bar");
        }
    }
}
