using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lab3_3240704204
{
    /// <summary>
    /// P5 的六种噪声手段。取值与 PersonalParameters.NoisePool 的下标一一对应，
    /// 这样参数表的下标可以直接转成枚举，不用再写一遍 switch。
    /// </summary>
    public enum NoiseKind
    {
        PerCharRotate = 0,
        WaveBaseline = 1,
        GradientStroke = 2,
        SameColorCurve = 3,
        CharAdhesion = 4,
        DotMatrix = 5,
        None = -1
    }

    /// <summary>一张验证码：位图 + 正文。调用方负责 Dispose（Bitmap 是非托管资源）。</summary>
    public sealed class CaptchaResult : IDisposable
    {
        public Bitmap Image { get; private set; }
        public string Text { get; private set; }

        public CaptchaResult(Bitmap image, string text)
        {
            Image = image;
            Text = text;
        }

        public void Dispose()
        {
            if (Image != null)
            {
                Image.Dispose();
                Image = null;
            }
        }
    }

    /// <summary>
    /// 验证码绘制。字符轮廓一律用 GraphicsPath.AddString 生成，
    /// 再用 Matrix 做平移 / 旋转，填充与描边都落在同一个路径对象上；
    /// 噪声同样用路径、画笔和网格点完成，没有逐像素 SetPixel 的写法。
    /// </summary>
    public static class CaptchaRenderer
    {
        public const int ImageWidth = 240;
        public const int ImageHeight = 80;
        private const string FontFamilyName = "Arial";

        private static readonly Color InkDark = Color.FromArgb(20, 26, 60);
        private static readonly Color InkLight = Color.FromArgb(92, 26, 110);
        private static readonly Color BackgroundColor = Color.FromArgb(250, 250, 246);
        private static readonly Color BorderColor = Color.FromArgb(120, 130, 150);

        /// <summary>生成一张验证码；first / second 为 P5 指定的两种噪声手段。</summary>
        public static CaptchaResult Render(Random rng, NoiseKind first, NoiseKind second)
        {
            if (rng == null) throw new ArgumentNullException("rng");
            if (first == second && first != NoiseKind.None) second = NoiseKind.None;

            string text = CaptchaText.Create(rng);
            Bitmap bitmap = new Bitmap(ImageWidth, ImageHeight, PixelFormat.Format32bppArgb);
            try
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    g.Clear(BackgroundColor);

                    bool rotate = Has(first, second, NoiseKind.PerCharRotate);
                    bool wave = Has(first, second, NoiseKind.WaveBaseline);
                    bool gradient = Has(first, second, NoiseKind.GradientStroke);
                    bool sameColorCurve = Has(first, second, NoiseKind.SameColorCurve);
                    bool adhesion = Has(first, second, NoiseKind.CharAdhesion);
                    bool dots = Has(first, second, NoiseKind.DotMatrix);

                    if (dots) DrawDotMatrix(g, rng);
                    DrawCharacters(g, text, rng, rotate, wave, gradient, adhesion);
                    if (dots) DrawDotOverlay(g, rng);
                    if (sameColorCurve) DrawSameColorCurves(g, rng);

                    using (Pen border = new Pen(BorderColor, 1f))
                        g.DrawRectangle(border, 0, 0, ImageWidth - 1, ImageHeight - 1);
                }
                return new CaptchaResult(bitmap, text);
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        /// <summary>批量输出：每张 PNG 配一个同名 .txt 标注，另写一份清单文件。</summary>
        public static void GenerateBatch(int count, string outputDir, Random rng, NoiseKind first, NoiseKind second)
        {
            Directory.CreateDirectory(outputDir);
            for (int i = 1; i <= count; i++)
            {
                using (CaptchaResult result = Render(rng, first, second))
                {
                    string baseName = i.ToString("D4", CultureInfo.InvariantCulture);
                    result.Image.Save(Path.Combine(outputDir, baseName + ".png"), ImageFormat.Png);
                    File.WriteAllText(Path.Combine(outputDir, baseName + ".txt"), result.Text, new UTF8Encoding(false));
                }
            }
            WriteManifest(count, outputDir, first, second);
        }

        private static void WriteManifest(int count, string dir, NoiseKind first, NoiseKind second)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("实验3_3240704204 验证码批量输出清单");
            sb.AppendLine("学号: " + PersonalParameters.StudentId + "    S = " + PersonalParameters.S);
            sb.AppendLine("P4 码制: " + PersonalParameters.CaptchaCodeName);
            sb.AppendLine("P5 噪声: " + NoiseName(first) + " + " + NoiseName(second));
            sb.AppendLine("数量: " + count);
            sb.AppendLine("生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine("说明: 每个 PNG 的同名 .txt 保存该图验证码正文");
            File.WriteAllText(Path.Combine(dir, "_manifest.txt"), sb.ToString(), new UTF8Encoding(false));
        }

        private static string NoiseName(NoiseKind kind)
        {
            if (kind == NoiseKind.None) return "无";
            return PersonalParameters.NoisePool[(int)kind];
        }

        private static bool Has(NoiseKind first, NoiseKind second, NoiseKind kind)
        {
            return first == kind || second == kind;
        }

        /// <summary>逐字符构造 GraphicsPath，再用 Matrix 变换后统一填充。</summary>
        private static void DrawCharacters(Graphics g, string text, Random rng, bool rotate, bool wave, bool gradient, bool adhesion)
        {
            int count = text.Length;
            float slot = adhesion ? 40f : 50f;
            if (count >= 5) slot = adhesion ? 33f : 41f;
            float startX = (ImageWidth - slot * count) / 2f;
            float baseY = ImageHeight / 2f + 2f;

            List<GraphicsPath> paths = new List<GraphicsPath>(count);
            List<RectangleF> bounds = new List<RectangleF>(count);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    GraphicsPath path = new GraphicsPath();
                    bool added = false;
                    try
                    {
                        using (FontFamily family = new FontFamily(FontFamilyName))
                        {
                            path.AddString(text[i].ToString(), family, (int)FontStyle.Bold, 40f,
                                new PointF(0f, 0f), StringFormat.GenericTypographic);
                        }

                        RectangleF raw = path.GetBounds();
                        float angle = rotate ? (float)(rng.NextDouble() * 28.0 - 14.0) : 0f;
                        float dy = wave ? (float)Math.Sin(i * 1.7 + rng.NextDouble() * Math.PI) * 8f : 0f;
                        float cx = startX + slot * (i + 0.5f);
                        float cy = baseY + dy;

                        using (Matrix matrix = new Matrix())
                        {
                            matrix.Translate(-(raw.Left + raw.Width / 2f), -(raw.Top + raw.Height / 2f));
                            if (angle != 0f) matrix.Rotate(angle);
                            matrix.Translate(cx, cy);
                            path.Transform(matrix);
                        }

                        paths.Add(path);
                        bounds.Add(path.GetBounds());
                        added = true;
                    }
                    finally
                    {
                        if (!added) path.Dispose();
                    }
                }

                RectangleF inkBounds = bounds[0];
                for (int i = 1; i < bounds.Count; i++)
                    inkBounds = RectangleF.Union(inkBounds, bounds[i]);
                inkBounds.Inflate(4f, 4f);

                using (Brush brush = gradient
                    ? (Brush)new LinearGradientBrush(inkBounds, InkDark, InkLight, 30f)
                    : (Brush)new SolidBrush(InkDark))
                {
                    foreach (GraphicsPath path in paths)
                        g.FillPath(brush, path);
                }

                // 抗锯齿填充会让笔画偏细，再描一遍边，人眼更容易分辨。
                using (Pen pen = new Pen(Color.FromArgb(235, InkDark), 1.2f))
                {
                    pen.LineJoin = LineJoin.Round;
                    foreach (GraphicsPath path in paths)
                        g.DrawPath(pen, path);
                }

                if (adhesion)
                    DrawAdhesionBridges(g, bounds, rng);
            }
            finally
            {
                foreach (GraphicsPath path in paths)
                    path.Dispose();
            }
        }

        /// <summary>字符粘连：在相邻字符的包围盒之间填一条深色“桥”，让轮廓连成一片。</summary>
        private static void DrawAdhesionBridges(Graphics g, List<RectangleF> bounds, Random rng)
        {
            if (bounds.Count < 2) return;
            using (GraphicsPath bridges = new GraphicsPath())
            using (Brush brush = new SolidBrush(InkDark))
            {
                for (int i = 0; i < bounds.Count - 1; i++)
                {
                    RectangleF left = bounds[i];
                    RectangleF right = bounds[i + 1];

                    float top = Math.Max(left.Top, right.Top);
                    float bottom = Math.Min(left.Bottom, right.Bottom);
                    float y = top + (bottom - top) * (0.25f + (float)rng.NextDouble() * 0.45f);

                    float x1 = left.Right - 3f;
                    float x2 = right.Left + 3f;
                    if (x2 <= x1) { x2 = x1 + 2f; }
                    float midX = (x1 + x2) / 2f;
                    float width = (x2 - x1) + 10f;
                    float height = 4.5f + (float)rng.NextDouble() * 2.5f;

                    using (GraphicsPath blob = new GraphicsPath())
                    {
                        blob.AddEllipse(midX - width / 2f, y - height / 2f, width, height);
                        using (Matrix matrix = new Matrix())
                        {
                            matrix.RotateAt((float)(rng.NextDouble() * 36.0 - 18.0), new PointF(midX, y));
                            blob.Transform(matrix);
                        }
                        bridges.AddPath(blob, false);
                    }
                }

                g.FillPath(brush, bridges);
                using (Pen pen = new Pen(Color.FromArgb(235, InkDark), 1.1f))
                    g.DrawPath(pen, bridges);
            }
        }

        /// <summary>点阵纹理（背景）：规则网格上的细点，随机灰度与透明度，用来打乱二值化阈值。</summary>
        private static void DrawDotMatrix(Graphics g, Random rng)
        {
            const int step = 6;
            int phaseX = rng.Next(step);
            int phaseY = rng.Next(step);

            Brush[] palette = new Brush[6];
            try
            {
                for (int i = 0; i < palette.Length; i++)
                {
                    int gray = 70 + i * 14;
                    int alpha = 135 + i * 12;
                    palette[i] = new SolidBrush(Color.FromArgb(alpha, gray, gray, gray));
                }

                for (int y = 2; y < ImageHeight - 2; y += step)
                {
                    for (int x = 2; x < ImageWidth - 2; x += step)
                    {
                        float cx = x + phaseX * 0.5f + (float)(rng.NextDouble() - 0.5);
                        float cy = y + phaseY * 0.5f + (float)(rng.NextDouble() - 0.5);
                        float size = 1.3f + (float)rng.NextDouble() * 1.1f;
                        g.FillEllipse(palette[rng.Next(palette.Length)], cx, cy, size, size);
                    }
                }
            }
            finally
            {
                foreach (Brush brush in palette)
                {
                    if (brush != null) brush.Dispose();
                }
            }
        }

        /// <summary>点阵纹理（前景）：在字符笔面上再压一层低透明度暗点，人眼仍能顺着笔画读。</summary>
        private static void DrawDotOverlay(Graphics g, Random rng)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(70, 40, 45, 60)))
            {
                for (int i = 0; i < 90; i++)
                {
                    float cx = (float)rng.NextDouble() * ImageWidth;
                    float cy = (float)rng.NextDouble() * ImageHeight;
                    float size = 1.2f + (float)rng.NextDouble() * 1.6f;
                    g.FillEllipse(brush, cx, cy, size, size);
                }
            }
        }

        /// <summary>同色干扰曲线：与字符同色的细曲线穿过整幅图，两条即可。</summary>
        private static void DrawSameColorCurves(Graphics g, Random rng)
        {
            using (Pen pen = new Pen(Color.FromArgb(150, InkDark), 1.3f))
            {
                for (int k = 0; k < 2; k++)
                {
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        float y0 = 10f + (float)rng.NextDouble() * (ImageHeight - 20);
                        float y1 = 10f + (float)rng.NextDouble() * (ImageHeight - 20);
                        float y2 = 10f + (float)rng.NextDouble() * (ImageHeight - 20);
                        float y3 = y0 + (float)(rng.NextDouble() * 20.0 - 10.0);
                        path.AddBezier(-5f, y0, ImageWidth * 0.35f, y1, ImageWidth * 0.7f, y2, ImageWidth + 5f, y3);
                        g.DrawPath(pen, path);
                    }
                }
            }
        }
    }

    /// <summary>按 P4 码制生成验证码正文。</summary>
    public static class CaptchaText
    {
        private const string Digits = "0123456789";
        private const string UpperLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Mixed = Digits + UpperLetters;

        public static string Create(Random rng)
        {
            if (rng == null) throw new ArgumentNullException("rng");
            switch (PersonalParameters.CaptchaCodeIndex)
            {
                case 0:
                    return RandomString(rng, Digits, 4);
                case 1:
                    return RandomString(rng, UpperLetters, 5);
                case 2:
                    return RandomString(rng, Mixed, 6);
                default:
                    return RandomEquation(rng);
            }
        }

        private static string RandomString(Random rng, string pool, int length)
        {
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(pool[rng.Next(pool.Length)]);
            return sb.ToString();
        }

        /// <summary>算式码制：形如 7-2=5，保证 a &gt; b，结果为非负数。</summary>
        private static string RandomEquation(Random rng)
        {
            int a = rng.Next(2, 10);
            int b = rng.Next(1, a);
            return string.Format("{0}-{1}={2}", a, b, a - b);
        }
    }
}
