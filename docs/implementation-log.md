# 实施日志

更新日期：2026-11-19
本文件从建立项目协作规则这一轮开始记录；此前的安装、健康检查、GUID 修复等历史见 [第一阶段记录](phase1-implementation.md) 和 [问题记录](issues-and-solutions.md)，不补造未执行的验收。

## 2026-11-19：建立项目级 AGENTS 与文档同步规则

- 需求：为本项目后续对话建立规则，讨论确认内容及实际实施必须记录并同步文档。
- 实施基线：`97cbd71`；本轮交付提交可通过本文件的 Git 历史定位。
- 变更：新增根目录 `AGENTS.md`，规定开始前读取、文档分工、状态/证据、敏感信息与 Git 边界；新增决定记录与本日志；README 和下一轮任务书增加规则入口。
- 相关文件：`AGENTS.md`、`docs/decisions.md`、`docs/implementation-log.md`、`README.md`、`docs/tasks/next-implementation.md`。
- 验证结果：本轮文档差异检查通过；9 份 Markdown 文档的本地相对链接均可解析；规则与现有 A 开发、B 独立验收方案一致。未运行无关业务测试。
- 状态：文档规则已实施；不包含代码、数据库或 Docker 环境变更。
- 遗留：新对话需在本仓库启动并读取规则；仓库外对话需显式读取。不把 A 的历史验证结果记成 B 已完成。
- 保护边界：用户已有 `client/ProjectSettings/ProjectSettings.asset` 修改保留，本轮不提交。

## 2026-11-19：第一阶段环境收尾与独立验收交接

- 需求：按 `docs/tasks/next-implementation.md` 完成 Docker 默认环境、共享本机配置、启动检查与独立验收交接；不开始登录、题库或考试业务。
- 实施：Compose 固定 `mysql:8.4.9`，只绑定 `127.0.0.1`，增加容器健康检查；新增 `.env.example` 并忽略 `.env`、`.local/`；API 503 响应移除异常详情并保留服务端告警日志；重写 `tools/check-environment.ps1` 为只读环境检查/显式安装，新增 `tools/start-local.ps1` 负责加载配置、等待认证 `SELECT 1`、处理端口冲突和启动/停止 API。
- 文档：同步 README、`docs/multi-computer-deployment.md`、`docs/phase1-implementation.md`、`docs/decisions.md`、`docs/issues-and-solutions.md`、`docs/tasks/next-implementation.md` 和 A 机验收记录。
- 数据边界：未操作父目录中用户未跟踪资料；未修改或提交 `client/ProjectSettings/ProjectSettings.asset` 的用户改动；未删除 Docker 卷、原生 MySQL 数据或强制关闭已有进程。
- 验证：静态 PowerShell 语法检查、`dotnet build --no-restore`、API 503 脱敏响应检查和文档链接检查；Docker/Unity 实际运行受本机缺少 Docker CLI/Unity 可探测安装阻塞，详见验收记录。
- 状态：代码与文档已实施，提交/推送后交 B 独立验收；B 记录不得由 A 结果代填。

## 2026-11-19：补充本机 MySQL 端口冲突提示

- 实施：`tools/start-local.ps1` 在启动 Compose 前只读检查宿主机端口；发现不是项目 MySQL 容器占用时立即提示修改 `MYSQL_HOST_PORT`，不停止未知进程。
- 验证：PowerShell 解析通过，后端重新编译 0 警告、0 错误；Docker 引擎不可用，真实端口占用分支待现场执行。
- 提交：代码提交 `31227c7`；本记录随后同步。

## 2026-11-20：A 机 Docker 环境现场验证

- 环境：执行 `wsl --update`、`wsl --shutdown` 成功；重启 Docker Desktop 后 `docker info` 返回 `29.9.3`。
- 实际验证：生成未跟踪的本机 `.env`，使用 `MYSQL_HOST_PORT=3307`；`mysql:8.4.9` 容器健康，认证 `SELECT 1` 成功；临时持久化记录在 `docker compose restart mysql` 后仍存在并已删除；API `/health` 返回 HTTP 200 和 `database: ok`。
- 代码修复：发现 Docker Compose 的 stderr 进度和 PowerShell 原生退出码处理会让启动脚本误报失败，已在 `30bc13b` 修复并重新通过脚本解析。
- 未执行：Unity SampleScene 实际请求；当前 `C:\UnityProject\C_Client` 已有 Unity 编辑器实例，未强制关闭或批处理启动。
- 状态：A Docker/API/持久化已验证；Unity 和 B 机验收待完成。

## 2026-11-20：A 机 Unity 健康请求验收

- 用户反馈：在 Unity `SampleScene` 运行后，Console 输出 `API: {"status":"ok","database":"ok"}`，调用位置为 `client/Assets/Scripts/Infrastructure/ApiHealthProbe.cs:20`。
- 结论：A 机第一阶段的 API、Docker MySQL、持久化和 Unity 健康请求均有证据；B 机仍必须使用自己的环境独立复验。

## 2026-11-21 B 机首次部署（受阻）

完成仓库克隆及 README、AGENTS、部署方案、当前任务书和脚本阅读。生成本机随机 .env，git check-ignore 确认忽略。通过 winget 发起 Docker Desktop 4.99.0、.NET SDK 10.0.404 安装，下载哈希校验成功；尚未取得安装成功结果，不记为已安装。WSL 3.0.5 安装失败（需要管理员权限）。详细验收见 validation/2026-11-21-B-dce3fcb.md。没有提交或推送。

## 2026-11-21 依赖安装完成
用户明确授权安装后，Docker Desktop 4.99.0、WSL 3.0.5、Unity 2022.3.62f3c1、SDK 10.0.404 已确认安装。API 编译通过；Windows 功能要求手动重启。详细证据和剩余验收见 validation/2026-11-21-B-dce3fcb.md。

2026-11-21 重启后：完成 Docker 引擎、MySQL 8.4.9 初始化、应用账号认证、API 200 和重复启动验收；本机数据库端口 13306。Unity 导入完成但场景请求待验。详细证据见 B 验收记录。

2026-11-21 完成 A/B 环境对照、B 的 503/恢复、持久化及 Unity 实际请求补验，测试表已清理，源码未改；详见 [审查记录](validation/2026-11-21-AB-review.md)。

2026-11-21 文档交付：按用户授权整理本轮八份文档，更新 README 当前状态，提交并推送 origin/main；不包含 .env、本地日志、数据库数据或业务代码。

## 2026-11-22 毕业设计任务书资料核对

- 本机 B / LAPTOP-H681KV2I；仓库 D:\UnityProject\c_program_system；main，基线 e4eb4019ae0bf5755b8ce29ae82df7ba53f4d131。开始时 docs/implementation-log.md 已有未提交修改，本轮仅追加，不覆盖原内容。
- 已读取 README、决定记录、当前交接任务书、用户提供的任务书.docx 和任务书样本.pdf；初步检索期刊官方页面、DOI 与 Unity 2022.3 官方文档。
- 已向用户核实文献时间/语言要求及题目和身份栏信息；最终 Word 尚未生成，文献清单尚未完成最终筛选。
- 未修改代码、未运行业务验收、未提交或推送。下一步完成文献核对、模板填写和排版验证。

## 2026-11-22 任务书Word编制完成

- 按用户确认题目与语言/年份要求，基于原Word编制 output/documents/C语言程序设计考试系统的设计与实现_任务书.docx；保持四个主栏目，不添加个人信息。正文涵盖Unity取舍、项目状态、A/B独立验收、阶段计划与拟实施业务。
- 核对9篇中文、1篇英文（2022—2024年），每条提供核验链接及访问说明；张可昀姓名按出版PDF核正，英文使用2024年正式版题录并附作者预印本入口。未将摘要访问写为全文已读。
- DOCX重新读取、XML解析、10条题录与外部链接结构检查通过；缺少排版引擎，视觉分页未执行。详见 docs/validation/2026-11-22-B-taskbook.md。
- 未改业务代码或运行环境，未提交推送；保留原有文档修改。tmp/taskbook为编制与核对中间文件，不属于交付物。

## 2026-11-22 任务书语言与题录修订

按用户审阅意见，覆盖修订同题Word：使用待实施研究任务表述，删除既有基础、实施进度和A/B协作细节；精炼为架构、Unity选择、权限、题库考试、程序评测及成绩测试设计。文献保留9篇中文及1篇英文题录，删除DOI、链接、核验说明，链接移至聊天交付。重新读取和XML检查通过；全文无实施现状措辞或外部链接。未改代码，未提交推送。视觉渲染仍因缺少引擎未执行，不能宣称排版验收通过。

## 2026-11-22 英文参考文献内容核对

用户询问第10篇文献内容。核对作者公开接收稿 https://arxiv.org/html/2306.11722v2 ：该文为自动评分与反馈系统综述，分析2017—2021年的121篇论文，按评价技能、技术、语言范式、自动化和验证方式分类；指出正确性评价集中、代码质量评价不足、反馈及复现数据有限。对本项目的启示为测试用例评分、区分功能与质量、与人工结果对照，属于设计参考，不是Unity选型依据，也不是C语言系统实现说明。未修改Word、代码或阶段实施范围。

## 2026-11-22 参考文献页码格式调整
按用户要求，对任务书Word进行局部OOXML修改，删除10条题录的页码及英文文章编号，保留其余正文和格式。重新读取核对10条修改通过；未改代码、未提交推送；视觉渲染仍未执行。

交付路径补充：原Word写入报PermissionError（是否被应用占用待查），实际修改另存为 output/documents/C语言程序设计考试系统的设计与实现_任务书_修订版.docx；上述检查通过指此修订版，原文件保持不变。

## 2026-11-22 英文文献翻译篇幅及项目用途审查

- 依据：https://arxiv.org/pdf/2306.11722（v2作者接收稿）及HTML全文。公开PDF共43页；正文至第31页，参考文献由第31页开始，附录位于第42—43页。正式版题录仍按2024年ACM版本引用，接收稿中的占位DOI不得照录。
- 本机bundled Python/pypdf提取并按英文词正则粗统计：全文约22665词；参考文献前约15988词（包含少量页眉、图表文字等）；第2节约493词，第3节约1161词。不是精确编辑字数。
- 中文翻译工作量估计：摘要与正文约2.5万—3.5万汉字；含图表说明及附录可按约3万—4万汉字规划，参考文献通常保留原题录，不作逐项汉译。此为经验估算，非完成译稿统计。
- 建议（待用户/指导教师确认）：若允许节选，第2、3节合计约1650英文词，可译约2500—3600汉字，适合作为不少于2000汉字的外文翻译候选；全文翻译则工作量显著超出最低字数。是否允许节选必须按学校规则确认。
- 项目用途判断：支持测试用例评分、反馈层次、人工预期对照验证和代码质量评价边界；不提供Unity选型、完整系统架构或沙箱实现方案。练习即时反馈不可直接照搬考试答案公开策略。未修改Word或业务代码，未新增已确认设计决定，未提交推送。

## 2026-11-22 文献复核与翻译准备

逐条复核原10篇出版信息，公开全文取得3篇（3/7/10），7篇只获题录摘要。记录相关性、技术表述局限、具体可确认节选位置及不能确认事项，见 docs/tasks/literature-review-and-translation.md。第7篇全文通过本机pypdf补读；未生成大段版权原文或译文，已请求用户上传英文PDF。未修改任务书或代码、未安装工具、未提交推送。

## 2026-11-22 未取得全文文献的下载入口查询

用户要求7篇未取得全文文献的下载链接。再次检索未找到可验证的公开直达PDF地址；可提供原期刊知网出版平台文章页，页面有“下载本文/PDF”入口，但实际下载是否需要登录、机构权限或付费未验证。第9篇另找到国家科技期刊平台文章页 https://opaj.napstic.cn/periodicalArticle/0120240800827006 ，存在下载入口。不能将题录页标为免费PDF直链；全文审查状态不变。本轮未修改交付文档或代码。

## 2026-11-22 英文节选翻译Word

- 用户提供PDF：C:/Users/ASUS/Desktop/2306.11722v2.pdf；独立提取并核对第2—3节（3—5页），30个标题/段落项中英对应，保留引文编号和5条脚注。中文正文2609个汉字，排除标题、来源说明、脚注、译注。
- 交付：output/documents/编程自动评分与反馈工具_英文节选与中文翻译.docx。设置中英文标题与三级标题、正文中英文字体、小四正文、2字符首行缩进、1.5倍行距、标题段前后0.5行；第3.2.3节原文四种模式但仅述两类的矛盾以译注保留，不自行补造。
- 通过重新读取、逐项字号/行距/缩进/标题间距检查及OOXML解析；render_docx.py失败原因为缺少soffice.exe，视觉分页未验证。未安装软件、未改业务代码或原任务书、未提交推送。源文件SHA和统计见tmp/taskbook/translation/qa.json；验收见docs/validation/2026-11-22-B-translation.md。

## 2026-11-22 两份交付文档再次审校

- 依据用户要求，对照原Word栏目、PDF补充进度、用户排版规则和提供的论文第3—5页，审查最终任务书修订版及节选翻译。
- 修正英文提取错误objectoriented为object-oriented，其他断行合并核对正常；修正译文“符合行业标准”为“业界通用”，避免引入认证含义。中文正文现为2607汉字，不计标题、脚注、说明、译注。
- 任务书将Unity方案的成本绝对比较改为具体部署与适配开销，将公平性保证式表达改为降低违规与评分不一致风险；不增加研究范围、不变更文献清单。
- 另存两份审校版：output/documents/C语言程序设计考试系统的设计与实现_任务书_审校版.docx；output/documents/编程自动评分与反馈工具_英文节选与中文翻译_审校版.docx。保留原文件。
- 30个英文标题/段落项核对提取文本、30个中文对应项及引文编号一致；任务书10条题录、无链接无实施现状描述，文件XML解析均通过。无章节或段落级遗漏。视觉分页仍未验证，不宣称排版通过；7篇中文全文审查仍未完成，第4篇相关性较弱，替换仅为建议。
- 未安装软件、未改代码、未提交推送。详细检查摘要见tmp/taskbook/translation/review-qa.json及既有验收记录同日追加。

## 2026-11-22 毕设文档归档交付

用户授权提交及推送两份审校Word。在根目录创建docs/graduation/，复制最终任务书和英文节选译文，内容与审校输出SHA256一致；更新README入口、交接与验收归档记录。待提交推送本次交付及相关记录；起始main / e4eb401，远端main同一基线。初始implementation-log中的“B机待办检查”是既有无关修改，暂存版本排除该段、工作区保留。其余output旧稿、tmp中间文件和原论文不纳入提交。既有文献全文及视觉检查限制保持不变。

归档推送结果：3674e0f 已于2026-11-22成功推送origin/main，两份Word与相关记录已在远端。只读哈希核对通过；未做新的排版渲染或业务测试。此后仅补记本条交付结果，原有B机待办检查段及output/tmp仍留本地。

## 2026-11-23 远端文档拉取与任务书审查

从 `origin/main` 快进拉取至 `1c69586`，审查 `docs/graduation/` 中两份新增审校版 Word。OOXML 和 python-docx 检查确认任务书栏目、10篇参考文献、翻译节选范围、层级、字体、字号、缩进和行距符合已确认要求；翻译正文为2607个汉字。新增验收记录见 `docs/validation/2026-11-23-remote-document-review.md`。由于环境缺少 `soffice.exe`，仍未完成逐页视觉验收；未改业务代码。

## 2026-11-23 B 问题修复与文档视觉复验

修复 B 端复现的三个环境工具问题：`start-local.ps1` 对无容器命令输出改为 null-safe 读取；`check-environment.ps1` 枚举全部 Unity 版本化目录并按产品版本筛选；SDK 检查改为按项目目标主版本判断，而不是只判断是否存在 `dotnet` 命令。安装 LibreOffice 26.8.3.1 后重新渲染两份审校 Word，任务书3页、翻译11页逐页检查通过；移除翻译文档 Title 样式残留的蓝色下边框。`check-environment.ps1` 本机已确认正确发现 Unity 2022.3.62f3c1 和 .NET 10，Docker 引擎当前未运行而按预期失败。未执行 Docker/B 机现场复验。

本轮范围澄清：毕业设计任务书描述完整系统；第一阶段记录只描述环境建设。原“第二阶段任务书”表述已改为系统开发下一步实施计划，避免把开发计划误认为毕业设计任务书本身。

## 2026-11-23 统一开发工作区

核对两份本机 checkout 后，确定当前远端最新 `main` 的 `C_Programming_System` 为唯一开发工作区；`C:\UnityProject\C_Client` 在删除前落后11个提交且有未提交 ProjectSettings 修改，已按用户要求删除，不再并行开发。规则见 `docs/development-workspace.md`。

当前工作区已从 `explore/_repo_inspect2` 复制并改名为 `explore/C_Programming_System`；旧目录内容已清理，剩余空目录因当前工具锁定暂无法立即移除。Docker Compose 已固定项目名 `c_program_system`，旧 `repo_inspect2-mysql-1` 容器已停止并删除（数据卷保留），新 `c_program_system-mysql-1` 已从唯一工作区启动，API 登录和题目查询复验通过。

## 2026-11-23 系统开发首个客户端闭环

A 端开始执行系统开发：新增服务端账号注册/登录、PBKDF2 密码哈希、内存会话、已发布单选题查询、答案提交和 Student→Teacher 权限边界；首次业务访问自动建立三张 MySQL 表并写入三道演示题。Unity `SampleScene` 新增原生 IMGUI 登录/注册、题目列表、选项选择、提交结果和网络错误提示。API 编译通过；A 端启动 Docker/MySQL/API 后已实际验证注册、登录、3道题查询、答案提交正确和 Student→Teacher 返回403；Unity 编辑器完成脚本编译，PlayMode UI 操作仍待人工复验。设计与后续迁移计划见 `docs/design/system-development-slice.md` 和 `docs/tasks/phase2-minimal-business-loop.md`。

## 2026-11-23 题库组卷与客观题考试业务层

在首个客户端闭环之上新增教师题库查询、建题、修改、组卷，学生考试列表、唯一考试会话、逐题答案保存和一次性交卷评分；新增考试、考试题目、考试会话和考试答案表，并保留同一结构迁移文件。A 端实际验证：教师新增第4题并创建4题考试，学生开考获得4题、保存答案并交卷，服务端返回总分1；API 编译通过。Unity UI 暂不扩展，待业务接口稳定后统一迁移正式界面。

## 2026-11-23 题库隔离与导入

审查发现公开题目接口会暴露所有已发布题目，已增加题目 `owner_id/visibility`、考试学生分配表和教师私有题库规则。公开练习接口只返回 `PublicPractice`；学生只能看到被分配的考试。新增 `QuestionImportService`，支持教师上传 Excel 兼容 CSV，先解析校验再事务导入；模板见 `docs/templates/question-bank-import.csv`。A 端验证教师私有题不出现在学生公开题目列表，CSV 导入2题成功，分配考试可见并能开考。原生 `.xlsx` 解析暂未引入依赖。

## 2026-11-23 系统级仓库结构整理

将 Unity 工程移动到 `client/`，将服务端目录统一为同级 `server/`，将毕设文档移动到 `docs/graduation/`；更新启动脚本、环境检查、README 和文档链接。删除旧 `C_Client` 工作区，清理根目录生成的 Unity 解决方案文件。新目录 `C_Programming_System` 的 Docker/API 启动、API 登录、Unity 版本发现和 .NET 10 环境检查通过。

## 2026-11-23 班级、时间窗口和成绩业务层

新增教师班级创建、学生成员维护、按班级/学生分配考试、UTC 开始/结束时间检查和学生成绩查询。A 端实际验证创建“C语言一班”、加入 student，创建当前时间窗口内的班级考试，学生看到并开始考试、交卷得分1，成绩接口返回已提交记录；API 构建通过。

## 2026-11-23 考试时间边界补强

修正 `ExamStore`：已有考试会话在时间窗口外不会被重新返回；取题、保存答案和交卷均检查开始/结束时间；组卷拒绝重复题目，保留结束时间晚于开始时间校验。为避免锁定正在运行的 API，使用独立输出目录完成 API 构建，结果 0 警告、0 错误；未执行真实过期/未开始接口用例。

## 2026-11-23 教师/学生 Canvas 工作台

新增教师考试列表接口；学生考试摘要返回开始/结束时间；Unity `ExamClientDemo` 改为运行时 Canvas/TextMeshPro 工作台，覆盖教师班级、成员、题库、组卷和考试列表，以及学生考试列表、倒计时、自动保存、自动交卷、成绩查询。新增 `tools/test-business-boundaries.ps1`，覆盖过期、未开始、跨班级和角色越权预期状态。API 独立输出构建通过；Unity 编辑器 PlayMode、脚本导入和边界脚本真实 HTTP 执行待验收。

## 2026-11-23 Unity 插值语法修复

修复 `client/Assets/Scripts/Infrastructure/ExamClientDemo.cs` 中班级选中标记和选项选中标记的嵌套条件表达式。C# 插值字符串不能直接包含未加括号的三元表达式；改为先计算 `mark` 再插值。Unity 编辑器重新导入该脚本后，日志中未出现新的 CS8076/CS8361/CS1003 错误；旧错误记录保留为历史日志。API 构建仍为 0 警告、0 错误。

## 2026-11-23 Unity Text 重载修复

用户反馈 Unity 报告 CS7036，原因是部分 `Text` 调用使用四参数形式，而脚本只有带控件名称的五参数重载。新增四参数重载并转发到现有实现；Unity 重新导入脚本后，最新导入记录之后未出现 CS7036 或其他新的 C# 编译错误。

## 2026-11-23 UI 字体依赖与题库上传修正

截图复核发现运行时 UI 依赖 TextMeshPro Essentials 字体资源，未执行编辑器资源导入时文字为空。已将 `ExamClientDemo` 的运行时文字、输入框和按钮改为 Unity 内置 `UnityEngine.UI.Text/InputField`，保留 Canvas 布局和样式，TMP Essentials 不再是运行前置条件。教师工作台新增可选题库路径和上传按钮，通过 multipart/form-data 调用服务端导入接口；普通建班、组卷和考试流程不依赖导入。

## 2026-11-23 Excel 上传格式扩展

服务端题库导入继续保留 CSV，并新增 `.xlsx` 读取：使用 .NET 自带 `ZipArchive` 和 `XDocument` 读取第一个工作表、共享字符串和表头，复用原有行校验与事务导入；API 现在按上传文件名选择 CSV 或 XLSX 解析。客户端上传入口仍是可选操作，普通教师建题/组卷流程不依赖文件。

## 2026-11-23 Unity 内置字体名称修复

Unity 2022.3 报告 `Arial.ttf is no longer a valid built in font`。已将 Canvas Text、InputField 和 Button 的内置字体统一改为 `LegacyRuntime.ttf`；不引入外部字体资源，也不需要导入 TMP Essentials。

## 2026-11-23 双端 UI 适配讨论

用户确认客户端只面向 PC 与手机，手机固定竖屏，PC 至少覆盖三种常见分辨率。已根据现有需求文档、三端线框、Unity 客户端源码和后续功能清单形成适配基线：建议 PC 验收尺寸为 1280×720、1440×900、1920×1080，手机主设计尺寸为 390×844，并补测 360×800、430×932 竖屏。当前仅更新决定和设计讨论文档，未修改客户端代码；具体视觉稿、组件 prefab 和分辨率实测待后续确认。

## 2026-11-23 页面级 UI 线框第二版

根据当前功能与计划功能补充登录、学生首页、题库/练习、考试、成绩和教师工作台的 PC/手机竖屏结构示意，见 `docs/design/ui-layout-detailed.svg`。本轮仍为文档和线框产物，未修改 Unity 页面代码；SVG XML 静态解析通过。

## 2026-11-23 页面级 UI 线框第三版

用户修正设计基准：PC 为 2560×1440，手机为 1080×2160 固定竖屏，其他窄屏/宽屏按等比缩放适配，并要求教师端页面完整纳入设计。已新增 `docs/design/ui-layout-detailed-v3.svg`，覆盖学生端、教师端、教师班级/组卷子页面及统一缩放规则；未修改 Unity 页面代码，SVG XML 静态解析通过。此前较小分辨率线框保留为历史稿。

## 2026-11-23 ArtAssets 资源审查

审查 `client/Assets/ArtAssets/` 中17个 PNG 及其 Unity `.meta`。资源整体风格与 UI 设计一致，但 PC/手机背景包含烘焙的 Header/侧栏/底部结构，卡片和按钮没有九宫格边界，图标图集全部仍按单 Sprite 导入，代码编辑器图包含固定布局结构，且当前客户端代码未引用这些资源。已建立 `docs/design/artassets-review.md`，列出逐项保留、重做、切片和补充清单；本轮未修改图片或 Unity 导入配置。

按用户确认删除8个确定不采用的整页背景、固定结构代码编辑器图和不可直接九宫格复用的卡片/按钮图及其 `.meta`。新增 `docs/design/artassets-generation-prompts.md`，按 Unity 可用的独立背景、九宫格面板、按钮、图标图集、空状态、代码区背景、头像和 Logo 记录逐图生成提示词；未修改保留资源的导入配置。

## 2026-11-23 ArtAssets 第二批筛选与重命名

审查客户端 `Assets/ArtAssets/` 新增资源：保留并统一重命名为 `ui_*` 资源，包含手机背景、面板、按钮、学生/教师/通用/状态图标图集、题号状态、空状态、代码区背景、头像和 Logo；删除旧中文命名重复资源及 `unity_exam_ui_assets_16-19_preview.jpg` 预览拼图。PC 背景当前为 1672×941，仅作为开发占位，最终需重新生成 2560×1440。未修改 Unity 导入配置，未执行 PlayMode 或移动端验收。详细清单见 `docs/design/artassets-review-2026-11-23.md`。

审查结论：当前资源足以开始第一阶段 Unity UI 适配和开发，PC 最终背景可以在开发过程中替换，不构成 UI 层级、组件或数据绑定的阻塞。进入开发前仍需完成 Sprite 图集切片、九宫格边界配置和两套布局 Profile；运行时导入与跨端验收待后续执行。

## 2026-11-23 Canvas 重建警告修复

Unity 报告在 Canvas rebuild loop 内移除 `Placeholder`。原因是按钮回调或网络回调同步清空并重建动态 UI，销毁 InputField 子控件时正处于 Canvas 重建过程。已增加 `QueueUi`，所有页面切换和题目切换延后一帧执行，再清理旧控件，避免在重建回调中销毁 Placeholder。

## 2026-11-23 工程内独立 UI 层首版

根据 `ui-layout-detailed-v3.svg` 和已确认的双端适配约束，新增 `client/Assets/Scripts/UI/UiRoot.cs`、`UiSafeArea.cs`、`UiFactory.cs`、`UiTheme.cs`。这套代码只使用 Unity 内置 uGUI，在工程内提供 Canvas/CanvasScaler、SafeArea、横屏左侧导航、竖屏底部导航、滚动内容宿主和基础 Text/Input/Button/Panel 工厂，不直接接入 UnityProject 的框架包。

`ExamClientDemo` 的业务状态、请求和考试计时保持不变，Canvas 初始化与通用控件改由独立 UI 层提供；登录、教师工作台、学生首页、考试和成绩页设置页面标题及导航，未实现的导航入口显示待接入状态。为避免现有 Unity 编辑器项目锁，已在系统临时目录复制最小客户端工程并用 Unity 2022.3.62f3c1 完成脚本编译/资源导入，日志未出现新的 C# 编译错误。正式 SampleScene PlayMode、PC/手机分辨率和 SafeArea 仍待人工验收。

## 2026-11-24 UI 根预制体和空引用修复

用户反馈 `UiRoot.RefreshLayout` 第 189 行 `HorizontalLayoutGroup` 空引用，并指出 UI 结构应先做预制体。原因是 Unity 不允许在同一节点上同时挂载 `VerticalLayoutGroup` 和 `HorizontalLayoutGroup`，动态添加第二个组件后引用为空。现已新增 `client/Assets/Resources/UI/UiRoot.prefab`，固化 Canvas、SafeArea、Header、导航、Viewport、Content 和两套导航布局；`UiRoot.Create` 改为加载并实例化该预制体，页面按钮同步写入 Desktop/Mobile 两个容器，运行时只切换激活状态和布局属性。

在隔离客户端中执行预制体生成方法和 Unity 2022.3.62f3c1 batchmode 编译，预制体生成成功、脚本编译返回码为 0；正式 SampleScene PlayMode 仍待人工验证。

## 2026-11-24 完整页面预制体与 ArtAssets 接入

用户截图确认前版仍为左上窄条列表，并要求完整预制体和已确认素材。本轮对照 v3 线框、Solitaire `GameHomePanel.prefab` 的固定锚点和 `TaskBarWindowLayout` 的安全区缩放，重做 `UiRoot.prefab` 的 PC/手机两套完整基准布局。新增 `UiLayoutView`、`UiList`、`UiListItem`、`UiDesignFit` 与 `UiAssets`；移除运行时 `UiFactory`。登录、首页、考试、成绩、教师总览、班级成员、题库、发布表单、设置、交卷确认和手机题号抽屉均预先制作；业务列表克隆内部模板。

`ExamClientDemo` 改为绑定预制体控件、保留共享业务状态，教师各入口进入对应页面；横竖屏切换复制输入内容。API 路由未改。`ApiHealthProbe` 保留请求和日志，移除覆盖正式 Header 的 IMGUI 状态文字。接入背景、Logo、两种头像、面板、按钮和空状态，切分学生/教师图集，设置九宫格边界。新增编辑器重建/离屏验证工具；详情和实际验证记录见 [基准布局设计](design/ui-prefab-reference-layout.md) 与 [本轮验收](validation/2026-11-24-ui-reference-layout.md)。

## 2026-11-24 本地服务端重启

用户反馈客户端请求失败。检查发现旧 API 进程已退出，Docker Desktop 引擎未运行。启动 Docker Desktop 后执行 `tools/start-local.ps1`，项目 MySQL 在 `127.0.0.1:3307` 恢复 healthy，API 以 PID 27512 监听 `127.0.0.1:5000`。`GET /health` 返回 HTTP 200，内容为 `{"status":"ok","database":"ok"}`。
2026-11-24 修复学生端考试列表“开始考试”按钮交互：动态列表克隆条目会清理模板按钮监听并显式恢复 `interactable`，考试启动增加进行中提示和失败后的可重试状态清理。教师演示账号继续由服务端启动时幂等种子创建，已用 `teacher / teacher123` 登录 API 验证角色为 `Teacher`，教师班级接口返回成功。客户端 PlayMode 的实际鼠标点击仍需在 Unity Game 窗口人工复验。
2026-11-24 处理考试列表射线与班级时间考试反馈：确认 PC/手机两套 `PublishedList` 的透明视口 Image 设置了 `raycastTarget=1`，已改为 0，并同步修改 prefab builder，避免重建后回退。客户端创建考试时在 classIds 文本为空时使用当前已选班级 ID，增加标题、题目、学生范围校验和开始时间状态提示。API 端使用教师账号、班级 ID、当前时间窗口请求创建考试已返回成功。Unity PlayMode 鼠标点击和完整客户端发布流程仍需复验。
2026-11-24 复现学生考试列表问题：考试 3、4 已过结束时间，开考接口返回 404；考试 2 已有 `Submitted` 会话，客户端仍把它当作普通考试继续取题。服务端考试列表现返回当前学生的 `attemptStatus`，开考接口对已提交会话返回已提交记录；客户端按时间和会话状态显示并禁用“已结束/未开始/已完成”按钮，已提交考试不再进入答题页。
2026-11-24 重启正式本机 API 进程加载考试状态修复，`/api/exams` 返回考试 2/3 的 `attemptStatus=Submitted`，考试 4 已过期开考返回 404；服务端验证符合按钮状态规则。Unity Game 窗口实际点击仍待复验。

## 2026-11-25 UI 适配规则核对

本轮根据 UiRoot、UiDesignFit 和 ProjectSettings 核对 UI 布局切换条件及 Windows 窗口设置，补充到[基准布局设计](design/ui-prefab-reference-layout.md#基准尺寸)。本轮为源码核对，既有渲染与播放器结果见[验收记录](validation/2026-11-24-ui-reference-layout.md)。

## 2026-11-25 最小考试闭环收尾核对

对照现有 API、Unity 实现和验收记录，在[阶段任务书](tasks/phase2-minimal-business-loop.md#当前收尾状态)登记实现与复验缺口，并归档已确认的正式考试断网规则。本轮依据代码与已有记录核对，未重新执行运行验收。

## 2026-11-25 在线考试验收准备

本机健康接口返回 HTTP 200，API 与 MySQL 状态均为 ok。Unity 已打开客户端 SampleScene。将建议验收顺序补入[阶段任务书](tasks/phase2-minimal-business-loop.md#建议的下一步)。本轮仅确认联调前置状态。

## 2026-11-25 在线考试保存、恢复与计分补验

已登记用户确认的发卷、保存与重进结果，并对本地测试库执行只读计分核对。教师端查卷接口和入口已补齐。详见[验收补记](validation/2026-11-25-online-exam-recovery.md)。

## 2026-11-25 教师端客观题查卷

新增教师答卷列表和逐题答案查询接口，并在已发布考试列表接入查看答卷、查看答题和返回入口。服务端按考试创建教师校验归属，学生调用返回 403。API 编译通过，本机测试库验证 3 题答卷、总分 1 分和逐题结果返回。Unity 正式 PlayMode 点击复验待执行。

## 2026-11-25 单选题批改与成绩开放

接入教师逐题调整 0 分或满分、教师确认开放和学生查看本人失分明细。运行时数据库升级保留已有答卷，原始答案与自动分继续保存。A 机 API 已更新，实际结果见[批改与回看验收](validation/2026-11-25-grading-review.md)。

## 2026-11-25 正式考试与身份接入排序

核对交卷、答卷读取和登录会话的当前实现。阶段任务书补充到期交卷、断网续答和身份接入的建议顺序及验收条件。本机健康检查返回 200，本轮完成源码核对与任务整理。

## 2026-11-25 正式考试收尾实现

服务端新增到期后台结算、答卷状态查询、数据库服务端时间和答案 revision。客户端新增服务端时间偏移、本地待同步草稿、原子写入、截止前退避补传和截止后状态查询。A 机 API 编译、Unity 脚本编译和到期自动结算 HTTP 验证通过，B 机断网与迁移验收待执行。

## 2026-11-25 工作区改动审查

审查了基线提交、已跟踪删除与修改、client/server 新目录、服务端业务、Unity UI、验收记录和临时构建产物。确认 A 机实现已完成，B 机验收尚未执行。发现旧顶层 Assets/Server 与当前 client/server 迁移同时存在，Windows 下 Server/server 路径大小写碰撞，提交前需要单独整理。审查记录见 [工作区改动审查](validation/2026-11-25-workspace-audit.md)。

## 2026-11-28 修复远端候选的 B 机验收问题

基于 `8335622` 在本地分支 `codex/exam-closeout-fixes` 更新 `ExamApi.cs` 与空库结构、Unity 的答卷草稿同步和交卷流程，以及两个 PowerShell 检查入口。以 B 机隔离库和 Unity PlayMode 复验，执行情况见 [修复复验](validation/2026-11-28-B-closeout-fixes.md)。
