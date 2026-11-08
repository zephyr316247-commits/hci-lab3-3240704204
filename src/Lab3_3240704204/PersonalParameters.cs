using System;

namespace Lab3_3240704204
{
    /// <summary>
    /// 个人参数集中记录处（指导书 7.1 计算规则、7.3 第 4 条要求）。
    /// 本人学号 3240704204，取后四位 S = 4204。
    /// 走查 / 现场改造时只改 S，界面、验证码码制、噪声手段与饼图数据都跟着变，
    /// 不再把这个学号派生的数字散落在各处的代码里。
    /// </summary>
    public static class PersonalParameters
    {
        public const string StudentId = "3240704204";
        public const string StudentName = "王志远";
        public static readonly int S = 4204;                 // 学号后四位

        public const string ExperimentNo = "实验3";
        public const string ProjectName = "实验3_3240704204";

        // ---------------- P4 验证码码制：S mod 4 ----------------
        // 0 四位数字 / 1 五位大写字母 / 2 六位数字与字母混合 / 3 四位算式（如 7-2=5）
        public static int CaptchaCodeIndex { get { return S % 4; } }

        public static string CaptchaCodeName
        {
            get
            {
                switch (S % 4)
                {
                    case 0: return "四位数字";
                    case 1: return "五位大写字母";
                    case 2: return "六位数字与字母混合";
                    default: return "四位算式";
                }
            }
        }

        // ---------------- P5 验证码必用噪声手段：手段池[S mod 6] 与 手段池[(S div 6) mod 6]，相同则第二项下移一位 ----------------
        public static readonly string[] NoisePool =
        {
            "逐字符旋转", "波浪基线", "渐变笔画", "同色干扰曲线", "字符粘连", "点阵纹理"
        };

        public static int NoiseIndexA { get { return S % 6; } }

        public static int NoiseIndexB
        {
            get
            {
                int first = S % 6;
                int second = (S / 6) % 6;
                return second == first ? (second + 1) % 6 : second;
            }
        }

        public static string NoiseNameA { get { return NoisePool[NoiseIndexA]; } }
        public static string NoiseNameB { get { return NoisePool[NoiseIndexB]; } }

        // ---------------- P6 饼图三月销量 ----------------
        // A = (S mod 300) + 100 = 104；B = (S div 3 mod 400) + 50 = 251；C = (S div 7 mod 250) + 80 = 180
        public static int MarchSales { get { return (S % 300) + 100; } }
        public static int AprilSales { get { return ((S / 3) % 400) + 50; } }
        public static int MaySales { get { return ((S / 7) % 250) + 80; } }

        public static int SalesTotal { get { return MarchSales + AprilSales + MaySales; } }

        /// <summary>按当前可见总量计算百分比；总量为 0 时返回 0.00%，避免除零。</summary>
        public static string FormatPercent(int value, int visibleTotal)
        {
            if (visibleTotal <= 0) return "0.00%";
            return string.Format("{0:0.00}%", value * 100.0 / visibleTotal);
        }
    }
}
