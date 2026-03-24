using System.Diagnostics;

namespace DataMulingMachineFileMonitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private PollingFileCopier _fileCopier;
        private FileSystemWatcher _fileWatcher;

        private string machineInputFolder;
        private string dataMulingSharedFolder;
        private string dataMulingSharedFolderUsername;
        private string dataMulingSharedFolderPassword;
        private string dataMulingNetworkProfileName;
        private string dataMulingNetworkSSID;
        private string dataMulingNetworkPassword;
        private int filePollingIntervalSeconds;
        private bool autoReconnectToDataMulingNetwork;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            this._logger = logger;
            this._configuration = config;

            this.machineInputFolder = this._configuration.GetSection("MachineFileInputFolder").Value;
            this.dataMulingSharedFolder = this._configuration.GetSection("DataMulingSharedFolder").Value;
            this.dataMulingSharedFolderUsername = this._configuration.GetSection("DataMulingSharedFolderUsername").Value;
            this.dataMulingSharedFolderPassword = this._configuration.GetSection("DataMulingSharedFolderPassword").Value;
            this.dataMulingNetworkProfileName = this._configuration.GetSection("DataMulingNetworkProfileName").Value;
            this.dataMulingNetworkSSID = this._configuration.GetSection("DataMulingNetworkSSID").Value;
            this.dataMulingNetworkPassword = this._configuration.GetSection("DataMulingNetworkPassword").Value;
            this.filePollingIntervalSeconds = int.Parse(this._configuration.GetSection("FileCopyPollingIntervalSeconds").Value);
            this.autoReconnectToDataMulingNetwork = this._configuration.GetSection("AutoReconnectToDataMulingNetwork").Value.ToLower() == "true";

            this._fileCopier = new PollingFileCopier(this.filePollingIntervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Action<string, bool> logMsg = (msg, isError) =>
            {
                if (isError)
                    _logger.LogError(msg);
                else
                    _logger.LogInformation(msg);
            };

            logMsg($"Worker running at: {DateTimeOffset.Now}", false);

            //Add RasPi1 profile if it does not exist
            string getNetshProfilesCommandResult = this.RunCMDCommand(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "netsh.exe"),
                Arguments = "wlan show profiles",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            //list of profiles
            IEnumerable<string> profiles = getNetshProfilesCommandResult
                                           .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(line => line.Split(':', StringSplitOptions.RemoveEmptyEntries))
                                           .Where(split => split.Length > 1)
                                           .Select(split => split[1].Trim());

            if(!profiles.Any(p => p == this.dataMulingNetworkProfileName))   //Wanted profile is not created
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string profileTemplatePath = Path.Combine(currentDir, $"{this.dataMulingNetworkProfileName}.xml");

                //Create profile
                this.RunCMDCommand(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "netsh.exe"),
                    Arguments = $"wlan add profile filename=\"{profileTemplatePath}\" user=all",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });

                //Set profile's ssid and password
                this.RunCMDCommand(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "netsh.exe"),
                    Arguments = $"wlan set profileparameter name=\"{this.dataMulingNetworkProfileName}\" ssid=\"{this.dataMulingNetworkSSID}\" keyMaterial=\"{this.dataMulingNetworkPassword}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool driveMapped = false;
                    bool wifiConnected = false;
                    WifiChecker wifiChecker = new WifiChecker();
                    while (!stoppingToken.IsCancellationRequested && (!wifiConnected || !driveMapped))
                    {
                        wifiConnected = wifiChecker.isConnected(dataMulingNetworkSSID);
                        if (!wifiConnected)
                        {
                            if(this.autoReconnectToDataMulingNetwork)
                            {
                                logMsg($"Wifi not connected to SSID '{dataMulingNetworkSSID}'. Attempting connection...", true);
                                this.RunCMDCommand(new ProcessStartInfo
                                {
                                    FileName = Path.Combine(Environment.SystemDirectory, "netsh.exe"),
                                    Arguments = $"wlan connect {this.dataMulingNetworkSSID}",
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true
                                });
                            }
                            else
                                logMsg($"Wifi not connected to SSID '{dataMulingNetworkSSID}'. Auto-reconnect setting is off", true);
                        }
                        else
                        {
                            logMsg($"Wifi connected to SSID '{dataMulingNetworkSSID}'!", false);
                            try
                            {
                                logMsg($"Checking if network drive is accessible...", false);
                                Directory.EnumerateDirectories(this.dataMulingSharedFolder).ToList();   //Will throw if drive is not accessible
                                driveMapped = true;
                                logMsg($"Checking if network drive is accessible...done.", false);
                            }
                            catch (Exception e)
                            {
                                logMsg($"Network drive access failed! Attempting to create mapping...", true);

                                this.RunCMDCommand(new ProcessStartInfo
                                {
                                    FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                                    Arguments = $"/C net use {this.dataMulingSharedFolder} {this.dataMulingSharedFolderPassword} /USER:{this.dataMulingSharedFolderUsername} /persistent:yes",
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true
                                });
                            }
                        }

                        if (!stoppingToken.IsCancellationRequested && (!wifiConnected || !driveMapped))
                        {
                            logMsg($"Re-checking in 30 seconds...", false);
                            await Task.Delay(30000, stoppingToken);
                        }
                    }
                    
                    if(Directory.Exists(this.machineInputFolder))
                    {
                        this._fileWatcher = new FileSystemWatcher(machineInputFolder);
                        this._fileWatcher.Created += OnFileCreated;
                        this._fileWatcher.Changed += OnFileChanged;
                        this._fileWatcher.Renamed += OnFileRenamed;
                        this._fileWatcher.Deleted += OnFileDeleted;
                        this._fileWatcher.Error += OnFileError;

                        this._fileWatcher.IncludeSubdirectories = false;
                        this._fileWatcher.EnableRaisingEvents = true;

                        foreach (string file in Directory.EnumerateFiles(machineInputFolder))
                        {
                            this._fileCopier.FilesToCopy.Add(file);
                        }

                        while (!stoppingToken.IsCancellationRequested)
                        {
                            if (wifiChecker.isConnected(dataMulingNetworkSSID))
                                this._fileCopier.CopyReadyFilesToOutput(this.dataMulingSharedFolder, stoppingToken, logMsg);
                            else
                                throw new Exception("Lost Wifi Connection. Retrying..."); //Allows to go back to wifi loop check and reset the FileSystemWatcher

                            await Task.Delay(filePollingIntervalSeconds * 1000, stoppingToken);
                        }
                    }
                    else
                    {
                        logMsg($"Directory {this.machineInputFolder} does not exist... Retrying in {filePollingIntervalSeconds} seconds...", true);
                        await Task.Delay(filePollingIntervalSeconds * 1000, stoppingToken);
                    }
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs arg)
        {
            if (!this._fileCopier.FilesToCopy.Contains(arg.FullPath))
                this._fileCopier.FilesToCopy.Add(arg.FullPath);
            this._logger.LogInformation($"File {arg.Name} created.");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs arg)
        {
            if(!this._fileCopier.FilesToCopy.Contains(arg.FullPath)) 
                this._fileCopier.FilesToCopy.Add(arg.FullPath);
            this._logger.LogInformation($"File {arg.Name} changed. ({arg.ChangeType}).");
        }

        private void OnFileRenamed(object sender, RenamedEventArgs arg)
        {
            if (!this._fileCopier.FilesToCopy.Contains(arg.FullPath))
                this._fileCopier.FilesToCopy.Add(arg.FullPath);
            this._logger.LogInformation($"File {arg.OldName} renamed to {arg.Name}.");
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs arg)
        {
            this._logger.LogInformation($"File {arg.Name} deleted.");
        }

        private void OnFileError(object sender, ErrorEventArgs arg)
        {
            this._logger.LogInformation($"Error: {arg.GetException()}");
        }

        private void LogException(Exception ex)
        {
            this._logger.LogError($"Error: {ex.Message}{Environment.NewLine}StackTrace:{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}");
            if (ex.InnerException != null)
                LogException(ex.InnerException);
        }

        private string RunCMDCommand(ProcessStartInfo processInfo)
        {
            _logger.LogInformation($"Running command: {Path.GetFileNameWithoutExtension(processInfo.FileName)} {processInfo.Arguments}");
            Process proc = new Process();
            proc.StartInfo = processInfo;
            proc.EnableRaisingEvents = true;
            proc.Start();
            proc.WaitForExitAsync();

            string commandResult = proc.StandardOutput.ReadToEnd();
            _logger.LogInformation($"Command result:{Environment.NewLine}{commandResult}");

            return commandResult;
        }
    }
}