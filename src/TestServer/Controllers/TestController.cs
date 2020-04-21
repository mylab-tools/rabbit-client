using Microsoft.AspNetCore.Mvc;

namespace TestServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("single")]
        public IActionResult GetSimple()
        {
            return Ok(TestSimpleMqLogic.LastMsg);
        }

        [HttpGet("batch")]
        public IActionResult GetBatch()
        {
            return Ok(TestBatchMqLogic.LastMsgs);
        }
    }
}
