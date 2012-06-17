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
    `Data` LONGBLOB NOT NULL
)";
                using (var cmd = new MySqlCommand(txt, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Dispose()
        {
            
        }

        public void Append(string name, byte[] data, int expectedVersion)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql =
                        @"SELECT COALESCE(MAX(Version),0) 
                            FROM `ES_Events` 
                            WHERE Name=?name";
                    int version;
                    using (var cmd = new MySqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("?name", name);
                        version = (int)cmd.ExecuteScalar();
                        if (expectedVersion != -1)
                        {
                            if (version != expectedVersion)
                            {
                                throw new AppendOnlyStoreConcurrencyException(version, expectedVersion, name);
                            }
                        }
                    }

                    const string txt =
                           @"INSERT INTO `ES_Events` (`Name`, `Version`, `Data`) 
                                VALUES(?name, ?version, ?data)";

                    using (var cmd = new MySqlCommand(txt, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("?name", name);
                        cmd.Parameters.AddWithValue("?version", version+1);
                        cmd.Parameters.AddWithValue("?data", data);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
        }

        public IEnumerable<DataWithVersion> ReadRecords(string name, int afterVersion, int maxCount)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                const string sql =
                    @"SELECT `Data`, `Version` FROM `ES_Events`
                        WHERE `Name` = ?name AND `Version`>?version
                        ORDER BY `Version`
                        LIMIT 0,?take";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?name", name);
                    cmd.Parameters.AddWithValue("?version", afterVersion);
                    cmd.Parameters.AddWithValue("?take", maxCount);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = (byte[])reader["Data"];
                            var version = (int)reader["Version"];
                            yield return new DataWithVersion(version, data);
                        }
                    }
                }
            }
        }

        public IEnumerable<DataWithName> ReadRecords(int afterVersion, int maxCount)
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
                            yield return new DataWithName(name, data);
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