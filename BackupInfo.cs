
namespace backupwordservice;
public class BackupInfo
{
    public string Source { get; set; }
    public string SubFolder { get; set; }

    public string Conditions { get; set; }

    public string DestinationFolder { get; set; }
    public int KeepDays { get; set; }
}