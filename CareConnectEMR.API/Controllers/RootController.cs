using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CareConnectEMR.API.Controllers
{
    [Route("/")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class RootController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Redirect("/health");
        }
    }
}
