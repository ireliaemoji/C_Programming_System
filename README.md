# C 语言考试系统

第一阶段基础环境已建立：Unity 客户端、ASP.NET Core 健康检查接口和本地 MySQL 配置。

## 目录

- `Assets/`：Unity 客户端（Unity 2022.3.62f3c1）
- `Server/CExam.Api/`：ASP.NET Core API
- `docker-compose.yml`：本地 MySQL 8.0

## 本地启动

1. 启动数据库：`docker compose up -d mysql`
2. 启动 API：`dotnet run --project Server/CExam.Api --urls http://localhost:5000`
3. 检查接口：`http://localhost:5000/health`
4. 用 Unity 打开项目并运行 `Assets/Scenes/SampleScene.unity`，场景中的 `ApiHealthProbe` 会显示健康检查结果。

`/health` 返回 `200` 表示 API 和 MySQL 均可用；返回 `503` 表示 API 已启动但数据库尚未连接。

## 当前验收边界

本次只完成第一阶段基础环境，不包含账号、题库、考试和评测业务。当前机器未检测到 Docker 与 MySQL 命令，因此本地数据库连通性需要安装 Docker Desktop 或 MySQL 8.0 后再验收。
