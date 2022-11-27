using Microsoft.AspNetCore.Mvc;
using UIQ_CronTab.Services.Interfaces;

namespace UIQ_CronTab.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MakeDailyLogController : ControllerBase
    {
        private readonly IMakeDailyLogService _makeDailyLogService;

        public MakeDailyLogController(IMakeDailyLogService makeDailyLogService)
        {
            _makeDailyLogService = makeDailyLogService;
        }

        [HttpPost]
        public async Task Post()
        {
            _makeDailyLogService.MakeDailyLogAsync();
        }
    }
}