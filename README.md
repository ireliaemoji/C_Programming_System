# C 语言考试系统

## 当前状态

第一阶段基础环境的代码和仓库结构已建立，提交已推送到 GitHub。阶段验收仍有两项本机环境工作未完成：启动 MySQL 并验证数据库连接，以及关闭现有 Unity 编辑器实例后完成一次 Unity 批处理编译/运行验证。

## 目录

- `Assets/`：Unity 客户端（Unity 2022.3.62f3c1）
- `Server/CExam.Api/`：ASP.NET Core API（当前目标框架 `net10.0`）
- `docker-compose.yml`：本地 MySQL 8.0 配置
- `docs/phase1-implementation.md`：第一阶段实施与验收记录
- `docs/issues-and-solutions.md`：问题、原因、处理和剩余动作

## 本地启动

1. 安装并启动 Docker Desktop，确认 `docker --version` 可用。
2. 启动数据库：`docker compose up -d mysql`
3. 启动 API：`dotnet run --project Server/CExam.Api --urls http://localhost:5000`
4. 检查接口：`http://localhost:5000/health`
5. 用 Unity 打开项目并运行 `Assets/Scenes/SampleScene.unity`，场景中的 `ApiHealthProbe` 会显示健康检查结果。

`/health` 返回 `200` 表示 API 和 MySQL 均可用；返回 `503` 表示 API 已启动但数据库尚未连接。

## 阶段范围

第一阶段只包含基础环境、健康检查和本地数据库启动配置，不包含账号、题库、考试、代码评测和成绩业务。具体状态见 [`docs/phase1-implementation.md`](docs/phase1-implementation.md)。
