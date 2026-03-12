using FORAC.Utility;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace DataMulingFilePackager
{
    public class DataMulePackage
    {
        public string DirectoryPath { get; set; }
        public string DataFilePath { get; set; }
        public string PackageJSONFilePath { get; set; }

        public DataMulePackage(string directory, string dataFilePath, string packageJSONFileName)
        {
            this.DirectoryPath = directory;
            this.DataFilePath = dataFilePath;
            this.PackageJSONFilePath = packageJSONFileName;
        }
    }

    public class DataMulePackageJSONFile
    {
        public DataMulePackageJSONFileHeader header { get; set; }
        public string payload { get; set; }
        public string signature { get; set; }

        public void SetHeader(AppConfig config)
        {
            this.header = new DataMulePackageJSONFileHeader()
            {
                source = config.SourceName,
                destination = config.DestinationURL,
                organization = config.OrganizationName
            };
        }

        public void EncryptAndSetPayload(string jsonObj, string key)
        {
            this.payload = StringCipher.Encrypt(jsonObj, key);
        }

        public void SetSignature(string key)
        {
            this.signature = this.CreateSignature(key);
        }

        public bool ValidateSignature(string key)
        {
            string signature = CreateSignature(key);
            StringComparer compararer = StringComparer.Ordinal;
            return compararer.Compare(this.signature, signature) == 0;
        }

        private string CreateSignature(string key)
        {
            string encodedheader = Base64UrlEncoder.Encode(this.header.ToJsonString());
            string encodedPayload = Base64UrlEncoder.Encode(this.payload);
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.ASCII.GetBytes(key)))
            {
                byte[] signatureBytes = hmac.ComputeHash(Encoding.ASCII.GetBytes(encodedheader + "." + encodedPayload));
                return Convert.ToBase64String(signatureBytes);
            }
        }
    }

    public class DataMulePackageJSONFileHeader
    {
        public string source { get; set; }
        public string destination { get; set; }
        public string organization { get; set; }
        public string alg { get; set; } = "HS256";

        public string ToJsonString()
        {
            return $"{{\"organization\": \"{this.organization}\", \"source\": \"{this.source}\", \"destination\": \"{this.destination}\", \"alg\": \"{this.alg}\"}}";
        }
    }

    public class DataMulePackagePayload
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string DataFileName { get; set; }
        public DateTime DataFileDate { get; set; }
        public long DataFileSize { get; set; }
        public string DataFileSignature { get; set; }

        public DataMulePackagePayload(string dataFileName, DateTime dataFileDate, long dataFileSize, string username, string password, string dataFileSignature)
        {
            this.Username = username;
            this.Password = password;
            this.DataFileName = dataFileName;
            this.DataFileDate = dataFileDate;
            this.DataFileSize = dataFileSize;
            this.DataFileSignature = dataFileSignature;
        }

        public string ToJSONString()
        {
            return $"{{\"Username\": \"{this.Username}\", \"Password\": \"{this.Password}\", \"DataFileName\": \"{this.DataFileName}\", \"DataFileDate\": \"{this.DataFileDate.ToString("yyyy-MM-dd HH:mm:ss")}\", " +
                   $"\"DataFileSize\": {this.DataFileSize}, \"DataFileSignature\": \"{this.DataFileSignature}\"}}";
        }
    }
}
