using Microsoft.AspNetCore.Mvc.Filters;
using UIQ_CronTab.Services.Interfaces;

namespace UIQ_CronTab.Filters
{
    public class ActoinLogFilter : IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var logFileService = context.HttpContext.RequestServices.GetService(typeof(ILogFileService)) as ILogFileService;
            var content = $"[Requset] [Path]: [{context.HttpContext.Request.Method}]{context.HttpContext.Request.Path}, Parameters: {(context.ActionArguments.Any() ? System.Text.Json.JsonSerializer.Serialize(context.ActionArguments) : "[]")}";
            logFileService.WriteUiTransationLogFileAsync(content);
            return next();
        }
    }
}