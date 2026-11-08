from docx import Document
p = r"C:\Users\12807\Desktop\3240704204_王志远_实验3\实验报告.docx"
d = Document(p)
paras = [x.text for x in d.paragraphs if x.text.strip()]
print("paragraphs:", len(paras), "tables:", len(d.tables))
for t in d.tables:
    print("table rows:", len(t.rows), "first:", t.rows[0].cells[0].text)
print("--- headings ---")
for x in paras:
    if x.startswith(("1 ", "2 ", "3 ", "4 ", "5 ", "附录", "个人参数")):
        print(x[:40])
print("--- 关键内容核对 ---")
full = "\n".join(paras)
for key in ["20.50%", "35.00%", "57.00%", "62.44", "69.10", "3240704204", "图 3"]:
    print(key, "->", key in full)
