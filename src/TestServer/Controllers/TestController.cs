using Microsoft.AspNetCore.Mvc;

namespace TestServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("single")]
        public IActionResult GetSingleTestBox()
        {
            return Ok(TestSimpleMqLogic.Box);
        }

        [HttpGet("single-with-reject")]
        public IActionResult GetSimpleWithReject()
        {
            return Ok(TestSimpleMqLogicWithReject.Box);
        }

        [HttpGet("batch")]
        public IActionResult GetBatch()
        {
            return Ok(TestBatchMqLogic.Box);
        }

        [HttpGet("batch-with-reject")]
        public IActionResult GetBatchWithReject()
        {
            return Ok(TestBatchMqLogicWithReject.Box);
        }
    }
}
