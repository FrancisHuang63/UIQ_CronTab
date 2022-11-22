using Microsoft.Extensions.Options;
using UIQ_CronTab.Enums;

namespace UIQ_CronTab.Services
{
    public class MySqlDataBaseNcsLogService : MySqlDataBaseService
    {
        public MySqlDataBaseNcsLogService(IOptions<ConnectoinStringOption> connectoinStringOption) : base(connectoinStringOption, DataBaseEnum.NcsLog)
        {
        }
    }
}