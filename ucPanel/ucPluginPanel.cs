// ucPanel\ucPluginPanel.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ITM_Agent.Plugins;
using ITM_Agent.Services;

namespace ITM_Agent.ucPanel
{
    public partial class ucPluginPanel : UserControl
    {
        // 플러그인 정보를 보관하는 리스트 (PluginListItem은 플러그인명과 DLL 경로 정보를 저장)
        private List<PluginListItem> loadedPlugins = new List<PluginListItem>();
        private SettingsManager settingsManager;
        private LogManager logManager;
        
        // 플러그인 리스트가 변경될 때 통보용
        public event EventHandler PluginsChanged;
        
        public ucPluginPanel(SettingsManager settings)
        {
            InitializeComponent();
            settingsManager = settings;
            logManager = new LogManager(AppDomain.CurrentDomain.BaseDirectory);

            // settings.ini의 [RegPlugins] 섹션에서 기존에 등록된 플러그인 정보를 불러옴
            LoadPluginsFromSettings();
        }

        private void btn_PlugAdd_Click(object sender, EventArgs e)
        {
            /* 1) 파일 선택 대화상자 (전통적 using 블록) */
            using (OpenFileDialog open = new OpenFileDialog
            {
                Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            })
            {
                if (open.ShowDialog() != DialogResult.OK) return;
                string selectedDllPath = open.FileName;

                try
                {
                    /* 2) Assembly 로드 & 중복 체크 */
                    byte[] dllBytes = File.ReadAllBytes(selectedDllPath);
                    Assembly asm = Assembly.Load(dllBytes);
                    string pluginName = asm.GetName().Name;

                    if (loadedPlugins.Any(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("이미 등록된 플러그인입니다.", "중복", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    /* 3) Library 폴더 준비 */
                    string libraryFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library");
                    if (!Directory.Exists(libraryFolder)) Directory.CreateDirectory(libraryFolder);
                    
                    /* 4) 플러그인 DLL 복사 */
                    string destDllPath = Path.Combine(libraryFolder, Path.GetFileName(selectedDllPath));
                    if (File.Exists(destDllPath))
                    {
                        MessageBox.Show("동일한 DLL 파일이 이미 존재합니다.", "중복", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    File.Copy(selectedDllPath, destDllPath);

                    /* 5) 참조 DLL 자동 복사 */
                    foreach (AssemblyName refAsm in asm.GetReferencedAssemblies())
                    {
                        string refFile = refAsm.Name + ".dll";
                        string srcRef = Path.Combine(Path.GetDirectoryName(selectedDllPath), refFile);
                        string dstRef = Path.Combine(libraryFolder, refFile);
                        
                        if (File.Exists(srcRef) && !File.Exists(dstRef))
                            File.Copy(srcRef, dstRef);
                    }
                    
                    /* 6) 필수 NuGet DLL 강제 복사 (CodePages 등) */
                    string[] mustHave = { "System.Text.Encoding.CodePages.dll" };
                    foreach (var f in mustHave)
                    {
                        string src = Path.Combine(Path.GetDirectoryName(selectedDllPath), f);
                        string dst = Path.Combine(libraryFolder, f);
                        if (File.Exists(src) && !File.Exists(dst))
                            File.Copy(src, dst);
                    }
                    /* 7) 목록 설정 등록 */
                    var version = asm.GetName().Version.ToString();  // 어셈블리 버전 추출
                    var item = new PluginListItem
                    {
                        PluginName  = pluginName,
                        PluginVersion = version,     // ★ 설정
                        AssemblyPath= destDllPath
                    };
                    
                    loadedPlugins.Add(item);
                    //lb_PluginList.Items.Add(item.PluginName);
                    lb_PluginList.Items.Add(item.ToString());  // ★ 버전 포함 표시
                    SavePluginInfoToSettings(item);
                    logManager.LogEvent($"Plugin registered: {pluginName}");
                    PluginsChanged?.Invoke(this, EventArgs.Empty);     // ✅ 새로 추가
                }
                catch (Exception ex)
                {
                    MessageBox.Show("플러그인 로드 오류: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    logManager.LogError("플러그인 로드 오류: " + ex);
                }
            }   // using OpenFileDialog
        }

        /// <summary>
        /// btn_PlugRemove 클릭 이벤트 핸들러  
        /// lb_PluginList에서 선택된 플러그인을 삭제 전 확인 메시지를 띄우고,  
        /// loadedPlugins와 lb_PluginList, settings.ini의 [RegPlugins] 섹션, 그리고 Library 폴더의 DLL 파일을 삭제합니다.
        /// </summary>
        private void btn_PlugRemove_Click(object sender, EventArgs e)
        {
            if (lb_PluginList.SelectedItem == null)
            {
                MessageBox.Show("삭제할 플러그인을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedPluginName = lb_PluginList.SelectedItem.ToString();
            DialogResult result = MessageBox.Show($"플러그인 '{selectedPluginName}'을(를) 삭제하시겠습니까?", 
                                                  "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                var pluginItem = loadedPlugins.FirstOrDefault(p => p.PluginName.Equals(selectedPluginName, StringComparison.OrdinalIgnoreCase));
                if (pluginItem != null)
                {
                    // Library 폴더의 DLL 파일 삭제 시도
                    if (File.Exists(pluginItem.AssemblyPath))
                    {
                        try
                        {
                            File.Delete(pluginItem.AssemblyPath);
                            logManager.LogEvent($"DLL 파일 삭제됨: {pluginItem.AssemblyPath}");

                            // 삭제 후 파일이 남아있으면(파일 잠김 등) 안내
                            if (File.Exists(pluginItem.AssemblyPath))
                            {
                                MessageBox.Show("DLL 파일이 사용 중이거나 삭제되지 않았습니다. 프로그램을 재시작 후 다시 시도하세요.",
                                    "파일 삭제 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                logManager.LogError("DLL 파일이 삭제되지 않았음(잠김 등): " + pluginItem.AssemblyPath);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("DLL 파일 삭제 중 오류 발생: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            logManager.LogError("DLL 파일 삭제 중 오류: " + ex.Message);
                            return;
                        }
                    }
                    loadedPlugins.Remove(pluginItem);
                }
                lb_PluginList.Items.Remove(selectedPluginName);

                // settings.ini의 [RegPlugins] 섹션에서 해당 키 제거
                settingsManager.RemoveKeyFromSection("RegPlugins", selectedPluginName);
                logManager.LogEvent($"Plugin removed: {selectedPluginName}");
                PluginsChanged?.Invoke(this, EventArgs.Empty);     // ✅ 새로 추가
            }
        }

        /// <summary>
        /// settings.ini의 [RegPlugins] 섹션에 플러그인 정보를 기록합니다.
        /// 형식 예시:
        /// [RegPlugins]
        /// MyPlugin = C:\... \Library\MyPlugin.dll
        /// </summary>
        private void SavePluginInfoToSettings(PluginListItem pluginItem)
        {
            // (1)  플러그인 DLL은 항상 BaseDir\Library 에 복사되므로
            //      ini 파일에는 "Library\파일명.dll" 만 저장
            string relativePath = Path.Combine("Library", Path.GetFileName(pluginItem.AssemblyPath));
        
            // (2)  Settings.ini → [RegPlugins] 섹션에 기록
            settingsManager.SetValueToSection("RegPlugins",
                pluginItem.PluginName,
                relativePath);                       //  ← Library\Onto_WaferFlatData.dll
        }

        private void LoadPluginsFromSettings()
        {
            // ① [RegPlugins] 섹션 라인 전체 읽기
            var pluginEntries = settingsManager.GetFoldersFromSection("[RegPlugins]");
            foreach (string entry in pluginEntries)
            {
                // "PluginName = AssemblyPath" 형식 파싱
                string[] parts = entry.Split(new[] { '=' }, 2);
                if (parts.Length != 2) continue;
        
                string iniKeyName   = parts[0].Trim();   // INI에 기록된 키(=플러그인명)
                string assemblyPath = parts[1].Trim();   // 상대 or 절대 경로
        
                // ② 상대 경로 → 절대 경로 변환
                if (!Path.IsPathRooted(assemblyPath))
                    assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyPath);
        
                if (!File.Exists(assemblyPath))
                {
                    logManager.LogError($"플러그인 DLL을 찾을 수 없습니다: {assemblyPath}");
                    continue;
                }
        
                try
                {
                    /* ③ DLL 메모리 로드 → 파일 잠금 방지 */
                    byte[] dllBytes = File.ReadAllBytes(assemblyPath);
                    Assembly asm    = Assembly.Load(dllBytes);
        
                    /* ④ 어셈블리 메타데이터 추출 */
                    string asmName    = asm.GetName().Name;              // 실제 어셈블리 이름
                    string asmVersion = asm.GetName().Version.ToString();// 버전 문자열
        
                    /* ⑤ PluginListItem 구성 */
                    var item = new PluginListItem
                    {
                        PluginName    = asmName,
                        PluginVersion = asmVersion,
                        AssemblyPath  = assemblyPath
                    };
        
                    loadedPlugins.Add(item);                 // 내부 리스트 보존
                    lb_PluginList.Items.Add(item.ToString()); // "Name (v1.2.3.4)" 형식 표시
        
                    logManager.LogEvent($"Plugin auto-loaded: {item.ToString()}");
                }
                catch (Exception ex)
                {
                    logManager.LogError($"플러그인 로드 실패: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 외부에서 로드된 플러그인 목록을 반환합니다.
        /// </summary>
        public List<PluginListItem> GetLoadedPlugins()
        {
            return loadedPlugins;
        }
    }
}
