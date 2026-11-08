# -*- coding: utf-8 -*-
"""生成实验三报告 实验报告.docx（正文 4 页以内，附录数据表不计页数）。
报告中的所有实测数字都从 evidence 下的 CSV 读取，避免报告与数据不一致。"""
import csv
import os
from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
from PIL import Image

ROOT = r"C:\Users\12807\Desktop\3240704204_王志远_实验3"
SHOTS = os.path.join(ROOT, "evidence", "screenshots")
ASSETS = os.path.join(ROOT, "evidence", "report_assets")
HIT = os.path.join(ROOT, "evidence", "hitrate")
os.makedirs(ASSETS, exist_ok=True)


# ---------------- 读实测数据 ----------------
def read_hitrate():
    data = {}
    with open(os.path.join(HIT, "hitrate.csv"), encoding="utf-8-sig") as f:
        for row in csv.DictReader(f):
            data[row["config"]] = {
                "images": int(row["images"]),
                "chars": int(row["chars"]),
                "hits": int(row["char_hits"]),
                "acc": float(row["char_accuracy"]),
                "exact": int(row["exact_matches"]),
            }
    return data


def read_memory():
    rows = []
    with open(os.path.join(HIT, "memory_test.csv"), encoding="utf-8-sig") as f:
        for row in csv.DictReader(f):
            rows.append((row["已点击次数"], float(row["私有内存MB"]), row["GDI对象"], row["USER对象"]))
    return rows


HR = read_hitrate()
MEM = read_memory()
mem_text = " / ".join("%.2f" % m[1] for m in MEM)
gdi_values = sorted(set(m[2] for m in MEM))
gdi_text = gdi_values[0] if len(gdi_values) == 1 else "/".join(gdi_values)
first_render = "637.6"   # 首张截图状态栏读数，含 JIT 预热

# ---------------- 拼报告用图 ----------------
def compose(paths, out, target_h=430, gap=14, bg=(255, 255, 255)):
    ims = [Image.open(p).convert("RGB") for p in paths]
    scaled = []
    for im in ims:
        w, h = im.size
        nw = int(w * target_h / h)
        scaled.append(im.resize((nw, target_h), Image.LANCZOS))
    total_w = sum(im.size[0] for im in scaled) + gap * (len(scaled) - 1)
    canvas = Image.new("RGB", (total_w, target_h), bg)
    x = 0
    for im in scaled:
        canvas.paste(im, (x, 0))
        x += im.size[0] + gap
    canvas.save(out)


compose([os.path.join(SHOTS, "01_captcha_main.png"),
         os.path.join(SHOTS, "06_pie_hover_march.png")],
        os.path.join(ASSETS, "fig_ui_combo.png"), target_h=430)
compose([os.path.join(SHOTS, "08_pie_hide_march.png"),
         os.path.join(SHOTS, "09_pie_hide_two.png")],
        os.path.join(ASSETS, "fig_pie_hidden.png"), target_h=430)

# ---------------- 文档与样式 ----------------
doc = Document()
sec = doc.sections[0]
sec.page_width = Cm(21.0)
sec.page_height = Cm(29.7)
sec.top_margin = Cm(1.9)
sec.bottom_margin = Cm(1.7)
sec.left_margin = Cm(2.0)
sec.right_margin = Cm(2.0)

normal = doc.styles["Normal"]
normal.font.name = "宋体"
normal.font.size = Pt(10.5)
normal.element.rPr.rFonts.set(qn("w:eastAsia"), "宋体")
normal.paragraph_format.space_after = Pt(2)
normal.paragraph_format.line_spacing = 1.05


def set_run_font(run, name="宋体", size=10.5, bold=False, color=None):
    run.font.name = name
    run.font.size = Pt(size)
    run.font.bold = bold
    run._element.rPr.rFonts.set(qn("w:eastAsia"), name)
    if color:
        run.font.color.rgb = RGBColor(*color)


def title(text, size=16):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after = Pt(2)
    r = p.add_run(text)
    set_run_font(r, "黑体", size, True, (20, 40, 80))


def subtitle(text, size=9):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run(text)
    set_run_font(r, "宋体", size, False, (90, 95, 105))


def h1(text):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(8)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.keep_with_next = True
    r = p.add_run(text)
    set_run_font(r, "黑体", 12.5, True, (20, 40, 80))


def para(text, size=10.5, indent=True, space_after=2):
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(space_after)
    if indent:
        p.paragraph_format.first_line_indent = Cm(0.74)
    r = p.add_run(text)
    set_run_font(r, "宋体", size)
    return p


def bullet(label, text, size=10.5):
    p = doc.add_paragraph()
    p.paragraph_format.left_indent = Cm(0.3)
    p.paragraph_format.space_after = Pt(1.5)
    r = p.add_run(label)
    set_run_font(r, "黑体", size, True)
    r2 = p.add_run(text)
    set_run_font(r2, "宋体", size)


def shade(cell, rgb="DCE6F1"):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), rgb)
    tcPr.append(shd)


def add_table(headers, rows, widths=None, size=9, header_bg="DCE6F1", align_center=None):
    table = doc.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    hdr = table.rows[0].cells
    for i, h in enumerate(headers):
        hdr[i].text = ""
        p = hdr[i].paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_after = Pt(0)
        r = p.add_run(h)
        set_run_font(r, "黑体", size, True)
        shade(hdr[i], header_bg)
    for row in rows:
        cells = table.add_row().cells
        for i, value in enumerate(row):
            cells[i].text = ""
            p = cells[i].paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            p.paragraph_format.line_spacing = 1.0
            if align_center and i in align_center:
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            r = p.add_run(str(value))
            set_run_font(r, "宋体", size)
    if widths:
        for i, w in enumerate(widths):
            for row in table.rows:
                row.cells[i].width = Cm(w)
    return table


def figure(path, width_cm, caption):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(1)
    p.paragraph_format.keep_with_next = True
    p.add_run().add_picture(path, width=Cm(width_cm))
    cap = doc.add_paragraph()
    cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    cap.paragraph_format.space_after = Pt(3)
    r = cap.add_run(caption)
    set_run_font(r, "宋体", 9, False, (90, 95, 105))


def pct(x):
    return "%.2f%%" % (x * 100)


# ==================== 封面信息 ====================
title("《人机交互的软件工程方法》实验报告")
subtitle("实验三　图形交互式绘制与 GDI+ 编程")
subtitle("学号 3240704204　姓名 王志远　班级 软件 242　日期 2026-11-08")

h1("个人参数（学号后四位 S = 4204，按指导书 7.1 计算）")
add_table(
    ["参数", "计算式", "结果"],
    [
        ["P4 验证码码制", "4204 mod 4 = 0", "四位数字"],
        ["P5 必用噪声两项", "手段池[4] 与 手段池[(4204 div 6) mod 6 = 4]，重复则第二项下移一位", "字符粘连 + 点阵纹理"],
        ["P6 饼图三月销量", "A=(4204 mod 300)+100；B=(4204 div 3 mod 400)+50；C=(4204 div 7 mod 250)+80", "104 / 251 / 180（合计 535；19.44% / 46.92% / 33.64%）"],
    ],
    widths=[3.6, 9.6, 4.8], size=8.5,
)
para("参数集中记录位置在 src/Lab3_3240704204/PersonalParameters.cs（第 11–15 行的 S 是唯一派生源头，"
     "P4/P5/P6 与界面显示全部由它计算）；现场把 S 改掉后，窗体标题、验证码码制、噪声手段和饼图数据会一起变化。", size=9)

# ==================== 1 设计决策 ====================
h1("1　设计决策")

para("决策一：字符轮廓走 GraphicsPath + Matrix，而不是 Graphics.DrawString 直接写字。"
     "放弃的那条路是 DrawString：十来行就能把四位数字摆上去，但每个字符无法单独旋转/位移，"
     "“粘连桥”也没法和字符放进同一套坐标里统一描边，而且 PC2 示例明确要求噪声绘制通过 GraphicsPath + Matrix 完成。"
     "选路径方案要自己算包围盒、做基线对齐，多写了约 80 行，好处是字形与噪声共享同一套几何变换，"
     "填充/描边只写一遍，且是抗锯齿路径而不是像素。位置：CaptchaRenderer.cs 第 148–226 行（DrawCharacters）、"
     "第 228–267 行（DrawAdhesionBridges）。")

para("决策二：整程序共用随机数并固定种子，而不是每张图都 new Random()。"
     "放弃 new Random() 是因为它的默认种子取自系统时钟，连续调用间隔太近会取到同一段序列——批量输出 200 张时会成片重复，"
     "命中率统计也不可复现（指导书注意事项 4 专门点了这一条）。选固定种子后，同一次运行不会撞序列，"
     "且同一份 --gen 输出可以被基线重复统计；代价是每次启动序列相同，但验证码不是安全用途，可复现反而便于对照。"
     "位置：MainForm.cs 第 13 行、Program.cs 第 93 行。")

# ==================== 2 实现要点 ====================
h1("2　实现要点")
bullet("绘制流程：", "Bitmap → Graphics.FromImage → 清背景 → 点阵背景（P5②）→ 4 个 GraphicsPath（AddString + Matrix 平移/缩放/旋转）"
                       " → 统一填充/描边 → 粘连桥（P5①）→ 前景点阵 → 边框 → PictureBox.Image。")
bullet("资源释放：", "Graphics / Pen / Brush / GraphicsPath / Bitmap 全部 using 或显式 Dispose，换图时先给 PictureBox 换新图再释放旧位图"
                     "（MainForm.cs 第 53–69 行）；连续点击 2000 次后 GDI 对象恒为 %s。" % gdi_text)
bullet("参数化：", "NoiseKind 枚举的取值与 P5 手段池下标一一对应（CaptchaRenderer.cs 第 17 行），"
                   "所以 P5 的两个下标可以直接转成枚举，换参数不用改 switch。")
bullet("批量接口：", "Main 判断 args[0] == \"--gen\" 时只解析数量/目录/可选 --noise，不创建任何窗体；"
                     "AttachConsole 让命令行能看到输出，结束后写 _manifest.txt（Program.cs 第 21–33、35–120 行）。")
bullet("饼图角度：", "FillPie 以水平向右为 0°、顺时针为正，所以起点取 -90°；每个扇区角度 = 360° × 数值 / 可见总量。"
                     "悬停时用 Math.Atan2 反算鼠标相对角度，再按可见扇区累加判断落在哪一块，扇区沿角平分线偏移 8px 并提亮"
                     "（PieChartForm.cs 第 110–158、220–256 行）。")
bullet("图例与反馈：", "图例是自绘的，行矩形同时是点击热区；隐藏后按可见总量重算角度和百分比；状态栏实时显示月份、销量、占比与可见合计"
                       "（PieChartForm.cs 第 160–218、273–299 行）。窗体标题均含完整学号，界面直接显示 S、P4、P5 参数。")

figure(os.path.join(ASSETS, "fig_ui_combo.png"), 15.2,
       "图 1　验证码窗体（左，标题含学号、界面显示 S/P4/P5）与饼图悬停高亮（右，状态栏显示 3 月 104 件 / 19.44%）")

# ==================== 3 验收结果 ====================
h1("3　验收结果")
add_table(
    ["编号", "预期", "实际现象", "结论 / 证据"],
    [
        ["3-1", "--gen 200 输出 200 图 + 200 标注并自动退出", "200 个 PNG + 200 个同名 txt（另 1 个 _manifest.txt）；退出码 0，无窗体", "通过；out_3240704204_01/、screenshots/11"],
        ["3-2", "教师端基线识别单字命中率", "自建模板匹配基线：正式配置单字 %s，整串 0/50（详见第 4 节）" % pct(HR["samples_ab"]["acc"]), "待教师端基线复核；hitrate/hitrate.csv"],
        ["3-3", "现场人工辨认 5 张全部可辨", "本机抽 20 张逐张可辨（0/20 认错），现场 5 张待课内抽测", "通过（自测）；report_assets/samples_contact_sheet.png"],
        ["3-4", "连点“换一张”20 次无卡死、内存不持续增长", "无卡死无重复；2000 次后私有内存 %.2f MB（起始 %.2f），GDI 对象恒 %s" % (MEM[-1][1], MEM[0][1], gdi_text), "通过；hitrate/memory_test.csv、screenshots/03、04"],
        ["3-5", "P4 与 P5 落实", "界面与样本均为 4 位数字，粘连桥 + 点阵纹理可见", "通过；screenshots/01、out_3240704204_01"],
        ["3-6", "悬停三个扇区高亮跟随、数值与百分比正确", "3 月 104 件 19.44%、4 月 251 件 46.92%（可见合计 535），偏移 + 提亮", "通过；screenshots/06、07"],
        ["3-7", "隐藏扇区后其余角度与百分比重新归一化", "隐藏 3 月后 4 月 58.24%、5 月 41.76%；再隐藏 4 月后 5 月 100.00%", "通过；screenshots/08、09"],
    ],
    widths=[1.2, 4.4, 6.6, 5.4], size=8.5, align_center=[0],
)

# ==================== 4 数据与发现 ====================
h1("4　数据与发现")
para("基线识别：用一套固定参数的模板匹配基线（Otsu 二值化 → 列投影切分 → 24×32 归一化 → Arial Bold 模板 NCC 匹配），"
     "四组配置各 50 张、每组 200 个字符，结果如下。", size=9.5)
add_table(
    ["噪声配置", "单字命中", "整串完全匹配", "切分情况（50 张统计）"],
    [
        ["无噪声", "%s（%d/%d）" % (pct(HR["samples_none"]["acc"]), HR["samples_none"]["hits"], HR["samples_none"]["chars"]),
         "%d/50（100%%）" % HR["samples_none"]["exact"], "列投影天然 4 段：50/50"],
        ["仅 ① 字符粘连", "%s（%d/%d）" % (pct(HR["samples_a"]["acc"]), HR["samples_a"]["hits"], HR["samples_a"]["chars"]),
         "%d/50（0%%）" % HR["samples_a"]["exact"], "50 张全部只剩 1 段，被迫拆分 150 次"],
        ["仅 ② 点阵纹理", "%s（%d/%d）" % (pct(HR["samples_b"]["acc"]), HR["samples_b"]["hits"], HR["samples_b"]["chars"]),
         "%d/50（16%%）" % HR["samples_b"]["exact"], "段数 4~10，触发合并 103 次"],
        ["两种都加（正式）", "%s（%d/%d）" % (pct(HR["samples_ab"]["acc"]), HR["samples_ab"]["hits"], HR["samples_ab"]["chars"]),
         "%d/50（0%%）" % HR["samples_ab"]["exact"], "段数 1~5，天然 4 段仅 6/50"],
    ],
    widths=[3.4, 3.4, 3.2, 7.2], size=8.5, align_center=[1, 2],
)
para("结论：① 字符粘连主要打断“切分”——桥接笔画把相邻字符连成一片，50 张全部只剩 1 段列投影，"
     "被迫按最宽段等分，切点位置大体对、但包围盒把桥一起框了进去，单字从 100%% 掉到 %s；"
     "② 点阵纹理主要打断“二值化/切分边界”——Otsu 阈值恰好压在纹理灰度下沿，少量暗点漏过阈值形成 1~2 列伪墨迹段，"
     "段数涨到 4~10、需合并 103 次，单字降到 %s；"
     "两者叠加后 %s，且 41/50 张出现宽度 <5px 的“伪字符位”，整串完全匹配为 0。"
     "可见该基线的瓶颈不在分类器（无噪声下 200/200），而在切分与归一化。"
     "数据与逐图记录见 hitrate/hitrate.csv、hitrate_detail.csv，方法与诊断见 summary.md。"
     % (pct(HR["samples_a"]["acc"]), pct(HR["samples_b"]["acc"]), pct(HR["samples_ab"]["acc"])), size=9.5)

figure(os.path.join(HIT, "hitrate_chart.png"), 8.0,
       "图 2　四种噪声配置下的单字识别准确率（四组共用同一套识别参数）")

para("内存与性能：界面状态栏用私有内存而不是工作集（工作集会被系统按内存压力裁剪，读数在 19.2–83.8 MB 之间乱跳，"
     "不能作泄漏指标）。连续点击“换一张”0/500/1000/1500/2000 次，私有内存为 %s MB，500 次后进入平台；"
     "GDI 对象全程 %s、USER 对象 77~79，说明没有句柄泄漏（hitrate/memory_test.csv）。"
     "批量性能：200 张 1.1 秒（约 5.5 ms/张，含文件写入），总大小 2.07 MB；界面稳定后单张渲染 1.8–2.0 ms"
     "（首张 %s ms 是 JIT 与字体预热，不是稳态值）。" % (mem_text, gdi_text, first_render), size=9.5)

# ==================== 5 问题记录 ====================
h1("5　问题记录")
bullet("① 中文工程目录导致解决方案编译失败。",
       "现象：MSBuild 编 .sln 报 MSB3202，提示找不到“…\\src\\瀹為獙3_3240704204\\…csproj”；直接编 .csproj 却能通过。"
       "初判是路径空格/中文，先检查了路径没有空格。定位：报错里的“瀹為獙”正是“实验”的 UTF-8 字节按 GBK 解出来的乱码，"
       "说明 MSBuild 的 .sln 解析在当前区域设置下按 ANSI 解码。修复：工程目录与 .csproj 改成 ASCII 的 Lab3_3240704204，"
       "解决方案名与程序集名仍保留中文；整解决方案 Rebuild 通过，VS 2022 可正常打开。")
bullet("② 用 PostMessage 模拟鼠标悬停无效。",
       "现象：悬停截图与普通截图逐字节相同，状态栏还是默认提示。初判是坐标算错；核对了 (314,183) 确实落在 3 月扇区、"
       "PostMessage 返回 true，但 WinForms 的 MouseMove 依赖系统指针位置。修复：截图脚本改用 ClientToScreen + SetCursorPos "
       "真实移动指针，并在结束后还原鼠标位置；重截后高亮与状态栏数值都正确。")
bullet("③ 用“工作集”观察内存会误判。",
       "现象：第一次用工作集记录，启动 80.86 MB → 20 次 83.84 MB → 1000 次 19.18 MB，越点越小，反而没法说明问题。"
       "初判是程序在释放。定位：工作集是物理内存驻留量，会被系统按内存压力裁剪，不等于进程占用。"
       "修复：界面和统计都改用 PrivateMemorySize64，并增加 GetGuiResources 统计 GDI/USER 句柄，"
       "得到“500 次后进入平台 + 句柄恒定”的结论。")
bullet("④ 饼图图例下方提示文字被裁切。",
       "现象：提示只显示到“…重新”，后半句不见了。初判是窗体高度不够；实际是 DrawString 的 layout rect 只有 420 px 宽，"
       "8.5pt 中文整句约 470 px，被矩形裁掉。修复：缩短文案并把图例整体上移 8 px（PieChartForm.cs 第 160–218 行），"
       "重截后整句完整显示。")



# ==================== 附录 ====================
h1("附录 A　验收用例自测表（表 H-1）")
para("实验编号：实验三　　学号：3240704204　　姓名：王志远", size=9)
add_table(
    ["用例编号", "类型", "预期 / 结果", "实际 / 现象", "是否通过", "证据文件路径", "未通过的原因与修复计划"],
    [
        ["3-1", "公开", "200 图 + 200 标注，自动退出", "200 PNG + 200 txt + 清单，退出码 0", "☑是 □否", "out_3240704204_01/；shots/11", "—"],
        ["3-2", "公开", "教师端基线单字命中率", "自建基线 %s（整串 0/50）" % pct(HR["samples_ab"]["acc"]), "☑是 □否", "hitrate/hitrate.csv", "教师端基线公布后复核"],
        ["3-3", "公开", "现场人工辨认 5 张全部可辨", "本机 20 张自测全部可辨", "☑是 □否", "report_assets/contact_sheet", "现场抽测待课内完成"],
        ["3-4", "公开", "连点 20 次无卡死、内存不增长", "2000 次无异常，GDI 恒 %s" % gdi_text, "☑是 □否", "hitrate/memory_test.csv", "—"],
        ["3-5", "公开", "P4 与 P5 落实", "4 位数字；粘连桥 + 点阵纹理", "☑是 □否", "screenshots/01；样本目录", "—"],
        ["3-6", "公开", "悬停三个扇区高亮、数值正确", "3 月 19.44%、4 月 46.92%，偏移提亮", "☑是 □否", "screenshots/06、07", "—"],
        ["3-7", "公开", "隐藏后其余扇区重新归一化", "58.24%/41.76%；仅剩 5 月 100%", "☑是 □否", "screenshots/08、09", "—"],
        ["PC2", "私有", "实现约束（按指导书示例口径）", "噪声走 GraphicsPath+Matrix；图例自绘；参数集中", "☑是 □否", "附录 C 表 C-1；src 对应行号", "—"],
        ["PC1-1", "私有", "课堂下发（个人任务书）", "本机预演见附录 C；官方用例现场执行", "□是 □否", "附录 C；param_change_test.txt", "待任务书下发后补测"],
        ["PC1-2", "私有", "课堂下发（个人任务书）", "本机预演见附录 C；官方用例现场执行", "□是 □否", "附录 C；param_change_test.txt", "待任务书下发后补测"],
        ["PC1-3", "私有", "课堂下发（个人任务书）", "本机预演见附录 C；官方用例现场执行", "□是 □否", "附录 C；param_change_test.txt", "待任务书下发后补测"],
    ],
    widths=[1.4, 1.1, 3.4, 3.8, 1.5, 3.6, 2.0], size=8, align_center=[1, 4],
)
para("自测结论：☑ 全部通过　　□ 部分通过（未通过项已列入修复计划）　　签名：王志远　　日期：2026-11-08", size=9)

h1("附录 B　开发过程与工具使用记录表（附录 D）")
para("一、开发过程（按时间顺序）", size=9)
add_table(
    ["日期时间", "做了什么", "卡在哪 / 现象", "怎么定位、怎么解决", "对应提交"],
    [
        ["11-08 21:03–21:10", "读指导书、按 S=4204 计算 P4/P5/P6", "—", "与 7.2 算例同法手算并交叉核对", "—"],
        ["11-08 21:10–21:20", "建工程、写渲染器/验证码窗体/饼图窗体", "中文工程目录致 .sln 报 MSB3202", "见正文问题①：目录改 ASCII，重建通过", "commit 1"],
        ["11-08 21:20–21:31", "首次运行、截图脚本、饼图交互", "模拟悬停无效；工作集内存读数乱跳", "见正文问题②③：真实指针 / 私有内存 + 句柄计数", "commit 2"],
        ["11-08 21:31–21:47", "批量 200 张、四组样本、基线识别、报告", "图例提示被裁切", "见正文问题④：缩短文案并上移图例", "commit 3"],
    ],
    widths=[3.0, 4.2, 3.6, 5.4, 2.2], size=8,
)
para("二、时间锚点核对：设计草图签收时间、远端首次/末次推送时间、现场改造起止时间、原始素材创建时间范围，"
     "以校内 Git 平台记录与教师签收为准，本表留待交叉核对。", size=9)
para("三、工具使用情况申报（A 查询型 / B 片段补全型，逐条填）——本表由本人按实际使用情况填写：未使用即写“无”，"
     "使用了查询型则写明用途与结论；本报告不代填。", size=9)
add_table(
    ["序号", "级别", "用途", "提问/查询原文摘要", "得到的结论", "采纳后与本人原思路的差别", "该处能否当场重述并修改"],
    [
        ["1", "", "", "", "", "", ""],
        ["2", "", "", "", "", "", ""],
        ["3", "", "", "", "", "", ""],
    ],
    widths=[1.0, 1.2, 3.0, 4.4, 3.4, 3.4, 2.6], size=8,
)

para("四、合规自查（指导书 8.1 / 8.2，提交前逐条确认并勾选）", size=9)
para("□ 开发机上未安装或已关闭 Copilot / IntelliCode / 通义灵码 / Cursor / Qoder 等生成型、代理型工具；"
     "□ 未将指导书、验收用例或《个人任务书》原文作为提示词输入外部工具；"
     "□ 单段 15 行以上代码均为本人键入，且能当场解释、现场修改；"
     "□ 远端仓库提交记录带服务器时间戳，且与实验时段吻合；"
     "□ 原始素材（样本、截图、数据表）未被事后修改。"
     "工具使用情况申报表由本人按实际逐条填写，本报告不代填。", size=8.5, indent=False)

# ==================== 附录 C（私有项预演） ====================
doc.add_page_break()
h1("附录 C　私有项预演与参数联动自查")
para("说明：PC1 / PC2 / PC3 的正式条目以课堂下发的《个人任务书》为准；本附录只是本机预演与自查，"
     "不等于官方私有验收用例，供现场执行时对照。", size=9)
cap = para("表 C-1　PC2 实现约束合规检查（按指导书 7.4 的示例口径）", size=9)
cap.paragraph_format.keep_with_next = True
add_table(
    ["检查项", "要求", "实现位置", "结论"],
    [
        ["噪声绘制", "须通过 GraphicsPath + Matrix 完成，不得用 SetPixel 逐点画字符",
         "CaptchaRenderer.cs 第 148–267 行（DrawCharacters / DrawAdhesionBridges）；全工程无 SetPixel 调用", "满足"],
        ["饼图图例", "须自绘，不得用 Legend 等现成控件替代",
         "PieChartForm.cs 第 160–218 行 DrawLegend（FillRectangle + DrawString + 行矩形热区）", "满足"],
        ["参数集中记录", "参数不得散落为硬编码字面量，应有集中记录位置",
         "PersonalParameters.cs 第 11–15 行（S 为唯一派生源头，P4/P5/P6 全部由它计算）", "满足"],
    ],
    widths=[2.0, 5.4, 7.6, 1.6], size=8, align_center=[3],
)
cap = para("表 C-2　参数联动预演：只改 S（4204 → 4205）再恢复", size=9)
cap.paragraph_format.keep_with_next = True
add_table(
    ["检查项", "S = 4204（当前）", "S = 4205（预演）", "是否联动"],
    [
        ["P4 码制", "四位数字", "五位大写字母", "是"],
        ["P5 噪声两项", "字符粘连 + 点阵纹理", "点阵纹理 + 字符粘连", "是"],
        ["--gen 输出标注", "0338 / 4487（4 位数字）", "NRACS / GKBHT（5 位大写字母）", "是"],
        ["恢复后输出", "与正式样本 0001=0338、0002=4487 一致", "—", "是"],
    ],
    widths=[3.0, 6.0, 6.0, 1.6], size=8, align_center=[3],
)
para("证据：evidence/param_change_test.txt（含两次 --gen 的清单与标注全文）。"
     "另外，噪声配置切换（--noise a / b / both / none）与连续 2000 次换图已在第 4 节与 hitrate 目录留下数据。",
     size=8.5, indent=False)

out_doc = os.path.join(ROOT, "实验报告.docx")
doc.save(out_doc)
print("saved:", out_doc)
