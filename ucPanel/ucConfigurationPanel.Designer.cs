//ucPanel\ucConfigurationPanel.Designer.cs
namespace ITM_Agent.ucPanel
{
    partial class ucConfigurationPanel
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

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btn_TargetRemove;
        private System.Windows.Forms.Button btn_TargetFolder;
        private System.Windows.Forms.ListBox lb_TargetList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_ExcludeRemove;
        private System.Windows.Forms.Button btn_ExcludeFolder;
        private System.Windows.Forms.ListBox lb_ExcludeList;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btn_BaseFolder;
        private System.Windows.Forms.Label lb_BaseFolder;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btn_RegRemove;
        private System.Windows.Forms.Button btn_RegEdit;
        private System.Windows.Forms.Button btn_RegAdd;
        private ucOverrideNamesPanel ucOverrideNamesPanel;
        private System.Windows.Forms.ListBox lb_RegexList;

        #region 구성 요소 디자이너에서 생성한 코드

        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.btn_TargetRemove = new System.Windows.Forms.Button();
            this.btn_TargetFolder = new System.Windows.Forms.Button();
            this.lb_TargetList = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btn_BaseFolder = new System.Windows.Forms.Button();
            this.lb_BaseFolder = new System.Windows.Forms.Label();
            this.btn_ExcludeRemove = new System.Windows.Forms.Button();
            this.btn_ExcludeFolder = new System.Windows.Forms.Button();
            this.lb_ExcludeList = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.lb_RegexList = new System.Windows.Forms.ListBox();
            this.btn_RegRemove = new System.Windows.Forms.Button();
            this.btn_RegEdit = new System.Windows.Forms.Button();
            this.btn_RegAdd = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            //
            // tabControl1
            //
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 6);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(653, 330);
            this.tabControl1.TabIndex = 14;
            //
            // tabPage1
            //
            this.tabPage1.Controls.Add(this.splitContainer2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(645, 304);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Categorize";
            this.tabPage1.UseVisualStyleBackColor = true;
            //
            // splitContainer2
            //
            this.splitContainer2.Location = new System.Drawing.Point(6, 6);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            //
            // splitContainer2.Panel1
            //
            this.splitContainer2.Panel1.Controls.Add(this.groupBox1);
            //
            // splitContainer2.Panel2
            //
            this.splitContainer2.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer2.Size = new System.Drawing.Size(632, 293);
            this.splitContainer2.SplitterDistance = 235;
            this.splitContainer2.TabIndex = 1;
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.splitContainer1);
            this.groupBox1.Location = new System.Drawing.Point(3, 9);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(624, 223);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "● Folders to Monitor";
            //
            // splitContainer1
            //
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 17);
            this.splitContainer1.Name = "splitContainer1";
            //
            // splitContainer1.Panel1
            //
            this.splitContainer1.Panel1.Controls.Add(this.btn_TargetRemove);
            this.splitContainer1.Panel1.Controls.Add(this.btn_TargetFolder);
            this.splitContainer1.Panel1.Controls.Add(this.lb_TargetList);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            //
            // splitContainer1.Panel2
            //
            this.splitContainer1.Panel2.Controls.Add(this.btn_ExcludeRemove);
            this.splitContainer1.Panel2.Controls.Add(this.btn_ExcludeFolder);
            this.splitContainer1.Panel2.Controls.Add(this.lb_ExcludeList);
            this.splitContainer1.Panel2.Controls.Add(this.label2);
            this.splitContainer1.Size = new System.Drawing.Size(618, 203);
            this.splitContainer1.SplitterDistance = 307;
            this.splitContainer1.TabIndex = 0;
            //
            // btn_TargetRemove
            //
            this.btn_TargetRemove.Location = new System.Drawing.Point(160, 170);
            this.btn_TargetRemove.Name = "btn_TargetRemove";
            this.btn_TargetRemove.Size = new System.Drawing.Size(133, 23);
            this.btn_TargetRemove.TabIndex = 3;
            this.btn_TargetRemove.Text = "Remove";
            this.btn_TargetRemove.UseVisualStyleBackColor = true;
            //
            // btn_TargetFolder
            //
            this.btn_TargetFolder.Location = new System.Drawing.Point(15, 170);
            this.btn_TargetFolder.Name = "btn_TargetFolder";
            this.btn_TargetFolder.Size = new System.Drawing.Size(133, 23);
            this.btn_TargetFolder.TabIndex = 2;
            this.btn_TargetFolder.Text = "Select Folders";
            this.btn_TargetFolder.UseVisualStyleBackColor = true;
            //
            // lb_TargetList
            //
            this.lb_TargetList.FormattingEnabled = true;
            this.lb_TargetList.ItemHeight = 12;
            this.lb_TargetList.Location = new System.Drawing.Point(15, 24);
            this.lb_TargetList.Name = "lb_TargetList";
            this.lb_TargetList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lb_TargetList.Size = new System.Drawing.Size(278, 136);
            this.lb_TargetList.TabIndex = 1;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = ">Target Folders";
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.btn_BaseFolder);
            this.groupBox2.Controls.Add(this.lb_BaseFolder);
            this.groupBox2.Location = new System.Drawing.Point(3, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(624, 50);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "● Save to Folder";
            //
            // btn_BaseFolder
            //
            this.btn_BaseFolder.Location = new System.Drawing.Point(471, 16);
            this.btn_BaseFolder.Name = "btn_BaseFolder";
            this.btn_BaseFolder.Size = new System.Drawing.Size(133, 27);
            this.btn_BaseFolder.TabIndex = 5;
            this.btn_BaseFolder.Text = "Select Folder";
            this.btn_BaseFolder.UseVisualStyleBackColor = true;
            //
            // lb_BaseFolder
            //
            this.lb_BaseFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lb_BaseFolder.Location = new System.Drawing.Point(18, 21);
            this.lb_BaseFolder.Name = "lb_BaseFolder";
            this.lb_BaseFolder.Size = new System.Drawing.Size(441, 22);
            this.lb_BaseFolder.TabIndex = 0;
            this.lb_BaseFolder.Text = "label3";
            //
            // btn_ExcludeRemove
            //
            this.btn_ExcludeRemove.Location = new System.Drawing.Point(157, 170);
            this.btn_ExcludeRemove.Name = "btn_ExcludeRemove";
            this.btn_ExcludeRemove.Size = new System.Drawing.Size(133, 23);
            this.btn_ExcludeRemove.TabIndex = 5;
            this.btn_ExcludeRemove.Text = "Remove";
            this.btn_ExcludeRemove.UseVisualStyleBackColor = true;
            //
            // btn_ExcludeFolder
            //
            this.btn_ExcludeFolder.Location = new System.Drawing.Point(12, 170);
            this.btn_ExcludeFolder.Name = "btn_ExcludeFolder";
            this.btn_ExcludeFolder.Size = new System.Drawing.Size(133, 23);
            this.btn_ExcludeFolder.TabIndex = 4;
            this.btn_ExcludeFolder.Text = "Select Folders";
            this.btn_ExcludeFolder.UseVisualStyleBackColor = true;
            //
            // lb_ExcludeList
            //
            this.lb_ExcludeList.FormattingEnabled = true;
            this.lb_ExcludeList.ItemHeight = 12;
            this.lb_ExcludeList.Location = new System.Drawing.Point(12, 24);
            this.lb_ExcludeList.Name = "lb_ExcludeList";
            this.lb_ExcludeList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lb_ExcludeList.Size = new System.Drawing.Size(278, 136);
            this.lb_ExcludeList.TabIndex = 3;
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = ">Exclude Folders";
            //
            // tabPage2
            //
            this.tabPage2.Controls.Add(this.groupBox3);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(645, 304);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Regex";
            this.tabPage2.UseVisualStyleBackColor = true;
            //
            // groupBox3
            //
            this.groupBox3.Controls.Add(this.lb_RegexList);
            this.groupBox3.Controls.Add(this.btn_RegRemove);
            this.groupBox3.Controls.Add(this.btn_RegEdit);
            this.groupBox3.Controls.Add(this.btn_RegAdd);
            this.groupBox3.Location = new System.Drawing.Point(9, 15);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(624, 283);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "● Regular Expressions";
            //
            // lb_RegexList
            //
            this.lb_RegexList.FormattingEnabled = true;
            this.lb_RegexList.ItemHeight = 12;
            this.lb_RegexList.Location = new System.Drawing.Point(18, 20);
            this.lb_RegexList.Name = "lb_RegexList";
            this.lb_RegexList.Size = new System.Drawing.Size(586, 208);
            this.lb_RegexList.TabIndex = 8;
            //
            // btn_RegRemove
            //
            this.btn_RegRemove.Location = new System.Drawing.Point(433, 238);
            this.btn_RegRemove.Name = "btn_RegRemove";
            this.btn_RegRemove.Size = new System.Drawing.Size(171, 33);
            this.btn_RegRemove.TabIndex = 7;
            this.btn_RegRemove.Text = "Remove";
            this.btn_RegRemove.UseVisualStyleBackColor = true;
            //
            // btn_RegEdit
            //
            this.btn_RegEdit.Location = new System.Drawing.Point(226, 238);
            this.btn_RegEdit.Name = "btn_RegEdit";
            this.btn_RegEdit.Size = new System.Drawing.Size(171, 33);
            this.btn_RegEdit.TabIndex = 5;
            this.btn_RegEdit.Text = "Edit Regex";
            this.btn_RegEdit.UseVisualStyleBackColor = true;
            //
            // btn_RegAdd
            //
            this.btn_RegAdd.Location = new System.Drawing.Point(18, 238);
            this.btn_RegAdd.Name = "btn_RegAdd";
            this.btn_RegAdd.Size = new System.Drawing.Size(171, 33);
            this.btn_RegAdd.TabIndex = 4;
            this.btn_RegAdd.Text = "Add Regex";
            this.btn_RegAdd.UseVisualStyleBackColor = true;
            //
            // ucConfigurationPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Name = "ucConfigurationPanel";
            this.Size = new System.Drawing.Size(676, 340);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion
    }
}
