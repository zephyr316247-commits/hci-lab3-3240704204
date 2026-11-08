# -*- coding: utf-8 -*-
"""生成报告用的验证码预览图：
1) samples_overview.png  —— 200 张样本缩略总览（10 列 × 20 行）
2) samples_contact_sheet.png —— 20 张 1:1 放大图 + 真值，用于人工可读性自查
"""
import os
from PIL import Image, ImageDraw, ImageFont

root = r"C:\Users\12807\Desktop\3240704204_王志远_实验3"
sample_dir = os.path.join(root, "evidence", "out_3240704204_01")
asset_dir = os.path.join(root, "evidence", "report_assets")
os.makedirs(asset_dir, exist_ok=True)

files = sorted(f for f in os.listdir(sample_dir) if f.endswith(".png"))
print("samples:", len(files))

# ---------- 200 张总览 ----------
cols, rows = 10, 20
tw, th = 240, 80
sw, sh = tw // 2, th // 2          # 缩略 120x40
pad = 4
canvas = Image.new("RGB", (cols * (sw + pad) + pad, rows * (sh + pad) + pad), (235, 238, 242))
for idx, name in enumerate(files):
    im = Image.open(os.path.join(sample_dir, name)).convert("RGB").resize((sw, sh), Image.LANCZOS)
    x = pad + (idx % cols) * (sw + pad)
    y = pad + (idx // cols) * (sh + pad)
    canvas.paste(im, (x, y))
canvas.save(os.path.join(asset_dir, "samples_overview.png"))
print("overview:", canvas.size)

# ---------- 20 张人工辨认用 ----------
font = ImageFont.truetype(r"C:\Windows\Fonts\msyh.ttc", 14)
cols2, rows2 = 4, 5
scale = 1.6
w2, h2 = int(tw * scale), int(th * scale)
label_h = 24
canvas2 = Image.new("RGB", (cols2 * (w2 + 10) + 10, rows2 * (h2 + label_h + 10) + 10), (245, 247, 250))
d = ImageDraw.Draw(canvas2)
for idx, name in enumerate(files[:20]):
    im = Image.open(os.path.join(sample_dir, name)).convert("RGB").resize((w2, h2), Image.LANCZOS)
    x = 10 + (idx % cols2) * (w2 + 10)
    y = 10 + (idx // cols2) * (h2 + label_h + 10)
    canvas2.paste(im, (x, y))
    with open(os.path.join(sample_dir, name.replace(".png", ".txt")), encoding="utf-8") as f:
        label = f.read().strip()
    d.text((x + 2, y + h2 + 2), "%s  %s" % (name, label), fill=(90, 95, 105), font=font)
canvas2.save(os.path.join(asset_dir, "samples_contact_sheet.png"))
print("contact sheet:", canvas2.size)
