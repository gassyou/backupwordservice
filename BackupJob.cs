
using backupwordservice;
using Quartz;
using Quartz.Util;

[DisallowConcurrentExecution]
public class BackupJob : IJob
{

    // 通过依赖注入的方式获取配置
    private readonly IConfiguration _configuration;
    private readonly List<BackupInfo> _backups;

    private readonly ILogger<BackupJob> _logger;

    public BackupJob(IConfiguration configuration, ILogger<BackupJob> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _backups = _configuration.GetSection("Backups").Get<List<BackupInfo>>();
    }

    Task IJob.Execute(IJobExecutionContext context)
    {
        var today = DateTime.Now;
        var todayString = today.ToString("yyyy-MM-dd");

        // 循环处理每个备份配置
        foreach (var backup in _backups)
        {
            _logger.LogInformation($"开始备份{backup.Source} 目录");

            // 以日期格式创建备份文件夹
            var backupFolder = Path.Combine(backup.DestinationFolder, todayString);
            // 如果备份文件夹存在 则删除
            if (Directory.Exists(backupFolder))
            {
                Directory.Delete(backupFolder, true);
            }
            DirectoryInfo backFolderDir = new DirectoryInfo(backupFolder);

            var source = CombineSourceFolder(backup.Source, backup.SubFolder, today);
            string[] conditions = ConvertConditions(backup.Conditions);

            FileAttributes attr = File.GetAttributes(source);
            // 判断是否为文件夹
            if (!attr.HasFlag(FileAttributes.Directory))
            {
                // 如果是文件，则直接复制到备份文件夹
                CopyFile(backup.Source, backupFolder, conditions);
                continue;
            }

            // 复制文件到备份文件夹
            CopyFolder(new DirectoryInfo(source), backFolderDir, conditions);
            _logger.LogInformation($"{backup.Source} 目录备份结束");


            _logger.LogInformation($"开始删除过期的备份文件夹");
            DeleteExpiredBackupFiles(backFolderDir, backup.KeepDays, today);
           _logger.LogInformation($"结束删除过期的备份文件夹\n");
        }
        return Task.CompletedTask;
    }

    // 删除过期备份文件
    private void DeleteExpiredBackupFiles(DirectoryInfo destinationFolder, int days, DateTime today)
    {
        foreach (DirectoryInfo dir in destinationFolder.GetDirectories())
        {
            if (today.CompareTo(dir.CreationTime) > days)
            {
                dir.Delete(true);
                _logger.LogInformation(dir.FullName + " 删除成功。");
            }
        }
    }

    // 合并源 目录 
    private string CombineSourceFolder(string path, string subFolder, DateTime today)
    {
        if (!subFolder.IsNullOrWhiteSpace())
        {
            path = Path.Combine(path, today.Year.ToString(), today.Month.ToString(), today.Day.ToString());
        }
        return path;
    }

    // 转换条件
    private string[] ConvertConditions(string condition)
    {
        if (condition.IsNullOrWhiteSpace())
        {
            return [];
        }

        String[] conditions = condition.Split(',');

        // 循环conditions
        for (int i = 0; i < conditions.Length; i++)
        {
            if (conditions[i].Contains("today"))
            {
                conditions[i] = DateTime.Now.ToString(conditions[i].Split(":")[1]);
            }
        }
        return conditions;
    }

    // 拷贝文件到指定目录
    private void CopyFile(string sourceFile, string destFile, String[] conditions)
    {
        // 检查目标文件夹是否存在，如果不存在则创建
        if (!Directory.Exists(Path.GetDirectoryName(destFile)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destFile));
        }

        FileInfo source = new FileInfo(sourceFile);

        // 复制文件到目标文件夹
        bool result = conditions.Aggregate(true, (result, item) => result && source.Name.Contains(item));
        if (result && source.Exists)
        {
            source.CopyTo(destFile, true);
        }
    }

    private void CopyFolder(DirectoryInfo source, DirectoryInfo target, String[] conditions)
    {
        // 检查源文件夹是否存在
        if (!source.Exists)
        {
            _logger.LogInformation($"{target.FullName} 不存在，跳过复制");
            return;
        }

        // 检查目标文件夹是否存在，如果不存在则创建
        if (!Directory.Exists(target.FullName))
        {
            Directory.CreateDirectory(target.FullName);
        }

        // 遍历源文件夹下的所有文件
        foreach (FileInfo fi in source.GetFiles())
        {
            // 复制文件到目标文件夹
            bool result = conditions.Aggregate(true, (result, item) => result && fi.Name.Contains(item));
            if (result)
            {
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }
        }

        // 遍历源文件夹下的所有子文件夹
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            // 创建目标文件夹
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);

            // 递归调用CopyAll方法
            CopyFolder(diSourceSubDir, nextTargetSubDir, conditions);
        }
    }
}