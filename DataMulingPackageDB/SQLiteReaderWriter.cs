using Microsoft.Data.Sqlite;

namespace DataMulingPackageDB
{
    public class SQLiteReaderWriter : IDisposable
    {
        private SqliteConnection _connection;

        public SQLiteReaderWriter(string dbFilePath)
        {
            this._connection = new SqliteConnection($"Data Source={dbFilePath};");
            this._connection.Open();
        }

        public void Dispose()
        {
            this._connection.Close();
            this._connection.Dispose();
        }

        public List<Package> GetAllUnreceivedPackages()
        {
            SqliteCommand qry = this._connection.CreateCommand();
            qry.CommandText = $"SELECT rowId, * FROM {Package.TABLE_NAME} WHERE {Package.FIELD_PACKAGENAME} IS NOT NULL AND {Package.FIELD_SENTANDRECEIVED} IS FALSE";

            return Package.GetPackagesFromSelectQueryResult(qry.ExecuteReader());
        }

        public List<Package> GetFileInfoByOriginalName(string originalFileName)
        {
            SqliteCommand qry = this._connection.CreateCommand();
            qry.CommandText = $"SELECT rowId, * FROM {Package.TABLE_NAME} WHERE {Package.FIELD_ORIGINALNAME} ='{originalFileName}'";

            return Package.GetPackagesFromSelectQueryResult(qry.ExecuteReader());
        }

        public List<Package> GetFileInfoByPackageName(string packageName)
        {
            SqliteCommand qry = this._connection.CreateCommand();
            qry.CommandText = $"SELECT rowId, * FROM {Package.TABLE_NAME} WHERE {Package.FIELD_PACKAGENAME} ='{packageName}'";

            return Package.GetPackagesFromSelectQueryResult(qry.ExecuteReader());
        }

        public List<Package> GetReceivedFileInfoOlderThanXDays(int nbDays)
        {
            DateTime olderThan = DateTime.Now.AddDays(nbDays * -1);

            SqliteCommand qry = this._connection.CreateCommand();
            qry.CommandText = $"SELECT rowId, * FROM {Package.TABLE_NAME} WHERE date({Package.FIELD_LASTWRITTENTO}) < date('{olderThan.ToString("yyyy-MM-dd HH:mm:ss")}') " +
                              $"AND {Package.FIELD_SENTANDRECEIVED} IS TRUE";

            return Package.GetPackagesFromSelectQueryResult(qry.ExecuteReader());
        }

        public void InsertPackageInfo(Package package)
        {
            SqliteCommand insert = this._connection.CreateCommand();
            insert.CommandText = package.GetInsertQuery();
            insert.ExecuteNonQuery();
        }

        public void SetPackageReceived(int packageId, bool isReceived)
        {
            SqliteCommand updateCommand = this._connection.CreateCommand();
            updateCommand.CommandText = $"UPDATE {Package.TABLE_NAME} SET {Package.FIELD_SENTANDRECEIVED} = {(isReceived ? "TRUE" : "FALSE")} WHERE {Package.FIELD_ROWID} = {packageId}";
            updateCommand.ExecuteNonQuery();
        }

        public void DeletePackageInfo(int packageId) 
        {
            SqliteCommand deleteCmd = this._connection.CreateCommand();
            deleteCmd.CommandText = $"DELETE FROM {Package.TABLE_NAME} WHERE {Package.FIELD_ROWID} = {packageId}";
            deleteCmd.ExecuteNonQuery();
        }
    }
}