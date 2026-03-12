using System.Security.Cryptography;

namespace FORAC.Utility
{
    public static class FileHelper
    {
        public static readonly string TEMP_FOLDER_PATH = Path.GetTempPath();

        public static string GetTempDirectoryFilePath()
        {
            return Path.Combine(TEMP_FOLDER_PATH, Guid.NewGuid().ToString());
        }

        public static string GetTempFilePath(string suffix)
        {
            return Path.Combine(TEMP_FOLDER_PATH, GetTempFileName(suffix));
        }

        public static string GetTempFileName(string suffix = "")
        {
            return Guid.NewGuid().ToString() + suffix;
        }

        public static string CreateSignatureForFile(string filePath)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream inStream = new FileStream(filePath, FileMode.Open))
                {
                    byte[] hashValue = md5.ComputeHash(inStream);

                    return Convert.ToBase64String(hashValue);
                }
            }
        }

        public static void DeleteDirectoryAndContentIfExists(IEnumerable<string> directoryPaths)
        {
            foreach (string dirName in directoryPaths)
            {
                if (Directory.Exists(dirName))
                    Directory.Delete(dirName, true);
            }
        }

        public static void DeleteFileIfExists(IEnumerable<string> fileNames)
        {
            foreach (string fileName in fileNames)
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }
        }
    }
}
