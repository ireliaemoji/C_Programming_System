# C 语言考试系统

## 项目协作规则

本项目相关对话开始工作前读取 [AGENTS.md](AGENTS.md)。所有已确认的讨论结论与实际实施，必须同步到 [决定记录](docs/decisions.md)、[实施日志](docs/implementation-log.md) 及对应设计、任务、问题和验收文档；最终聊天回复不能代替文档更新。其他电脑拉取仓库后适用相同规则，仓库外对话需显式读取。

## 当前状态

本地演示账号：学生 `student / student123`，教师 `teacher / teacher123`。教师账号由 API 首次访问业务接口时幂等创建，仅用于本地测试。

第一阶段环境收尾已实施：A/B 使用固定的 MySQL 8.4.9 Docker 镜像、回环端口和各自 `.env` 私密配置；`tools/check-environment.ps1` 默认只读检查，`tools/start-local.ps1` 负责启动项目数据库和本机 API。A/B 的 Docker、API、持久化和 Unity 健康请求均已验证；B 发现三项环境工具缺陷待 A 修复，完整阶段尚未通过，见下方 A/B 审查记录。

本项目是系统级仓库；本机路径和唯一工作区规则见 [唯一开发工作区](docs/development-workspace.md)。客户端与服务端在仓库根目录下同级，旧的 `C:\UnityProject\C_Client` 已删除。

## 目录

- `docs/graduation/`：[毕业设计任务书](docs/graduation/C语言程序设计考试系统的设计与实现_任务书_审校版.docx)与[英文节选及中文翻译](docs/graduation/编程自动评分与反馈工具_英文节选与中文翻译_审校版.docx)。
- `AGENTS.md`：项目对话、实施与文档同步规则
- `docs/decisions.md`：已确认决定、方案替代关系和待确认项
- `docs/implementation-log.md`：每轮实施与验证记录
- `client/`：Unity 客户端（Unity 2022.3.62f3c1）
- `server/CExam.Api/`：ASP.NET Core API（当前目标框架 `net10.0`）
- `docker-compose.yml`：本地 MySQL 8.4.9 配置（仅发布 127.0.0.1）
- `.env.example`：本地 Docker/API 配置示例（不得提交 `.env`）
- `tools/check-environment.ps1`：只读环境检查和可选依赖安装
- `tools/start-local.ps1`：加载 `.env`、启动 Docker MySQL、验证认证查询并启动 API
- `docs/phase1-implementation.md`：第一阶段实施与验收记录
- `docs/issues-and-solutions.md`：问题、原因、处理和剩余动作
- `docs/multi-computer-deployment.md`：多电脑数据源安排、迁移与安全边界
- `docs/tasks/next-implementation.md`：可交给另一个对话窗口的下一轮实施任务书
- `docs/tasks/phase2-minimal-business-loop.md`：第一阶段工具复验后的系统开发下一步实施计划（不是毕业设计任务书）
- `docs/design/system-development-slice.md`：首个客户端闭环的 API、数据、安全边界和 UI 设计
- `docs/tasks/ui-layer-integration.md`：工程内独立 UI 层首版范围和验收步骤
- `docs/development-workspace.md`：唯一开发工作区和旧副本处理规则
- `docs/validation/template.md`：独立验收与问题复验模板
- `docs/validation/2026-11-25-workspace-audit.md`：当前工作区改动审查、阶段结论和 B 机交付方式

## 本地启动

推荐流程只使用项目专用 Docker MySQL；不会启动、停止或删除本机已有的原生 MySQL 数据目录。

1. 检查环境：`powershell -ExecutionPolicy Bypass -File tools/check-environment.ps1`
2. 首次使用复制 `.env.example` 为 `.env`，仅在本机设置随机的 `MYSQL_ROOT_PASSWORD` 和 `MYSQL_PASSWORD`；可按需修改 `MYSQL_HOST_PORT`。
3. 缺少必需软件时可执行：`powershell -ExecutionPolicy Bypass -File tools/check-environment.ps1 -InstallMissing`。Unity Hub、Docker Desktop 启动、授权、WSL/虚拟化和系统重启仍可能需要人工完成。
4. 启动数据库和 API：`powershell -ExecutionPolicy Bypass -File tools/start-local.ps1`。脚本等待容器内应用账号认证并执行 `SELECT 1`，再在 `127.0.0.1:5000` 启动 API；重复执行不会覆盖卷、凭据或占用中的健康 API。
5. 检查接口：`http://127.0.0.1:5000/health`。
6. 用 Unity 打开 `client/` 项目并运行 `client/Assets/Scenes/SampleScene.unity`，场景中的 `ApiHealthProbe` 会显示健康检查结果。

停止 API 可执行 `powershell -ExecutionPolicy Bypass -File tools/start-local.ps1 -Stop`，然后执行 `docker compose down` 也可只停止项目容器。不要使用 `docker compose down -v`，以免删除项目卷；不要把 A 的卷或 `.env` 复制到 B。

`/health` 返回 `200` 表示 API 和 MySQL 均可用；返回 `503` 表示 API 已启动但数据库尚未连接，响应只包含状态字段，不返回内部异常详情。

## 阶段范围

第一阶段只包含基础环境、健康检查和本地数据库启动配置，不包含账号、题库、考试、代码评测和成绩业务。具体状态见 [`docs/phase1-implementation.md`](docs/phase1-implementation.md)。

## 多电脑部署计划

本机 A 同时开发客户端和服务端，B 按指定提交独立运行并记录问题，A 修复后 B 复验。两边数据库独立，通过结构迁移和种子数据建立一致的测试条件，不同步整个数据库，也不随 Docker 账号迁移数据。先前共享数据库的方案仅保留为后续集中演示或正式部署选项。详见 [开发与验收方案](docs/multi-computer-deployment.md)。

B 机首次部署进度见 [2026-11-21 验收记录](docs/validation/2026-11-21-B-dce3fcb.md)：核心运行验收（含 Unity、503、持久化）通过。三项环境工具缺陷已在本地修复，待 B 在修复提交上复验；LibreOffice 26.8.3.1 已安装并完成两份毕业设计 Word 的逐页渲染检查。复验通过后按 [系统开发下一步实施计划](docs/tasks/phase2-minimal-business-loop.md) 开始系统开发。毕业设计任务书本身覆盖整个系统，不等同于当前阶段记录。

A 端已开始系统开发首个客户端闭环，当前实现和限制见 [闭环设计](docs/design/system-development-slice.md)；真实数据库和 Unity PlayMode 复验完成前，不宣称业务流程已通过。

业务层已继续扩展教师题库维护、组卷、学生考试会话、答案保存和客观题交卷评分；接口验证见 [考试业务层验收](docs/validation/2026-11-23-exam-business-slice.md)。

当前业务层还包括教师班级、学生成员、考试时间窗口和学生成绩查询；验证见 [班级考试成绩验收](docs/validation/2026-11-23-class-exam-grade-slice.md)。

Unity `SampleScene` 当前实例化完整 `UiRoot.prefab`，使用预先制作的 PC/手机页面与 Unity 内置 Text/InputField：教师可创建班级、维护成员、可选上传 CSV 或 `.xlsx` 题库并组卷，学生可查看考试、倒计时、自动保存、交卷和查看成绩。TextMeshPro Essentials 不再是运行前置条件。过期/未开始、跨班级和角色越权可用 `tools/test-business-boundaries.ps1` 复验；真实 PlayMode 和 A/B 现场验收仍需单独记录。

UI 层采用工程内独立 uGUI 实现，`client/Assets/Resources/UI/UiRoot.prefab` 保存完整 PC 2560×1440 与手机 1080×2160 基准布局和已确认 ArtAssets；固定页面不再动态生成，数据列表只克隆预制体内部模板。缩放与素材说明见 [基准布局](docs/design/ui-prefab-reference-layout.md)，[渲染和隔离播放器验收](docs/validation/2026-11-24-ui-reference-layout.md)已通过，正式 API 联调、移动真机和 B 端仍待验收。后续步骤见 [UI 交接](docs/tasks/ui-layer-integration.md)。

教师在已发布考试中可批改已交卷的单选题。考试结束后，教师确认开放，学生可从成绩页查看自己的答卷与失分明细。见 [批改与回看验收](docs/validation/2026-11-25-grading-review.md)。

正式考试收尾已在 A 机接入服务端到期自动交卷、服务端时间校准、答案 revision、本地待同步草稿和退避补传。B 机验收任务见 [考试收尾验收](docs/tasks/2026-11-25-B-exam-offline-closeout.md)。

API 启动后可执行 `powershell -ExecutionPolicy Bypass -File tools/test-business-boundaries.ps1`，脚本会创建带时间后缀的临时账号和班级，验证过期/未开始考试返回 404、跨班级学生不能开考、其他教师不能维护班级以及学生不能创建教师班级。脚本不会删除数据库卷；测试数据需按本地演示数据清理策略处理。

题库导入模板见 [`docs/templates/question-bank-import.csv`](docs/templates/question-bank-import.csv)。当前采用 Excel 兼容 CSV；题目可见性和考试学生分配由服务端控制，公开练习接口不会返回教师私有题。
