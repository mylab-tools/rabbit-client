using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LoadTestServer;
using Microsoft.AspNetCore.Mvc.Testing;
using MyLab.Mq.Communication;
using Newtonsoft.Json.Linq;
using Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace LoadTests
{
    public class ChannelLeakTest  : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
    {
        private readonly WebApplicationFactory<Startup> _appFactory;
        private readonly ITestOutputHelper _output;

        public ChannelLeakTest(WebApplicationFactory<Startup> appFactory, ITestOutputHelper output)
        {
            _appFactory = appFactory;
            _output = output;
        }

        [Fact] 
        public async Task ShouldNotLeakChannels()
        {
            //Arrange
            var mq = TestQueueFactory.Default.CreateWithRandomId();
            TestMqTools.Reset();

            var client = _appFactory.CreateClient();

            //Act
            List<Task> tasks= new List<Task>();

            for (int j = 0; j < 100; j++)
            {

                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(Task.Run(() => Publish(client, mq.Name)));
                }
                Task.WaitAll(tasks.ToArray());
                
                _output.WriteLine($"Ch count: {GetAppChannelCount()}");
            }

            client.Dispose();
            
            await Task.Delay(TimeSpan.FromSeconds(10));

            //Assert
            Assert.Equal(10, await GetChannelCount());
        }

        void Publish(HttpClient client, string queueName)
        {
            client.PostAsync(new Uri($"/test?queue={queueName}", UriKind.Relative), new ByteArrayContent(new byte[0])).Wait();
        }

        int GetAppChannelCount()
        {
            var chp = (MqChannelProvider)_appFactory.Services.GetService(typeof(IMqChannelProvider));
            return chp.ChannelCount;
        }

        async Task<int> GetChannelCount()
        {
            using var webClient = new WebClient
            {
                BaseAddress = "http://localhost:10161",
                Credentials = new NetworkCredential("guest", "guest"),
                QueryString = new NameValueCollection
                {
                    {"page", "1"},
                    {"page_size", "1"}
                }
            };

            var data = await webClient.DownloadDataTaskAsync(new Uri("/api/connections", UriKind.Relative));

            var jo = JObject.Parse(Encoding.UTF8.GetString(data));
            var channelCntStr = (string)jo["items"][0]["channels"];
            return int.Parse(channelCntStr);
        }

        public void Dispose()
        {
            _appFactory.Dispose();
            TestMqTools.Close();
        }
    }
}
