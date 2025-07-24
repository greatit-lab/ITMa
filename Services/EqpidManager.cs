// Services\EqpidManager.cs
using System;
using System.Windows.Forms;
//using MySql.Data.MySqlClient;  // ☒ MySql 드라이버 제거
using Npgsql;                 // ☑ PostgreSQL 드라이버 추가
using ConnectInfo;            // DatabaseInfo 참조 (변경 없음)
using System.Globalization;
using System.Management;

namespace ITM_Agent.Services
{
    /// <summary>
    /// Eqpid 값을 관리하는 클래스입니다.
    /// 설정 파일(Settings.ini)에 [Eqpid] 섹션이 없거나 값이 없을 경우 EqpidInputForm을 통해 장비명을 입력받아 저장합니다.
    /// </summary>
    public class EqpidManager
    {
        private readonly SettingsManager settingsManager;
        private readonly LogManager logManager;
        private readonly string appVersion;

        public EqpidManager(SettingsManager settings, LogManager logManager, string appVersion)
        {
            this.settingsManager = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            this.appVersion = appVersion ?? throw new ArgumentNullException(nameof(appVersion));
        }

        public void InitializeEqpid()
        {
            logManager.LogEvent("[EqpidManager] Initializing Eqpid.");
            
            string eqpid = settingsManager.GetEqpid();
            if (string.IsNullOrEmpty(eqpid))
            {
                logManager.LogEvent("[EqpidManager] Eqpid is empty. Prompting for input.");
                PromptForEqpid();
            }
            else
            {
                logManager.LogEvent($"[EqpidManager] Eqpid found: {eqpid}");
            }
        }

        private void PromptForEqpid()
        {
            bool isValidInput = false;
        
            while (!isValidInput)
            {
                using (var form = new EqpidInputForm())
                {
                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        // (기존 코드) eqpid, type 설정
                        logManager.LogEvent($"[EqpidManager] Eqpid input accepted: {form.Eqpid}");
                        settingsManager.SetEqpid(form.Eqpid.ToUpper());
                        settingsManager.SetType(form.Type);
                        logManager.LogEvent($"[EqpidManager] Type set to: {form.Type}");
                        
                        // 여기서 DB 업로드를 호출
                        UploadAgentInfoToDatabase(form.Eqpid.ToUpper(), form.Type);
        
                        isValidInput = true;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        // (기존 코드)
                        logManager.LogEvent("[EqpidManager] Eqpid input canceled. Application will exit.");
                        MessageBox.Show("Eqpid 입력이 취소되었습니다. 애플리케이션을 종료합니다.",
                                        "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Environment.Exit(0);
                    }
                }
            }
        }
        
        /// <summary>
        /// Eqpid와 Type, 그리고 PC 시스템 정보를 DB에 업로드하는 메서드
        /// </summary>
        private void UploadAgentInfoToDatabase(string eqpid, string type)
        {
            /* 0) 연결 문자열 */
            string connString = DatabaseInfo.CreateDefault().GetConnectionString();
        
            /* 1) 시스템 정보 수집 */
            string osVersion = SystemInfoCollector.GetOSVersion();
            string architecture = SystemInfoCollector.GetArchitecture();
            string machineName = SystemInfoCollector.GetMachineName();
            string locale = SystemInfoCollector.GetLocale();
            string timeZone = SystemInfoCollector.GetTimeZone();
            //string pcNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            DateTime pcNow = DateTime.Now;    // 그대로 DateTime
            
            try
            {
                using (var conn = new NpgsqlConnection(connString))     // ☑ NpgsqlConnection
                {
                    conn.Open();
        
                    /* 2) 기존 레코드 존재 여부 확인 */
                    const string SELECT_SQL = @"
                        SELECT COUNT(*) FROM public.agent_info
                        WHERE eqpid = @eqpid AND pc_name = @pc_name;";
                    int existingRecords = 0;
                    using (var selCmd = new NpgsqlCommand(SELECT_SQL, conn))
                    {
                        selectCmd.Parameters.AddWithValue("@eqpid", eqpid);
                        selectCmd.Parameters.AddWithValue("@pc_name", machineName);
                        existingRecords = Convert.ToInt32(selectCmd.ExecuteScalar());
                    }
                    
                    if (existingRecords > 0)
                    {
                        logManager.LogEvent(
                            $"[EqpidManager] Entry already exists for Eqpid: {eqpid} and PC Name: {machineName}. Skipping upload.");
                        return;
                    }
                    
                    /* 3) INSERT … ON CONFLICT → UPSERT */
                    const string insertQuery = @"
                        INSERT INTO public.agent_info
                        (eqpid, type, os, system_type, pc_name, locale, timezone, app_ver, reg_date, servtime)
                        VALUES
                        (@eqpid, @type, @os, @arch, @pc_name, @loc, @tz, @app_ver, @pc_now::timestamp(0), NOW()::timestamp(0))
                        ON CONFLICT (eqpid, pc_name)
                        DO UPDATE SET
                            type = EXCLUDED.type,
                            os = EXCLUDED.os,
                            system_type = EXCLUDED.system_type,
                            locale = EXCLUDED.locale,
                            timezone = EXCLUDED.timezone,
                            app_ver = EXCLUDED.app_ver,
                            reg_date = EXCLUDED.reg_date,
                            servtime = NOW();";

                    using (var insCmd = new NpgsqlCommand(INSERT_SQL, conn))
                    {
                        insCmd.Parameters.AddWithValue("@eqpid", eqpid);
                        insCmd.Parameters.AddWithValue("@type", type);
                        insCmd.Parameters.AddWithValue("@os", osVersion);
                        insCmd.Parameters.AddWithValue("@arch", architecture);
                        insCmd.Parameters.AddWithValue("@pc_name", machineName);
                        insCmd.Parameters.AddWithValue("@loc", locale);
                        insCmd.Parameters.AddWithValue("@tz", timeZone);
                        insCmd.Parameters.AddWithValue("@app_ver", appVersion);
                        //insCmd.Parameters.AddWithValue("@pc_now", pcNow);
                        insCmd.Parameters.Add("@pc_now",NpgsqlTypes.NpgsqlDbType.Timestamp).Value = pcNow;
    
                        int rows = insCmd.ExecuteNonQuery();
                        logManager.LogEvent($"[EqpidManager] DB 업로드 완료. (rows inserted/updated={rows})");
                    }
                }
            }
            /* 23505 = unique_violation */
            catch (PostgresException pex) when (pex.SqlState == "23505")
            {
                logManager.LogEvent($"[EqpidManager] Duplicate entry skipped: {pex.Message}");
            }
            catch (Exception ex)
            {
                logManager.LogError($"[EqpidManager] DB 업로드 실패: {ex.Message}");
            }
        }
    }
    
    public static class SystemInfoCollector
    {
        public static string GetOSVersion()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                        return obj["Caption"].ToString();
                }
            }
            catch (Exception ex)
            {
                return $"Unknown OS (Error: {ex.Message})";
            }
            return "Unknown OS";
        }
    
        public static string GetArchitecture() => 
            Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

        public static string GetMachineName() => Environment.MachineName;

        public static string GetLocale() => CultureInfo.CurrentUICulture.Name;

        public static string GetTimeZone() => TimeZoneInfo.Local.StandardName;
    }
}
