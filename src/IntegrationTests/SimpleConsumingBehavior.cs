using System;
using System.Net.Http;
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
    public class SimpleConsumingBehavior : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _appFactory;
        private readonly ITestOutputHelper _output;

        public SimpleConsumingBehavior(WebApplicationFactory<Startup> appFactory, ITestOutputHelper output)
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
                        registrar.RegisterConsumer(TestSimpleMqConsumer<TestSimpleMqLogic>.Create(queueId));
                    })
                        .AddSingleton<IMqConnectionProvider, TestConnectionProvider>();
                });
            }).CreateClient();

            using var queueCtx = TestQueue.CreateWithId(queueId);
            var sender = queueCtx.CreateSender();

            //Act
            sender.Queue(new TestMqMsg { Content = "foo" });
            await Task.Delay(500);

            var resp = await client.GetAsync("test/single");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            var testBox = JsonConvert.DeserializeObject<SingleMessageTestBox>(respStr);

            //Assert
            Assert.Equal("foo", testBox.AckMsg.Payload.Content);
            Assert.Null(testBox.RejectedMsg);

            await PrintStatus(client);
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
                            registrar.RegisterConsumer(TestSimpleMqConsumer<TestSimpleMqLogicWithReject>.Create(queueId));
                        })
                        .AddSingleton<IMqConnectionProvider, TestConnectionProvider>();
                });
            }).CreateClient();

            using var queueCtx = TestQueue.CreateWithId(queueId);
            var sender = queueCtx.CreateSender();

            //Act
            sender.Queue(new TestMqMsg { Content = "foo" });
            await Task.Delay(500);

            var resp = await client.GetAsync("test/single-with-reject");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            var testBox = JsonConvert.DeserializeObject<SingleMessageTestBox>(respStr);

            //Assert
            Assert.NotNull(testBox.AckMsg);
            Assert.Equal("foo", testBox.AckMsg.Payload.Content);
            Assert.NotNull(testBox.RejectedMsg);
            Assert.Equal("foo", testBox.RejectedMsg.Payload.Content);

            await PrintStatus(client);
        }

        async Task PrintStatus(HttpClient client)
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
    }
}
