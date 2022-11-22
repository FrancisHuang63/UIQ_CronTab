namespace UIQ_CronTab.Services.Interfaces
{
    public interface ISshCommandService
    {
        public Task<string> RunCommandAsync(string command);
    }
}
