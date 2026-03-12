namespace DataMulingFilePackager
{
    public class FileMetadata
    {
        public string OriginalFileName { get; set; }
        public string PackageName { get; set; }
        public DateTime LastModificationDate { get; set; }

        public FileMetadata() { }

        public FileMetadata(FileInfo fi, string packageName)
        {
            this.OriginalFileName = fi.Name;
            this.PackageName = packageName;
            this.LastModificationDate = fi.LastWriteTime;
        }
    }
}
