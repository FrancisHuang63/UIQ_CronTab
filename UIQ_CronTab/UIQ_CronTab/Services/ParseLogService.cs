using System.Text.RegularExpressions;
using UIQ_CronTab.Models;
using UIQ_CronTab.Services.Interfaces;

namespace UIQ_CronTab.Services
{
    public class ParseLogService : IParseLogService
    {
        private readonly ISshCommandService _sshCommandService;
        private readonly IDataBaseService _dataBaseNcsUiService;
        private readonly IDataBaseService _dataBaseNcsLogService;
        private readonly ILogFileService _logFileService;

        private readonly string _systemName;
        private readonly string _shellPath;
        private readonly string _uiPath;

        public ParseLogService(ISshCommandService sshCommandService, IEnumerable<IDataBaseService> dataBaseServices
            , ILogFileService logFileService, IConfiguration configuration)
        {
            _sshCommandService = sshCommandService;
            _dataBaseNcsUiService = dataBaseServices.Single(x => x.DataBase == Enums.DataBaseEnum.NcsUi);
            _dataBaseNcsLogService = dataBaseServices.Single(x => x.DataBase == Enums.DataBaseEnum.NcsLog);
            _logFileService = logFileService;

            _systemName = configuration.GetValue<string>("SystemName");
            _shellPath = configuration.GetValue<string>("ShellPath");
            _uiPath = configuration.GetValue<string>("UiPath");
        }

        public async Task ParseLog()
        {
            var all_acc_path = await _sshCommandService.RunCommandAsync($"sh {_uiPath}shell/log_path.sh");
            await _sshCommandService.RunCommandAsync($"{_shellPath}log_collector.ksh " + all_acc_path);
            var all_member_info = await _sshCommandService.RunCommandAsync($"{_shellPath}get_all_member_info.ksh {all_acc_path}");
            var all_member_info_array = ParseString(all_member_info, true);
            var model_cfg = await GetModelConfig();
            var batch_cfg = await GetBatchConfig();
            var log_path = $"{_uiPath}log";

            foreach (var info in model_cfg)
            {
                var monitoring_info = new MonitoringInfo
                {
                    model = info.Model_Name,
                    nickname = info.Nickname,
                    member = info.Member_Name,
                    account = info.Account,
                };

                var logFilePath = $"{log_path}/{info.Model_Name}/{info.Nickname}/{info.Member_Name}.log";
                if (File.Exists(logFilePath) == false)
                {
                    monitoring_info.comment = "no log";
                    continue;
                }

                GenerateMonitoringInfo(monitoring_info, info, all_member_info_array);
                await SetMonitoringInfo(monitoring_info, info, batch_cfg, logFilePath);
            }
        }

        private void GenerateMonitoringInfo(MonitoringInfo monitoringInfo, ModelMember modelMember, Dictionary<string, Dictionary<string, string>> allMemberInfo)
        {
            var path = modelMember.Member_Path ?? string.Empty;
            var key = $"/{_systemName}/{modelMember.Account}{path}/{modelMember.Model_Name}/{modelMember.Member_Name}";
            int.TryParse(allMemberInfo[key][nameof(MonitoringInfo.lid)], out var lid);
            monitoringInfo.lid = lid;

            monitoringInfo.dtg = allMemberInfo[$"/{_systemName}/{modelMember.Account}{path}/{modelMember.Model_Name}/{modelMember.Member_Name}"][nameof(MonitoringInfo.dtg)];
            monitoringInfo.run = monitoringInfo.dtg.Substring(6, 2);
            monitoringInfo.complete_run_type = allMemberInfo[key][nameof(MonitoringInfo.run)];
            monitoringInfo.run_type = monitoringInfo.complete_run_type == "Default" ? string.Empty : monitoringInfo.complete_run_type.Split("_").FirstOrDefault();
            monitoringInfo.cron_mode = allMemberInfo[key][nameof(MonitoringInfo.cron_mode)];

            int.TryParse(allMemberInfo[key][nameof(MonitoringInfo.typhoon_mode)], out var typhoon_mode);
            monitoringInfo.typhoon_mode = typhoon_mode;

            monitoringInfo.manual = 0;
            monitoringInfo.start_flag = 1;
            monitoringInfo.stage_flag = 0;
            monitoringInfo.status = "pausing";
            monitoringInfo.sms_name = string.Empty;
            monitoringInfo.sms_time = null;
            monitoringInfo.start_time = null;
            monitoringInfo.end_time = null;
            monitoringInfo.pre_start = null;
            monitoringInfo.pre_end = null;
            monitoringInfo.run_end = null;
            monitoringInfo.shell_name = string.Empty;
            monitoringInfo.shell_time = null;
            monitoringInfo.error_message = string.Empty;
        }

        private async Task SetMonitoringInfo(MonitoringInfo monitoring_info, ModelMember info, IEnumerable<BatchConfig> batchConfig, string logFilePath)
        {
            var replace_str = new List<string>() { "\r", "\n", "\r\n", " ", "\n\r", "]", "[", ".sms", "none", ".ksh" };
            var logDatas = await _logFileService.ReadLogFileAsync(logFilePath);

            foreach (var logData in logDatas.Split("\n"))
            {
                var buffer = logData.ToString();
                //Model Member M|P DTG
                if (Regex.IsMatch(buffer, $"^{info.Model_Name} {info.Member_Name} (M|P|[0-9]|Step)"))
                {
                    //manual flag : check this run is auto or manual
                    monitoring_info.manual = Regex.IsMatch(buffer, "OP") ? 1 : 0;
                    monitoring_info.start_flag = 1; //start_flag=1 代表尚未抓取start_time ,start_flag=0代表已抓取完成
                    monitoring_info.stage_flag = 0; //stage_flag=0 代表.sms未填入
                    replace_str.ForEach(x => monitoring_info.run_type = monitoring_info.run_type.Replace(x, string.Empty));
                    monitoring_info.status = monitoring_info.lid == 1 ? "RUNNING" : monitoring_info.status;
                    monitoring_info.sms_name = string.Empty;
                    monitoring_info.sms_time = null;
                    monitoring_info.shell_name = string.Empty;

                    continue;
                }

                //[ HH:MM:SS ] -> sms_name.sms
                //[HH:MM:SS] -> name.ksh stage_start
                var isMatchStageStart = Regex.IsMatch(buffer, "stage_start");
                if ((Regex.IsMatch(buffer, ".sms") || isMatchStageStart)
                    && Regex.IsMatch(buffer, "->"))
                {
                    monitoring_info.status = (Regex.IsMatch(buffer, "finish")) ? "PAUSING" : monitoring_info.status; //status:finish
                    monitoring_info.status = (Regex.IsMatch(buffer, "Cancelled")) ? "Cancelled" : monitoring_info.status; //status:Cancelled
                    monitoring_info.status = (Regex.IsMatch(buffer, "fail")) ? "FAIL" : monitoring_info.status; //status:fail
                    var pattern = "/\\[\\s*(\\d{2}):(\\d{2}):(\\d{2})\\s*\\](\\s+)(->|<-)+(\\s+)(\\w+)/";
                    var replacement = "${1}:$2:$3 $7";

                    buffer = Regex.Replace(buffer, pattern, replacement);
                    if (isMatchStageStart == false || monitoring_info.stage_flag == 0)
                    {
                        var splitBuffer = buffer.Split(" ");
                        monitoring_info.sms_time = DateTime.TryParse(splitBuffer[0], out var time) 
                            ? time : monitoring_info.sms_time;
                        monitoring_info.sms_name = splitBuffer[1];
                    }
                    monitoring_info.status = monitoring_info.lid == 1 ? "RUNNING" : monitoring_info.status;
                    monitoring_info.stage_flag = isMatchStageStart ? monitoring_info.stage_flag : 1;
                    monitoring_info.shell_name = string.Empty;

                    //=====替換掉多餘字元=====
                    replace_str.ForEach(x => monitoring_info.sms_name.Replace(x, string.Empty));

                    //判斷起始時間
                    if (monitoring_info.start_flag == 1)
                    {
                        monitoring_info.start_time = monitoring_info.sms_time;
                        monitoring_info.start_flag = 0; //start_time已抓取完成
                    }

                    //predict batch start and end time
                    var start = monitoring_info.start_time;
                    var sms_start_time = monitoring_info.sms_time; //工作包起始時間
                    var min0 = GetPartTime(info.Model_Name, info.Member_Name, info.Nickname, monitoring_info.sms_name, monitoring_info.run_type, monitoring_info.dtg, batchConfig); //工作包預測起始時間(前面工作時間)
                    var min1 = GetBatchInfoByName(info.Model_Name, info.Member_Name, info.Nickname, monitoring_info.sms_name, monitoring_info.run_type, monitoring_info.dtg, nameof(BatchConfig.Time)); //工作包時間長度

                    //min2模組全部執行完成所需時間
                    var min2 = GetTotalTime(info.Model_Name, info.Member_Name, info.Nickname, monitoring_info.run_type, monitoring_info.dtg, batchConfig);
                    monitoring_info.pre_start = start.Value.AddMinutes(min0);  //工作包預測起始時間(實際時間+前面工作時間)
                    monitoring_info.pre_end = sms_start_time.Value.AddMinutes(min1); //預測工作包結束時間(實際起始+工作長度)
                    monitoring_info.end_time = start.Value.AddMinutes(min2); //預測模組全部執行完成時間(實際起始+所有工作長度)

                    //例外處理finish 或 fail的判斷
                    if (Regex.IsMatch(buffer, "finish"))
                    {
                        monitoring_info.status = "PAUSING";
                    }
                    else if (Regex.IsMatch(buffer, "Cancelled"))
                    {
                        monitoring_info.status = "Cancelled";
                    }
                    else if (Regex.IsMatch(buffer, "fail"))
                    {
                        monitoring_info.status = "FAIL";
                    }

                    continue;
                }

                //[ HH:MM:SS ]
                if (Regex.IsMatch(buffer, "[\\s*(\\d{2}):(\\d{2}):(\\d{2})\\s*]"))
                {
                    var pattern = "/\\[\\s*(\\d{2}):(\\d{2}):(\\d{2})\\s*\\](\\s+)(.)+(\\s+)(\\w+)/";
                    var replacement = "${1}:$2:$3 $7";
                    buffer = Regex.Replace(pattern, replacement, buffer);
                    //Regex.IsMatch("[\s*(\d{2}):(\d{2}):(\d{2})\s*]",buffer)會有誤判
                    if (buffer.Split(" ").Length < 2) continue;

                    var splitBuffer = buffer.Split(" ");
                    monitoring_info.shell_time = DateTime.TryParse(splitBuffer[0], out var time) 
                        ? time : monitoring_info.shell_time;
                    var shtmp = splitBuffer[1];
                    monitoring_info.shell_name = Regex.IsMatch(shtmp, "/.*\\.k?sh/") ? shtmp : monitoring_info.shell_name;

                    //判斷起始時間
                    if (monitoring_info.start_flag == 1)
                    {
                        monitoring_info.start_time = monitoring_info.shell_time;
                        var start = monitoring_info.start_time;

                        //min2模組全部執行完成所需時間
                        var min2 = GetTotalTime(info.Model_Name, info.Member_Name, info.Nickname, monitoring_info.run_type, monitoring_info.dtg, batchConfig);
                        monitoring_info.end_time = start.Value.AddMinutes(min2); //預測模組全部執行完成時間
                        monitoring_info.start_flag = 0; //start_time已抓取完成
                    }

                    //例外處理 finish 或 fail 的判斷
                    if (Regex.IsMatch(buffer, "finish"))
                    {
                        monitoring_info.run_end = monitoring_info.shell_time; //記錄結束時間
                        monitoring_info.status = "PAUSING";
                        if (monitoring_info.run_end == null)
                        {
                            var runEndString = Regex.Replace(buffer, "/\\[\\s*(\\d{2}):(\\d{2}):(\\d{2})\\s*\\](\\s+)(\\w+)/", "${1}:$2:$3");
                            monitoring_info.run_end = DateTime.TryParse(runEndString, out var runEnd) 
                                ? runEnd : monitoring_info.run_end;
                        }
                    }
                    else if (Regex.IsMatch(buffer, "Cancelled"))
                    {
                        monitoring_info.run_end = monitoring_info.shell_time; //記錄結束時間
                        monitoring_info.status = "Cancelled";
                        if (monitoring_info.run_end == null)
                        {
                            var runEndString = Regex.Replace(buffer, "/\\[\\s*(\\d{2}):(\\d{2}):(\\d{2})\\s*\\](\\s+)(\\w+)/", "${1}:$2:$3");
                            monitoring_info.run_end = DateTime.TryParse(runEndString, out var runEnd)
                                ? runEnd : monitoring_info.run_end;
                        }

                        var now_time = DateTime.Now.TWNow();
                        monitoring_info.error_message = $"[{now_time}]{info.Model_Name}_{info.Member_Name}_{info.Nickname}(C) {monitoring_info.status}, run_end:{monitoring_info.run_end}\n";
                    }
                    else if (Regex.IsMatch(buffer, "fail"))
                    {
                        monitoring_info.run_end = (monitoring_info.status != "running") ? monitoring_info.shell_time : null; //記錄結束時間
                        monitoring_info.status = "FAIL";
                        if (monitoring_info.run_end == null)
                        {
                            var runEndString = Regex.Replace(buffer, "/\\[\\s*(\\d{2}):(\\d{2}):(\\d{2})\\s*\\](\\s+)(\\w+)/", "${1}:$2:$3");
                            monitoring_info.run_end = DateTime.TryParse(runEndString, out var runEnd)
                                ? runEnd : monitoring_info.run_end;
                        }
                        var now_time = DateTime.Now.TWNow();
                        monitoring_info.error_message = $"[{now_time}]{info.Model_Name}_{info.Member_Name}_{info.Nickname}(C) {monitoring_info.status}, run_end:{monitoring_info.run_end}\n";
                    }

                    continue;
                }
            }
        }

        /// <summary>
        /// 工作包預測起始時間(前面工作時間長度)
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="memberName"></param>
        /// <param name="nickname"></param>
        /// <param name="smsName"></param>
        /// <param name="runType"></param>
        /// <param name="dtg"></param>
        /// <param name="batchConfig"></param>
        /// <returns></returns>
        private object GetPartTime(string modelName, string memberName, string nickname, string smsName, string runType, string dtg, IEnumerable<BatchConfig> batchConfig)
        {
            var key = modelName + memberName + nickname;
            var count = 0;     //計算總時間
            var tmpPosition = GetBatchInfoByName(modelName, memberName, nickname, smsName, runType, dtg, nameof(BatchConfig.Position)); //取出工作包的次序

            if (tmpPosition > 0)
            {  //工作包次序 > 0 表示有前面的工作
                for (var i = 0; i < batchConfig.Count(); i++)  //所有的batch資訊
                {
                    var batch = batchConfig.ElementAt(i);
                    smsName = batch.Batch;         //batch名稱
                    var position = batch.Position; //batch順序
                    var number = GetBatchNumber(modelName, memberName, nickname, position, batchConfig);    //計算同層batch個數

                    if (position >= tmpPosition) return count;
                    if (number < 2) //表示batch同層無重複
                    {
                        count += batch.Time;
                        continue;
                    }

                    count += GetBatchInfoByPosition(modelName, memberName, nickname, position, runType, dtg, nameof(BatchConfig.Time));
                    i += number - 1;  //position由sql指令排序過
                }
            }

            return count;
        }

        private async Task<IEnumerable<BatchConfig>> GetBatchConfig()
        {
            var sql = @"SELECT concat(`model`.`model_name`,`member`.`member_name`,`member`.`nickname`) AS model_member_nick,
                            `batch`.`batch_name` AS `batch`,
                            `batch`.`batch_position` AS `position`,
                            `batch`.`batch_type` AS `type`,
                            `batch`.`batch_dtg` AS `dtg`,
                            `batch`.`batch_time` AS `time`
                        FROM
                            ((`model` join `member`) join `batch`)
                        WHERE
                            ((`model`.`model_id` = `member`.`model_id`)
                            AND(`member`.`member_id` = `batch`.`member_id`))
                        ORDER BY
                            model_member_nick,
                            `batch`.`batch_position`;";
            var result = (await _dataBaseNcsUiService.QueryAsync<BatchConfig>(sql)).ToList();
            result.ForEach(x => x.Batch = x.Batch.Trim());

            return result;
        }

        private async Task<IEnumerable<ModelMember>> GetModelConfig()
        {
            var sql = @"SELECT *
                        FROM(`member`, `model`)
                        WHERE
                            `model`.`model_id` = member.model_id
                        ORDER BY
                            `model_position` asc,
                            `member_position` asc
                        ";
            var result = await _dataBaseNcsUiService.QueryAsync<ModelMember>(sql);
            return result;
        }

        private Dictionary<string, Dictionary<string, string>> ParseString(string str, bool processSections)
        {
            var lines = str.Split("\n");
            var result = new Dictionary<string, Dictionary<string, string>>();
            var inSect = string.Empty;
            foreach (var line in lines)
            {
                var item = line.Trim();
                if (item == null || item.StartsWith("#") || item.StartsWith(";"))  //註解跳過
                    continue;

                if (item.StartsWith("[") && item.IndexOf("]") > -1)  //元素開頭 ex:[/ncs/npcapln/NFS/M03]
                {
                    inSect = item.Substring(1, item.Length - item.IndexOf("]") - 1);
                    continue;
                }
                if (item.IndexOf("=") == -1)  //(We don't use "=== false" because value 0 is not valid as well)
                    continue;

                var spiltItem = item.Split("=", 2);
                if (processSections && inSect.Any())  //ProcessSections=true  多元素(二維陣列)
                    result.Add(inSect, new Dictionary<string, string> { { spiltItem[0].Trim(), spiltItem[1].Trim() } });
                //else  //ProcessSections=false 單一元素(一維陣列)
                //    return[trim(tmp[0])] = ltrim(tmp[1]);
            }
            return result;
        }
    }
}