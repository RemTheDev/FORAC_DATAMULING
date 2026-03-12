using System.Net;
using System.Net.Sockets;
using System.Text;
using DataMulingPackageDB;

namespace DataMuling_TCPServer_Service
{
    public class TCPServer : IDisposable
    {
        private const int bufferSize = 256;
        private TcpListener server;
        private SQLiteReaderWriter dataMulingPackagesDB;
        private Action<string> ServerLog;

        public AppConfig Config;
        public bool KeepServerRunning;
        public int Port;
        

        public TCPServer(AppConfig config, int port, Action<string> log)
        {
            this.Config = config;
            this.Port = port;
            this.server = new TcpListener(IPAddress.Any, port);
            this.dataMulingPackagesDB = new SQLiteReaderWriter(config.FileInformationDbPath);

            this.ServerLog = log;
        }

        public async Task Start(CancellationToken stoppingToken)
        {
            try
            {
                this.ServerLog($"Starting server... Press Ctrl+C to stop.");
                this.server.Start();
                this.ServerLog($"Server is running... Listening on port {this.Port}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    using (TcpClient client = await server.AcceptTcpClientAsync(stoppingToken))
                    {
                        using (NetworkStream netStream = client.GetStream())
                        {
                            int byteRead = 0;
                            byte[] buffer;
                            string data = "";

                            do
                            {
                                buffer = new byte[bufferSize];
                                byteRead = netStream.Read(buffer, 0, bufferSize);
                                data += Encoding.ASCII.GetString(buffer, 0, byteRead);
                            } while (byteRead > 0 && buffer[byteRead - 1] != 4);
                                
                            if(byteRead > 0)
                            {
                                data = data.Remove(data.Length - 1, 1);
                                if (data == "getFileInfos")
                                {
                                    this.ServerLog($"Available file names requested.");
                                    string infosToSend = "";
                                    foreach (Package package in this.dataMulingPackagesDB.GetAllUnreceivedPackages())
                                    {
                                        string filePath = Path.Combine(this.Config.AvailablePackagesFilesPath, package.PackageName);
                                        if (File.Exists(filePath))
                                        {
                                            FileInfo fi = new FileInfo(filePath);
                                            infosToSend += $"{fi.Name}|{fi.Length};";
                                        }
                                        else
                                            this.dataMulingPackagesDB.DeletePackageInfo(package.RowId);
                                    }
                                    if (infosToSend.Length > 0)
                                    {
                                        infosToSend = infosToSend.Remove(infosToSend.Length - 1, 1);
                                        netStream.Write(Encoding.ASCII.GetBytes(infosToSend));
                                    }
                                    netStream.WriteByte(4); //send EOT byte

                                    this.ServerLog($"Available file names sent.");
                                }
                                else if (data.StartsWith("sendFile|"))
                                {
                                    string[] fileInfo = data.Split('|');
                                    string fileToReceive = fileInfo[1];
                                    if (int.TryParse(fileInfo[2], out int fileSizeBytes))
                                    {
                                        DateTime fileInfoLastWrittenTo;
                                        if (DateTime.TryParseExact(fileInfo[3], "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out fileInfoLastWrittenTo))
                                        {
                                            List<Package> existingPackages = this.dataMulingPackagesDB.GetFileInfoByOriginalName(fileToReceive);
                                            if (!existingPackages.Any() || existingPackages.Max(p => p.LastWrittenTo) < fileInfoLastWrittenTo)
                                            {
                                                string filePath = Path.Combine(Config.SharedFileInputPath, fileToReceive);
                                                this.ServerLog($"Client requests to send file {fileToReceive}.");
                                                netStream.Write(Encoding.ASCII.GetBytes("Ok"));
                                                netStream.WriteByte(4); //send EOT byte

                                                int nbByteRead = 0;
                                                int totalByteRead = 0;
                                                byte[] fileBytes;

                                                using (FileStream fs = File.Create(filePath)) //Will overwrite old file if it exists
                                                {
                                                    do
                                                    {
                                                        fileBytes = new byte[bufferSize];
                                                        nbByteRead = netStream.Read(fileBytes, 0, bufferSize);
                                                        totalByteRead += nbByteRead;
                                                        fs.Write(fileBytes, 0, nbByteRead);
                                                    } while (totalByteRead < fileSizeBytes);
                                                }
                                                this.ServerLog($"File {filePath} received. ({totalByteRead} bytes)");
                                            }
                                            else
                                            {
                                                netStream.Write(Encoding.ASCII.GetBytes("File already up to date."));
                                                netStream.WriteByte(4); //send EOT byte
                                            }
                                        }
                                        else
                                        {
                                            netStream.Write(Encoding.ASCII.GetBytes("Invalid modification date."));
                                            netStream.WriteByte(4); //send EOT byte
                                        }
                                    }
                                    else
                                    {
                                        netStream.Write(Encoding.ASCII.GetBytes("Invalid file size."));
                                        netStream.WriteByte(4); //send EOT byte
                                    }
                                }
                                else if (data.StartsWith("getFile-"))
                                {
                                    string requestedFileName = data.Substring("getFile-".Length);
                                    this.ServerLog($"Client requests to get file {requestedFileName}.");
                                    string filePath = Path.Combine(Config.AvailablePackagesFilesPath, requestedFileName);
                                    if (File.Exists(filePath))
                                    {
                                        using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
                                        {
                                            byte[] fileBytes = new byte[bufferSize];
                                            int nbByteRead = 0;

                                            do
                                            {
                                                fileBytes = new byte[bufferSize];
                                                nbByteRead = fs.Read(fileBytes, 0, bufferSize);

                                                if (nbByteRead > 0)
                                                    netStream.Write(fileBytes, 0, nbByteRead);
                                            } while (nbByteRead > 0);
                                        }
                                        this.ServerLog($"File {requestedFileName} sent.");
                                    }
                                    else
                                        this.ServerLog($"Requested file does not exists.");
                                }
                                else if (data.StartsWith("packageReceived-"))
                                {
                                    string receivedPackage = data.Substring("packageReceived-".Length);
                                    this.ServerLog($"Package {receivedPackage} successfully reached destination.");

                                    List<Package> packages = this.dataMulingPackagesDB.GetFileInfoByPackageName(receivedPackage);

                                    if (packages.Any())
                                    {
                                        foreach(Package package in packages)
                                        {
                                            this.dataMulingPackagesDB.SetPackageReceived(package.RowId, true);
                                        }

                                        string filePath = Path.Combine(Config.AvailablePackagesFilesPath, receivedPackage);
                                        if (File.Exists(filePath))
                                            File.Delete(filePath);
                                        netStream.Write(Encoding.ASCII.GetBytes("Ok"));
                                    }
                                    else
                                    {
                                        netStream.Write(Encoding.ASCII.GetBytes("NotMyPackage"));
                                    }

                                    netStream.WriteByte(4); //send EOT byte
                                }
                                else
                                    this.ServerLog($"Request not recognised.");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException toEx)
            { }
            catch (Exception ex)
            {
                ServerLog(ex.Message);
                ServerLog(ex.StackTrace);
            }
            finally
            {
                server.Stop();
            }
        }

        public void Dispose()
        {
            server.Stop();
        }
    }
}
