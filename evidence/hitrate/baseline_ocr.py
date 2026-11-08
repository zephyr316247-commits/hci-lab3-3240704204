# -*- coding: utf-8 -*-
"""
实验三 验证码识别——模板匹配基线程序
====================================================
用一套固定参数（不针对任何噪声配置调参）对四个样本目录做识别，
统计"单字命中率"和"整串完全匹配率"。

处理流程（每一步都可解释、无随机性）：
  1) 灰度化 + Otsu 全局阈值二值化，得到"墨迹=1"的二值图；
  2) 按列求墨迹投影，用列间空白切成若干段，再用"拆分/合并"规则整理成 4 段；
  3) 每段裁剪到墨迹包围盒，缩放到 24x32 固定尺寸；
  4) 用 Arial Bold 40px 渲染 0~9 做模板（同样裁包围盒、缩放到 24x32），
     用零均值归一化互相关(NCC)打分，取最高分的数字作为识别结果；
  5) 与同名 .txt 真值比对评分（真值只用于评分，绝不参与预测）。

所有输出写在本脚本所在目录：脚本自动生成 hitrate.csv / hitrate_detail.csv /
hitrate_chart.png，方法说明与结论见同目录的 summary.md。
"""

import csv
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

try:      # Windows 控制台默认 GBK，这里强制 UTF-8 以免中文诊断信息乱码
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

# ---------------- 全局固定参数（四组配置共用，绝不按配置分别调参） ----------------
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
FONT_INK = r"C:\Windows\Fonts\arialbd.ttf"      # 与验证码生成端一致的字体
FONT_CN = r"C:\Windows\Fonts\msyh.ttc"          # 中文标签用字体（画图用）
CONFIGS = ["samples_none", "samples_a", "samples_b", "samples_ab"]
CFG_LABEL = {
    "samples_none": "无噪声",
    "samples_a": "仅粘连",
    "samples_b": "仅点阵",
    "samples_ab": "两者都有",
}
GLYPH_W, GLYPH_H = 24, 32      # 归一化后的字符尺寸
FONT_SIZE = 40                 # 模板渲染字号，与原图字符高度量级一致
N_CHARS = 4                    # 验证码固定 4 位


# ------------------------------ 1. 灰度化 + 二值化 ------------------------------
def load_gray(path):
    """读入 PNG 并转灰度（背景约 250，墨迹约 20~26）。"""
    return np.asarray(Image.open(path).convert("L"), dtype=np.uint8)


def otsu_threshold(gray):
    """Otsu 类间方差最大化求全局阈值（逐图自适应，但同一算法对四组配置一致）。"""
    hist = np.bincount(gray.ravel(), minlength=256).astype(np.float64)
    total = float(gray.size)
    sum_all = float((np.arange(256) * hist).sum())

    w_back = 0.0        # 背景（亮）像素累计权重
    sum_back = 0.0      # 背景像素累计灰度
    best_t, best_var = 0, -1.0
    for t in range(256):
        w_back += hist[t]
        if w_back == 0:
            continue
        w_fore = total - w_back
        if w_fore == 0:
            break
        sum_back += t * hist[t]
        m_back = sum_back / w_back
        m_fore = (sum_all - sum_back) / w_fore
        var = w_back * w_fore * (m_back - m_fore) ** 2
        if var > best_var:
            best_var, best_t = var, t
    return best_t


def binarize(gray):
    """返回 (二值图, 阈值)：墨迹=1，背景=0。"""
    t = otsu_threshold(gray)
    ink = (gray < t).astype(np.uint8)
    return ink, t


# ------------------------------ 2. 垂直投影切分 ------------------------------
def _runs_of_ink(ink):
    """把"每列是否有墨迹"变成连续区间列表 [(x0, x1), ...]（含两端）。"""
    col = ink.sum(axis=0)
    segs, start = [], None
    for x in range(ink.shape[1]):
        if col[x] > 0 and start is None:
            start = x
        elif col[x] == 0 and start is not None:
            segs.append((start, x - 1))
            start = None
    if start is not None:
        segs.append((start, ink.shape[1] - 1))
    return segs


def split_widest(segs, ink):
    """段数不足 4：把最宽的一段对半再分。

    粘连时字符之间的空白列被桥接笔画填掉，投影谷底是一片近似等高的
    平台（实测间隙列仍留有 6~7 个墨迹像素），谷底位置不可靠，因此这里
    不再用投影选点，而是退化为"字距均匀"的先验：最宽段中点切一刀。
    """
    widths = [b - a for a, b in segs]
    i = int(np.argmax(widths))          # 最宽的一段（并列取最左）
    x0, x1 = segs[i]
    if x1 - x0 < 3:                     # 宽度不足 3 列，无法再分
        return None
    cut = (x0 + x1 + 1) // 2            # 中点
    return segs[:i] + [(x0, cut - 1), (cut, x1)] + segs[i + 1:]


def merge_narrowest(segs):
    """段数超过 4：把最窄的一段并入相邻更窄的一段（合并后区间取并集）。"""
    widths = [b - a for a, b in segs]
    i = int(np.argmin(widths))
    if i == len(segs) - 1:
        j = i - 1                              # 只有左邻
    elif i == 0:
        j = i + 1                              # 只有右邻
    else:
        j = i - 1 if widths[i - 1] <= widths[i + 1] else i + 1
    lo, hi = min(i, j), max(i, j)
    merged = (segs[lo][0], segs[hi][1])
    return segs[:lo] + [merged] + segs[hi + 1:]


def force_four_segments(segs, ink, stats):
    """把任意段数整理成恰好 4 段，同时记录拆分/合并次数（用于诊断）。"""
    if len(segs) == N_CHARS:
        stats["exact"] += 1
    while len(segs) > N_CHARS:
        new = merge_narrowest(segs)
        if new == segs:
            break
        segs = new
        stats["merges"] += 1
    while len(segs) < N_CHARS:
        new = split_widest(segs, ink)
        if new is None:
            break
        segs = new
        stats["splits"] += 1
    return segs


# --------------------- 3. 裁剪包围盒 + 缩放到 24x32（统一归一化） ---------------------
def normalize_patch(ink, x0, x1):
    """在一个列区间内裁剪墨迹包围盒，缩放到 24x32，返回 0~1 浮点图。"""
    sub = ink[:, x0:x1 + 1]
    rows = np.where(sub.any(axis=1))[0]
    cols = np.where(sub.any(axis=0))[0]
    if rows.size == 0 or cols.size == 0:       # 该段没有墨迹：返回全零
        return np.zeros((GLYPH_H, GLYPH_W), dtype=np.float32)
    crop = sub[rows.min():rows.max() + 1, cols.min():cols.max() + 1]
    img = Image.fromarray((crop * 255).astype(np.uint8), mode="L")
    img = img.resize((GLYPH_W, GLYPH_H), Image.BILINEAR)   # 拉伸到固定尺寸
    return np.asarray(img, dtype=np.float32) / 255.0


# ------------------------------ 4. 模板与打分 ------------------------------
def build_templates():
    """用 PIL 渲染 0~9（Arial Bold 40px），裁包围盒后缩放到 24x32。"""
    font = ImageFont.truetype(FONT_INK, FONT_SIZE)
    templates = {}
    for d in range(10):
        canvas = Image.new("L", (120, 120), 255)      # 白底
        ImageDraw.Draw(canvas).text((20, 20), str(d), font=font, fill=0)
        arr = np.asarray(canvas, dtype=np.uint8)
        mask = (arr < 128).astype(np.uint8)           # 与识别端同一二值口径
        rows = np.where(mask.any(axis=1))[0]
        cols = np.where(mask.any(axis=0))[0]
        crop = mask[rows.min():rows.max() + 1, cols.min():cols.max() + 1]
        img = Image.fromarray((crop * 255).astype(np.uint8), mode="L")
        img = img.resize((GLYPH_W, GLYPH_H), Image.BILINEAR)
        templates[d] = np.asarray(img, dtype=np.float32) / 255.0
    return templates


def ncc(a, b):
    """零均值归一化互相关：对亮度/墨迹量不敏感，只比较形状纹理。"""
    a = a.ravel() - a.mean()
    b = b.ravel() - b.mean()
    na, nb = np.linalg.norm(a), np.linalg.norm(b)
    if na < 1e-8 or nb < 1e-8:
        return 0.0
    return float(np.dot(a, b) / (na * nb))


def recognize(ink, templates, stats):
    """单张图：切分 -> 归一化 -> 与 10 个模板逐一打分 -> 取最高分。"""
    segs = _runs_of_ink(ink)
    stats["raw_segments"].append(len(segs))
    segs = force_four_segments(segs, ink, stats)

    # 段数仍不足 4（极端情况）时补空段，保证输出 4 位
    while len(segs) < N_CHARS:
        segs.append(segs[-1] if segs else (0, 0))

    digits, scores = [], []
    for x0, x1 in segs[:N_CHARS]:
        patch = normalize_patch(ink, x0, x1)
        per = {d: ncc(patch, templates[d]) for d in range(10)}
        best = max(per, key=per.get)
        digits.append(str(best))
        scores.append(per[best])
    return "".join(digits), scores


# ------------------------------ 5. 评分与输出 ------------------------------
def read_label(path):
    with open(path, "r", encoding="utf-8-sig") as f:
        return f.read().strip()


def evaluate():
    templates = build_templates()
    rows, detail = [], []

    for cfg in CONFIGS:
        folder = os.path.join(BASE_DIR, cfg)
        files = sorted(f for f in os.listdir(folder) if f.lower().endswith(".png"))
        stats = {"exact": 0, "splits": 0, "merges": 0, "raw_segments": []}
        char_hits = chars = exact = 0

        for name in files:
            img_path = os.path.join(folder, name)
            label = read_label(os.path.join(folder, os.path.splitext(name)[0] + ".txt"))
            gray = load_gray(img_path)
            ink, _ = binarize(gray)
            pred, _ = recognize(ink, templates, stats)

            # 逐字比对（长度不齐时按位置比较，缺位算错）
            correct = sum(1 for i in range(N_CHARS)
                          if i < len(pred) and i < len(label) and pred[i] == label[i])
            char_hits += correct
            chars += N_CHARS
            exact += int(pred == label)
            detail.append([cfg, name, label, pred, correct])

        rows.append([cfg, len(files), chars, char_hits,
                     f"{char_hits / chars:.4f}" if chars else "0.0000",
                     exact, f"{exact / len(files):.4f}" if files else "0.0000"])
        # 控制台诊断：切分干预次数，用于解释噪声打断了哪一步
        raw = stats["raw_segments"]
        print(f"[{cfg}] 图像数={len(files)} 单字命中={char_hits}/{chars} "
              f"整串命中={exact}/{len(files)} | 原始列投影段数 {min(raw)}~{max(raw)} "
              f"天然4段={stats['exact']} 追加拆分={stats['splits']} 合并={stats['merges']}")

    return rows, detail


def write_hitrate(rows):
    path = os.path.join(BASE_DIR, "hitrate.csv")
    with open(path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["config", "images", "chars", "char_hits",
                    "char_accuracy", "exact_matches", "exact_accuracy"])
        for r in rows:
            w.writerow(r[:7])
    print("已写出", path)


def write_detail(detail):
    path = os.path.join(BASE_DIR, "hitrate_detail.csv")
    with open(path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["config", "image", "label", "prediction", "char_correct"])
        w.writerows(detail)
    print("已写出", path)


def draw_chart(rows):
    """用 Pillow 画单字准确率柱状图（不用 matplotlib）。"""
    W, H = 760, 500
    left, right, top, bottom = 96, 40, 100, 85
    plot_w, plot_h = W - left - right, H - top - bottom
    img = Image.new("RGB", (W, H), "white")
    d = ImageDraw.Draw(img)
    try:
        f_title = ImageFont.truetype(FONT_CN, 24)
        f_tick = ImageFont.truetype(FONT_CN, 16)
        f_bar = ImageFont.truetype(FONT_CN, 16)
        f_val = ImageFont.truetype(FONT_CN, 18)
    except OSError:
        f_title = f_tick = f_bar = f_val = ImageFont.load_default()

    title = "四种噪声配置下的单字识别准确率"
    tw = d.textbbox((0, 0), title, font=f_title)[2]
    d.text(((W - tw) / 2, 24), title, font=f_title, fill=(30, 30, 30))

    # 坐标轴与网格（0~100%）
    d.line([(left, top), (left, top + plot_h), (left + plot_w, top + plot_h)],
           fill=(60, 60, 60), width=2)
    for v in range(0, 101, 20):
        y = top + plot_h - plot_h * v / 100.0
        d.line([(left, y), (left + plot_w, y)], fill=(225, 225, 225), width=1)
        d.text((left - 12 - d.textbbox((0, 0), f"{v}", font=f_tick)[2], y - 8),
               f"{v}", font=f_tick, fill=(80, 80, 80))

    # 纵轴标题：先画到小图再旋转 90 度，贴在坐标轴左侧
    ylab = "单字准确率 (%)"
    lw = d.textbbox((0, 0), ylab, font=f_tick)[2] + 8
    timg = Image.new("RGBA", (lw, 26), (255, 255, 255, 0))
    ImageDraw.Draw(timg).text((4, 4), ylab, font=f_tick, fill=(60, 60, 60))
    timg = timg.rotate(90, expand=True)
    img.paste(timg, (8, int(top + plot_h / 2 - timg.height / 2)), timg)

    n = len(rows)
    slot = plot_w / n
    bar_w = slot * 0.52
    colors = [(96, 128, 190), (222, 128, 96), (110, 175, 130), (186, 120, 180)]
    for i, r in enumerate(rows):
        cfg, acc = r[0], float(r[4])
        cx = left + slot * (i + 0.5)
        h = plot_h * acc
        x0, x1 = cx - bar_w / 2, cx + bar_w / 2
        y0 = top + plot_h - h
        d.rectangle([x0, y0, x1, top + plot_h], fill=colors[i % len(colors)])
        # 柱顶数值
        txt = f"{acc * 100:.1f}%"
        tx = cx - d.textbbox((0, 0), txt, font=f_val)[2] / 2
        d.text((tx, y0 - 26), txt, font=f_val, fill=(30, 30, 30))
        # 柱下配置名
        lab = CFG_LABEL[cfg]
        lx = cx - d.textbbox((0, 0), lab, font=f_bar)[2] / 2
        d.text((lx, top + plot_h + 12), lab, font=f_bar, fill=(40, 40, 40))
        d.text((lx, top + plot_h + 34), cfg.replace("samples_", ""),
               font=f_tick, fill=(130, 130, 130))

    path = os.path.join(BASE_DIR, "hitrate_chart.png")
    img.save(path)
    print("已写出", path)


def main():
    rows, detail = evaluate()
    write_hitrate(rows)
    write_detail(detail)
    draw_chart(rows)
    print("\n配置              单字准确率   整串完全匹配")
    for r in rows:
        print(f"{r[0]:<16} {float(r[4]) * 100:6.2f}%      {r[5]}/{r[1]} ({float(r[6]) * 100:.1f}%)")


if __name__ == "__main__":
    main()
