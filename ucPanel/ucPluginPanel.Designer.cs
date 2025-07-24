// ucPanel\ucPluginPanel.Designer.cs
namespace ITM_Agent.ucPanel
{
    partial class ucPluginPanel
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
        
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox lb_PluginList;
        private System.Windows.Forms.Button btn_PlugAdd;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.Button btn_PlugRemove;
        
        #region 구성 요소 디자이너에서 생성한 코드
        
        private void InitializeComponent()
        {
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lb_PluginList = new System.Windows.Forms.ListBox();
            this.btn_PlugRemove = new System.Windows.Forms.Button();
            this.btn_PlugAdd = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            //
            // tabPage1
            //
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(645, 305);
            this.tabPage1.TabIndex = 1;
            this.tabPage1.Text = "Plugin";
            this.tabPage1.UseVisualStyleBackColor = true;
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.lb_PluginList);
            this.groupBox1.Controls.Add(this.btn_PlugRemove);
            this.groupBox1.Controls.Add(this.btn_PlugAdd);
            this.groupBox1.Location = new System.Drawing.Point(9, 15);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(624, 284);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "● Plugin Listup";
            //
            // lb_PluginList
            //
            this.lb_PluginList.FormattingEnabled = true;
            this.lb_PluginList.ItemHeight = 12;
            this.lb_PluginList.Location = new System.Drawing.Point(18, 20);
            this.lb_PluginList.Name = "lb_PluginList";
            this.lb_PluginList.Size = new System.Drawing.Size(479, 256);
            this.lb_PluginList.TabIndex = 8;
            //
            // btn_PlugRemove
            //
            this.btn_PlugRemove.Location = new System.Drawing.Point(515, 59);
            this.btn_PlugRemove.Name = "btn_PlugRemove";
            this.btn_PlugRemove.Size = new System.Drawing.Size(89, 33);
            this.btn_PlugRemove.TabIndex = 7;
            this.btn_PlugRemove.Text = "Remove";
            this.btn_PlugRemove.UseVisualStyleBackColor = true;
            this.btn_PlugRemove.Click += new System.EventHandler(this.btn_PlugRemove_Click);            
            //
            // btn_PlugAdd
            //
            this.btn_PlugAdd.Location = new System.Drawing.Point(515, 20);
            this.btn_PlugAdd.Name = "btn_PlugAdd";
            this.btn_PlugAdd.Size = new System.Drawing.Size(89, 33);
            this.btn_PlugAdd.TabIndex = 4;
            this.btn_PlugAdd.Text = "Add Plugin";
            this.btn_PlugAdd.UseVisualStyleBackColor = true;
            this.btn_PlugAdd.Click += new System.EventHandler(this.btn_PlugAdd_Click);
            //
            // tabControl1
            //
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Location = new System.Drawing.Point(12, 6);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(653, 331);
            this.tabControl1.TabIndex = 15;
            //
            // ucPluginPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Name = "ucPluginPanel";
            this.Size = new System.Drawing.Size(676, 340);
            this.tabPage1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion
    }
}
