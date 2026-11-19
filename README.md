# C 语言考试系统

## 项目协作规则

本项目相关对话开始工作前读取 [AGENTS.md](AGENTS.md)。所有已确认的讨论结论与实际实施，必须同步到 [决定记录](docs/decisions.md)、[实施日志](docs/implementation-log.md) 及对应设计、任务、问题和验收文档；最终聊天回复不能代替文档更新。其他电脑拉取仓库后适用相同规则，仓库外对话需显式读取。

## 当前状态

第一阶段的 API、原生 MySQL 和 Unity 健康检查闭环已验证成功。用户已执行 Windows 虚拟机平台/WSL 功能启用命令，尚待重启后验证 Docker 引擎与容器。当前采用“A 开发、B 独立验收”：两台电脑各运行本机 API 和独立 Docker MySQL，通过 Git 交接代码与脱敏问题记录。工具尚未按新方案调整，下一步先完成第一阶段环境收尾。

## 目录

- `AGENTS.md`：项目对话、实施与文档同步规则
- `docs/decisions.md`：已确认决定、方案替代关系和待确认项
- `docs/implementation-log.md`：每轮实施与验证记录
- `Assets/`：Unity 客户端（Unity 2022.3.62f3c1）
- `Server/CExam.Api/`：ASP.NET Core API（当前目标框架 `net10.0`）
- `docker-compose.yml`：本地 MySQL 8.0 配置
- `tools/check-environment.ps1`：环境检查、可选依赖安装和本地 MySQL 启动工具
- `docs/phase1-implementation.md`：第一阶段实施与验收记录
- `docs/issues-and-solutions.md`：问题、原因、处理和剩余动作
- `docs/multi-computer-deployment.md`：多电脑数据源安排、迁移与安全边界
- `docs/tasks/next-implementation.md`：可交给另一个对话窗口的下一轮实施任务书
- `docs/validation/template.md`：独立验收与问题复验模板

## 本地启动

以下是当前历史实现的启动方式；其中安装/启动工具仍包含原生 MySQL 路径，尚未改成新的 Docker 默认流程。新一轮实施要求见 [交接任务书](docs/tasks/next-implementation.md)。

1. 检查环境：`powershell -ExecutionPolicy Bypass -File tools/check-environment.ps1`
2. 如果需要自动安装缺失的软件：`powershell -ExecutionPolicy Bypass -File tools/check-environment.ps1 -InstallMissing`
3. Docker Desktop 可用时启动数据库：`docker compose up -d mysql`
4. Docker 引擎不可用时启动本地 MySQL：`powershell -ExecutionPolicy Bypass -File tools/check-environment.ps1 -StartMySql`
5. 启动 API：`dotnet run --project Server/CExam.Api --urls http://localhost:5000`
6. 检查接口：`http://localhost:5000/health`
7. 用 Unity 打开项目并运行 `Assets/Scenes/SampleScene.unity`，场景中的 `ApiHealthProbe` 会显示健康检查结果。

`/health` 返回 `200` 表示 API 和 MySQL 均可用；返回 `503` 表示 API 已启动但数据库尚未连接。

## 阶段范围

第一阶段只包含基础环境、健康检查和本地数据库启动配置，不包含账号、题库、考试、代码评测和成绩业务。具体状态见 [`docs/phase1-implementation.md`](docs/phase1-implementation.md)。

## 多电脑部署计划

本机 A 同时开发客户端和服务端，B 按指定提交独立运行并记录问题，A 修复后 B 复验。两边数据库独立，通过结构迁移和种子数据建立一致的测试条件，不同步整个数据库，也不随 Docker 账号迁移数据。先前共享数据库的方案仅保留为后续集中演示或正式部署选项。详见 [开发与验收方案](docs/multi-computer-deployment.md)。
