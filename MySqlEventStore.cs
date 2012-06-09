using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace lokad_iddd_sample
{
    /// <summary>
    /// This is a SQL event storage simplified to demonstrate essential principles.
    /// If you need more robust SQL implementation, check out Event Store of
    /// Jonathan Oliver
    /// </summary>
    public sealed class MySqlEventStore : IEventStore
    {
        readonly string _connectionString;
        readonly IEventStoreStrategy _strategy;

        public MySqlEventStore(string connectionString, IEventStoreStrategy strategy)
        {
            _connectionString = connectionString;
            _strategy = strategy;
        }

        public void Initialize()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                const string txt = @"
CREATE TABLE IF NOT EXISTS `ES_Events` (
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

        public EventStream LoadEventStream(IIdentity id)
        {
            var name = _strategy.IdentityToString(id);

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                const string sql =
                    @"SELECT `Data`, `Version` FROM `ES_Events`
                        WHERE `Name` = ?p1
                        ORDER BY `Version`";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?p1", name);
                    var stream = new EventStream();
                    var version = 0;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = (byte[])reader["Data"];
                            version = (int)reader["Version"];
                            stream.Events.Add(_strategy.DeserializeEvent(data));
                        }
                    }
                    stream.Version = version;
                    return stream;
                }
            }
        }

        public EventStream LoadEventStreamAfterVersion(IIdentity id, int version)
        {
            throw new NotImplementedException();
        }

        public void AppendToStream(IIdentity id, int expectedVersion, ICollection<IEvent> events)
        {
            var name = _strategy.IdentityToString(id);
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql = @"
SELECT COALESCE(MAX(Version),0) 
FROM `ES_Events` 
WHERE Name=?name";

                    using (var cmd = new MySqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("?name", name);
                        var version = (int) (long) cmd.ExecuteScalar();
                        if (version != expectedVersion)
                        {
                            throw EventStoreConcurrencyException.Create(version, expectedVersion, name);
                        }
                    }
                    var current = expectedVersion;
                    foreach (var @event in events)
                    {
                        const string txt =
                            @"INSERT INTO `ES_Events` (`Name`, `Version`, `Data`) 
                                VALUES(?name, ?version, ?data)";

                        using (var cmd = new MySqlCommand(txt, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("?name", name);
                            cmd.Parameters.AddWithValue("?version", ++current);
                            cmd.Parameters.AddWithValue("?data", _strategy.SerializeEvent(@event));
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
        }
    }
}