# 实验三：图形交互式绘制与 GDI+ 编程

人机交互的软件工程方法 · 上机实验 3
学号 3240704204　姓名 王志远　班级 软件 242

## 1. 个人参数（由学号后四位 S = 4204 派生）

| 参数 | 计算式 | 结果 |
|---|---|---|
| P4 验证码码制 | 4204 mod 4 = 0 | 四位数字 |
| P5 必用噪声手段 | 手段池[4] 与 手段池[(4204 div 6) mod 6 = 4]，重复则下移一位 | 字符粘连 + 点阵纹理 |
| P6 饼图三月销量 | A=(4204 mod 300)+100；B=(4204 div 3 mod 400)+50；C=(4204 div 7 mod 250)+80 | 104 / 251 / 180（合计 535，19.44% / 46.92% / 33.64%） |

参数集中记录处：`src/Lab3_3240704204/PersonalParameters.cs`（第 11–15 行的 `S` 是唯一的派生源头，
P4/P5/P6 全部由它计算；现场改 S 后所有界面与输出随之变化）。

## 2. 目录结构（对应附录 C）

```
3240704204_王志远_实验3/
├─ src/                      Visual Studio 2022 工程（.sln、.csproj、源码）
├─ evidence/                 原始素材与实测数据
│   ├─ screenshots/          界面与现场截图
│   ├─ out_3240704204_01/    --gen 200 的 200 张 PNG + 200 个同名标注 txt + 清单
│   ├─ hitrate/              基线识别程序、四组样本、命中率数据与图表
│   ├─ report_assets/        报告用拼图
│   └─ scripts/              截图、抽样、制表用的脚本
├─ 实验报告.docx
├─ git-log.txt
└─ README.md
```

## 3. 编译与运行

用 Visual Studio 2022 打开 `src/实验3_3240704204.sln`，或命令行：

```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
  src\实验3_3240704204.sln /t:Rebuild /p:Configuration=Release
```

- 界面模式：直接运行 `bin\Release\实验3_3240704204.exe`，可"换一张"、切换四种噪声配置、打开饼形图；
- 批量模式（不弹窗、跑完自动退出）：

```
实验3_3240704204.exe --gen 200 out_3240704204_01
实验3_3240704204.exe --gen 50 out_a --noise a      # 仅字符粘连
实验3_3240704204.exe --gen 50 out_b --noise b      # 仅点阵纹理
实验3_3240704204.exe --gen 50 out_ab --noise both  # P5 正式配置（默认）
```

批量输出内容：`0001.png` + 同名 `0001.txt`（标注正文），另附 `_manifest.txt` 记录学号、参数、数量与生成时间。

## 4. 说明

- 工程目录名用 ASCII（`Lab3_3240704204`）而程序集/解决方案名保持中文：中文工程路径会让 MSBuild
  的 `.sln` 解析在当前区域设置下按 ANSI 解码，出现 MSB3202 找不到工程文件（详见实验报告"问题记录"）。
- 远端仓库：https://github.com/zephyr316247-commits/hci-lab3-3240704204 （GitHub，私有）。私有仓库请在 Settings → Collaborators 添加教师账号，
  或提交前改为 Public；git-log.txt 里附了 git ls-remote 与 GitHub pushed_at 服务端时间。
