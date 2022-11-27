using UIQ_CronTab;
using UIQ_CronTab.Filters;
using UIQ_CronTab.Services;
using UIQ_CronTab.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddMvc(config =>
{
    config.Filters.Add(new ExceptionFilter());
    config.Filters.Add(new ActoinLogFilter());
    config.Filters.Add(new ResultLogFilter());
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IDataBaseService, MySqlDataBaseNcsUiService>();
builder.Services.AddScoped<IDataBaseService, MySqlDataBaseNcsLogService>();
builder.Services.AddScoped<IParseLogService, ParseLogService>();
builder.Services.AddScoped<IPhaseLogService, PhaseLogService>();
builder.Services.AddScoped<IMakeDailyLogService, MakeDailyLogService>();
builder.Services.AddScoped<ISshCommandService, SshCommandService>();
builder.Services.AddScoped<ILogFileService, LogFileService>();
builder.Services.Configure<ConnectoinStringOption>(builder.Configuration.GetSection("MySqlOptions").GetSection("ConnectionString"));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
