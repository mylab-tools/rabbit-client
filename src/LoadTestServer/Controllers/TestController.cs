using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyLab.Mq;
using MyLab.Mq.PubSub;

namespace LoadTestServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IMqPublisher _publisher;

        public TestController(IMqPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        public IActionResult Post([FromQuery] string queue)
        {
            Debug.WriteLine("====>>>");

            var msg = new OutgoingMqEnvelop<string>
            {
                PublishTarget = new PublishTarget
                {
                    Routing = queue
                },
                Message = new MqMessage<string>("foo")
            };

            _publisher.Publish(msg);

            return Ok();
        }
    }
}
