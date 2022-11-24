namespace UIQ_CronTab.Models
{
    public class MonitoringInfo
    {
        public string model { get; set; }
        public string member { get; set; }
        public string nickname { get; set; }
        public string account { get; set; }
        public int? lid { get; set; }
        public string dtg { get; set; }
        public string run { get; set; }
        public string complete_run_type { get; set; }
        public string run_type { get; set; }
        public string cron_mode { get; set; }
        public int? typhoon_mode { get; set; }
        public int? manual { get; set; }
        public int? start_flag { get; set; }
        public int? stage_flag { get; set; }
        public string status { get; set; }
        public string sms_name { get; set; }
        public DateTime? sms_time { get; set; }
        public DateTime? start_time { get; set; }
        public DateTime? end_time { get; set; }
        public DateTime? pre_start { get; set; }
        public DateTime? pre_end { get; set; }
        public DateTime? run_end { get; set; }
        public string shell_name { get; set; }
        public DateTime? shell_time { get; set; }
        public string error_message { get; set; }

        [Notmapped]
        public string comment { get; set; }
    }
}