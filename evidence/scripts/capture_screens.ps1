$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$src = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

public static class Win32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr hWnd, EnumProc proc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern uint GetGuiResources(IntPtr hProcess, uint flag);

    public const uint WM_MOUSEMOVE = 0x0200;
    public const uint BM_CLICK = 0x00F5;
    public const uint WM_CLOSE = 0x0010;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    public static string GetText(IntPtr hWnd) { StringBuilder sb = new StringBuilder(512); GetWindowText(hWnd, sb, sb.Capacity); return sb.ToString(); }

    public static IntPtr FindChild(IntPtr parent, string contains)
    {
        IntPtr found = IntPtr.Zero;
        EnumChildWindows(parent, delegate(IntPtr h, IntPtr l) {
            if (GetText(h).Contains(contains)) { found = h; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static IntPtr FindWindowContains(string titlePart)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr l) {
            if (!IsWindowVisible(h)) return true;
            if (GetText(h).Contains(titlePart)) { found = h; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static void BringToFront(IntPtr hWnd)
    {
        // 先把窗口临时置顶，避免被其它窗口遮挡后 CopyFromScreen 截到别的内容
        SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(400);
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

    public static void MoveToClient(IntPtr hWnd, int x, int y)
    {
        POINT p = new POINT();
        p.X = x;
        p.Y = y;
        ClientToScreen(hWnd, ref p);
        SetCursorPos(p.X, p.Y);
    }

    public static void ClickClient(IntPtr hWnd, int x, int y)
    {
        POINT p = new POINT();
        p.X = x;
        p.Y = y;
        ClientToScreen(hWnd, ref p);
        SetCursorPos(p.X, p.Y);
        System.Threading.Thread.Sleep(120);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(90);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }
}
"@

Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing

$root = "C:\Users\12807\Desktop\3240704204_王志远_实验3"
$exe = Join-Path $root "src\Lab3_3240704204\bin\Release\实验3_3240704204.exe"
$shots = Join-Path $root "evidence\screenshots"
New-Item -ItemType Directory -Force -Path $shots | Out-Null

Get-Process -Name "实验3_3240704204" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

$proc = Start-Process -FilePath $exe -PassThru
$main = [IntPtr]::Zero
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Milliseconds 200
    $proc.Refresh()
    if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { $main = $proc.MainWindowHandle; break }
}
if ($main -eq [IntPtr]::Zero) { throw "主窗口没有出现" }
Start-Sleep -Milliseconds 800
[Win32]::ShowWindow($main, 9) | Out-Null
[Win32]::BringToFront($main) | Out-Null
Start-Sleep -Milliseconds 500

$cursor = New-Object Win32+POINT
[Win32]::GetCursorPos([ref]$cursor) | Out-Null

function Get-Stats {
    $proc.Refresh()
    return [pscustomobject]@{
        PrivateMB = [math]::Round($proc.PrivateMemorySize64 / 1MB, 2)
        WorkingSetMB = [math]::Round($proc.WorkingSet64 / 1MB, 2)
        Gdi = [Win32]::GetGuiResources($proc.Handle, 0)
        User = [Win32]::GetGuiResources($proc.Handle, 1)
    }
}

$rows = New-Object System.Collections.Generic.List[string]
$rows.Add("已点击次数,私有内存MB,工作集MB,GDI对象,USER对象")
$s0 = Get-Stats
$rows.Add("0,$($s0.PrivateMB),$($s0.WorkingSetMB),$($s0.Gdi),$($s0.User)")
[Win32]::Capture($main, (Join-Path $shots "01_captcha_main.png"))
Write-Output "01 main captured: private=$($s0.PrivateMB) gdi=$($s0.Gdi)"

# 显示答案（自测），证明码制为 4 位数字
$chk = [Win32]::FindChild($main, "显示答案")
if ($chk -ne [IntPtr]::Zero) {
    [Win32]::PostMessage($chk, [Win32]::BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 400
    [Win32]::BringToFront($main) | Out-Null
    [Win32]::Capture($main, (Join-Path $shots "02_captcha_answer.png"))
    [Win32]::PostMessage($chk, [Win32]::BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 300
}

# 连续点击“换一张”2000 次，每 500 次记录一次内存与句柄
$btnNext = [Win32]::FindChild($main, "换一张")
if ($btnNext -eq [IntPtr]::Zero) { throw "找不到换一张按钮" }
for ($i = 1; $i -le 2000; $i++) {
    [Win32]::SendMessage($btnNext, [Win32]::BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 6
    if ($i % 500 -eq 0) {
        $s = Get-Stats
        $rows.Add("$i,$($s.PrivateMB),$($s.WorkingSetMB),$($s.Gdi),$($s.User)")
        Write-Output "clicks=$i private=$($s.PrivateMB) MB gdi=$($s.Gdi) user=$($s.User)"
        if ($i -eq 1000) {
            Start-Sleep -Milliseconds 300
            [Win32]::BringToFront($main) | Out-Null
            [Win32]::Capture($main, (Join-Path $shots "03_captcha_after1000.png"))
        }
    }
}
Start-Sleep -Milliseconds 400
[Win32]::BringToFront($main) | Out-Null
[Win32]::Capture($main, (Join-Path $shots "04_captcha_after2000.png"))
Write-Output "04 captured after 2000 clicks"

# 打开饼形图
$btnPie = [Win32]::FindChild($main, "饼形图")
[Win32]::PostMessage($btnPie, [Win32]::BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
$pie = [IntPtr]::Zero
for ($i = 0; $i -lt 50; $i++) {
    Start-Sleep -Milliseconds 200
    $pie = [Win32]::FindWindowContains("饼形图")
    if ($pie -ne [IntPtr]::Zero) { break }
}
if ($pie -eq [IntPtr]::Zero) { throw "饼形图窗口没有出现" }
[Win32]::BringToFront($pie) | Out-Null
Start-Sleep -Milliseconds 700
[Win32]::Capture($pie, (Join-Path $shots "05_pie_normal.png"))
Write-Output "05 pie normal captured"

# 悬停 3 月扇区：客户区 (314, 173)
[Win32]::MoveToClient($pie, 314, 173)
Start-Sleep -Milliseconds 700
[Win32]::Capture($pie, (Join-Path $shots "06_pie_hover_march.png"))
Write-Output "06 pie hover march captured"

# 悬停 4 月扇区：客户区 (306, 276)
[Win32]::MoveToClient($pie, 306, 276)
Start-Sleep -Milliseconds 700
[Win32]::Capture($pie, (Join-Path $shots "07_pie_hover_april.png"))
Write-Output "07 pie hover april captured"

# 点击图例第 1 行（3 月，y=370）隐藏
[Win32]::ClickClient($pie, 200, 370)
Start-Sleep -Milliseconds 300
[Win32]::MoveToClient($pie, 30, 20)
Start-Sleep -Milliseconds 500
[Win32]::Capture($pie, (Join-Path $shots "08_pie_hide_march.png"))
Write-Output "08 pie hide march captured"

# 再隐藏 4 月（y=404），只剩 5 月
[Win32]::ClickClient($pie, 200, 404)
Start-Sleep -Milliseconds 300
[Win32]::MoveToClient($pie, 30, 20)
Start-Sleep -Milliseconds 500
[Win32]::Capture($pie, (Join-Path $shots "09_pie_hide_two.png"))
Write-Output "09 pie hide two captured"

# 恢复两个扇区
foreach ($y in @(370, 404)) {
    [Win32]::ClickClient($pie, 200, $y)
    Start-Sleep -Milliseconds 250
}
[Win32]::MoveToClient($pie, 30, 20)
Start-Sleep -Milliseconds 400
[Win32]::Capture($pie, (Join-Path $shots "10_pie_restored.png"))

# 关闭窗口
[Win32]::PostMessage($pie, [Win32]::WM_CLOSE, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
Start-Sleep -Milliseconds 800
$btnExit = [Win32]::FindChild($main, "退出程序")
if ($btnExit -ne [IntPtr]::Zero) {
    [Win32]::PostMessage($btnExit, [Win32]::BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
}
Start-Sleep -Milliseconds 800
$proc.Refresh()
if (!$proc.HasExited) { $proc.Kill() }

# 还原鼠标位置
[Win32]::SetCursorPos($cursor.X, $cursor.Y) | Out-Null

$csv = Join-Path $root "evidence\hitrate\memory_test.csv"
$rows | Set-Content -Path $csv -Encoding UTF8
Write-Output "done"
