using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendLink
{
    class SQLiteHelper : IDisposable
    {
        private SQLiteConnection sqlConnection;
        private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\SendLink";
        private static string databaseFilePath = appDataPath + @"\pastelink.db";

        public SQLiteHelper()
        {
            System.IO.Directory.CreateDirectory(appDataPath);
            sqlConnection = new SQLiteConnection("Data Source=" + databaseFilePath);
            sqlConnection.Open();
            CreateTables();
        }


        private void CreateTables()
        {
            using (SQLiteCommand cmd = sqlConnection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS [Paste] ([Id] INTEGER NOT NULL, [PasteString] text NOT NULL, [ReceivedTime] text NOT NULL, [DeviceName] text NOT NULL, CONSTRAINT[sqlite_master_PK_Paste] PRIMARY KEY([Id])) ";
                cmd.ExecuteNonQuery();
            }
            
        }

        public void InsertPaste(Paste paste)
        {
            using (SQLiteCommand cmd = sqlConnection.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO [Paste]
                                        ([PasteString]
                                        ,[ReceivedTime]
                                        ,[DeviceName])
                                    VALUES
                                        (@PasteString
                                        ,@ReceivedTime
                                        ,@DeviceName);";
                cmd.Parameters.AddWithValue("@PasteString", paste.pasteString);
                cmd.Parameters.AddWithValue("@ReceivedTime", String.Format("{0:G}", paste.receivedTime));
                cmd.Parameters.AddWithValue("@DeviceName", paste.deviceName);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeletePaste(long pasteId)
        {
            
            using (SQLiteCommand cmd = sqlConnection.CreateCommand())
            {
                cmd.CommandText = @"DELETE FROM [Paste] WHERE [Id] == (@PasteId);";
                cmd.Parameters.AddWithValue("@PasteId", pasteId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<Paste> LoadPasteByOffset(int start, int offset)
        {
            List<Paste> nList = new List<Paste>();
            using(SQLiteCommand cmd = sqlConnection.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM [Paste] ORDER BY [ReceivedTime] DESC LIMIT (@OFFSET) OFFSET (@START)";
                cmd.Parameters.AddWithValue("@OFFSET", offset);
                cmd.Parameters.AddWithValue("@START", start);
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Paste nPaste = new Paste(
                        reader.GetString(reader.GetOrdinal("PasteString")),
                        DateTime.Parse(reader.GetString(reader.GetOrdinal("ReceivedTime"))),
                        reader.GetString(reader.GetOrdinal("DeviceName"))
                    );
                    nPaste.ID = reader.GetInt64(reader.GetOrdinal("Id"));
                    nList.Add(nPaste);
                }
            }
            return nList;
        }

        public SQLiteConnection GetSqlConnection()
        {
            return sqlConnection;
        }

        public void Dispose()
        {
            sqlConnection.Dispose();
        }
    }
}
