
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


            // 判断源文件是存在
            if (!File.Exists(backup.Source) && !Directory.Exists(backup.Source))
            {
                _logger.LogInformation($"{backup.Source} 源目录不存在,备份结束。（不删除过期备份） \r\n");
                continue;
            }

            // 以日期格式创建备份文件夹
            var backupFolder = Path.Combine(backup.DestinationFolder, todayString);
            // 如果备份文件夹存在 则删除
            if (Directory.Exists(backupFolder))
            {
                Directory.Delete(backupFolder, true);
            }
            DirectoryInfo backFolderDir = new DirectoryInfo(backupFolder);

            // 获取备份条件
            string[] conditions = CreateBackupConditions(backup.Conditions);

            // 判断源文件是否为文件夹
            FileAttributes attr = File.GetAttributes(backup.Source);
            if (!attr.HasFlag(FileAttributes.Directory))
            {
                // 如果是文件，则直接复制到备份文件夹
                CopyFile(new FileInfo(backup.Source), backFolderDir, conditions);
            }
            else 
            {
                var source = CombineSourceFolder(backup.Source, backup.SubFolder, today);

                // 复制文件到备份文件夹
                CopyFolder(new DirectoryInfo(source), backFolderDir, conditions);
            }

            _logger.LogInformation($"{backup.Source} 目录备份结束");


            _logger.LogInformation($"开始删除过期的备份文件夹");
            DeleteExpiredBackupFiles(new DirectoryInfo(backup.DestinationFolder), backup.KeepDays, today);
            _logger.LogInformation($"结束删除过期的备份文件夹 \r\n");
        }
        return Task.CompletedTask;
    }

    // 删除过期备份文件
    private void DeleteExpiredBackupFiles(DirectoryInfo destinationFolder, int days, DateTime today)
    {
        if (!destinationFolder.Exists)
        {
            _logger.LogInformation($"{destinationFolder.FullName} 备份目录不存在");
            return;
        }
        foreach (DirectoryInfo dir in destinationFolder.GetDirectories())
        {
            // 计算日期差
            var daysDiff = today.Subtract(dir.CreationTime).Days;
            if (daysDiff > days)
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
            subFolder = subFolder.Replace(">>", "-");
            var todayString = today.ToString(subFolder);

            foreach (var item in todayString.Split("-"))
            {
                path = Path.Combine(path, item);
            }
        }
        return path;
    }

    // 转换条件
    private string[] CreateBackupConditions(string condition)
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
    private void CopyFile(FileInfo source, DirectoryInfo target, String[] conditions)
    {
        // 检查源文件是否存在，如果不存在则 警告并返回
        if (!source.Exists)
        {
            _logger.LogInformation($"{source.FullName} 不存在，跳过复制");
            return;
        }
        
        // 检查目标文件夹是否存在，如果不存在则创建
        if (!target.Exists)
        {
            target.Create();
        }

        // 复制文件到目标文件夹
        bool result = conditions.Aggregate(true, (result, item) => result && source.Name.Contains(item));
        if (result)
        {
            source.CopyTo(Path.Combine(target.ToString(), source.Name), true);
        }
    }

    private void CopyFolder(DirectoryInfo source, DirectoryInfo target, String[] conditions)
    {
        // 检查源文件夹是否存在
        if (!source.Exists)
        {
            _logger.LogInformation($"{source.FullName} 不存在，跳过复制");
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
            CopyFile(fi, target, conditions);
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