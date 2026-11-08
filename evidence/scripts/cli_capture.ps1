$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$src = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

public static class Win32Cli
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr hWnd, StringBuilder name, int max);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);

    public static string GetText(IntPtr hWnd) { StringBuilder sb = new StringBuilder(512); GetWindowText(hWnd, sb, sb.Capacity); return sb.ToString(); }
    public static string GetClass(IntPtr hWnd) { StringBuilder sb = new StringBuilder(256); GetClassName(hWnd, sb, sb.Capacity); return sb.ToString(); }

    public static IntPtr FindConsoleWindow(string titlePart)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr l) {
            if (!IsWindowVisible(h)) return true;
            string cls = GetClass(h); if ((cls == "ConsoleWindowClass" || cls == "CASCADIA_HOSTING_WINDOW_CLASS") && GetText(h).Contains(titlePart)) { found = h; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static void Capture(IntPtr hWnd, string path)
    {
        RECT r;
        int hr = DwmGetWindowAttribute(hWnd, 9, out r, Marshal.SizeOf(typeof(RECT)));
        if (hr != 0 || r.Right <= r.Left || r.Bottom <= r.Top) { GetWindowRect(hWnd, out r); }
        int w = r.Right - r.Left;
        int h = r.Bottom - r.Top;
        using (Bitmap bmp = new Bitmap(w, h))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(r.Left, r.Top, 0, 0, new Size(w, h));
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
"@
Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing

$root = "C:\Users\12807\Desktop\3240704204_王志远_实验3"
$exe = Join-Path $root "src\Lab3_3240704204\bin\Release\实验3_3240704204.exe"
$outDir = Join-Path $root "evidence\out_3240704204_01"
$shots = Join-Path $root "evidence\screenshots"

$cmdLine = 'title 实验三 批量输出验证码 && chcp 65001 >nul && "' + $exe + '" --gen 200 "' + $outDir + '" && echo. && echo ---- 输出目录统计 ---- && dir /b "' + $outDir + '\*.png" | find /c /v "" && echo 张 PNG 图片 && pause'
$cmd = Start-Process -FilePath "cmd.exe" -ArgumentList "/k", $cmdLine -PassThru
Start-Sleep -Seconds 10
$console = [Win32Cli]::FindConsoleWindow("批量输出")
if ($console -eq [IntPtr]::Zero) { throw "找不到控制台窗口" }
[Win32Cli]::SetForegroundWindow($console) | Out-Null
Start-Sleep -Milliseconds 600
[Win32Cli]::Capture($console, (Join-Path $shots "11_cli_gen200.png"))
Write-Output "11 cli captured"
Start-Sleep -Milliseconds 300
if (!$cmd.HasExited) { Stop-Process -Id $cmd.Id -Force }
