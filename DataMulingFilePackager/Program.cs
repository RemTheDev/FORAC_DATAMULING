// See https://aka.ms/new-console-template for more information
using DataMulingFilePackager;

ConfigurationReader configReader = new ConfigurationReader();
AppConfig config = configReader.ReadSection<AppConfig>("AppConfig");

if(!File.Exists(config.LogFolderLocation))
    Directory.CreateDirectory(config.LogFolderLocation);

string logFileName = Path.Combine(config.LogFolderLocation, $"{DateTime.Today.ToString("yyyyMMdd")}_log.txt");
using (StreamWriter stream = File.AppendText(logFileName))
{
    Action<string, bool> Log = (log, requireVerbose) =>
    {
        string formatedLog = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} | {log}";
        if (config.VerboseLog || !requireVerbose)
            stream.WriteLine(formatedLog);
        Console.WriteLine(formatedLog);
    };

    using (FilePackager packager = new FilePackager(config))
    {
        packager.DeleteOldPackages(Log);
        packager.PackageFiles(Log);
    }
}