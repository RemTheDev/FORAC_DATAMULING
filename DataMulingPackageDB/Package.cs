using Microsoft.Data.Sqlite;
using System.Globalization;

namespace DataMulingPackageDB
{
    public class Package
    {
        public const string TABLE_NAME = "Packages";
        public const string FIELD_ROWID = "rowid";
        public const string FIELD_ORIGINALNAME = "OriginalName";
        public const string FIELD_LASTWRITTENTO = "LastWrittenTo";
        public const string FIELD_PACKAGENAME = "PackageName";
        public const string FIELD_SENTANDRECEIVED = "SentAndReceived";

        public int RowId { get; set; }
        public string OriginalName { get; set; }
        public DateTime LastWrittenTo { get; set; }
        public string PackageName { get; set; }
        public bool SentAndReceived { get; set; }

        public string GetInsertQuery()
        {
            return $"INSERT INTO {TABLE_NAME} ({FIELD_ORIGINALNAME}, {FIELD_LASTWRITTENTO}, {FIELD_PACKAGENAME}, {FIELD_SENTANDRECEIVED}) " +
                   $"VALUES ('{this.OriginalName}', '{this.LastWrittenTo.ToString("yyyy-MM-dd HH:mm:ss")}', '{this.PackageName}', {(this.SentAndReceived ? "TRUE" : "FALSE")})";
        }

        public static List<Package> GetPackagesFromSelectQueryResult(SqliteDataReader resultReader)
        {
            List<Package> result = new List<Package>();
            while (resultReader.Read())
            {
                Package package = new Package();
                package.RowId = resultReader.GetInt32(0);
                package.OriginalName = resultReader.GetString(1);
                if(!resultReader.IsDBNull(2))
                    package.LastWrittenTo = DateTime.ParseExact(resultReader.GetString(2), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                package.PackageName = resultReader.GetString(3);
                package.SentAndReceived = resultReader.GetBoolean(4);
                result.Add(package);
            }

            return result;
        }
    }
}
