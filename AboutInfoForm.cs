// AboutInfoForm.cs
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Imaging;
using ITM_Agent.Properties;

namespace ITM_Agent
{
    public partial class AboutInfoForm : Form
    {
        private System.ComponentModel.IContainer components = null;

        public AboutInfoForm()
        {
            InitializeComponent();
            LoadIconSafe();
            lb_Version.Text = MainForm.VersionInfo;

            // ▼▼▼ [추가] 리소스에서 텍스트를 불러와 UI에 적용 ▼▼▼
            this.label1.Text = Resources.AboutInfo_Desc1;
            this.label2.Text = Resources.AboutInfo_Desc2;
            this.label3.Text = Resources.AboutInfo_Desc3;
            this.label4.Text = Resources.AboutInfo_Desc4;
        }

        /// <summary>아이콘 안전 로드</summary>
        private void LoadIconSafe()                                     // [추가]
        {
            try
            {
                string path = Path.Combine(
                    Application.StartupPath, "Resources", "Icons", "icon.png");

                Image baseImg = File.Exists(path)
                    ? Image.FromFile(path)
                    : SystemIcons.Application.ToBitmap();

                picIcon.Image = ApplyOpacity(baseImg, 0.5f);    // 0~1, 값이 낮을수록 더 투명
            }
            catch
            {
                picIcon.Image = SystemIcons.Application.ToBitmap();
            }
        }

        private static Bitmap ApplyOpacity(Image src, float opacity)    // [추가]
        {
            // 32bpp ARGB 포맷으로 새 Bitmap 생성
            var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                var matrix = new ColorMatrix
                {
                    Matrix33 = opacity   // Alpha 채널만 조정 (0=완전투명, 1=원본)
                };
                var attr = new ImageAttributes();
                attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(src,
                    new Rectangle(0, 0, src.Width, src.Height),
                    0, 0, src.Width, src.Height,
                    GraphicsUnit.Pixel, attr);
            }
            return bmp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }
    }
}
