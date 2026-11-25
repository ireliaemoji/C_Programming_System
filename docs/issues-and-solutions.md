# 问题与解决方案记录

更新日期：2026-11-19

## ISSUE-001：本地项目路径与用户描述存在空格歧义

- 现象：用户描述为 `C:\UnityProject C_Client`。
- 原因：实际目录是 `C:\UnityProject\C_Client`。
- 处理：使用实际存在的 Unity 项目目录作为 Git 仓库根目录，并保留该路径记录。
- 状态：已解决。

## ISSUE-002：Unity 项目原本不是 Git 仓库

- 现象：项目目录没有 `.git`，远程仓库只有初始 README 和 `.gitignore`。
- 处理：在 Unity 项目根目录初始化 Git，关联远程 `main`，提交 Unity 项目、API、Docker 配置和文档后推送。
- 状态：已解决。

## ISSUE-003：Docker 与 MySQL 未安装或未加入命令行

- 现象：`docker --version` 和 `mysql --version` 均无法执行。
- 影响：无法启动本地 MySQL，也无法完成 `/health` 返回 200 的数据库验收。
- 处理：本阶段只提交 `docker-compose.yml` 和连接配置，没有擅自安装系统软件。
- 下一步：由用户确认后安装 Docker Desktop（推荐），或安装 MySQL 8.0。
- 状态：待用户决定安装方式。

## ISSUE-004：Unity 批处理验证被已有 Unity 实例占用

- 现象：Unity batchmode 返回“Project already open in another instance”。
- 处理：未强制关闭用户正在使用的 Unity 编辑器，避免丢失未保存内容。
- 下一步：关闭当前 Unity 编辑器后重新执行批处理验证。
- 状态：待复验。

## ISSUE-005：MySqlConnector 2.4.0 NuGet 安全告警

- 现象：`dotnet build` 报告 `NU1902`，对应 GitHub Advisory `GHSA-473q-m89c-ghf8`。
- 风险：公告描述在零配置 TLS 且未验证证书时，攻击者可能诱导连接先发送明文数据库密码。使用本机回环连接、配置 CA 并进行证书校验可避免公告描述的条件，但不应把旧版本带入正式环境。
- 处理：升级到 `MySqlConnector 2.6.1`，重新还原并编译。
- 结果：当前 `dotnet build` 为 0 警告、0 错误。
- 参考：[GitHub Advisory GHSA-473q-m89c-ghf8](https://github.com/advisories/GHSA-473q-m89c-ghf8)、[NuGet MySqlConnector](https://www.nuget.org/packages/MySqlConnector)。
- 状态：已解决；正式环境仍需配置 CA/证书校验和安全连接参数。

## ISSUE-006：第一阶段完成状态容易被误判

- 现象：代码已经提交，但本机依赖和 Unity 运行验收未完成。
- 处理：将状态拆分为“实施完成，环境验收待补”，并在 `phase1-implementation.md` 中记录证据和停止条件。
- 状态：已解决。

## ISSUE-007：Docker Desktop 已安装但引擎无法启动

- 现象：Docker CLI 29.9.3 和 Compose 插件已安装，但 `docker desktop status` 显示 `stopped`，`docker info` 报 Docker Desktop unable to start。
- 原因：当前 PowerShell 不是管理员会话，WSL2 和 Virtual Machine Platform 的状态无法读取或启用；Docker Desktop 的 Linux 后端尚未就绪。
- 处理：保留 Docker Desktop 安装结果；没有强制修改 Windows 可选功能或重启机器。
- 下一步：管理员 PowerShell 执行 `wsl --install --no-distribution` 或启用对应 Windows 功能，重启后再启动 Docker Desktop。
- 后续：用户已执行 VirtualMachinePlatform 和 WSL 功能启用命令，尚未重启；不能据此确认 Docker 已恢复。
- 状态：待重启和引擎/容器验证。

## ISSUE-008：MySQL 安装后未注册 Windows 服务

- 现象：MySQL Community Server 8.4.9 文件已安装，但当前用户没有对应 Windows 服务。
- 原因：安装器在非管理员会话中无法完成服务注册。
- 处理：使用 `C:\Users\hello\AppData\Local\CExam\mysql-data` 初始化用户拥有的开发数据目录，并启动本地 `mysqld`；创建 `c_exam` 数据库和 `exam_app` 开发账号。
- 验证：API `/health` 返回 HTTP 200，响应为 `{"status":"ok","database":"ok"}`。
- 状态：开发环境已可用；如需开机自动启动或系统服务，需要管理员权限单独配置。

## ISSUE-009：Unity 健康探针 meta GUID 无效

- 现象：Unity 日志报告 `ApiHealthProbe.cs.meta` 的 GUID 无效，并将场景中的 `ApiHealthProbe` 组件识别为 Missing Script。
- 原因：前次手写 meta 文件时 GUID 少了 1 位，只有 31 位十六进制字符。
- 处理：生成 32 位 GUID，并同步更新 `SampleScene.unity` 的脚本引用。
- 复验：用户已提供 `ApiHealthProbe.cs:20` 的 `API: {"status":"ok","database":"ok"}` 成功日志。
- 状态：实际场景请求已通过；独立批处理验证仍记录于阶段文档。

## ISSUE-010：Unity 报告 API unavailable

- 现象：`ApiHealthProbe` 输出 `Cannot connect to destination host`。
- 原因：探针请求的是 `http://localhost:5000/health`，但当时 ASP.NET Core API 未启动；MySQL 3306 端口本身正常。
- 处理：启动 `dotnet run --project server/CExam.Api --urls http://localhost:5000`，随后验证 `/health` 返回 HTTP 200 和 `database: ok`。
- 结论：这是本地 API 进程生命周期问题，不是 Unity 请求代码或 Docker 引擎问题。
- 状态：已解决；每次运行 Unity 前需保持 API 进程运行。

## ISSUE-011：本地 MySQL X Protocol 暴露到所有网卡

- 现象：初始开发实例除了 `127.0.0.1:3306` 外，还监听 `0.0.0.0:33060` 和 `[::]:33060`。
- 原因：MySQL 8.4 默认启用了 X Plugin；即使普通 SQL 连接绑定到回环地址，X Protocol 仍可能监听所有网卡。
- 处理：安全停止开发实例后，以 `--bind-address=127.0.0.1 --mysqlx=0` 重启；同时更新环境工具，后续启动不会再开放 33060。
- 结果：当前只保留 `127.0.0.1:3306`，API `/health` 仍返回 200。
- 状态：已解决。

## ISSUE-012：换电脑后 Docker 不会自动共享数据

- 原因：容器和 named volume 属于各自宿主机，相同 Compose 文件、卷名或 Git 仓库不会同步实际数据库。
- 当前决定：A 开发、B 独立验收，两台电脑各运行本机 API 和独立 Docker MySQL，通过 Git 同步代码、结构迁移、种子数据与脱敏问题记录。共享 B 数据库仅保留为未来部署选项。
- 安全边界：当前不开放跨电脑 API/数据库端口，不同步真实数据或秘密。Compose 的端口映射、示例密码和原生安装工具仍需在下一轮调整。
- 状态：方案已记录；新工具与 Docker 验收尚未实施。见 [多电脑方案](multi-computer-deployment.md)、[下一轮任务书](tasks/next-implementation.md) 和 [验收模板](validation/template.md)。

## ISSUE-013：Docker 默认流程与本机原生 MySQL 的边界

- 现象：旧 Compose 发布所有网卡的 3306、使用示例密码；旧检查工具还可启动原生 MySQL，无法作为 A/B 一致的 Docker 流程。
- 处理：固定 `mysql:8.4.9`，仅绑定 `127.0.0.1`，凭据改由未跟踪 `.env` 提供；环境检查不再安装/启动原生 MySQL，启动脚本只管理项目 Compose 服务和自己记录的 API 进程。
- 数据保护：未停止或覆盖已有原生 MySQL；已有 Docker 卷密码不匹配时保留卷，按原密码恢复或人工迁移，不执行 `down -v`。
- 状态：已实施；Docker 引擎和容器实际验证待 A/B 现场执行。

## ISSUE-014：健康检查失败响应泄露内部异常

- 现象：旧 `/health` 503 JSON 包含数据库异常消息。
- 原因：异常消息直接写入客户端响应。
- 处理：响应仅返回 `status=degraded` 和 `database=unavailable`，异常通过服务端日志记录。
- 复验：A 机可用 API 启动后用不可用连接配置请求 `/health`，应得到 503 且 JSON 不包含 `detail` 或连接字符串；运行记录见 [A 机验收记录](validation/2026-11-19-A-0bb9dbc.md)。
- 状态：代码已实施，现场 Docker/Unity 验证待执行。

## 2026-11-21 B-ENV-01 环境前置条件未满足

.NET SDK 当前仅检出 3.0.101、8.0.400，项目要求 net10.0；安装脚本存在 dotnet 即不安装新版，因此另外显式调用 winget 安装 SDK 10。Docker 安装尚无成功结果。WSL 安装返回 0x80073d28，明确要求管理员权限；原 wsl --status 也返回失败。Unity 在 D:\UnityHub\unity 找到 2022.3.17f1c1 与 2021.3.33f1c1，未找到要求的 2022.3.62f3c1。需管理员完成安装、按系统提示重启，再复验；Unity 通过 Hub 安装精确版本并完成授权。数据库/API/Unity 请求及持久化尚未执行，不能判定通过。

B-ENV-01 复验：管理员安装已解决 WSL 安装权限阻塞，Docker/Unity/.NET 安装成功；现阻塞为 Windows 功能 RestartNeeded=True。新增脚本 Unity 版本化安装目录发现遗漏，详见 B 验收记录；待 A 修复。

## 2026-11-21 重启后复验

用户反馈已重启。Docker Desktop 启动后引擎 29.9.3 可连接；WSL 3.0.5 可执行。MySQL 8.4.9 镜像拉取完成。

- 首次 start-local.ps1 失败：无容器时 docker compose ps -q mysql 返回空，直接 .Trim() 抛出空值异常。保持源码未改，交 A 修复；通过 docker compose up 继续部署。
- 3306 被占用，未停止已有服务，将仅本机 .env 的 MYSQL_HOST_PORT 改为 13306；容器发布检查为 127.0.0.1:13306->3306/tcp。
- 启动脚本数据库应用账号认证 SELECT 1 成功。API 在 http://127.0.0.1:5000/health 返回 200，正文 {"status":"ok","database":"ok"}。
- 重复 start-local.ps1 成功识别健康 API，保留既有进程。
- Unity 首次导入及脚本编译完成，编辑器进入 Untitled 场景；SampleScene Play 请求未完成（UI 控制窗口绑定失败），不能标为通过。
- 包含测试表和容器停止/恢复的组合命令被工具自动审批拒绝，未执行；503 与持久化仍未验证。
- 当前结论：数据库/API 与重复启动通过；完整 B 验收部分通过。待 A 修复首次启动空值处理及 Unity 安装目录发现问题，B 后续补验 SampleScene、503、数据持久化。

2026-11-21 审查新增 B-ENV-02/03/04：首次空容器 null.Trim、Unity 路径/版本枚举遗漏、已有旧 SDK 时不安装目标 SDK。根因及复现详见 [A/B 审查](validation/2026-11-21-AB-review.md)。

## 2026-11-22 任务书模板与渲染前提

- 问题：附件 Word 答辩日期年份笔误且遗漏答辩阶段；PDF 可提供对应日期依据，建议校正，不沿用样本的智能家居文献。
- 文档验证工具：bundled Python 可读取 DOCX 和 PDF；render_docx.py 运行失败，已证实原因是 PATH 中未找到 LibreOffice soffice.exe；常见安装路径亦未发现。当前未完成视觉排版验收，不能宣称 Word 排版通过。
- 下一步：确认可用渲染工具后再做完整逐页视觉检查；本轮不据此安装新软件。文献题录和全文可访问性须分别标注，检索到题录不等于阅读全文。

### 2026-11-22 任务书复核

已修正模板日期笔误并依据PDF补全答辩阶段；Word已生成。确认未发现可用Word/LibreOffice排版引擎，渲染限制仍未解除；仅完成结构核对，用户打开Word后仍需核查实际分页。参考文献入口可查询题录，部分全文须机构权限；ACM页面本次403，已提供作者主页和arXiv入口交叉核验。

### 2026-11-22 任务书表达问题修订

用户指出前版将设计任务与当前实施状态混写，且题录夹带链接及说明，不符合用途。已重写为待实施任务，删除进度叙述及核验信息，按参考PDF保留编号题录；核验链接仅在聊天提供。结构及措辞检查通过；视觉检查限制仍存在。

### 2026-11-22 文献格式复核
用户要求参考PDF省略页码；已删除10条题录页码及英文文章编号，重新读取核对通过。既有视觉渲染限制未改变。

原Word覆盖写入受PermissionError阻止，具体占用原因未查；改为另存修订版成功，10条题录实际复核通过，无需关闭用户应用。

## 2026-11-22 全文访问与文献适用性问题

原10篇均有出版记录，但7篇公开入口仅有题录摘要，未取得全文，不能视为逐篇全文审查完成。第3与第7篇部分技术表述不宜照搬为安全依据；第4篇对桌面C考试系统直接相关性较弱，暂建议重新评估，未替换。翻译源PDF待用户上传，版本页码需据上传文件复核。详情见 docs/tasks/literature-review-and-translation.md。

## 2026-11-22 节选翻译复核
用户已提供英文源PDF，翻译资料前提解除。原文第3.2.3节称四种模式却只说明两类特征，已忠实翻译并加译注，未自行修补论文。Word结构格式检查通过，render_docx.py仍因缺少soffice.exe失败，视觉分页未通过验收。

## 2026-11-22 再审发现与修正

英文提取把PDF换行处object-oriented的词内连字符误删；已修复并复核其他断行。译文industry-standard先前译作“符合行业标准”容易误导为认证要求，改为“业界通用”。任务书收紧Unity开销比较和公平性措辞。无章节段落遗漏；正文现2607汉字。审校版结构检查通过，7篇中文全文与两份文件视觉分页仍未验收；不将结构检查等同视觉通过。

## 2026-11-22 归档交付边界
两份审校版归档至docs/graduation/；复制与源文件哈希一致。未因推送而将视觉排版或7篇全文审查标为通过，遗留项不变。既有无关B机待办日志段仅留工作区，不混入本次提交。

## 2026-11-23 远端文档审查遗留项

两份远端审校版的结构和内容检查通过，任务书可作为“基本合理、主要内容完备”的待提交稿。学校专用模板中的学生/专业班级/学号、指导教师、签字和盖章栏仍需按校方要求确认；当前留空是用户明确要求，不视为最终表单验收。`render_docx.py` 因缺少 `soffice.exe` 未生成页面图，分页、字体替代和参考文献换行仍待人工复核；7篇中文文献只有题录或摘要级核验，不能称为全文审查完成。

## 2026-11-23 B 工具问题修复

B-ENV-02：已将 `docker compose ps -q mysql` 输出通过 `Out-String` 转为可安全 `.Trim()` 的字符串，首次无容器时会继续端口占用检查，不再因 null 中断。

B-ENV-03：已覆盖 `C:\Program Files\Unity\<version>\Editor\Unity.exe` 和本地用户版本化目录，并遍历全部候选后按 `ProductVersion` 匹配，已在本机发现项目要求的 Unity 2022.3.62f3c1。

B-ENV-04：已从项目 `TargetFramework` 推导目标 .NET 主版本，检查 `dotnet --list-sdks`；已有旧 SDK 时会在 `-InstallMissing` 下安装目标 SDK，缺失时仍以非零状态报告。当前本机已发现 .NET 10。Docker 引擎未运行，B 端首次启动和空环境复验待在修复提交上执行。

## 2026-11-23 系统开发切片验证边界

A 端已实现账号/会话、题目查询和单选答案提交的首个客户端闭环。启动项目 MySQL 与 API 后，注册/登录、3道题查询、答案写入、错误边界和 Student 访问教师接口返回 403 已通过；Unity 编辑器脚本编译通过，SampleScene PlayMode 的完整 UI 操作仍待人工复验。不能把 API 通过等同于客户端流程完全验收。

## 2026-11-23 班级与考试时间边界

业务层新增班级成员和考试时间窗口。考试创建必须指定学生或班级；开考、保存答案和交卷均由服务端检查开始/结束时间，学生只能查询自己已提交的成绩。A 端已验证班级加入、当前窗口考试、交卷评分和成绩查询；未来仍需补充过期考试和跨班级越权的负向用例。

## 2026-11-23 题库组卷与考试评分切片

业务层已扩展教师建题/修改、组卷、学生开考、逐题保存答案和一次性交卷评分。A 端真实接口已验证教师创建第4题和4题考试，学生获得4题并提交后得到总分1。当前未实现倒计时、断线恢复、程序题评测和成绩查询；这些属于后续业务范围，不应误记为本轮已完成。

## 2026-11-23 题库公开泄露风险修复

原实现仅用 `is_published` 区分题目，公开题目接口会返回所有已发布题目，考试也没有学生分配关系。已增加题目归属和 `TeacherPrivate/ClassOnly/PublicPractice` 可见性、考试分配表；公开接口仅返回公开练习题，学生只能查看分配给自己的考试。教师导入暂采用 Excel 兼容 CSV，原生 `.xlsx` 仍待后续需求确认和依赖评估。

## 2026-11-23 交卷时间校验缺口

现象：原交卷更新 SQL 只检查进行中状态，没有检查考试结束时间；已存在的考试会话在窗口外也可能从开考接口返回。

原因：时间条件只加在创建会话和保存答案路径，读取已有会话、取题和交卷路径未共享完整窗口条件。

处理：在 `StartAttemptAsync`、`GetAttemptQuestionsAsync`、`SaveExamAnswerAsync`、`SubmitAttemptAsync` 统一检查 UTC 开始/结束时间；组卷增加重复题目输入校验。

状态：代码已构建通过；真实过期、未开始和跨班级接口复验待运行环境执行。

## 2026-11-23 UI 与边界验收待执行

本轮已补齐代码路径，但以下证据尚未取得：Unity 2022.3 编辑器重新导入脚本后的 0 编译错误、教师/学生 PlayMode 操作、当前 API 重启后执行 `tools/test-business-boundaries.ps1` 的真实 HTTP 结果，以及 B 端独立复验。未将代码落盘或 API 构建结果写成完整 UI 验收通过。

## 2026-11-23 Unity UI 编译错误

现象：Unity 报告 `ExamClientDemo.cs` 第79、108、140行 CS8076/CS8361/CS1003。

原因：字符串插值中直接嵌入 `condition ? "..." : "..."`，冒号被解析为插值格式语法。

处理：将条件表达式移到插值前的局部 `mark` 变量，再拼接到字符串；未搬迁 C 盘旧 Unity 资源，因为当前错误与资源无关。

状态：已修复；编辑器重新导入后未发现新的同类编译错误，PlayMode 业务操作仍待验收。

## 2026-11-23 Unity Text 调用参数错误

现象：`ExamClientDemo.cs` 多处调用 `Text` 缺少 `color` 对应的重载参数，Unity 报 CS7036。

处理：新增 `Text(Transform, string, int, Color)` 重载，自动生成控件名称并复用五参数实现。

状态：已修复；最新 Unity 脚本导入后未出现新的 CS7036/CS 编译错误。

## 2026-11-23 UI 无文字与导入入口问题

现象：未导入 TextMeshPro Essentials 时，运行时 Canvas 只有按钮色块，没有文字；教师题库导入入口也没有从客户端上传文件。

原因：运行时控件使用 TMP 组件但没有可用 TMP 字体资源；导入接口只存在服务端，客户端工作台没有 multipart 文件上传控件。

处理：改用 Unity 内置 Text/InputField 和内置 Arial 字体；新增可选文件路径和上传按钮，普通教师流程不要求填写导入文件。当前上传格式为 Excel 可打开的 CSV 模板，原生 `.xlsx` 仍未解析。

状态：代码已修复，Unity 最新导入记录后无本脚本 C# 编译错误；PlayMode 上传和真实文件导入待执行。

## 2026-11-23 原生 XLSX 导入

需求将“Excel 文件导入”明确为客户端上传。现有 Excel 兼容 CSV 入口保留，并在服务端增加标准 `.xlsx` 解析，不增加第三方包；只读取第一个工作表，要求首行使用既有九列模板表头。客户端通过 `multipart/form-data` 上传文件名和内容，导入按钮为可选。

状态：API 构建通过；客户端实际上传 `.xlsx` 和错误文件的 PlayMode 验收待执行。

## 2026-11-23 LegacyRuntime 字体修复

- 现象：`Resources.GetBuiltinResource<Font>("Arial.ttf")` 在 Unity 2022.3 抛出 ArgumentException，Canvas 初始化中断。
- 处理：替换为 Unity 2022.3 支持的 `LegacyRuntime.ttf`。
- 状态：代码修复完成，待 Unity PlayMode 重新运行确认文字显示和上传流程。

## 2026-11-23 Canvas rebuild loop 警告

现象：`Trying to remove Placeholder (UnityEngine.UI.Text) from rebuild list while we are already inside a rebuild loop.`

原因：动态页面按钮回调内同步执行 `ClearContent()`，销毁 InputField Placeholder 时触发 CanvasUpdateRegistry 重建列表冲突。

处理：新增 `QueueUi`/`ApplyUiNextFrame`，登录、角色页面、考试切题、成绩页和返回操作在下一帧重建 UI；业务请求和数据保持不变。

状态：Unity 最新脚本导入记录之后未出现本脚本编译错误；运行时警告需在 PlayMode 点击切换页面复验。

## 2026-11-23 UI 层接入后的验证边界

本轮将原运行时 Canvas 的通用结构抽到工程内 `Assets/Scripts/UI/`，业务页面通过 `UiRoot` 使用横屏左侧导航、竖屏底部导航和 SafeArea。隔离临时工程的 Unity 2022.3.62f3c1 导入/脚本编译未发现新的 CS 错误；正式客户端因 Unity 编辑器已占用，未强制关闭或抢占实例。

待查事项：正式 `SampleScene` PlayMode 仍需人工确认登录页文字、页面切换不再触发 Canvas rebuild 警告、横屏/竖屏导航切换、刘海/底部 inset 和考试计时/自动保存。上述事项属于运行时验收，不把隔离工程编译结果表述为完整 UI 通过。

## 2026-11-24 UiRoot.RefreshLayout 空引用

- 现象：`UiRoot.Initialize` 调用 `RefreshLayout` 时，第 189 行访问 `horizontalNavigation` 触发 `NullReferenceException`。
- 原因：`BuildShell` 在同一个 GameObject 上动态添加 `VerticalLayoutGroup` 和 `HorizontalLayoutGroup`；Unity UI 布局组件不支持这种并存方式，第二个引用未建立。
- 处理：删除运行时根层级构建，新增 `Resources/UI/UiRoot.prefab`；预制体中使用 `DesktopItems`（纵向）和 `MobileItems`（横向）两个独立容器，运行时分别生成按钮并按布局模式切换容器。`UiRoot.BindPrefabReferences` 增加完整引用检查。
- 状态：隔离 Unity 预制体生成和脚本编译通过；正式工程 PlayMode 需确认实际 prefab 导入和页面切换。

## 2026-11-24 控件挤成窄条且未使用 ArtAssets

- 用户反馈：1920×1080 截图中页面控件挤在左上角，长文本互相覆盖，页面未使用已确认素材。
- 已证实原因：前版 prefab 中 Content/导航布局组的 `m_ChildControlWidth/Height=0`，`LayoutElement.preferredWidth/Height` 没有控制实际子节点尺寸；内容控件沿用默认 100×100。前版仅保存容器，没有制作完整页面，也没有 Sprite 绑定。之前将 Canvas 序列化的根尺寸/缩放归零直接当作根因的说法缺少运行时证据，本轮以实际布局组配置和渲染结果为准。
- 修复：固定页面控件完整保存在 prefab，删除运行时控件工厂；列表模板布局开启宽/高控制；绑定 ArtAssets 并设置切片/九宫格；业务脚本只写文本/事件/列表数据。适配以完整设计区为基准，不再逐个改写页面结构。
- 验证：离屏渲染/边界检查和隔离播放器结果统一记录在 [本轮验收](validation/2026-11-24-ui-reference-layout.md)；真实服务端登录、教师上传和真机仍须单独验收。
2026-11-24 用户反馈学生端两个考试按钮无法点击。静态检查确认按钮由 `UiList.Add` 动态克隆并绑定回调，但模板按钮的运行时状态未在每次刷新时显式重置，失败请求也会保留临时考试状态，造成“点击无响应”的体验。现已在 `UiList.Add` 清理监听并设置 `interactable`，考试启动显示状态并在失败时清理状态。API 与教师账号登录已验证，Unity Game 窗口鼠标点击待人工复验。
## 2026-11-24 PublishedList 射线拦截与班级时间考试请求

- 现象：学生端开始考试按钮仍无法点击，教师端班级时间考试显示请求失败。
- 已证实原因：PC/手机 `PublishedList` 的透明视口 Image 开启 `raycastTarget`，会在动态考试条目的 Button 之前命中射线。服务端班级时间考试接口在本机使用教师令牌、班级 ID、题目 ID 和 UTC 时间窗口请求已返回成功。
- 处理：关闭两套 `PublishedList` 视口 Image 的射线拦截，并在 `ExamUiPrefabBuilder` 中固定该配置。客户端创建考试在 classIds 文本为空时回退到当前选中班级 ID，增加提交前校验和时间状态提示。
- 待验证：Unity 重新导入 prefab 后点击两个开始考试按钮，使用教师端完整填写并提交班级时间考试。
## 2026-11-24 已结束与已完成考试仍触发开考请求

- 现象：班级测试点击后无响应或 404，私有分配考试完成后再次点击显示请求失败。
- 已证实原因：班级考试 3、4 已超过结束时间，服务端按时间窗口返回 404。私有考试 2 的学生会话状态为 `Submitted`，客户端考试列表未携带会话状态，仍显示“开始考试”并请求取题。
- 处理：`ExamSummary` 增加 `attemptStatus`，学生考试查询返回当前用户的会话状态。客户端根据状态和 UTC 时间显示“已完成”“已结束”“未开始”或“开始考试”，不可开考条目按钮置灰。服务端开考接口对已提交会话返回其完成状态，客户端不再进入答题页。
- 验证边界：新输出构建通过并已重启正式 API，HTTP JSON 已复验。Unity PlayMode 仍需点击复验。

## 2026-11-25 教师端缺少学生答卷查看

用户在在线考试验收中发现教师端无法查卷。原有已发布考试列表只显示考试摘要。现已增加归属校验和逐题查卷入口，后续补齐单选题改分和成绩开放。详见[批改与回看验收](validation/2026-11-25-grading-review.md)。
