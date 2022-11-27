using Microsoft.AspNetCore.Mvc;
using UIQ_CronTab.Models.ApiRequest;
using UIQ_CronTab.Services.Interfaces;

namespace UIQ_CronTab
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhaseLogController : ControllerBase
    {
        private readonly IPhaseLogService _phaseLogService;

        public PhaseLogController(IPhaseLogService phaseLogService)
        {
            _phaseLogService = phaseLogService;
        }

        [HttpPost]
        public async Task Post([FromBody] PhaseLogRequest request)
        {
            await _phaseLogService.PhaseLogAsync(request);
        }
    }
}