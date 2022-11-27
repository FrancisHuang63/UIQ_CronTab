using Microsoft.AspNetCore.Mvc;
using UIQ_CronTab.Services.Interfaces;

namespace UIQ_CronTab.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParseLogController : ControllerBase
    {
        private readonly IParseLogService _parseLogService;

        public ParseLogController(IParseLogService parseLogService)
        {
            _parseLogService = parseLogService;
        }

        [HttpPost]
        public async Task Post()
        {
            await _parseLogService.ParseLogAsync();
        }
    }
}