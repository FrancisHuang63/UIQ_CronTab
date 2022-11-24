namespace UIQ_CronTab.Models.ApiResponse
{
    public class ApiResponseBase<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public T Data { get; set; }

        public ApiResponseBase()
        {

        }

        public ApiResponseBase(T data)
        {
            Data = data;
            Success = true;
        }

        public ApiResponseBase(string message)
        {
            Message = message;
            Success = false;
        }
    }
}
