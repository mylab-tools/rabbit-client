using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TestServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetSimple()
        {
            return Ok(TestSimpleMqLogic.LastMsg);
        }

        [HttpGet]
        public IActionResult GetBatch()
        {
            return Ok(TestBatchMqLogic.LastMsgs);
        }
    }
}
