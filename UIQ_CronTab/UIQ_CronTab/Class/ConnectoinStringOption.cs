using UIQ_CronTab.Enums;

namespace UIQ_CronTab
{
    public class ConnectoinStringOption
    {
        public string NcsUi { get; set; }

        public string NcsLog { get; set; }

        public string GetConnectoinString(DataBaseEnum dataBase)
        {
            switch (dataBase)
            {
                case DataBaseEnum.NcsUi:
                    return NcsUi;
                case DataBaseEnum.NcsLog:
                    return NcsLog;
                default:
                    return NcsUi;
            }
        }
    }
}
