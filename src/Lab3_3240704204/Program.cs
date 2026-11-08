using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lab3_3240704204
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        private const int AttachParentProcess = -1;

        /// <summary>
        /// 入口。带 --gen 参数时只做批量输出，不创建任何窗体，跑完直接返回；
        /// 不带参数时进入正常的界面模式（指导书注意事项 5 的要求）。
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && string.Equals(args[0], "--gen", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = RunBatch(args);
                return;
            }

            Application.Run(new MainForm());
        }

        private static int RunBatch(string[] args)
        {
            AttachConsole(AttachParentProcess);

            if (args.Length < 2)
            {
                WriteLine("用法: 实验3_3240704204.exe --gen <数量> <输出目录> [--noise both|a|b|none]");
                return 2;
            }

            int count;
            if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count <= 0 || count > 5000)
            {
                WriteLine("数量必须是 1~5000 之间的整数。");
                return 2;
            }

            string outputDir = args.Length >= 3
                ? Path.GetFullPath(args[2])
                : Path.GetFullPath("out_" + PersonalParameters.StudentId + "_01");

            NoiseKind first = (NoiseKind)PersonalParameters.NoiseIndexA;
            NoiseKind second = (NoiseKind)PersonalParameters.NoiseIndexB;

            for (int i = 3; i < args.Length; i++)
            {
                if (!string.Equals(args[i], "--noise", StringComparison.OrdinalIgnoreCase) || i + 1 >= args.Length)
                    continue;

                string mode = args[i + 1].ToLowerInvariant();
                switch (mode)
                {
                    case "both":
                        first = (NoiseKind)PersonalParameters.NoiseIndexA;
                        second = (NoiseKind)PersonalParameters.NoiseIndexB;
                        break;
                    case "a":
                        first = NoiseKind.CharAdhesion;
                        second = NoiseKind.None;
                        break;
                    case "b":
                        first = NoiseKind.DotMatrix;
                        second = NoiseKind.None;
                        break;
                    case "none":
                        first = NoiseKind.None;
                        second = NoiseKind.None;
                        break;
                    default:
                        WriteLine("未知的 --noise 参数，可用: both / a / b / none");
                        return 2;
                }
                i++;
            }

            try
            {
                // 整个程序共用一个 Random 实例：同一次运行里反复 new Random() 可能拿到同一段序列。
                Random rng = new Random(PersonalParameters.S);
                CaptchaRenderer.GenerateBatch(count, outputDir, rng, first, second);
                WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "已输出 {0} 张验证码到 {1}", count, outputDir));
                return 0;
            }
            catch (Exception ex)
            {
                WriteLine("生成失败: " + ex.Message);
                return 1;
            }
        }

        private static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
