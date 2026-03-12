using System.Collections.Concurrent;

namespace DataMulingMachineFileMonitor
{
    public class PollingFileCopier
    {
        private int pollingIntervalSeconds;
        public ConcurrentBag<string> FilesToCopy { get; set; }

        public PollingFileCopier(int pollingInterval)
        {
            this.pollingIntervalSeconds = pollingInterval;

            this.FilesToCopy = new ConcurrentBag<string>();
        }

        public void CopyReadyFilesToOutput(string outputDir, CancellationToken stoppingToken, Action<string, bool> log)
        {
            List<string> toRequeue = new List<string>();
            while(this.FilesToCopy.Count > 0 && !stoppingToken.IsCancellationRequested)
            {
                string filePath;
                if(this.FilesToCopy.TryTake(out filePath))
                {
                    if (File.Exists(filePath)) //File might have been renamed or deleted
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        if (fileInfo.LastWriteTime < DateTime.Now.AddSeconds(pollingIntervalSeconds * -1))
                        {
                            string fileOutput = Path.Combine(outputDir, fileInfo.Name);
                            try
                            {
                                if (!File.Exists(fileOutput))
                                {
                                    File.Copy(fileInfo.FullName, fileOutput);
                                }
                                else
                                {
                                    FileInfo existingFile = new FileInfo(fileOutput);
                                    if (existingFile.LastWriteTime < fileInfo.LastWriteTime)
                                        File.Copy(fileInfo.FullName, fileOutput, true);
                                }
                            }
                            catch (Exception e)
                            {
                                log($"Error copying file: {e.Message}. Requeueing...", true);
                                toRequeue.Add(filePath); //If copy fails in any ways, requeue file for next iteration
                            }
                        }
                        else
                            toRequeue.Add(filePath);
                    }
                }
            }

            foreach(string toQueue in toRequeue)
            {
                if(!this.FilesToCopy.Contains(toQueue))
                    this.FilesToCopy.Add(toQueue);
            }
        }
    }
}
