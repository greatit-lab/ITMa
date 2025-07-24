// Services\EqpidManager.cs
using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;  // MySql.Data.dll 참조 필요
using ConnectInfo;            // ConnectInfo.dll( DatabaseInfo ) 참조
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
            // 1) ConnectInfo.dll을 통해 Default DB 정보 가져오기
            DatabaseInfo dbInfo = DatabaseInfo.CreateDefault();
            string connectionString = dbInfo.GetConnectionString();
        
            // 2) 시스템 정보 수집
            string osVersion = SystemInfoCollector.GetOSVersion();
            string architecture = SystemInfoCollector.GetArchitecture();
            string machineName = SystemInfoCollector.GetMachineName();
            string locale = SystemInfoCollector.GetLocale();
            string timeZone = SystemInfoCollector.GetTimeZone();
            string pcNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
        
                    // 3) 기존 데이터 존재 여부 확인
                    string selectQuery = @"
                        SELECT COUNT(*) FROM itm.agent_info 
                        WHERE eqpid = @eqpid AND pc_name = @pc_name;
                    ";
        
                    int existingRecords = 0;
                    using (MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn))
                    {
                        selectCmd.Parameters.AddWithValue("@eqpid", eqpid);
                        selectCmd.Parameters.AddWithValue("@pc_name", machineName);
        
                        existingRecords = Convert.ToInt32(selectCmd.ExecuteScalar());
                    }
        
                    // 4) 조건에 따른 처리
                    if (existingRecords > 0)
                    {
                        logManager.LogEvent($"[EqpidManager] Entry already exists for Eqpid: {eqpid} and PC Name: {machineName}. Skipping upload.");
                    }
                    else
                    {
                        // 데이터가 없으면 INSERT
                        string insertQuery = @"
                            INSERT INTO itm.agent_info
                            (eqpid, type, os, system_type, pc_name, locale, timezone, app_ver, reg_date, servtime)
                            VALUES
                            (@eqpid, @type, @os, @arch, @pc_name, @loc, @tz, @app_ver, @pc_now, NOW())
                            ON DUPLICATE KEY UPDATE
                                type = @type,
                                os = @os,
                                system_type = @arch,
                                locale = @loc,
                                timezone = @tz,
                                app_ver = @app_ver,
                                reg_date = @pc_now,
                                servtime = NOW();
                        ";
        
                        using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@eqpid", eqpid);
                            insertCmd.Parameters.AddWithValue("@type", type);
                            insertCmd.Parameters.AddWithValue("@os", osVersion);
                            insertCmd.Parameters.AddWithValue("@arch", architecture);
                            insertCmd.Parameters.AddWithValue("@pc_name", machineName);
                            insertCmd.Parameters.AddWithValue("@loc", locale);
                            insertCmd.Parameters.AddWithValue("@tz", timeZone);
                            insertCmd.Parameters.AddWithValue("@app_ver", appVersion);
                            insertCmd.Parameters.AddWithValue("@pc_now", pcNow);
        
                            int rowsAffected = insertCmd.ExecuteNonQuery();
                            logManager.LogEvent($"[EqpidManager] DB 업로드 완료. (rows inserted/updated={rowsAffected})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.LogError($"[EqpidManager] DB 업로드 실패: {ex.Message}");
            }
        }
    }
    
    public class SystemInfoCollector
    {
        public static string GetOSVersion()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["Caption"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Unknown OS (Error: {ex.Message})";
            }
            return "Unknown OS";
        }
    
        public static string GetArchitecture()
        {
            // 64비트 OS 여부 판단
            return Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        }
    
        public static string GetMachineName()
        {
            // 예: "DESKTOP-ABCD123"
            return Environment.MachineName;
        }
    
        public static string GetLocale()
        {
            // 현재 UI 문화권
            // 예: "ko-KR"
            return CultureInfo.CurrentUICulture.Name;
        }
    
        public static string GetTimeZone()
        {
            // 예: "Korea Standard Time"
            return TimeZoneInfo.Local.StandardName; 
        }
    }
}
