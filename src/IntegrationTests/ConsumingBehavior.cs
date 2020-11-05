using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using MyLab.Mq;
using Newtonsoft.Json;
using TestServer;
using Xunit;

namespace IntegrationTests
{
    public partial class ConsumingBehavior : IClassFixture<WebApplicationFactory<Startup>>
    {
        [Fact]
        public async Task ShouldConsumeSimpleMessage()
        {
            //Arrange
            using var queue = CreateTestQueue();

            var client = CreateTestClientWithSingleConsumer<TestSimpleMqLogic>(queue);

            //Act
            await PublishMessages(queue, "foo");
            
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
        public async Task ShouldRejectSimpleMessage()
        {
            //Arrange
            using var queue = CreateTestQueue();

            var client = CreateTestClientWithSingleConsumer<TestSimpleMqLogicWithReject>(queue);

            //Act
            await PublishMessages(queue, "foo");

            var resp = await client.GetAsync("test/single-with-reject");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            var testBox = JsonConvert.DeserializeObject<SingleMessageTestBox>(respStr);

            //Assert
            Assert.Null(testBox.AckMsg);
            Assert.NotNull(testBox.RejectedMsg);
            Assert.Equal("foo", testBox.RejectedMsg.Payload.Content);

            await PrintStatus(client);
        }

        [Fact]
        public async Task ShouldConsumeMessageBatch()
        {
            //Arrange
            using var queue = CreateTestQueue();

            var client = CreateTestClientWithBatchConsumer<TestBatchMqLogic>(queue);

            //Act
            await PublishMessages(queue, "foo", "bar");

            var resp = await client.GetAsync("test/batch");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            var testBox = JsonConvert.DeserializeObject<BatchMessageTestBox>(respStr);

            //Assert
            Assert.Null(testBox.RejectedMsgs);
            Assert.NotNull(testBox.AckMsgs);
            Assert.Equal(2, testBox.AckMsgs.Length);
            Assert.Contains(testBox.AckMsgs, m => m.Payload.Content == "foo");
            Assert.Contains(testBox.AckMsgs, m => m.Payload.Content == "bar");

            await PrintStatus(client);
        }

        [Fact]
        public async Task ShouldRejectMessageBatch()
        {
            //Arrange
            using var queue = CreateTestQueue();

            var client = CreateTestClientWithBatchConsumer<TestBatchMqLogicWithReject>(queue);

            //Act
            await PublishMessages(queue, "foo", "bar");

            var resp = await client.GetAsync("test/batch-with-reject");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            var testBox = JsonConvert.DeserializeObject<BatchMessageTestBox>(respStr);

            //Assert
            Assert.Null(testBox.AckMsgs);
            Assert.NotNull(testBox.RejectedMsgs);
            Assert.Equal(2, testBox.RejectedMsgs.Length);
            Assert.Contains(testBox.RejectedMsgs, m => m.Payload.Content == "foo");
            Assert.Contains(testBox.RejectedMsgs, m => m.Payload.Content == "bar");

            await PrintStatus(client);
        }

        [Fact]
        public async Task ShouldUseIncomingMessageScopeServices()
        {
            //Arrange
            using var queue = CreateTestQueue();

            var client = CreateTestClientWithSingleConsumer<MqLogicWithScopedDependency>(queue);

            //Act
            await PublishMessages(queue, "foo");

            var resp = await client.GetAsync("test/from-scope");
            var respStr = await resp.Content.ReadAsStringAsync();

            _output.WriteLine(respStr);
            resp.EnsureSuccessStatusCode();

            //Assert
            Assert.Equal("foo", respStr);

            await PrintStatus(client);
        }
    }
}
