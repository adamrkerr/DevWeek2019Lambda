using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DevWeek2019Lambda.Controllers
{
    [Route("api/[controller]")]
    public class LoggingController : Controller
    {
        private readonly ILogger<LoggingController> _logger;

        public LoggingController(ILogger<LoggingController> logger) {
            this._logger = logger;
        }

        [HttpGet("gateway")]
        public IActionResult GetAPIGateway(string msg)
        {
            var builder = new StringBuilder();

            //1024 bytes * 1024 kb * 8 mb should overflow lambda
            for (int i = 0; i < (1024 * 1024 * 8); i++)
            {
                builder.Append("A");
            }

            return Ok(builder.ToString());
        }

        // GET api/console/{msg}
        [HttpGet("console/{msg}")]
        public IActionResult GetConsole(string msg)
        {
            Console.WriteLine($"CONSOLE: {msg}");

            return Ok($"Message logged to console: {msg}");
        }

        // GET api/custom/{msg}
        [HttpGet("custom/{msg}")]
        public IActionResult GetCustom(string msg)
        {
            //Messages may take a while to appear in this log
            _logger.LogCritical("LOGGER: {0}", msg);

            return Ok($"Message logged to cloudwatch: {msg}");
        }

    }
}
