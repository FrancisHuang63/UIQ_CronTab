using UIQ_CronTab.Models.ApiRequest;

namespace UIQ_CronTab.Services.Interfaces
{
    public interface IPhaseLogService
    {
        public Task PhaseLogAsync(PhaseLogRequest request);
    }
}
