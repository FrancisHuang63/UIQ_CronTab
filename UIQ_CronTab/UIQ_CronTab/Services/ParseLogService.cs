using UIQ_CronTab.Models;
using UIQ_CronTab.Services.Interfaces;

namespace UIQ_CronTab.Services
{
    public class ParseLogService : IParseLogService
    {
        private readonly ISshCommandService _sshCommandService;
        private readonly IDataBaseService _dataBaseNcsUiService;
        private readonly IDataBaseService _dataBaseNcsLogService;

        private readonly string _systemName;
        private readonly string _shellPath;
        private readonly string _uiPath;

        public ParseLogService(ISshCommandService sshCommandService, IEnumerable<IDataBaseService> dataBaseServices
            , IConfiguration configuration)
        {
            _sshCommandService = sshCommandService;
            _dataBaseNcsUiService = dataBaseServices.Single(x => x.DataBase == Enums.DataBaseEnum.NcsUi);
            _dataBaseNcsLogService = dataBaseServices.Single(x => x.DataBase == Enums.DataBaseEnum.NcsLog);

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