namespace Lab3_3240704204
{
    partial class MainForm
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

        #region Windows 窗体设计器生成的代码

        private void InitializeComponent()
        {
            this.gbCaptcha = new System.Windows.Forms.GroupBox();
            this.picCaptcha = new System.Windows.Forms.PictureBox();
            this.lblMeta = new System.Windows.Forms.Label();
            this.chkAnswer = new System.Windows.Forms.CheckBox();
            this.lblAnswer = new System.Windows.Forms.Label();
            this.gbNoise = new System.Windows.Forms.GroupBox();
            this.rbBoth = new System.Windows.Forms.RadioButton();
            this.rbAdhesion = new System.Windows.Forms.RadioButton();
            this.rbDot = new System.Windows.Forms.RadioButton();
            this.rbNone = new System.Windows.Forms.RadioButton();
            this.gbControl = new System.Windows.Forms.GroupBox();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnPie = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblMemory = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblElapsed = new System.Windows.Forms.ToolStripStatusLabel();
            this.gbCaptcha.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picCaptcha)).BeginInit();
            this.gbNoise.SuspendLayout();
            this.gbControl.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbCaptcha
            // 
            this.gbCaptcha.Controls.Add(this.picCaptcha);
            this.gbCaptcha.Controls.Add(this.lblMeta);
            this.gbCaptcha.Controls.Add(this.chkAnswer);
            this.gbCaptcha.Controls.Add(this.lblAnswer);
            this.gbCaptcha.Location = new System.Drawing.Point(12, 12);
            this.gbCaptcha.Name = "gbCaptcha";
            this.gbCaptcha.Size = new System.Drawing.Size(416, 172);
            this.gbCaptcha.TabIndex = 0;
            this.gbCaptcha.TabStop = false;
            this.gbCaptcha.Text = "验证码";
            // 
            // picCaptcha
            // 
            this.picCaptcha.BackColor = System.Drawing.Color.White;
            this.picCaptcha.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picCaptcha.Location = new System.Drawing.Point(16, 26);
            this.picCaptcha.Name = "picCaptcha";
            this.picCaptcha.Size = new System.Drawing.Size(240, 84);
            this.picCaptcha.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picCaptcha.TabIndex = 0;
            this.picCaptcha.TabStop = false;
            // 
            // lblMeta
            // 
            this.lblMeta.Location = new System.Drawing.Point(16, 118);
            this.lblMeta.Name = "lblMeta";
            this.lblMeta.Size = new System.Drawing.Size(386, 46);
            this.lblMeta.TabIndex = 3;
            this.lblMeta.Text = "个人参数";
            // 
            // chkAnswer
            // 
            this.chkAnswer.Location = new System.Drawing.Point(268, 30);
            this.chkAnswer.Name = "chkAnswer";
            this.chkAnswer.Size = new System.Drawing.Size(134, 24);
            this.chkAnswer.TabIndex = 1;
            this.chkAnswer.Text = "显示答案（自测）";
            this.chkAnswer.UseVisualStyleBackColor = true;
            this.chkAnswer.CheckedChanged += new System.EventHandler(this.chkAnswer_CheckedChanged);
            // 
            // lblAnswer
            // 
            this.lblAnswer.ForeColor = System.Drawing.Color.DimGray;
            this.lblAnswer.Location = new System.Drawing.Point(268, 60);
            this.lblAnswer.Name = "lblAnswer";
            this.lblAnswer.Size = new System.Drawing.Size(132, 48);
            this.lblAnswer.TabIndex = 2;
            this.lblAnswer.Text = "（已隐藏）";
            // 
            // gbNoise
            // 
            this.gbNoise.Controls.Add(this.rbBoth);
            this.gbNoise.Controls.Add(this.rbAdhesion);
            this.gbNoise.Controls.Add(this.rbDot);
            this.gbNoise.Controls.Add(this.rbNone);
            this.gbNoise.Location = new System.Drawing.Point(12, 192);
            this.gbNoise.Name = "gbNoise";
            this.gbNoise.Size = new System.Drawing.Size(416, 86);
            this.gbNoise.TabIndex = 1;
            this.gbNoise.TabStop = false;
            this.gbNoise.Text = "噪声配置（正式配置按 P5 取两种，其余用于命中率对照自测）";
            // 
            // rbBoth
            // 
            this.rbBoth.AutoSize = true;
            this.rbBoth.Checked = true;
            this.rbBoth.Location = new System.Drawing.Point(16, 22);
            this.rbBoth.Name = "rbBoth";
            this.rbBoth.Size = new System.Drawing.Size(150, 21);
            this.rbBoth.TabIndex = 0;
            this.rbBoth.TabStop = true;
            this.rbBoth.Text = "两种噪声（P5 正式）";
            this.rbBoth.UseVisualStyleBackColor = true;
            this.rbBoth.CheckedChanged += new System.EventHandler(this.NoiseOption_CheckedChanged);
            // 
            // rbAdhesion
            // 
            this.rbAdhesion.AutoSize = true;
            this.rbAdhesion.Location = new System.Drawing.Point(16, 50);
            this.rbAdhesion.Name = "rbAdhesion";
            this.rbAdhesion.Size = new System.Drawing.Size(140, 21);
            this.rbAdhesion.TabIndex = 1;
            this.rbAdhesion.Text = "仅 ① 字符粘连";
            this.rbAdhesion.UseVisualStyleBackColor = true;
            this.rbAdhesion.CheckedChanged += new System.EventHandler(this.NoiseOption_CheckedChanged);
            // 
            // rbDot
            // 
            this.rbDot.AutoSize = true;
            this.rbDot.Location = new System.Drawing.Point(220, 22);
            this.rbDot.Name = "rbDot";
            this.rbDot.Size = new System.Drawing.Size(142, 21);
            this.rbDot.TabIndex = 2;
            this.rbDot.Text = "仅 ② 点阵纹理";
            this.rbDot.UseVisualStyleBackColor = true;
            this.rbDot.CheckedChanged += new System.EventHandler(this.NoiseOption_CheckedChanged);
            // 
            // rbNone
            // 
            this.rbNone.AutoSize = true;
            this.rbNone.Location = new System.Drawing.Point(220, 50);
            this.rbNone.Name = "rbNone";
            this.rbNone.Size = new System.Drawing.Size(140, 21);
            this.rbNone.TabIndex = 3;
            this.rbNone.Text = "无噪声（基线对照）";
            this.rbNone.UseVisualStyleBackColor = true;
            this.rbNone.CheckedChanged += new System.EventHandler(this.NoiseOption_CheckedChanged);
            // 
            // gbControl
            // 
            this.gbControl.Controls.Add(this.btnNext);
            this.gbControl.Controls.Add(this.btnPie);
            this.gbControl.Controls.Add(this.btnExit);
            this.gbControl.Location = new System.Drawing.Point(12, 286);
            this.gbControl.Name = "gbControl";
            this.gbControl.Size = new System.Drawing.Size(416, 66);
            this.gbControl.TabIndex = 2;
            this.gbControl.TabStop = false;
            this.gbControl.Text = "控制";
            // 
            // btnNext
            // 
            this.btnNext.Location = new System.Drawing.Point(16, 22);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(116, 30);
            this.btnNext.TabIndex = 0;
            this.btnNext.Text = "换一张(&N)";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnPie
            // 
            this.btnPie.Location = new System.Drawing.Point(144, 22);
            this.btnPie.Name = "btnPie";
            this.btnPie.Size = new System.Drawing.Size(116, 30);
            this.btnPie.TabIndex = 1;
            this.btnPie.Text = "饼形图(&P)";
            this.btnPie.UseVisualStyleBackColor = true;
            this.btnPie.Click += new System.EventHandler(this.btnPie_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(272, 22);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(116, 30);
            this.btnExit.TabIndex = 2;
            this.btnExit.Text = "退出程序(&X)";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblCount,
            this.lblMemory,
            this.lblElapsed});
            this.statusStrip.Location = new System.Drawing.Point(0, 356);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(440, 22);
            this.statusStrip.TabIndex = 3;
            // 
            // lblCount
            // 
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(80, 17);
            this.lblCount.Text = "已生成 0 张";
            // 
            // lblMemory
            // 
            this.lblMemory.Name = "lblMemory";
            this.lblMemory.Size = new System.Drawing.Size(100, 17);
            this.lblMemory.Text = "工作集 0.0 MB";
            // 
            // lblElapsed
            // 
            this.lblElapsed.Name = "lblElapsed";
            this.lblElapsed.Size = new System.Drawing.Size(100, 17);
            this.lblElapsed.Spring = true;
            this.lblElapsed.Text = "用时 0.0 ms";
            this.lblElapsed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(440, 378);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.gbControl);
            this.Controls.Add(this.gbNoise);
            this.Controls.Add(this.gbCaptcha);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "动态绘制验证码";
            this.gbCaptcha.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picCaptcha)).EndInit();
            this.gbNoise.ResumeLayout(false);
            this.gbNoise.PerformLayout();
            this.gbControl.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox gbCaptcha;
        private System.Windows.Forms.PictureBox picCaptcha;
        private System.Windows.Forms.Label lblMeta;
        private System.Windows.Forms.CheckBox chkAnswer;
        private System.Windows.Forms.Label lblAnswer;
        private System.Windows.Forms.GroupBox gbNoise;
        private System.Windows.Forms.RadioButton rbBoth;
        private System.Windows.Forms.RadioButton rbAdhesion;
        private System.Windows.Forms.RadioButton rbDot;
        private System.Windows.Forms.RadioButton rbNone;
        private System.Windows.Forms.GroupBox gbControl;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnPie;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblCount;
        private System.Windows.Forms.ToolStripStatusLabel lblMemory;
        private System.Windows.Forms.ToolStripStatusLabel lblElapsed;
    }
}
