using Microsoft.Extensions.Options;
using UIQ_CronTab.Enums;

namespace UIQ_CronTab.Services
{
    public class MySqlDataBaseNcsUiService : MySqlDataBaseService
    {
        public MySqlDataBaseNcsUiService(IOptions<ConnectoinStringOption> connectoinStringConfigure) : base(connectoinStringConfigure, DataBaseEnum.NcsUi)
        {
        }
    }
}
