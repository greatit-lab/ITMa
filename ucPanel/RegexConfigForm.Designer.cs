//ucPanel\RegexConfigForm.Designer.cs
namespace ITM_Agent.ucPanel
{
    partial class RegexConfigForm
    {
        private System.ComponentModel.IContainer components = null;
        
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        
        private System.Windows.Forms.Button btn_RegSelectFolder;
        private System.Windows.Forms.Button btn_RegApply;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tb_RegInput;
        private System.Windows.Forms.TextBox tb_RegFolder;
        private System.Windows.Forms.Button btn_RegCancel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        
        #region 구성 요소 디자이너에서 생성한 코드
        
        private void InitializeComponent()
        {
            this.btn_RegSelectFolder = new System.Windows.Forms.Button();
            this.btn_RegApply = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tb_RegInput = new System.Windows.Forms.TextBox();
            this.tb_RegFolder = new System.Windows.Forms.TextBox();
            this.btn_RegCancel = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // btn_RegSelectFolder
            //
            this.btn_RegSelectFolder.Location = new System.Drawing.Point(260, 78);
            this.btn_RegSelectFolder.Name = "btn_RegSelectFolder";
            this.btn_RegSelectFolder.Size = new System.Drawing.Size(64, 23);
            this.btn_RegSelectFolder.TabIndex = 0;
            this.btn_RegSelectFolder.Text = "Select";
            this.btn_RegSelectFolder.UseVisualStyleBackColor = true;
            //
            // btn_RegApply
            //
            this.btn_RegApply.Location = new System.Drawing.Point(68, 117);
            this.btn_RegApply.Name = "btn_RegApply";
            this.btn_RegApply.Size = new System.Drawing.Size(95, 23);
            this.btn_RegApply.TabIndex = 1;
            this.btn_RegApply.Text = "Apply";
            this.btn_RegApply.UseVisualStyleBackColor = true;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "▶ Regex";
            //
            // tb_RegInput
            //
            this.tb_RegInput.Location = new System.Drawing.Point(20, 25);
            this.tb_RegInput.Name = "tb_RegInput";
            this.tb_RegInput.Size = new System.Drawing.Size(304, 21);
            this.tb_RegInput.TabIndex = 3;
            //
            // tb_RegFolder
            //
            this.tb_RegFolder.Location = new System.Drawing.Point(20, 79);
            this.tb_RegFolder.Name = "tb_RegFolder";
            this.tb_RegFolder.Size = new System.Drawing.Size(234, 21);
            this.tb_RegFolder.TabIndex = 4;
            //
            // btn_RegCancel
            //
            this.btn_RegCancel.Location = new System.Drawing.Point(191, 117);
            this.btn_RegCancel.Name = "btn_RegCancel";
            this.btn_RegCancel.Size = new System.Drawing.Size(95, 23);
            this.btn_RegCancel.TabIndex = 5;
            this.btn_RegCancel.Text = "Cancel";
            this.btn_RegCancel.UseVisualStyleBackColor = true;
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 12);
            this.label2.TabIndex = 6;
            this.label2.Text = "▶ Target Folder";
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(148, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "▽▽▽▽";
            //
            // ucScreen1_Reg
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(346, 154);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_RegCancel);
            this.Controls.Add(this.tb_RegFolder);
            this.Controls.Add(this.tb_RegInput);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_RegApply);
            this.Controls.Add(this.btn_RegSelectFolder);
            this.Name = "RegexConfigForm";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }
}
