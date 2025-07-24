// ConnectInfo\DatabaseInfo.cs
using System;
using MySql.Data.MySqlClient;

namespace ConnectInfo
{
    public sealed class DatabaseInfo
    {
        /* -- 하드코딩된 접속 정보 -- */
        private const string _server = "00.000.00.00";
        private const string _database = "itm";
        private const string _userId = "userid";
        private const string _password = "pw";
        private const int _port = 3306;
        
        private DatabaseInfo() {}
        
        public static DatabaseInfo CreateDefault() => new DatabaseInfo();
        
        public string GetConnectionString()
        {
            var csb = new MySqlConnectionStringBuilder
            {
                Server = _server,
                Database = _database,
                UserID = _userId,
                Password = _password,
                Port = (uint)_port,
                SslMode = MySqlSslMode.Disabled,    // MySqlSslMode.None => Disabled 로 변경
                CharacterSet = "utf8"
            };
            return csb.ConnectionString;
        }
        
        /* C# 7.3 호환: 전통적 using 블록 */
        public void TestConnection()
        {
            Console.WriteLine($"[DB] Connection ▶ {GetConnectionString()}");
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                Console.WriteLine($"[DB] 연결 성공 ▶ {conn.ServerVersion}");
            }
        }
    }
}
