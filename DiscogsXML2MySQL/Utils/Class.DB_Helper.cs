using System;
using System.Data;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using CDR.Logging;
using CDR.Console2File;

namespace CDR
{
    static class DB_Helper
    {
        // Belangrijk bij connectie string "charset=utf8mb4" anders kun je geen extended characters inserten
        // Wat voor andere opties je ook meegeeft en utf8mb4 probeert te forceren!
        private const string MYSQLCONNECTION_STRING = "Connection Protocol=Sockets;Connect Timeout=10;Port=3306; Server={0};Database={1};User Id={2};Password={3};Port={4};Pooling=false;Maximum Pool Size=100;Minimum Pool Size=0;Connection Lifetime=30;Default Command Timeout=300;Connection Reset=True;AllowUserVariables=true;Keepalive=0";

#if DEBUG
        public static string mysqlServer = "localhost";
        public static string mysqlDB = "discogs";
        public static string mysqlUser = "";
        public static string mysqlPassword = "";
        public static int mysqlServerPort = 3306;
#else
        public static string mysqlServer = "localhost";
        public static string mysqlDB  = "discogs";
        public static string mysqlUser = "";
        public static string mysqlPassword = "";
        public static int mysqlServerPort = 3306;
#endif

        public static MySqlConnection NewMySQLConnection(string dbName = null)
        {
            MySqlConnection connection = new MySqlConnection();

            if (string.IsNullOrEmpty(dbName))
            {
                dbName = mysqlDB;
            }

            // doe 3 pogingen!
            for (int i = 0; i <= 3; i++)
            {
                try
                {
                    string connectionString = string.Format(MYSQLCONNECTION_STRING, mysqlServer, dbName, mysqlUser, mysqlPassword, mysqlServerPort);

                    connection.ConnectionString = connectionString;
                    connection.Open();
                    if (connection == null || connection.State != ConnectionState.Open)
                    {
                        connection = null;
                        continue;
                    }

                    MySqlCommand command = new MySqlCommand();
                    command.Connection = connection;

                    // We willen natuurlijk in utf8 praten
                    // wacht 8 uur maximaal voordat na geen gebruik de connectie wordt beeindigd
                    command.CommandText = "SET NAMES utf8mb4;\r\n" +
                                          "SET SESSION wait_timeout = 28800;";
                    command.ExecuteNonQuery();

                    // als we hier komen dan is alles gelukt
                    break;
                }
                catch
                {
                    // Log the error
                    connection = null;
                }
            } //for


            return connection;
        }

        public static bool LOAD_DATA_LOCAL_INFILE(MySqlConnection conn, string tabFilename, string tablename, string fieldList = "", string autoincrementField = "")
        {
            try
            {
                tabFilename = tabFilename.Replace('\\', '/'); // is expected by mysql driver even on windows!?
                if (!System.IO.File.Exists(tabFilename))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(fieldList))
                {
                    fieldList = $"({fieldList})";
                }
                if (!string.IsNullOrEmpty(autoincrementField))
                {
                    autoincrementField = $"SET {autoincrementField} = NULL";
                }

                MySqlCommand command = new MySqlCommand();
                command.Connection = conn;
                command.CommandTimeout = 0; //no timeout!
                command.CommandType = CommandType.Text;
                command.CommandText = $"LOAD DATA LOCAL INFILE '{tabFilename}'" +
                                      $"  INTO TABLE `{tablename}`" +
                                      $"  FIELDS TERMINATED BY '\\t'" +
                                      $"  LINES TERMINATED BY '\\r\\n' {fieldList}" +
                                      $"  {autoincrementField};";
                command.ExecuteNonQuery();

                return true;
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError($"TableName={tablename} | {e.ToString()}");
                ConsoleLogger.WriteLine($"TableName={tablename}");
                ConsoleLogger.WriteLine(e.ToString());
            }

            return false;
        }


        public static string EscapeMySQL(int v)
        {
            return EscapeMySQL(v.ToString());
        }

        public static string EscapeMySQL(string s)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
            // delete any byte that's between 0x00 and 0x1F except 0x09 (tab), 0x0A (LF), and 0x0D (CR).
            foreach (char c in s)
            {
                if (c == '\\' || c == '\t' || c == '\n' || c == '\r')
                {
                    // escape char
                    switch (c)
                    {
                        case '\\':
                            sb.Append(@"\\");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                    } //switch
                }
                else if ((ushort)c > 0x1f)
                {
                    sb.Append(c);
                }
            } //foreach

            return sb.ToString();
        }

    }
}