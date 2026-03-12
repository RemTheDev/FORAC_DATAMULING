namespace DataMuling_TCPServer_Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _Configuration;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _Configuration = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Action<string> ServerLog = (string log) =>
                {
                    _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - Server: {log}");
                };
                _logger.LogInformation($"Getting configuration...");
                AppConfig appConfig = this._Configuration.GetSection("AppConfig").Get<AppConfig>();
                _logger.LogInformation($"Expected db path: {appConfig.FileInformationDbPath}");
                _logger.LogInformation($"Expected package folder: {appConfig.AvailablePackagesFilesPath}");
                _logger.LogInformation($"Expected shared folder: {appConfig.SharedFileInputPath}");
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                        if (int.TryParse(appConfig.Port, out int port))
                        {
                            TCPServer server = new TCPServer(appConfig, port, ServerLog);
                            Task serverTask = server.Start(stoppingToken);
                            serverTask.Wait();
                        }
                        else
                        {
                            _logger.LogInformation("Server failed to start.");
                            _logger.LogInformation("Press any key to exit...");
                            Console.ReadKey();
                        }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException cancelEx) 
            {
                _logger.LogInformation("Cancellation was requested. Server is stopping...");
            }
            catch(Exception e)
            {
                _logger.LogError(0, e, e.Message, null);
            }
        }
    }
}
