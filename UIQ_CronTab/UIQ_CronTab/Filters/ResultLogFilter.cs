using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UIQ_CronTab.Services.Interfaces;

namespace UIQ_CronTab.Filters
{
    public class ResultLogFilter : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context)
        {
            return;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            var logFileService = context.HttpContext.RequestServices.GetService(typeof(ILogFileService)) as ILogFileService;
            var resultValue = string.Empty;
            if (context.Result is JsonResult jsonResult)
            {
                resultValue = System.Text.Json.JsonSerializer.Serialize(jsonResult.Value);
            }
            else if (context.Result is ViewResult viewResult)
            {
                resultValue = System.Text.Json.JsonSerializer.Serialize(viewResult.ViewData);
            }

            var content = $"[Response][Path]: [{context.HttpContext.Request.Method}]{context.HttpContext.Request.Path}, [ResultData]: {resultValue}";
            logFileService.WriteUiTransationLogFileAsync(content);
            return;
        }
    }
}