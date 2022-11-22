namespace UIQ_CronTab.Services.Interfaces
{
    public interface ILogFileService
    {
        string RootPath { get; }

        public Task<string> ReadLogFileAsync(string filePath);

        public Task WriteDataIntoLogFileAsync(string directoryPath, string fullFilePath, string newData);

        public Task WriteUiActionLogFileAsync(string message);

        public Task WriteUiErrorLogFileAsync(string message);

        public Task WriteUiTransationLogFileAsync(string content);
    }
}
