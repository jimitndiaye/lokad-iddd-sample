using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Sample.StorageImplementations.mySql
{
    /// <summary>
    /// This is a SQL event storage simplified to demonstrate essential principles.
    /// If you need more robust SQL implementation, check out Event Store of
    /// Jonathan Oliver
    /// </summary>
    public sealed class MySqlEventStore : IAppendOnlyStore
    {
        readonly string _connectionString;
        

        public MySqlEventStore(string connectionString)
        {
            _connectionString = connectionString;
            
        }

        public void Initialize()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                const string txt = @"
CREATE TABLE IF NOT EXISTS `ES_Events` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` nvarchar(50) NOT NULL,
    `Version` int NOT NULL,
    `Data` varbinary(10240) NOT NULL
)";
                using (var cmd = new MySqlCommand(txt, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AppendToStream(IIdentity id, int expectedVersion, ICollection<IEvent> events)
        {
            
        }

        public void Dispose()
        {
            
        }

        public void Append(string key, byte[] buffer, int serverVersion = -1)
        {
            
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
SELECT COALESCE(MAX(Version),0) 
FROM `ES_Events` 
WHERE Name=?name";
                    int version;
                    using (var cmd = new MySqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("?name", key);
                        version = (int)cmd.ExecuteScalar();
                        if (serverVersion != -1)
                        {
                            if (version != serverVersion)
                            {
                                throw new AppendOnlyStoreConcurrencyException(version, serverVersion, key);
                            }
                        }
                    }

                    const string txt =
                           @"INSERT INTO `ES_Events` (`Name`, `Version`, `Data`) 
                                VALUES(?name, ?version, ?data)";

                    using (var cmd = new MySqlCommand(txt, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("?name", key);
                        cmd.Parameters.AddWithValue("?version", version+1);
                        cmd.Parameters.AddWithValue("?data", buffer);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
        }

        public IEnumerable<TapeRecord> ReadRecords(string key, int afterVersion, int maxCount)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                const string sql =
                    @"SELECT `Data`, `Version` FROM `ES_Events`
                        WHERE `Name` = ?p1 AND `Version`>?p2
                        ORDER BY `Version`
                        LIMIT 0,?take";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?p1", key);
                    cmd.Parameters.AddWithValue("?p2", afterVersion);
                    cmd.Parameters.AddWithValue("?take", maxCount);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = (byte[])reader["Data"];
                            var version = (int)reader["Version"];
                            yield return new TapeRecord(version, data);
                        }
                    }
                }
            }
        }

        public IEnumerable<AppendRecord> ReadRecords(int afterVersion, int maxCount)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                const string sql =
                    @"SELECT `Data`, `Name` FROM `ES_Events`
                        WHERE `Id`>?after
                        ORDER BY `Id`
                        LIMIT 0,?take";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?after", afterVersion);
                    cmd.Parameters.AddWithValue("?take", maxCount);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = (byte[])reader["Data"];
                            var name = (string)reader["Name"];
                            yield return new AppendRecord(name, data);
                        }
                    }
                }
            }
        }

        

        public void Close()
        {
            
        }
    }
}