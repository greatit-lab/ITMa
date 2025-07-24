// ucPanel\RegexConfigForm.cs
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ITM_Agent.ucPanel
{
    /// <summary>
    /// 정규표현식 패턴과 대상 폴더를 설정하기 위한 폼
    /// </summary>
    public partial class RegexConfigForm : Form
    {
        private readonly string baseFolderPath;

        public string RegexPattern
        {
            get => tb_RegInput.Text;
            set => tb_RegInput.Text = value;
        }

        public string TargetFolder
        {
            get => tb_RegFolder.Text;
            set => tb_RegFolder.Text = value;
        }

        public RegexConfigForm(string baseFolderPath)
        {
            this.baseFolderPath = baseFolderPath ?? AppDomain.CurrentDomain.BaseDirectory;

            InitializeComponent();

            this.Text = "Regex Configuration";
            tb_RegFolder.ReadOnly = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            btn_RegSelectFolder.Click += btn_RegSelectFolder_Click;
            btn_RegApply.Click += btn_RegApply_Click;
            btn_RegCancel.Click += (sender, e) => this.DialogResult = DialogResult.Cancel;

            this.Load += RegexConfigForm_Load;
        }

        private void RegexConfigForm_Load(object sender, EventArgs e)
        {
            // 폼 위치나 초기화 로직 필요 시 추가
        }

        private void btn_RegSelectFolder_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = baseFolderPath;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    TargetFolder = folderDialog.SelectedPath;
                }
            }
        }

        private void btn_RegApply_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RegexPattern))
            {
                MessageBox.Show("정규표현식을 입력해주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TargetFolder))
            {
                MessageBox.Show("복사 폴더를 선택해주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
