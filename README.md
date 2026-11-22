# C 语言考试系统

## 项目协作规则

本项目相关对话开始工作前读取 [AGENTS.md](AGENTS.md)。所有已确认的讨论结论与实际实施，必须同步到 [决定记录](docs/decisions.md)、[实施日志](docs/implementation-log.md) 及对应设计、任务、问题和验收文档；最终聊天回复不能代替文档更新。其他电脑拉取仓库后适用相同规则，仓库外对话需显式读取。

## 当前状态

第一阶段环境收尾已实施：A/B 使用固定的 MySQL 8.4.9 Docker 镜像、回环端口和各自 `.env` 私密配置；`tools/check-environment.ps1` 默认只读检查，`tools/start-local.ps1` 负责启动项目数据库和本机 API。A/B 的 Docker、API、持久化和 Unity 健康请求均已验证；B 发现三项环境工具缺陷待 A 修复，完整阶段尚未通过，见下方 A/B 审查记录。

## 目录

- `毕设文档/`：[毕业设计任务书](毕设文档/C语言程序设计考试系统的设计与实现_任务书_审校版.docx)与[英文节选及中文翻译](毕设文档/编程自动评分与反馈工具_英文节选与中文翻译_审校版.docx)；内容已审校，视觉分页与部分中文文献全文审查仍待完成。
- `AGENTS.md`：项目对话、实施与文档同步规则
- `docs/decisions.md`：已确认决定、方案替代关系和待确认项
- `docs/implementation-log.md`：每轮实施与验证记录
- `Assets/`：Unity 客户端（Unity 2022.3.62f3c1）
- `Server/CExam.Api/`：ASP.NET Core API（当前目标框架 `net10.0`）
- `docker-compose.yml`：本地 MySQL 8.4.9 配置（仅发布 127.0.0.1）
- `.env.example`：本地 Docker/API 配置示例（不得提交 `.env`）
- `tools/check-environment.ps1`：只读环境检查和可选依赖安装
- `tools/start-local.ps1`：加载 `.env`、启动 Docker MySQL、验证认证查询并启动 API
- `docs/phase1-implementation.md`：第一阶段实施与验收记录
- `docs/issues-and-solutions.md`：问题、原因、处理和剩余动作
- `docs/multi-computer-deployment.md`：多电脑数据源安排、迁移与安全边界
- `docs/tasks/next-implementation.md`：可交给另一个对话窗口的下一轮实施任务书
- `docs/validation/template.md`：独立验收与问题复验模板

## 本地启动

推荐流程只使用项目专用 Docker MySQL；不会启动、停止或删除本机已有的原生 MySQL 数据目录。

1. 检查环境：`powershell -ExecutionPolicy Bypass -File tools/check-environment.ps1`
2. 首次使用复制 `.env.example` 为 `.env`，仅在本机设置随机的 `MYSQL_ROOT_PASSWORD` 和 `MYSQL_PASSWORD`；可按需修改 `MYSQL_HOST_PORT`。
3. 缺少必需软件时可执行：`powershell -ExecutionPolicy Bypass -File tools/check-environment.ps1 -InstallMissing`。Unity Hub、Docker Desktop 启动、授权、WSL/虚拟化和系统重启仍可能需要人工完成。
4. 启动数据库和 API：`powershell -ExecutionPolicy Bypass -File tools/start-local.ps1`。脚本等待容器内应用账号认证并执行 `SELECT 1`，再在 `127.0.0.1:5000` 启动 API；重复执行不会覆盖卷、凭据或占用中的健康 API。
5. 检查接口：`http://127.0.0.1:5000/health`。
6. 用 Unity 打开项目并运行 `Assets/Scenes/SampleScene.unity`，场景中的 `ApiHealthProbe` 会显示健康检查结果。

停止 API 可执行 `powershell -ExecutionPolicy Bypass -File tools/start-local.ps1 -Stop`，然后执行 `docker compose down` 也可只停止项目容器。不要使用 `docker compose down -v`，以免删除项目卷；不要把 A 的卷或 `.env` 复制到 B。

`/health` 返回 `200` 表示 API 和 MySQL 均可用；返回 `503` 表示 API 已启动但数据库尚未连接，响应只包含状态字段，不返回内部异常详情。

## 阶段范围

第一阶段只包含基础环境、健康检查和本地数据库启动配置，不包含账号、题库、考试、代码评测和成绩业务。具体状态见 [`docs/phase1-implementation.md`](docs/phase1-implementation.md)。

## 多电脑部署计划

本机 A 同时开发客户端和服务端，B 按指定提交独立运行并记录问题，A 修复后 B 复验。两边数据库独立，通过结构迁移和种子数据建立一致的测试条件，不同步整个数据库，也不随 Docker 账号迁移数据。先前共享数据库的方案仅保留为后续集中演示或正式部署选项。详见 [开发与验收方案](docs/multi-computer-deployment.md)。

B 机首次部署进度见 [2026-11-21 验收记录](docs/validation/2026-11-21-B-dce3fcb.md)：核心运行验收（含 Unity、503、持久化）通过；三项环境工具缺陷待修复，详见 [A/B 审查](docs/validation/2026-11-21-AB-review.md)。
