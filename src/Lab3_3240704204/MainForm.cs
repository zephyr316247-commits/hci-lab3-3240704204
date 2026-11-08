using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Lab3_3240704204
{
    /// <summary>
    /// 验证码窗体。绘制全部在 CaptchaRenderer 里完成，这里只负责事件、重绘与状态反馈。
    /// 注意：换图时先 Dispose 旧位图，否则连续点“换一张”内存会一路涨（验收 3-4）。
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly Random rng = new Random(PersonalParameters.S * 7919 + 11);
        private CaptchaResult current;
        private int generatedCount;

        public MainForm()
        {
            InitializeComponent();

            Text = "实验三 验证码生成 — " + PersonalParameters.StudentId;
            gbCaptcha.Text = string.Format("验证码（P4：{0}　P5：{1} + {2}）",
                PersonalParameters.CaptchaCodeName, PersonalParameters.NoiseNameA, PersonalParameters.NoiseNameB);
            lblMeta.Text = string.Format(
                "学号 {0}　　S = {1}\r\nP4 码制：{2}　　P5 噪声：{3} + {4}",
                PersonalParameters.StudentId, PersonalParameters.S,
                PersonalParameters.CaptchaCodeName,
                PersonalParameters.NoiseNameA, PersonalParameters.NoiseNameB);

            RenderNext();
        }

        private NoiseKind FirstNoise
        {
            get
            {
                if (rbAdhesion.Checked) return NoiseKind.CharAdhesion;
                if (rbDot.Checked) return NoiseKind.DotMatrix;
                if (rbNone.Checked) return NoiseKind.None;
                return (NoiseKind)PersonalParameters.NoiseIndexA;
            }
        }

        private NoiseKind SecondNoise
        {
            get
            {
                if (rbAdhesion.Checked || rbDot.Checked || rbNone.Checked) return NoiseKind.None;
                return (NoiseKind)PersonalParameters.NoiseIndexB;
            }
        }

        private void RenderNext()
        {
            Stopwatch watch = Stopwatch.StartNew();
            CaptchaResult next = CaptchaRenderer.Render(rng, FirstNoise, SecondNoise);
            watch.Stop();

            CaptchaResult previous = current;
            current = next;
            picCaptcha.Image = current.Image;
            if (previous != null) previous.Dispose();

            generatedCount++;
            lblCount.Text = string.Format("已生成 {0} 张", generatedCount);
            lblElapsed.Text = string.Format("用时 {0:0.0} ms/张", watch.Elapsed.TotalMilliseconds);
            UpdateMemoryLabel();
            UpdateAnswerLabel();
        }

        private void UpdateMemoryLabel()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                process.Refresh();
                // 用私有内存而不是工作集：工作集会随系统内存压力被裁剪，连续观察时读数会忽高忽低。
                lblMemory.Text = string.Format("内存 {0:0.0} MB", process.PrivateMemorySize64 / 1048576.0);
            }
        }

        private void UpdateAnswerLabel()
        {
            lblAnswer.Text = (chkAnswer.Checked && current != null) ? ("答案：" + current.Text) : "（已隐藏）";
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            RenderNext();
        }

        private void btnPie_Click(object sender, EventArgs e)
        {
            using (PieChartForm form = new PieChartForm())
            {
                form.ShowDialog(this);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void NoiseOption_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radio = sender as RadioButton;
            if (radio == null || !radio.Checked) return;
            RenderNext();
        }

        private void chkAnswer_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAnswerLabel();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (current != null)
            {
                current.Dispose();
                current = null;
            }
            base.OnFormClosed(e);
        }
    }
}
