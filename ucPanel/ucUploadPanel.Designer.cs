// ucPanel\ucUploadPanel.Designer.cs
namespace ITM_Agent.ucPanel
{
    partial class ucUploadPanel
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
        
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btn_FlatSet;
        private System.Windows.Forms.Button btn_FlatClear;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cb_PreAlign_Path;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cb_WaferFlat_Path;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cb_ImgPath;
        private System.Windows.Forms.Button btn_ImgSet;
        private System.Windows.Forms.ComboBox cb_FlatPlugin;
        private System.Windows.Forms.Button btn_PreAlignSet;
        private System.Windows.Forms.ComboBox cb_PreAlignPlugin;
        private System.Windows.Forms.ComboBox cb_ImagePlugin;
        
        #region 구성 요소 디자이너에서 생성한 코드
        
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btn_WaveClear = new System.Windows.Forms.Button();
            this.label12 = new System.Windows.Forms.Label();
            this.cb_WavePlugin = new System.Windows.Forms.ComboBox();
            this.cb_WavePath = new System.Windows.Forms.ComboBox();
            this.btn_WaveSet = new System.Windows.Forms.Button();
            this.btn_EvClear = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.cb_EvPlugin = new System.Windows.Forms.ComboBox();
            this.cb_EvPath = new System.Windows.Forms.ComboBox();
            this.btn_EvSet = new System.Windows.Forms.Button();
            this.btn_ErrClear = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.cb_ErrPlugin = new System.Windows.Forms.ComboBox();
            this.cb_ErrPath = new System.Windows.Forms.ComboBox();
            this.btn_ErrSet = new System.Windows.Forms.Button();
            this.btn_ImgClear = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.btn_PreAlignClear = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cb_ImagePlugin = new System.Windows.Forms.ComboBox();
            this.btn_PreAlignSet = new System.Windows.Forms.Button();
            this.cb_PreAlignPlugin = new System.Windows.Forms.ComboBox();
            this.cb_ImgPath = new System.Windows.Forms.ComboBox();
            this.cb_PreAlign_Path = new System.Windows.Forms.ComboBox();
            this.cb_FlatPlugin = new System.Windows.Forms.ComboBox();
            this.cb_WaferFlat_Path = new System.Windows.Forms.ComboBox();
            this.btn_ImgSet = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_FlatSet = new System.Windows.Forms.Button();
            this.btn_FlatClear = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.btn_WaveClear);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.cb_WavePlugin);
            this.groupBox1.Controls.Add(this.cb_WavePath);
            this.groupBox1.Controls.Add(this.btn_WaveSet);
            this.groupBox1.Controls.Add(this.btn_EvClear);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.cb_EvPlugin);
            this.groupBox1.Controls.Add(this.cb_EvPath);
            this.groupBox1.Controls.Add(this.btn_EvSet);
            this.groupBox1.Controls.Add(this.btn_ErrClear);
            this.groupBox1.Controls.Add(this.label10);
            this.groupBox1.Controls.Add(this.cb_ErrPlugin);
            this.groupBox1.Controls.Add(this.cb_ErrPath);
            this.groupBox1.Controls.Add(this.btn_ErrSet);
            this.groupBox1.Controls.Add(this.btn_ImgClear);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.btn_PreAlignClear);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.cb_ImagePlugin);
            this.groupBox1.Controls.Add(this.btn_PreAlignSet);
            this.groupBox1.Controls.Add(this.cb_PreAlignPlugin);
            this.groupBox1.Controls.Add(this.cb_ImgPath);
            this.groupBox1.Controls.Add(this.cb_PreAlign_Path);
            this.groupBox1.Controls.Add(this.cb_FlatPlugin);
            this.groupBox1.Controls.Add(this.cb_WaferFlat_Path);
            this.groupBox1.Controls.Add(this.btn_ImgSet);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.btn_FlatSet);
            this.groupBox1.Controls.Add(this.btn_FlatClear);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(25, 21);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(624, 305);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "● Database Uploading";
            //
            // btn_WaveClear
            //
            this.btn_WaveClear.Location = new System.Drawing.Point(543, 273);
            this.btn_WaveClear.Name = "btn_WaveClear";
            this.btn_WaveClear.Size = new System.Drawing.Size(75, 22);
            this.btn_WaveClear.TabIndex = 53;
            this.btn_WaveClear.Text = "Clear";
            this.btn_WaveClear.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label12.Font = new System.Drawing.Font("굴림", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label12.Location = new System.Drawing.Point(253, 276);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(25, 22);
            this.label12.TabIndex = 52;
            this.label12.Text = "→";
            //
            // cb_WavePlugin
            //
            this.cb_WavePlugin.FormattingEnabled = true;
            this.cb_WavePlugin.Location = new System.Drawing.Point(283, 274);
            this.cb_WavePlugin.Name = "cb_WavePlugin";
            this.cb_WavePlugin.Size = new System.Drawing.Size(175, 20);
            this.cb_WavePlugin.TabIndex = 51;
            //
            // cb_WavePath
            //
            this.cb_WavePath.FormattingEnabled = true;
            this.cb_WavePath.Location = new System.Drawing.Point(30, 274);
            this.cb_WavePath.Name = "cb_WavePath";
            this.cb_WavePath.Size = new System.Drawing.Size(214, 20);
            this.cb_WavePath.TabIndex = 49;
            //
            // btn_WaveSet
            //
            this.btn_WaveSet.Location = new System.Drawing.Point(465, 273);
            this.btn_WaveSet.Name = "btn_WaveSet";
            this.btn_WaveSet.Size = new System.Drawing.Size(75, 22);
            this.btn_WaveSet.TabIndex = 50;
            this.btn_WaveSet.Text = "Set";
            this.btn_WaveSet.UseVisualStyleBackColor = true;
            //
            // btn_EvClear
            //
            this.btn_EvClear.Location = new System.Drawing.Point(543, 226);
            this.btn_EvClear.Name = "btn_EvClear";
            this.btn_EvClear.Size = new System.Drawing.Size(75, 22);
            this.btn_EvClear.TabIndex = 48;
            this.btn_EvClear.Text = "Clear";
            this.btn_EvClear.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label11.Font = new System.Drawing.Font("굴림", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label11.Location = new System.Drawing.Point(253, 229);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(25, 22);
            this.label11.TabIndex = 47;
            this.label11.Text = "→";
            //
            // cb_EvPlugin
            //
            this.cb_EvPlugin.FormattingEnabled = true;
            this.cb_EvPlugin.Location = new System.Drawing.Point(283, 227);
            this.cb_EvPlugin.Name = "cb_EvPlugin";
            this.cb_EvPlugin.Size = new System.Drawing.Size(175, 20);
            this.cb_EvPlugin.TabIndex = 46;
            //
            // cb_EvPath
            //
            this.cb_EvPath.FormattingEnabled = true;
            this.cb_EvPath.Location = new System.Drawing.Point(30, 227);
            this.cb_EvPath.Name = "cb_EvPath";
            this.cb_EvPath.Size = new System.Drawing.Size(214, 20);
            this.cb_EvPath.TabIndex = 44;
            //
            // btn_EvSet
            //
            this.btn_EvSet.Location = new System.Drawing.Point(465, 226);
            this.btn_EvSet.Name = "btn_EvSet";
            this.btn_EvSet.Size = new System.Drawing.Size(75, 22);
            this.btn_EvSet.TabIndex = 45;
            this.btn_EvSet.Text = "Set";
            this.btn_EvSet.UseVisualStyleBackColor = true;
            //
            // btn_ErrClear
            //
            this.btn_ErrClear.Location = new System.Drawing.Point(543, 180);
            this.btn_ErrClear.Name = "btn_ErrClear";
            this.btn_ErrClear.Size = new System.Drawing.Size(75, 22);
            this.btn_ErrClear.TabIndex = 43;
            this.btn_ErrClear.Text = "Clear";
            this.btn_ErrClear.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label10.Font = new System.Drawing.Font("굴림", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label10.Location = new System.Drawing.Point(253, 183);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(25, 22);
            this.label10.TabIndex = 42;
            this.label10.Text = "→";
            //
            // cb_ErrPlugin
            //
            this.cb_ErrPlugin.FormattingEnabled = true;
            this.cb_ErrPlugin.Location = new System.Drawing.Point(283, 181);
            this.cb_ErrPlugin.Name = "cb_ErrPlugin";
            this.cb_ErrPlugin.Size = new System.Drawing.Size(175, 20);
            this.cb_ErrPlugin.TabIndex = 41;
            //
            // cb_ErrPath
            //
            this.cb_ErrPath.FormattingEnabled = true;
            this.cb_ErrPath.Location = new System.Drawing.Point(30, 181);
            this.cb_ErrPath.Name = "cb_ErrPath";
            this.cb_ErrPath.Size = new System.Drawing.Size(214, 20);
            this.cb_ErrPath.TabIndex = 39;
            //
            // btn_ErrSet
            //
            this.btn_ErrSet.Location = new System.Drawing.Point(465, 180);
            this.btn_ErrSet.Name = "btn_ErrSet";
            this.btn_ErrSet.Size = new System.Drawing.Size(75, 22);
            this.btn_ErrSet.TabIndex = 40;
            this.btn_ErrSet.Text = "Set";
            this.btn_ErrSet.UseVisualStyleBackColor = true;
            //
            // btn_ImgClear
            //
            this.btn_ImgClear.Location = new System.Drawing.Point(543, 131);
            this.btn_ImgClear.Name = "btn_ImgClear";
            this.btn_ImgClear.Size = new System.Drawing.Size(75, 22);
            this.btn_ImgClear.TabIndex = 38;
            this.btn_ImgClear.Text = "Clear";
            this.btn_ImgClear.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label9.Font = new System.Drawing.Font("굴림", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label9.Location = new System.Drawing.Point(253, 134);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(25, 22);
            this.label9.TabIndex = 37;
            this.label9.Text = "→";
            //
            // btn_PreAlignClear
            //
            this.btn_PreAlignClear.Location = new System.Drawing.Point(543, 82);
            this.btn_PreAlignClear.Name = "btn_PreAlignClear";
            this.btn_PreAlignClear.Size = new System.Drawing.Size(75, 22);
            this.btn_PreAlignClear.TabIndex = 36;
            this.btn_PreAlignClear.Text = "Clear";
            this.btn_PreAlignClear.UseVisualStyleBackColor = true;
            //
            // label8
            //
            this.label8.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label8.Font = new System.Drawing.Font("굴림", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label8.Location = new System.Drawing.Point(253, 85);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(25, 22);
            this.label8.TabIndex = 35;
            this.label8.Text = "→";
            //
            // label4
            //
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label4.Font = new System.Drawing.Font("굴림", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label4.Location = new System.Drawing.Point(253, 39);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(25, 22);
            this.label4.TabIndex = 33;
            this.label4.Text = "→";
            //
            // cb_ImagePlugin
            //
            this.cb_ImagePlugin.FormattingEnabled = true;
            this.cb_ImagePlugin.Location = new System.Drawing.Point(283, 132);
            this.cb_ImagePlugin.Name = "cb_ImagePlugin";
            this.cb_ImagePlugin.Size = new System.Drawing.Size(175, 20);
            this.cb_ImagePlugin.TabIndex = 32;
            //
            // btn_PreAlignSet
            //
            this.btn_PreAlignSet.Location = new System.Drawing.Point(465, 82);
            this.btn_PreAlignSet.Name = "btn_PreAlignSet";
            this.btn_PreAlignSet.Size = new System.Drawing.Size(75, 22);
            this.btn_PreAlignSet.TabIndex = 31;
            this.btn_PreAlignSet.Text = "Set";
            this.btn_PreAlignSet.UseVisualStyleBackColor = true;
            //
            // cb_PreAlignPlugin
            //
            this.cb_PreAlignPlugin.FormattingEnabled = true;
            this.cb_PreAlignPlugin.Location = new System.Drawing.Point(283, 83);
            this.cb_PreAlignPlugin.Name = "cb_PreAlignPlugin";
            this.cb_PreAlignPlugin.Size = new System.Drawing.Size(175, 20);
            this.cb_PreAlignPlugin.TabIndex = 30;
            //
            // cb_ImgPath
            //
            this.cb_ImgPath.FormattingEnabled = true;
            this.cb_ImgPath.Location = new System.Drawing.Point(30, 132);
            this.cb_ImgPath.Name = "cb_ImgPath";
            this.cb_ImgPath.Size = new System.Drawing.Size(214, 20);
            this.cb_ImgPath.TabIndex = 13;
            //
            // cb_PreAlign_Path
            //
            this.cb_PreAlign_Path.FormattingEnabled = true;
            this.cb_PreAlign_Path.Location = new System.Drawing.Point(30, 83);
            this.cb_PreAlign_Path.Name = "cb_PreAlign_Path";
            this.cb_PreAlign_Path.Size = new System.Drawing.Size(214, 20);
            this.cb_PreAlign_Path.TabIndex = 8;
            //
            // cb_FlatPlugin
            //
            this.cb_FlatPlugin.FormattingEnabled = true;
            this.cb_FlatPlugin.Location = new System.Drawing.Point(283, 36);
            this.cb_FlatPlugin.Name = "cb_FlatPlugin";
            this.cb_FlatPlugin.Size = new System.Drawing.Size(175, 20);
            this.cb_FlatPlugin.TabIndex = 29;
            //
            // cb_WaferFlat_Path
            //
            this.cb_WaferFlat_Path.FormattingEnabled = true;
            this.cb_WaferFlat_Path.Location = new System.Drawing.Point(30, 36);
            this.cb_WaferFlat_Path.Name = "cb_WaferFlat_Path";
            this.cb_WaferFlat_Path.Size = new System.Drawing.Size(214, 20);
            this.cb_WaferFlat_Path.TabIndex = 0;
            //
            // btn_ImgSet
            //
            this.btn_ImgSet.Location = new System.Drawing.Point(465, 131);
            this.btn_ImgSet.Name = "btn_ImgSet";
            this.btn_ImgSet.Size = new System.Drawing.Size(75, 22);
            this.btn_ImgSet.TabIndex = 28;
            this.btn_ImgSet.Text = "Set";
            this.btn_ImgSet.UseVisualStyleBackColor = true;
            //
            // label7
            //
            this.label7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label7.Location = new System.Drawing.Point(18, 258);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(146, 22);
            this.label7.TabIndex = 27;
            this.label7.Text = "• Wave Data Path";
            //
            // label6
            //
            this.label6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label6.Location = new System.Drawing.Point(18, 211);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(146, 22);
            this.label6.TabIndex = 23;
            this.label6.Text = "• Event Data Path";
            //
            // label5
            //
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label5.Location = new System.Drawing.Point(18, 165);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(146, 22);
            this.label5.TabIndex = 19;
            this.label5.Text = "• Error Data Path";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label2.Location = new System.Drawing.Point(18, 115);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(146, 22);
            this.label2.TabIndex = 15;
            this.label2.Text = "• Image Data Path";
            //
            // btn_FlatSet
            //
            this.btn_FlatSet.Location = new System.Drawing.Point(465, 35);
            this.btn_FlatSet.Name = "btn_FlatSet";
            this.btn_FlatSet.Size = new System.Drawing.Size(75, 22);
            this.btn_FlatSet.TabIndex = 11;
            this.btn_FlatSet.Text = "Set";
            this.btn_FlatSet.UseVisualStyleBackColor = true;
            //
            // btn_FlatClear
            //
            this.btn_FlatClear.Location = new System.Drawing.Point(543, 35);
            this.btn_FlatClear.Name = "btn_FlatClear";
            this.btn_FlatClear.Size = new System.Drawing.Size(75, 22);
            this.btn_FlatClear.TabIndex = 34;
            this.btn_FlatClear.Text = "Clear";
            this.btn_FlatClear.UseVisualStyleBackColor = true;
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label1.Location = new System.Drawing.Point(18, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(146, 22);
            this.label1.TabIndex = 10;
            this.label1.Text = "• PreAlign Data Path";
            //
            // label3
            //
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label3.Location = new System.Drawing.Point(18, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(146, 22);
            this.label3.TabIndex = 7;
            this.label3.Text = "• Wafer Flat Data Path";
            //
            // ucUploadPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "ucUploadPanel";
            this.Size = new System.Drawing.Size(676, 340);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion
        
        private System.Windows.Forms.Button btn_PreAlignClear;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btn_ImgClear;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btn_WaveClear;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox cb_WavePlugin;
        private System.Windows.Forms.ComboBox cb_WavePath;
        private System.Windows.Forms.Button btn_WaveSet;
        private System.Windows.Forms.Button btn_EvClear;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cb_EvPlugin;
        private System.Windows.Forms.ComboBox cb_EvPath;
        private System.Windows.Forms.Button btn_EvSet;
        private System.Windows.Forms.Button btn_ErrClear;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cb_ErrPlugin;
        private System.Windows.Forms.ComboBox cb_ErrPath;
        private System.Windows.Forms.Button btn_ErrSet;
    }
}
