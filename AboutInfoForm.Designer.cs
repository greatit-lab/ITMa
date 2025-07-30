// AboutInfoForm.Designer.cs
using System.Drawing;               // [추가]
using System.Windows.Forms;         // [추가]

namespace ITM_Agent
{
    partial class AboutInfoForm
    {
        /* ---------------- 디자이너 필드 ---------------- */
        private PictureBox picIcon;                    // [추가]
        private Label lblTitle;                        // [추가]
        private Label lblDesc;                         // [추가]
        private GroupBox grpDev;                       // [추가]
        private Label lblDevList;                      // [추가]
        private Button btnOk;                          // [추가]

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// </summary>
        private void InitializeComponent()             // [추가]
        {
            /* ----- 컨트롤 인스턴스 ----- */
            this.picIcon   = new PictureBox();         // [추가]
            this.lblTitle  = new Label();              // [추가]
            this.lblDesc   = new Label();              // [추가]
            this.grpDev    = new GroupBox();           // [추가]
            this.lblDevList= new Label();              // [추가]
            this.btnOk     = new Button();             // [추가]

            /* ----- Form 자체 ----- */
            this.SuspendLayout();                      // [추가]
            this.AutoScaleMode   = AutoScaleMode.None; // [추가]
            this.ClientSize      = new Size(420, 240); // [추가]
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // [추가]
            this.MaximizeBox     = false;              // [추가]
            this.MinimizeBox     = false;              // [추가]
            this.StartPosition   = FormStartPosition.CenterParent; // [추가]
            this.Text            = "About ITM Agent";  // [추가]

            /* ----- picIcon ----- */
            this.picIcon.Image   = null;
            this.picIcon.SizeMode    = PictureBoxSizeMode.Zoom; // [추가]
            this.picIcon.Location    = new Point(20, 20);       // [추가]
            this.picIcon.Size        = new Size(88, 88);        // [추가]

            /* ----- lblTitle ----- */
            this.lblTitle.AutoSize   = true;                    // [추가]
            this.lblTitle.Font       = new Font("Segoe UI", 12F, FontStyle.Bold); // [추가]
            this.lblTitle.Location   = new Point(130, 20);      // [추가]
            this.lblTitle.Text       = "ITM Agent v1.0.0";       // [추가]

            /* ----- lblDesc ----- */
            this.lblDesc.AutoSize    = true;                                // [추가]
            this.lblDesc.Location    = new Point(132, 55);                  // [추가]
            this.lblDesc.Text        =
                "• 폴더 모니터링, 이미지 → PDF 병합\n" +                  // [추가]
                "• 정규식 기반 파일 자동 정리/변환\n" +                  // [추가]
                "• PostgreSQL 업로드 플러그인 구조 지원";                 // [추가]

            /* ----- grpDev & lblDevList ----- */
            this.grpDev.Location     = new Point(20, 120);     // [추가]
            this.grpDev.Size         = new Size(380, 90);      // [추가]
            this.grpDev.Text         = "Developers";           // [추가]

            this.lblDevList.AutoSize = true;                   // [추가]
            this.lblDevList.Location = new Point(15, 25);      // [추가]
            this.lblDevList.Text     =
                "• Gizmo Lee  (Backend/DB)\n" +                 // [추가]
                "• Max Kim   (WinForms UI)\n" +                 // [추가]
                "• J.Doe     (QA / Release)";                  // [추가]

            this.grpDev.Controls.Add(this.lblDevList);         // [추가]

            /* ----- btnOk ----- */
            this.btnOk.Text         = "OK";                    // [추가]
            this.btnOk.DialogResult = DialogResult.OK;         // [추가]
            this.btnOk.Anchor       = AnchorStyles.Bottom | AnchorStyles.Right; // [추가]
            this.btnOk.Location     = new Point(315, 205);     // [추가]
            this.btnOk.Size         = new Size(75, 23);        // [추가]

            /* ----- Form 컨트롤 등록 ----- */
            this.Controls.Add(this.picIcon);                   // [추가]
            this.Controls.Add(this.lblTitle);                  // [추가]
            this.Controls.Add(this.lblDesc);                   // [추가]
            this.Controls.Add(this.grpDev);                    // [추가]
            this.Controls.Add(this.btnOk);                     // [추가]
            this.AcceptButton = this.btnOk;                    // [추가]

            this.ResumeLayout(false);                          // [추가]
            this.PerformLayout();                              // [추가]
        }
    }
}
