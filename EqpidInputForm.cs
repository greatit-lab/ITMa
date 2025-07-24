// EqpidInputForm.cs
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ITM_Agent
{
    /// <summary>
    /// 신규 Eqpid 등록을 위한 입력 폼입니다.
    /// 사용자가 장비명을 입력하면 OK를 누를 때 Eqpid 속성에 해당 값이 저장되며,
    /// Cancel 시 애플리케이션이 종료됩니다.
    /// </summary>
    public class EqpidInputForm : Form
    {
        public string Eqpid { get; private set; }

        private TextBox textBox;
        private Button submitButton;
        private Button cancelButton;
        private Label instructionLabel;
        private Label warningLabel;
        private PictureBox pictureBox;  // 이미지 표시를 위한 PictureBox

        private RadioButton rdo_Onto;
        private RadioButton rdo_Nova;

        public string Type { get; private set; }  // 선택된 Type 값을 저장

        public EqpidInputForm()
        {
            this.Text = "New EQPID Registry";
            this.Size = new Size(300, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ControlBox = false; // Close 버튼 비활성화

            instructionLabel = new Label()
            {
                Text = "신규로 등록 필요한 장비명을 입력하세요.",
                Top = 20,
                Left = 25,
                Width = 300,
                BackColor = Color.Transparent   // Label 배경을 투명하게 설정
            };

            textBox = new TextBox()
            {
                Top = 70,
                Left = 125,
                Width = 110
            };

            warningLabel = new Label()
            {
                Text = "장비명을 입력해주세요.",
                Top = 100,
                Left = 115,
                ForeColor = Color.Red,
                AutoSize = true,
                Visible = false
            };

            submitButton = new Button()
            {
                Text = "Submit",
                Top = 120,
                Left = 50,
                Width = 90
            };

            cancelButton = new Button()
            {
                Text = "Cancel",
                Top = 120,
                Left = 150,
                Width = 90
            };

            // 흐림 처리된 이미지 생성
            pictureBox = new PictureBox()
            {
                Image = CreateTransparentImage("Resources\\Icons\\icon.png", 128), // 투명도 적용 (128은 50% 알파)
                Location = new Point(22, 36),
                Size = new Size(75, 75),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            // TextBox에서 Enter 키로 Submit
            textBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter) // Enter 키 확인
                {
                    e.SuppressKeyPress = true; // Enter 키 소리 제거
                    submitButton.PerformClick(); // Submit 버튼 클릭
                }
            };

            // 라디오 버튼 초기화
            rdo_Onto = new RadioButton()
            {
                Text = "ONTO",
                Top = 45,
                Left = 115,
                AutoSize = true, // 텍스트 길이에 맞게 Width 자동 조정
                Checked = true // 기본값
            };

            rdo_Nova = new RadioButton()
            {
                Text = "NOVA",
                Top = 45, // rdo_Onto와 동일한 높이
                Left = rdo_Onto.Left + 75, // rdo_Onto 우측에 10px 간격으로 배치
                AutoSize = true // 텍스트 길이에 맞게 Width 자동 조정
            };

            // 이벤트 핸들러 설정
            submitButton.Click += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    warningLabel.Visible = true;
                    return;
                }

                Eqpid = textBox.Text.Trim();
                Type = rdo_Onto.Checked ? "ONTO" : "NOVA"; // Type 값 설정
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            // 컨트롤 추가
            this.Controls.Add(rdo_Onto);
            this.Controls.Add(rdo_Nova);

            cancelButton.Click += (sender, e) =>
            {
                this.DialogResult = DialogResult.Cancel;    // Cancel 반환
                this.Close();
            };

            this.Controls.Add(instructionLabel);
            this.Controls.Add(textBox);
            this.Controls.Add(warningLabel);
            this.Controls.Add(submitButton);
            this.Controls.Add(cancelButton);
            this.Controls.Add(pictureBox); // PictureBox 추가

            // Control 그리기 순서 조정 (Controls.Add 이후에 실행)
            this.Controls.SetChildIndex(pictureBox, 0);       // PictureBox를 가장 먼저 배치
            this.Controls.SetChildIndex(instructionLabel, 1); // instructionLabel을 PictureBox 위로 배치
        }

        /// <summary>
        /// 이미지에 Alpha(투명도) 값을 적용하는 메서드
        /// </summary>
        private Image CreateTransparentImage(string filePath, int alpha)
        {
            if (!File.Exists(filePath))
                return null;

            Bitmap original = new Bitmap(filePath);
            Bitmap transparentImage = new Bitmap(original.Width, original.Height);

            using (Graphics g = Graphics.FromImage(transparentImage))
            {
                ColorMatrix colorMatrix = new ColorMatrix
                {
                    Matrix33 = alpha / 255f // Alpha 값 (0~255 사이의 값)
                };
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                            0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            return transparentImage;
        }
    }
}
