using Microsoft.AspNetCore.Mvc;

namespace todo_app.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var portEnv = Environment.GetEnvironmentVariable("PORT");

            return Ok($"Server started in port {portEnv}");
        }
    }
}
