using System;
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
                        registrar.RegisterConsumer(TestSimpleMqConsumer.Create(queueId));
                    })
                        .AddSingleton<IMqConnectionProvider, TestConnectionProvider>();
                });
            }).CreateClient();

            using var queueCtx = TestQueue.CreateWithId(queueId);
            var sender = queueCtx.CreateSender();

            //Act
            sender.Queue(new TestMqMsg { Content = "foo" });

            var resp = await client.GetAsync("test/single");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            var respMsg = JsonConvert.DeserializeObject<TestMqMsg>(respStr);

            //Assert
            Assert.Equal("foo", respMsg.Content);
        }
    }
}
