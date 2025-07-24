// ucPanel\ucConfigurationPanel.cs
using ITM_Agent.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ITM_Agent.ucPanel
{
    /// <summary>
    /// TargetFolders, ExcludeFolders, BaseFolder, Regex 패턴 관리를 위한 UI 패널.
    /// SettingsManager를 통해 설정을 로드/저장하며,
    /// 사용자 액션 시 SettingsManager에 반영하고 UI를 갱신합니다.
    /// </summary>
    public partial class ucConfigurationPanel : UserControl
    {
        public event Action<string, Color> StatusUpdated;
        public event Action ListSelectionChanged;

        private readonly SettingsManager settingsManager;

        private const string TargetFoldersSection = "[TargetFolders]";
        private const string ExcludeFoldersSection = "[ExcludeFolders]";
        private const string RegexSection = "[Regex]";

        public string BaseFolderPath
        {
            get => lb_BaseFolder.Text; // lb_BaseFolder의 텍스트를 반환
        }


        public ucConfigurationPanel(SettingsManager settingsManager)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));

            InitializeComponent();
            LoadDataFromSettings();

            // 이벤트 핸들러 등록
            btn_TargetFolder.Click += (s, e) => AddFolder(TargetFoldersSection, lb_TargetList);
            btn_TargetRemove.Click += (s, e) => RemoveSelectedFolders(TargetFoldersSection, lb_TargetList);

            btn_ExcludeFolder.Click += (s, e) => AddFolder(ExcludeFoldersSection, lb_ExcludeList);
            btn_ExcludeRemove.Click += (s, e) => RemoveSelectedFolders(ExcludeFoldersSection, lb_ExcludeList);

            btn_BaseFolder.Click += btn_BaseFolder_Click;

            btn_RegAdd.Click += btn_RegAdd_Click;
            btn_RegEdit.Click += btn_RegEdit_Click;
            btn_RegRemove.Click += btn_RegRemove_Click;

            // 컨트롤 변경 시 상태 체크
            lb_TargetList.SelectedIndexChanged += (s, e) => ValidateRunButtonState();
            lb_RegexList.SelectedIndexChanged += (s, e) => ValidateRunButtonState();
            lb_BaseFolder.TextChanged += (s, e) => ValidateRunButtonState();

            // 목록 상태 변경 시 외부에도 알림
            lb_TargetList.SelectedIndexChanged += (s, e) => ListSelectionChanged?.Invoke();
            lb_ExcludeList.SelectedIndexChanged += (s, e) => ListSelectionChanged?.Invoke();
            lb_RegexList.SelectedIndexChanged += (s, e) => ListSelectionChanged?.Invoke();
        }

        private void LoadDataFromSettings()
        {
            // TargetFolders 로드
            LoadFolders(TargetFoldersSection, lb_TargetList);

            // ExcludeFolders 로드
            LoadFolders(ExcludeFoldersSection, lb_ExcludeList);

            // BaseFolder 로드
            LoadBaseFolder();

            // Regex 로드
            LoadRegexFromSettings();
        }

        private void LoadFolders(string section, ListBox listBox)
        {
            listBox.Items.Clear();
            var folders = settingsManager.GetFoldersFromSection(section);
            int index = 1;
            foreach (var folder in folders)
            {
                listBox.Items.Add($"{index++} {folder}");
            }
        }

        private void LoadBaseFolder()
        {
            var baseFolders = settingsManager.GetFoldersFromSection("[BaseFolder]");
            if (baseFolders.Count > 0)
            {
                lb_BaseFolder.Text = baseFolders[0];
                lb_BaseFolder.ForeColor = Color.Black;
            }
            else
            {
                lb_BaseFolder.Text = "폴더가 미선택되었습니다";
                lb_BaseFolder.ForeColor = Color.Red;
            }
        }

        private void LoadRegexFromSettings()
        {
            lb_RegexList.Items.Clear();
            var regexDict = settingsManager.GetRegexList();
            int index = 1;
            foreach (var kvp in regexDict)
            {
                lb_RegexList.Items.Add($"{index++} {kvp.Key} -> {kvp.Value}");
            }
        }

        private void AddFolder(string section, ListBox listBox)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolder = folderDialog.SelectedPath;

                    // 폴더 중복 체크
                    if (IsFolderAlreadyAdded(selectedFolder, listBox))
                    {
                        MessageBox.Show("해당 폴더는 이미 등록되어 있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // SettingsManager에 폴더 추가 로직 구현 필요
                    AddFolderToSettings(section, selectedFolder);
                    LoadFolders(section, listBox);
                    ValidateRunButtonState();
                }
            }
        }

        private void AddFolderToSettings(string section, string folder)
        {
            // 현재 섹션 폴더 목록 불러오기
            var folders = settingsManager.GetFoldersFromSection(section);
            folders.Add(folder);
            // 폴더를 다시 설정 파일에 반영
            UpdateFolderSectionInSettings(section, folders);
        }

        private void RemoveSelectedFolders(string section, ListBox listBox)
        {
            var selectedItems = listBox.SelectedItems.Cast<string>().ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("삭제할 폴더를 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("선택한 폴더를 정말 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // 현재 폴더 목록 불러오기
                var folders = settingsManager.GetFoldersFromSection(section);

                // 선택한 항목을 folders에서 제거
                foreach (var item in selectedItems)
                {
                    int spaceIndex = item.IndexOf(' ');
                    if (spaceIndex != -1)
                    {
                        string folderPath = item.Substring(spaceIndex + 1);
                        folders.Remove(folderPath);
                    }
                }

                // 갱신된 목록을 설정에 반영
                UpdateFolderSectionInSettings(section, folders);
                LoadFolders(section, listBox);
                ValidateRunButtonState();
            }
        }

        private void UpdateFolderSectionInSettings(string section, List<string> folders)
        {
            // Settings.ini를 직접 편집하는 로직 대신
            // SettingsManager가 folders를 해당 section에 다시 쓸 수 있는 메서드가 필요하다면 추가
            // 여기서는 SettingsManager가 폴더 목록을 반영하는 메서드를 가정할 수 있음.
            // 만약 없다면, SettingsManager 개선 필요.

            // 예: SettingsManager에 SetFoldersToSection(section, folders) 메서드를 추가했다고 가정
            // folders: 중복 제거, 정렬 등 필요시 처리
            settingsManager.SetFoldersToSection(section, folders);
        }

        private bool IsFolderAlreadyAdded(string folderPath, ListBox listBox)
        {
            return listBox.Items.Cast<string>().Any(item => item.Contains(folderPath));
        }

        private void btn_BaseFolder_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = lb_BaseFolder.Text == "폴더가 미선택되었습니다"
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : lb_BaseFolder.Text;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    // BaseFolder 설정 반영
                    settingsManager.SetBaseFolder(folderDialog.SelectedPath);
                    LoadBaseFolder();
                    ValidateRunButtonState();
                }
            }
        }

        private void ShowRegexConfigFormCentered(RegexConfigForm regexForm)
        {
            // RegexConfigForm을 정중앙에 위치시키기 위한 로직
            var parentPanel = this.Parent as Panel;
            if (parentPanel != null)
            {
                Point centerLocation = new Point(
                    parentPanel.Location.X + (parentPanel.Width - regexForm.Width) / 2,
                    parentPanel.Location.Y + (parentPanel.Height - regexForm.Height) / 2
                );

                regexForm.StartPosition = FormStartPosition.Manual;
                regexForm.Location = this.PointToScreen(centerLocation);
            }
        }

        private void btn_RegAdd_Click(object sender, EventArgs e)
        {
            using (var regexForm = new RegexConfigForm(BaseFolder))
            {
                ShowRegexConfigFormCentered(regexForm);
                if (regexForm.ShowDialog() == DialogResult.OK)
                {
                    string regex = regexForm.RegexPattern;
                    string targetFolder = regexForm.TargetFolder;

                    AddOrUpdateRegex(regex, targetFolder, null);
                }
            }
        }

        private void btn_RegEdit_Click(object sender, EventArgs e)
        {
            if (lb_RegexList.SelectedItem == null)
            {
                MessageBox.Show("수정할 항목을 선택하세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var (regex, targetFolder) = ParseSelectedRegexItem(lb_RegexList.SelectedItem.ToString());
            using (var regexForm = new RegexConfigForm(BaseFolder))
            {
                regexForm.RegexPattern = regex;
                regexForm.TargetFolder = targetFolder;

                ShowRegexConfigFormCentered(regexForm);

                if (regexForm.ShowDialog() == DialogResult.OK)
                {
                    AddOrUpdateRegex(regexForm.RegexPattern, regexForm.TargetFolder, regex);
                }
            }
        }

        private void btn_RegRemove_Click(object sender, EventArgs e)
        {
            if (lb_RegexList.SelectedItem == null)
            {
                MessageBox.Show("삭제할 항목을 선택하세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("선택한 항목을 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                var (regex, _) = ParseSelectedRegexItem(lb_RegexList.SelectedItem.ToString());
                RemoveRegex(regex);
            }
        }

        private (string regex, string folder) ParseSelectedRegexItem(string item)
        {
            int arrowIndex = item.IndexOf("->");
            string regex = item.Substring(item.IndexOf(' ') + 1, arrowIndex - item.IndexOf(' ') - 2).Trim();
            string targetFolder = item.Substring(arrowIndex + 2).Trim();
            return (regex, targetFolder);
        }

        private void AddOrUpdateRegex(string newRegex, string newFolder, string oldRegex = null)
        {
            // SettingsManager에서 Regex 목록 가져오기
            var regexDict = settingsManager.GetRegexList();

            // 만약 oldRegex가 null이 아니라면 수정 모드
            if (oldRegex != null && regexDict.ContainsKey(oldRegex))
            {
                // 기존 key 제거 후 새로운 key 추가
                regexDict.Remove(oldRegex);
            }

            regexDict[newRegex] = newFolder;

            // 수정된 Regex dict를 Settings에 반영
            settingsManager.SetRegexList(regexDict);
            LoadRegexFromSettings();
            ValidateRunButtonState();
        }

        private void RemoveRegex(string regex)
        {
            var regexDict = settingsManager.GetRegexList();
            if (regexDict.ContainsKey(regex))
            {
                regexDict.Remove(regex);
                settingsManager.SetRegexList(regexDict);
                LoadRegexFromSettings();
                ValidateRunButtonState();
            }
        }

        private void ValidateRunButtonState()
        {
            bool hasTargetFolders = lb_TargetList.Items.Count > 0;
            bool hasBaseFolder = !string.IsNullOrEmpty(lb_BaseFolder.Text) && lb_BaseFolder.Text != "폴더가 미선택되었습니다";
            bool hasRegexPatterns = lb_RegexList.Items.Count > 0;

            bool isReadyToRun = hasTargetFolders && hasBaseFolder && hasRegexPatterns;

            // 상태 업데이트
            if (isReadyToRun)
            {
                StatusUpdated?.Invoke("Ready to Run", Color.Green);
            }
            else
            {
                StatusUpdated?.Invoke("Stopped!", Color.Red);
            }
        }

        public void UpdateStatusOnRun(bool isRunning)
        {
            // 버튼 상태 업데이트
            SetControlsEnabled(!isRunning);

            // 상태 변경 이벤트 전달
            string status = isRunning ? "Running..." : "Stopped!";
            Color statusColor = isRunning ? Color.Blue : Color.Red;
            StatusUpdated?.Invoke(status, statusColor);
        }

        public void SetControlsEnabled(bool isEnabled)
        {
            // 공통 버튼 활성화/비활성화
            btn_TargetFolder.Enabled = isEnabled;
            btn_TargetRemove.Enabled = isEnabled;
            btn_ExcludeFolder.Enabled = isEnabled;
            btn_ExcludeRemove.Enabled = isEnabled;
            btn_BaseFolder.Enabled = isEnabled;

            // 정규 표현식 관련 버튼
            btn_RegAdd.Enabled = isEnabled;
            btn_RegEdit.Enabled = isEnabled;
            btn_RegRemove.Enabled = isEnabled;

            // 목록 선택 활성화 상태 동기화
            lb_TargetList.Enabled = isEnabled;
            lb_ExcludeList.Enabled = isEnabled;
            lb_RegexList.Enabled = isEnabled;
        }

        public string BaseFolder
        {
            get => (lb_BaseFolder.Text != "폴더가 미선택되었습니다") ? lb_BaseFolder.Text : null;
        }

        public void RefreshUI()
        {
            LoadDataFromSettings(); // 설정값을 다시 로드하여 UI 갱신
        }

        public string GetBaseFolder()
        {
            return lb_BaseFolder.Text;
        }

        public List<string> GetRegexList()
        {
            return lb_RegexList.Items
                .Cast<string>()
                .Select(item =>
                {
                    var parts = item.Split(new[] { "->" }, StringSplitOptions.None);
                    return parts.Length == 2 ? parts[1].Trim() : null;
                })
                .Where(folder => !string.IsNullOrWhiteSpace(folder))
                .ToList();
        }

        public void InitializePanel(bool isRunning)
        {
            SetButtonsEnabled(!isRunning); // Running 상태일 때 버튼 비활성화
        }

        public void SetButtonsEnabled(bool isEnabled)
        {
            btn_TargetFolder.Enabled = isEnabled;
            btn_TargetRemove.Enabled = isEnabled;
            btn_ExcludeFolder.Enabled = isEnabled;
            btn_ExcludeRemove.Enabled = isEnabled;
            btn_BaseFolder.Enabled = isEnabled;
            btn_RegAdd.Enabled = isEnabled;
            btn_RegEdit.Enabled = isEnabled;
            btn_RegRemove.Enabled = isEnabled;
        }

        public bool IsReadyToRun()
        {
            // TargetList / BaseFolder / RegexList 모두 충족하는지
            bool hasTarget = (lb_TargetList.Items.Count > 0);
            bool hasBase = (lb_BaseFolder.Text != "폴더가 미선택되었습니다"
                            && !string.IsNullOrEmpty(lb_BaseFolder.Text));
            bool hasRegex = (lb_RegexList.Items.Count > 0);
        
            return hasTarget && hasBase && hasRegex;
        }
        
        public string[] GetTargetFolders()
        {
            // lb_RegexList 항목은 "번호 regex -> targetFolder" 형식으로 되어 있다고 가정합니다.
            return lb_RegexList.Items
                 .Cast<string>()
                 .Select(item =>
                 {
                     // "1 someRegex -> C:\TargetFolder" 형식으로 되어 있으므로 "->"를 기준으로 분리
                     var parts = item.Split(new string[] { "->" }, StringSplitOptions.None);
                     if (parts.Length >= 2)
                     {
                         return parts[1].Trim(); // 오른쪽(타겟 폴더) 부분 반환
                     }
                     return string.Empty;
                 })
                 .Where(folder => !string.IsNullOrEmpty(folder))
                 .ToArray();
        }
    }
}
