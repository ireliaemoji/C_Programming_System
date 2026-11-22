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
- 处理：启动 `dotnet run --project Server/CExam.Api --urls http://localhost:5000`，随后验证 `/health` 返回 HTTP 200 和 `database: ok`。
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
两份审校版归档至毕设文档/；复制与源文件哈希一致。未因推送而将视觉排版或7篇全文审查标为通过，遗留项不变。既有无关B机待办日志段仅留工作区，不混入本次提交。
