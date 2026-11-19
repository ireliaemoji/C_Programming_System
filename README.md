# C 语言考试系统

## 当前状态

第一阶段的 API、MySQL 和 Unity 健康检查闭环已验证成功。用户已执行 Windows 虚拟机平台/WSL 功能启用命令，尚待重启后验证 Docker 引擎与容器。未来将以另一台电脑的 Docker MySQL 为唯一数据源，本机通过该电脑的 API 访问；该部署尚未实施，详见多电脑部署文档。

## 目录

- `Assets/`：Unity 客户端（Unity 2022.3.62f3c1）
- `Server/CExam.Api/`：ASP.NET Core API（当前目标框架 `net10.0`）
- `docker-compose.yml`：本地 MySQL 8.0 配置
- `tools/check-environment.ps1`：环境检查、可选依赖安装和本地 MySQL 启动工具
- `docs/phase1-implementation.md`：第一阶段实施与验收记录
- `docs/issues-and-solutions.md`：问题、原因、处理和剩余动作
- `docs/multi-computer-deployment.md`：多电脑数据源安排、迁移与安全边界

## 本地启动

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

计划由另一台电脑运行 API 和 Docker MySQL，本机 Unity 连接其 API。Git 和相同卷名不会自动同步数据库；首次迁移后统一写入部署机数据源。当前 Compose 端口映射、示例密码和原生 MySQL 安装工具仍需在远程部署前调整，不能直接当作已完成安全配置使用。详见 [多电脑访问、数据迁移与安全边界](docs/multi-computer-deployment.md)。
