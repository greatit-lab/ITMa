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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chk_infoDel = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cb_info_Retention = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.chk_PerfoMode = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // chk_DebugMode
            // 
            this.chk_DebugMode.AutoSize = true;
            this.chk_DebugMode.Location = new System.Drawing.Point(505, 66);
            this.chk_DebugMode.Name = "chk_DebugMode";
            this.chk_DebugMode.Size = new System.Drawing.Size(15, 14);
            this.chk_DebugMode.TabIndex = 0;
            this.chk_DebugMode.UseVisualStyleBackColor = true;
            this.chk_DebugMode.CheckedChanged += new System.EventHandler(this.chk_DebugMode_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.chk_PerfoMode);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.chk_DebugMode);
            this.groupBox1.Location = new System.Drawing.Point(25, 21);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(624, 98);
            this.groupBox1.TabIndex = 19;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "● Logging Option";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(226, 12);
            this.label1.TabIndex = 41;
            this.label1.Text = "• Enable System Performance Logging";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.cb_info_Retention);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.chk_infoDel);
            this.groupBox2.Location = new System.Drawing.Point(25, 137);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(624, 90);
            this.groupBox2.TabIndex = 42;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "● Data Retention Option";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(207, 12);
            this.label2.TabIndex = 41;
            this.label2.Text = "• Auto-delete [ .info] metadata files";
            // 
            // chk_infoDel
            // 
            this.chk_infoDel.AutoSize = true;
            this.chk_infoDel.Location = new System.Drawing.Point(505, 31);
            this.chk_infoDel.Name = "chk_infoDel";
            this.chk_infoDel.Size = new System.Drawing.Size(15, 14);
            this.chk_infoDel.TabIndex = 0;
            this.chk_infoDel.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(29, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(241, 12);
            this.label3.TabIndex = 42;
            this.label3.Text = "→ Retention period (days) before deletion";
            // 
            // cb_info_Retention
            // 
            this.cb_info_Retention.FormattingEnabled = true;
            this.cb_info_Retention.Location = new System.Drawing.Point(471, 58);
            this.cb_info_Retention.Name = "cb_info_Retention";
            this.cb_info_Retention.Size = new System.Drawing.Size(77, 20);
            this.cb_info_Retention.TabIndex = 43;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(554, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 12);
            this.label4.TabIndex = 44;
            this.label4.Text = "days";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(20, 66);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(137, 12);
            this.label5.TabIndex = 43;
            this.label5.Text = "• Enable Debug Logging";
            // 
            // chk_PerfoMode
            // 
            this.chk_PerfoMode.AutoSize = true;
            this.chk_PerfoMode.Location = new System.Drawing.Point(505, 31);
            this.chk_PerfoMode.Name = "chk_PerfoMode";
            this.chk_PerfoMode.Size = new System.Drawing.Size(15, 14);
            this.chk_PerfoMode.TabIndex = 42;
            this.chk_PerfoMode.UseVisualStyleBackColor = true;
            this.chk_PerfoMode.CheckedChanged += new System.EventHandler(this.chk_PerfoMode_CheckedChanged);   // [추가]
            // 
            // ucOptionPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "ucOptionPanel";
            this.Size = new System.Drawing.Size(676, 340);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            
        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chk_infoDel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cb_info_Retention;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chk_PerfoMode;
    }
}
