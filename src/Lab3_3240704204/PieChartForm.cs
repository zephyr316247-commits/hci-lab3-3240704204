using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Lab3_3240704204
{
    /// <summary>
    /// 饼形图窗体。饼、主标题、图例全部自绘（没有用 Legend 控件），
    /// 鼠标位置到扇区的判定用 Math.Atan2 反算角度，隐藏扇区后按可见总量重新归一化。
    /// </summary>
    public partial class PieChartForm : Form
    {
        private sealed class Sector
        {
            public string Month;
            public int Value;
            public Color Color;
            public bool Visible;
        }

        private const float PieRadius = 88f;
        private const float StartAngle = -90f;          // 水平向右为 0°，从 12 点方向开始顺时针
        private const float LegendRowHeight = 34f;
        private const float LegendRowTop = 354f;
        private const string TitleText = "每月商品销量占比饼形图";

        private readonly Sector[] sectors;
        private readonly RectangleF pieRect;
        private readonly PointF pieCenter;
        private int hoverSector = -1;
        private int hoverLegendRow = -1;

        public PieChartForm()
        {
            InitializeComponent();

            Text = "实验三 商品销售占比饼形图 — " + PersonalParameters.StudentId;
            DoubleBuffered = true;
            ResizeRedraw = true;

            // P6 三个月的销量，数值全部来自 PersonalParameters，界面里不出现裸的数字。
            sectors = new Sector[]
            {
                new Sector { Month = "3 月", Value = PersonalParameters.MarchSales, Color = Color.FromArgb(37, 99, 235), Visible = true },
                new Sector { Month = "4 月", Value = PersonalParameters.AprilSales, Color = Color.FromArgb(245, 158, 11), Visible = true },
                new Sector { Month = "5 月", Value = PersonalParameters.MaySales, Color = Color.FromArgb(16, 185, 129), Visible = true }
            };

            pieCenter = new PointF(ClientSize.Width / 2f, 222f);
            pieRect = new RectangleF(pieCenter.X - PieRadius, pieCenter.Y - PieRadius, PieRadius * 2f, PieRadius * 2f);

            lblStatus.Text = "把鼠标移到扇区上：高亮跟随并显示销量与百分比；点击图例行可隐藏 / 恢复扇区";
        }

        private RectangleF LegendBox
        {
            get { return new RectangleF(110f, 342f, 340f, 124f); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.FromArgb(248, 250, 253));

            int totalVisible = TotalVisible();
            DrawTitle(g);
            DrawPie(g, totalVisible);
            DrawLegend(g, totalVisible);
        }

        private int TotalVisible()
        {
            int total = 0;
            foreach (Sector sector in sectors)
            {
                if (sector.Visible) total += Math.Max(0, sector.Value);
            }
            return total;
        }

        private void DrawTitle(Graphics g)
        {
            using (Font titleFont = new Font("Microsoft YaHei", 18f, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(Color.FromArgb(30, 64, 175)))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString(TitleText, titleFont, titleBrush,
                    new RectangleF(0f, 16f, ClientSize.Width, 34f), format);
            }

            using (Font subFont = new Font("Microsoft YaHei", 9f))
            using (Brush subBrush = new SolidBrush(Color.FromArgb(90, 100, 120)))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center })
            {
                string subtitle = string.Format(
                    "P6 数据：3 月 {0} 件　4 月 {1} 件　5 月 {2} 件（合计 {3} 件）",
                    PersonalParameters.MarchSales, PersonalParameters.AprilSales,
                    PersonalParameters.MaySales, PersonalParameters.SalesTotal);
                g.DrawString(subtitle, subFont, subBrush,
                    new RectangleF(0f, 54f, ClientSize.Width, 20f), format);
            }
        }

        private void DrawPie(Graphics g, int totalVisible)
        {
            if (totalVisible <= 0)
            {
                using (Font font = new Font("Microsoft YaHei", 10f))
                using (Brush brush = new SolidBrush(Color.Firebrick))
                using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("没有可显示的有效数据", font, brush, pieRect, format);
                }
                return;
            }

            float start = StartAngle;
            for (int i = 0; i < sectors.Length; i++)
            {
                Sector sector = sectors[i];
                if (!sector.Visible) continue;

                float sweep = 360f * Math.Max(0, sector.Value) / totalVisible;
                RectangleF rect = pieRect;
                bool highlighted = (i == hoverSector);

                if (highlighted)
                {
                    float middle = (start + sweep / 2f) * (float)Math.PI / 180f;
                    rect.Offset((float)Math.Cos(middle) * 8f, (float)Math.Sin(middle) * 8f);
                }

                Color color = highlighted ? Lighten(sector.Color, 0.24f) : sector.Color;

                using (Brush brush = new SolidBrush(color))
                using (Pen separator = new Pen(Color.White, 2f))
                {
                    g.FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, start, sweep);
                    g.DrawPie(separator, rect.X, rect.Y, rect.Width, rect.Height, start, sweep);
                }

                if (highlighted)
                {
                    using (Pen highlightPen = new Pen(color, 3f))
                    {
                        g.DrawPie(highlightPen, rect.X, rect.Y, rect.Width, rect.Height, start, sweep);
                    }
                }

                start += sweep;
            }
        }

        private void DrawLegend(Graphics g, int totalVisible)
        {
            RectangleF box = LegendBox;

            using (Pen frame = new Pen(Color.FromArgb(160, 175, 195), 1.2f))
            {
                g.DrawRectangle(frame, box.X, box.Y, box.Width, box.Height);
            }

            using (Font rowFont = new Font("Microsoft YaHei", 10f, FontStyle.Bold))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < sectors.Length; i++)
                {
                    Sector sector = sectors[i];
                    float y = LegendRowTop + i * LegendRowHeight;

                    Color indicator = sector.Visible ? sector.Color : Color.FromArgb(195, 200, 210);
                    using (Brush indicatorBrush = new SolidBrush(indicator))
                        g.FillRectangle(indicatorBrush, box.X + 18f, y + 4f, 16f, 16f);
                    using (Pen indicatorPen = new Pen(Color.FromArgb(90, 100, 120), 1f))
                        g.DrawRectangle(indicatorPen, box.X + 18f, y + 4f, 16f, 16f);

                    string text;
                    if (sector.Visible)
                    {
                        text = string.Format("{0} 销量占比：{1:0.00}%　（{2} 件）",
                            sector.Month, sector.Value * 100f / totalVisible, sector.Value);
                    }
                    else
                    {
                        text = string.Format("{0}（已隐藏，其余扇区重新归一化）", sector.Month);
                    }

                    using (Brush textBrush = new SolidBrush(sector.Visible ? Color.FromArgb(35, 45, 65) : Color.FromArgb(150, 155, 165)))
                    {
                        g.DrawString(text, rowFont, textBrush,
                            new RectangleF(box.X + 44f, y, box.Width - 52f, 24f), format);
                    }
                }
            }

            using (Font tipFont = new Font("Microsoft YaHei", 8f))
            using (Brush tipBrush = new SolidBrush(Color.FromArgb(130, 140, 155)))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString("提示：点击图例行可隐藏 / 恢复扇区；隐藏后其余扇区按可见总量重新归一化",
                    tipFont, tipBrush, new RectangleF(box.X - 40f, box.Bottom + 6f, box.Width + 80f, 20f), format);
            }
        }

        private static Color Lighten(Color color, float amount)
        {
            return Color.FromArgb(
                color.A,
                (int)Math.Min(255, color.R + (255 - color.R) * amount),
                (int)Math.Min(255, color.G + (255 - color.G) * amount),
                (int)Math.Min(255, color.B + (255 - color.B) * amount));
        }

        private int HitTestSector(Point location)
        {
            float dx = location.X - pieCenter.X;
            float dy = location.Y - pieCenter.Y;
            if (dx * dx + dy * dy > PieRadius * PieRadius) return -1;

            float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            float relative = angle - StartAngle;
            while (relative < 0f) relative += 360f;
            while (relative >= 360f) relative -= 360f;

            int totalVisible = TotalVisible();
            if (totalVisible <= 0) return -1;

            float accumulated = 0f;
            for (int i = 0; i < sectors.Length; i++)
            {
                if (!sectors[i].Visible) continue;
                float sweep = 360f * Math.Max(0, sectors[i].Value) / totalVisible;
                if (relative < accumulated + sweep) return i;
                accumulated += sweep;
            }
            return -1;
        }

        private int HitTestLegend(Point location)
        {
            RectangleF box = LegendBox;
            if (!box.Contains(location)) return -1;

            for (int i = 0; i < sectors.Length; i++)
            {
                RectangleF row = new RectangleF(box.X, LegendRowTop + i * LegendRowHeight, box.Width, LegendRowHeight - 2f);
                if (row.Contains(location)) return i;
            }
            return -1;
        }

        private void UpdateStatus(int sectorIndex)
        {
            if (sectorIndex >= 0 && sectorIndex < sectors.Length && sectors[sectorIndex].Visible)
            {
                Sector sector = sectors[sectorIndex];
                int totalVisible = TotalVisible();
                lblStatus.Text = string.Format("{0}：销量 {1} 件，占当前显示总量 {2:0.00}%（可见合计 {3} 件）",
                    sector.Month, sector.Value, sector.Value * 100.0 / totalVisible, totalVisible);
            }
            else
            {
                lblStatus.Text = "把鼠标移到扇区上：高亮跟随并显示销量与百分比；点击图例行可隐藏 / 恢复扇区";
            }
        }

        private void ToggleSector(int index)
        {
            Sector sector = sectors[index];

            if (sector.Visible)
            {
                int visibleCount = 0;
                foreach (Sector item in sectors)
                {
                    if (item.Visible) visibleCount++;
                }
                if (visibleCount <= 1)
                {
                    lblStatus.Text = "至少要保留一个可见扇区，全部隐藏后无法计算占比。";
                    return;
                }
                sector.Visible = false;
            }
            else
            {
                sector.Visible = true;
            }

            hoverSector = -1;
            UpdateStatus(-1);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int sector = HitTestSector(e.Location);
            int legend = HitTestLegend(e.Location);
            bool changed = sector != hoverSector || legend != hoverLegendRow;

            hoverSector = sector;
            hoverLegendRow = legend;
            Cursor = legend >= 0 ? Cursors.Hand : Cursors.Default;

            if (changed)
            {
                UpdateStatus(hoverSector);
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (hoverSector != -1 || hoverLegendRow != -1)
            {
                hoverSector = -1;
                hoverLegendRow = -1;
                UpdateStatus(-1);
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            int legend = HitTestLegend(e.Location);
            if (legend >= 0)
            {
                ToggleSector(legend);
            }
        }
    }
}
