// ucPanel\ucOptionPanel.Designer.cs
namespace ITM_Agent.ucPanel
{
    partial class ucOptionPanel
    {
        /// <summary>필수 디자이너 변수</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Debug Mode 체크박스</summary>
        private System.Windows.Forms.CheckBox chk_DebugMode;

        /// <summary>
        /// 사용 중 리소스 정리
        /// </summary>
        /// <param name="disposing">관리되는 리소스 해제 여부</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// 디자이너 지원에 필요한 메서드 — 코드 수정 금지
        /// </summary>
        private void InitializeComponent()
        {
            this.chk_DebugMode = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chk_DebugMode
            // 
            this.chk_DebugMode.AutoSize = true;
            this.chk_DebugMode.Location = new System.Drawing.Point(563, 31);
            this.chk_DebugMode.Name = "chk_DebugMode";
            this.chk_DebugMode.Size = new System.Drawing.Size(15, 14);
            this.chk_DebugMode.TabIndex = 0;
            this.chk_DebugMode.UseVisualStyleBackColor = true;
            this.chk_DebugMode.CheckedChanged += new System.EventHandler(this.chk_DebugMode_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.chk_DebugMode);
            this.groupBox1.Location = new System.Drawing.Point(25, 21);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(624, 305);
            this.groupBox1.TabIndex = 19;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "● Database Uploading";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(137, 12);
            this.label1.TabIndex = 41;
            this.label1.Text = "• Enable Debug Logging";
            // 
            // ucOptionPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "ucOptionPanel";
            this.Size = new System.Drawing.Size(676, 340);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            
        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
    }
}
