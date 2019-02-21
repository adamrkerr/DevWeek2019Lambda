using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DevWeek2019Lambda.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnvironmentController : ControllerBase
    {
        private readonly IConfiguration _config;

        public EnvironmentController(IConfiguration config)
        {
            _config = config;
        }

        // GET api/override
        [HttpGet("override")]
        public IActionResult Get()
        {            
            return Ok(_config.GetValue<string>("EnvironmentOverride"));
        }
    }
}