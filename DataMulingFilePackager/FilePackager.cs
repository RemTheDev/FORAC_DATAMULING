using System.IO.Compression;
using System.Text.Json;
using DataMulingPackageDB;
using FORAC.Utility;

namespace DataMulingFilePackager
{
    public class FilePackager : IDisposable
    {
        public AppConfig AppConfig { get; set; }
        public SQLiteReaderWriter sqLiteDb { get; set; }

        public FilePackager(AppConfig config)
        {
            this.AppConfig = config;
            this.sqLiteDb = new SQLiteReaderWriter(config.FileInformationDbPath);
        }

        public void Dispose()
        {
            this.sqLiteDb.Dispose();
        }

        public void DeleteOldPackages(Action<string, bool> log)
        {
            int nbPackDeleted = 0;
            foreach(Package package in this.sqLiteDb.GetReceivedFileInfoOlderThanXDays(this.AppConfig.MaxPackageAgeDays))
            {
                this.sqLiteDb.DeletePackageInfo(package.RowId);
                nbPackDeleted++;
            }

            if(nbPackDeleted > 0)
                log($"Deleted {nbPackDeleted} packages older than {this.AppConfig.MaxPackageAgeDays} days.", true);
        }

        public void PackageFiles(Action<string, bool> log)
        {
            //*************Temp files structure****************
            //%TEMP FOLDER%/
            //  zipFile.zip                         <- TO DELETE (one of many)
            //  dataTempDir/                        <- TO DELETE
            //      rawDataDir/                     <- ZIPPED TO zipFile.zip  (one of many)
            //          dataFile.xxx
            //  packageTempDir/                     <- TO DELETE
            //      tempPkgDir/                     <- ZIPPED TO finalPackagePath.zip (one of many)
            //          packageJSONFilePath.json
            //          encryptedZipFile.zip
            //%FINAL PACKAGE PATH%/
            //  finalPackagePath.zip                <- ONE FINAL PACKAGE PER INPUT FILE (one of many)
            //*************************************************

            List<string> tempFolders = new List<string>();
            List<string> tempFiles = new List<string>();

            try
            {
                IEnumerable<string> availableFilesForPackaging = Directory.EnumerateFiles(this.AppConfig.DataFilesPath).Where(f => f != this.AppConfig.PackageHeaderFileName);
                if(availableFilesForPackaging.Any())
                {
                    log("Creating folders for file packaging...", true);
                    string dataTempDir = FileHelper.GetTempDirectoryFilePath();
                    string packageTempDir = FileHelper.GetTempDirectoryFilePath();
                    log($"{dataTempDir}...", true);
                    tempFolders.Add(dataTempDir);
                    Directory.CreateDirectory(dataTempDir);
                    log($"{packageTempDir}...", true);
                    tempFolders.Add(packageTempDir);
                    Directory.CreateDirectory(packageTempDir);

                    log("Copying data files for zipping...", true);
                    int nbPkgCreated = 0;
                    foreach (string dataFile in availableFilesForPackaging)
                    {
                        FileInfo dataFileInfo = new FileInfo(dataFile);
                        List<Package> existingPackages = this.sqLiteDb.GetFileInfoByOriginalName(dataFileInfo.Name);
                        //If package doesn't exist of if data is more recent than existing package
                        if (!existingPackages.Any() || existingPackages.Max(p => p.LastWrittenTo) < dataFileInfo.LastWriteTime)
                        {
                            string strDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                            string rdmShortGuid = RandomHelper.GenerateShortGuidWithoutSpecialChar();
                            string packageName = $"{strDateTime}_{rdmShortGuid}.zip";

                            string fileTempName = FileHelper.GetTempFileName();
                            string rawDataDir = Path.Combine(dataTempDir, fileTempName);
                            string tempPkgDir = Path.Combine(packageTempDir, fileTempName);
                            string zipFile = FileHelper.GetTempFilePath(".zip");
                            string encryptedFile = Path.Combine(tempPkgDir, FileHelper.GetTempFileName(".aes"));
                            string packageJSONFilePath = Path.Combine(tempPkgDir, this.AppConfig.PackageHeaderFileName);
                            string finalPackagePath = Path.Combine(this.AppConfig.PackageFilesPath, packageName);

                            tempFiles.Add(zipFile);
                            Directory.CreateDirectory(rawDataDir);
                            Directory.CreateDirectory(tempPkgDir);

                            log("Encrypting data files...", true);
                            StringCipher.EncryptFile(dataFile, encryptedFile, AppConfig.SecretEncryptionKey);

                            log("Preparing package header file...", true);
                            DataMulePackagePayload payload = new DataMulePackagePayload(dataFileInfo.Name, dataFileInfo.LastWriteTime, dataFileInfo.Length,
                                                                                        AppConfig.Username, AppConfig.Password, FileHelper.CreateSignatureForFile(encryptedFile));
                            DataMulePackageJSONFile packageJSONFile = new DataMulePackageJSONFile();
                            packageJSONFile.SetHeader(this.AppConfig);
                            packageJSONFile.EncryptAndSetPayload(payload.ToJSONString(), this.AppConfig.SecretEncryptionKey);
                            packageJSONFile.SetSignature(this.AppConfig.SecretEncryptionKey);

                            log("Writing package...", true);
                            File.WriteAllText(packageJSONFilePath, JsonSerializer.Serialize<DataMulePackageJSONFile>(packageJSONFile));
                            ZipFile.CreateFromDirectory(tempPkgDir, finalPackagePath);
                            log($"Written package {finalPackagePath}!", false);

                            Package packageInfo = new Package()
                            {
                                OriginalName = dataFileInfo.Name,
                                LastWrittenTo = dataFileInfo.LastWriteTime,
                                PackageName = packageName,
                                SentAndReceived = false
                            };
                            this.sqLiteDb.InsertPackageInfo(packageInfo);

                            nbPkgCreated++;

                            File.Delete(dataFile);
                            log($"Deleted file {dataFile}.", true);
                            if (existingPackages.Any()) //If a package existed of an old version of the file, delete the old package
                            {
                                foreach (Package existingPackage in existingPackages)
                                {
                                    string packagePath = Path.Combine(this.AppConfig.PackageFilesPath, existingPackage.PackageName);
                                    if (File.Exists(packagePath))
                                    {
                                        File.Delete(packagePath);
                                        log($"Deleted old package {packagePath}.", true);
                                    }
                                    this.sqLiteDb.DeletePackageInfo(existingPackage.RowId);
                                }
                            }
                        }
                        else
                            log($"File {dataFileInfo.Name} is already packaged and up to date.", true);
                    }

                    log($"{nbPkgCreated} secure packages created at {this.AppConfig.PackageFilesPath}.", false);
                }
            }
            catch (Exception e)
            {
                log($"ERROR : {e.Message}.\n{e.StackTrace}", false);
            }
            finally
            {
                foreach(string folder in tempFolders)
                {
                    if (Directory.Exists(folder))
                        Directory.Delete(folder, true);
                }

                foreach(string file in tempFiles)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }
    }
}
