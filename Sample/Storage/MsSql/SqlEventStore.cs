#region (c) 2012-2012 Lokad - New BSD License 

// Copyright (c) Lokad 2012-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System.Collections.Generic;
using System.Data.SqlClient;

namespace Sample.Storage.MsSql
{
    /// <summary>
    /// <para>This is a SQL event storage simplified to demonstrate essential principles.
    /// If you need more robust SQL implementation, check out Event Store of
    /// Jonathan Oliver</para>
    /// <para>This code is frozen to match IDDD book. For latest practices see Lokad.CQRS Project</para>
    /// </summary>
    public sealed class SqlAppendOnlyStore : IAppendOnlyStore
    {
        readonly string _connectionString;
        

        public SqlAppendOnlyStore(string connectionString)
        {
            _connectionString = connectionString;
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
                        ) ON [PRIMARY]
";
                using (var cmd = new SqlCommand(txt,conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public void Dispose()
        {
            
        }

        public void Append(string name, byte[] data, long expectedVersion = -1)
        {
            
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    const string sql =
                        @"SELECT ISNULL(MAX(Version),0) 
                            FROM Events 
                            WHERE Name=@name";

                    int version;
                    using (var cmd = new SqlCommand(sql, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        version = (int)cmd.ExecuteScalar();
                        if (expectedVersion >= 0)
                        {
                            if (version != expectedVersion)
                            {
                                throw new AppendOnlyStoreConcurrencyException(version, expectedVersion, name);
                            }
                        }
                    }
                    const string txt =
                           @"INSERT INTO Events (Name,Version,Data) 
                                VALUES(@name,@version,@data)";

                    using (var cmd = new SqlCommand(txt, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@version", version + 1);
                        cmd.Parameters.AddWithValue("@data", data);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
        }

        public IEnumerable<DataWithVersion> ReadRecords(string name, long afterVersion, int maxCount)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                const string sql =
                    @"SELECT TOP (@take) Data, Version FROM Events
                        WHERE Name = @p1 AND Version > @skip
                        ORDER BY Version";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@p1", name);
                    cmd.Parameters.AddWithValue("@take", maxCount);
                    cmd.Parameters.AddWithValue("@skip", afterVersion);
                    
                    
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

        public IEnumerable<DataWithName> ReadRecords(long afterVersion, int maxCount)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                const string sql =
                    @"SELECT TOP (@take) Data, Name FROM Events
                        WHERE Id > @skip
                        ORDER BY Id";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    
                    cmd.Parameters.AddWithValue("@take", maxCount);
                    cmd.Parameters.AddWithValue("@skip", afterVersion);

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