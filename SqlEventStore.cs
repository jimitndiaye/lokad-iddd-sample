#region (c) 2012-2012 Lokad - New BSD License 

// Copyright (c) Lokad 2012-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace lokad_iddd_sample
{
    /// <summary>
    /// This is a SQL event storage simplified to demonstrate essential principles.
    /// If you need more robust SQL implementation, check out Event Store of
    /// Jonathan Oliver
    /// </summary>
    public sealed class SqlEventStore : IEventStore
    {
        readonly string _connectionString;
        readonly IEventStoreStrategy _strategy;

        public SqlEventStore(string connectionString, IEventStoreStrategy strategy)
        {
            _connectionString = connectionString;
            _strategy = strategy;
        }

        public void Initialize()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                const string txt =
                    @"IF NOT EXISTS 
                        (SELECT * FROM sys.objects 
                            WHERE object_id = OBJECT_ID(N'[dbo].[Events]') 
                            AND type in (N'U'))

                        CREATE TABLE [dbo].[Events](
                            [Id] [int] PRIMARY KEY IDENTITY,
	                        [Name] [nvarchar](50) NOT NULL,
	                        [Version] [int] NOT NULL,
	                        [Data] [varbinary](max) NOT NULL
                        ) ON [PRIMARY]";
                using (var cmd = new SqlCommand(txt,conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public EventStream LoadEventStream(IIdentity id)
        {
            var name = _strategy.IdentityToString(id);

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                const string sql =
                    @"SELECT Data, Version FROM Events
                        WHERE Name = @p1
                        ORDER BY Version";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("p1", name);
                    var stream = new EventStream();
                    var version = 0;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = (byte[]) reader["Data"];
                            version = (int) reader["Version"];
                            stream.Events.Add(_strategy.DeserializeEvent(data));
                        }
                    }
                    stream.Version = version;
                    return stream;
                }
            }
        }

        public void AppendToStream(IIdentity id, int expectedVersion, ICollection<IEvent> events)
        {
            var name = _strategy.IdentityToString(id);
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql =
                        @"SELECT ISNULL(MAX(Version),0) 
                            FROM Events 
                            WHERE Name=@name";

                    using (var cmd = new SqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        var version = (int) cmd.ExecuteScalar();
                        if (version != expectedVersion)
                        {
                            throw EventStoreConcurrencyException.Create(version, expectedVersion, name);
                        }
                    }
                    var current = expectedVersion;
                    foreach (var @event in events)
                    {
                        const string txt =
                            @"INSERT INTO Events (Name,Version,Data) 
                                VALUES(@name,@version,@data)";

                        using (var cmd = new SqlCommand(txt, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@version", ++current);
                            cmd.Parameters.AddWithValue("@data", _strategy.SerializeEvent(@event));
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
        }
    }
}