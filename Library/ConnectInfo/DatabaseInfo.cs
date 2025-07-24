// ConnectInfo\DatabaseInfo.cs
using System;
using Npgsql;   // ★ MySql → Npgsql 교체

namespace ConnectInfo
{
    public sealed class DatabaseInfo
    {
        /* ── 하드코딩 예시는 그대로 두고 포트만 PostgreSQL 기본값(5432) ── */
        private const string _server   = "00.000.00.00";
        private const string _database = "itm";
        private const string _userId   = "userid";
        private const string _password = "pw";
        private const int    _port     = 5432;

        private DatabaseInfo() { }
        public static DatabaseInfo CreateDefault() => new DatabaseInfo();

        /// <summary>
        /// PostgreSQL 전용 연결 문자열 생성
        /// </summary>
        public string GetConnectionString()                        // ★ 전체 교체
        {
            var csb = new NpgsqlConnectionStringBuilder
            {
                Host     = _server,
                Database = _database,
                Username = _userId,
                Password = _password,
                Port     = _port,
                Encoding = "UTF8",
                SslMode  = SslMode.Disable,   // 필요 시 Enable 로 변경
                // ▼ 기본 스키마를 public 으로 지정
                SearchPath = "public"
            };
            return csb.ConnectionString;
        }

        /// <summary>
        /// DB 연결 테스트(콘솔 전용)
        /// </summary>
        public void TestConnection()                               // ★ 전체 교체
        {
            Console.WriteLine($"[DB] Connection ▶ {GetConnectionString()}");

            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                Console.WriteLine($"[DB] 연결 성공 ▶ {conn.PostgreSqlVersion}");
            }
        }
    }
}
