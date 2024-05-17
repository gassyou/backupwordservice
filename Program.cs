
using backupwordservice.log;
using backupwordservice.LogUtils;
using Quartz;

var builder = Host.CreateDefaultBuilder(args);

builder.UseWindowsService().ConfigureServices((hostContext, services) =>
{
    services.AddQuartz(q =>
    {
        q.UseMicrosoftDependencyInjectionJobFactory();
        q.AddJobAndTrigger<BackupJob>(hostContext.Configuration);
    });
    services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
}).ConfigureLogging((hostContext, logging) =>
{
    logging.ClearProviders();
    logging.AddProvider(new CoustomFileLoggerProvider(new CustomFileLoggerConfiguration
    {
        MinLevel = (LogLevel)Enum.Parse(typeof(LogLevel), hostContext.Configuration["FileLogPath:MinLogLevel"]),
        LogLevel = LogLevel.Information,
        LogPath = hostContext.Configuration["FileLogPath:LogPath"],
    }));
}).Build().Run();
