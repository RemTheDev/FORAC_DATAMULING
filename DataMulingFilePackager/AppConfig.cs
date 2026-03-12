namespace DataMulingFilePackager
{
    public class AppConfig
    {
        public string FileInformationDbPath { get; set; }
        public string LogFolderLocation { get; set; }
        public string DataFilesPath { get; set; }
        public string PackageFilesPath { get; set; }
        public string PackageHeaderFileName { get; set; }
        public string OrganizationName { get; set; }
        public string SourceName { get; set; }
        public string DestinationURL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SecretEncryptionKey { get; set; }
        public int MaxPackageAgeDays { get; set; }
        public bool VerboseLog { get; set; }
    }
}
