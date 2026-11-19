# 第一阶段实施与验收记录

更新日期：2026-11-19（复审）
来源文档：`C语言考试系统开发文档.md` 第 10 节“第 1 阶段：基础环境”，以及需求分析与子系统设计文档的已确认规则。

## 目标

第一阶段要求建立 Unity 客户端、ASP.NET Core 服务、Git 仓库和本地 MySQL 开发环境，并达到以下验收标准：

- ASP.NET Core 可以启动；
- Unity 可以访问健康检查接口；
- MySQL 可以连接。

## 已实施

- 将 `C:\UnityProject\C_Client` 初始化为 Git 仓库并关联 `https://github.com/ireliaemoji/C_Programming_System.git`。
- 保留 Unity 2022.3.62f3c1 项目结构和 `SampleScene`。
- 创建 `Server/CExam.Api` ASP.NET Core 最小 API。
- 实现 `GET /health`：连接配置中的 MySQL，执行 `SELECT 1`；成功返回 HTTP 200，数据库不可用返回 HTTP 503。
- 创建 `docker-compose.yml`，定义 MySQL 8.0、本地数据库 `c_exam` 和应用账号。
- 在 `SampleScene` 中加入 `ApiHealthProbe`，运行场景时请求 `/health` 并显示结果。
- 将 `MySqlConnector` 固定为 2.6.1。2.4.0 的安全告警已通过升级处理，详见问题记录。
- 新增 `tools/check-environment.ps1`，支持环境检查、`winget` 安装缺失依赖，以及启动用户拥有的本地 MySQL 开发实例。
- 已安装 Docker Desktop 4.99.0 和 MySQL Community Server 8.4.9。
- 修复 `ApiHealthProbe.cs.meta` 无效 GUID，并同步修复 `SampleScene` 的脚本引用。
- 已推送最新提交：`85c089a`。

## 验收证据

| 项目 | 当前结果 | 说明 |
|---|---|---|
| API 编译 | 已通过 | `dotnet build Server/CExam.Api/CExam.Api.csproj --no-restore`，0 警告、0 错误 |
| API 启动 | 已通过 | 已在 `http://localhost:5000` 启动并访问 `/health` |
| API 无数据库行为 | 已验证 | 当前返回 503，并明确报告 `database: unavailable` |
| MySQL 连接 | 已通过 | MySQL 8.4.9 本地实例运行于 `127.0.0.1:3306`，`/health` 返回 200 |
| Docker 安装 | 已通过 | Docker Desktop 4.99.0 已安装 |
| Docker 引擎 | 未完成 | WSL2/Virtual Machine Platform 尚未启用，Docker Desktop 显示 stopped |
| Unity 脚本资源导入 | 待人工复验 | 已修复无效 GUID；当前编辑器需关闭/重开或刷新 Asset Database |
| Unity 批处理验证 | 未完成 | 现有 Unity 实例占用项目，批处理启动被拒绝 |
| GitHub 推送 | 已通过 | `main` 已与远程提交同步 |

## 阶段结论

第一阶段的代码实施已完成。MySQL 8.4.9 已安装，并已使用本地开发数据目录验证连接，API `/health` 已返回 HTTP 200；健康探针的 Unity GUID 问题已修复，但 Unity 仍需人工刷新/重开后运行验证；Docker Desktop 已安装但 Docker 引擎仍因 WSL2/虚拟化前置条件未就绪而不可用。因此当前状态为“代码完成，MySQL 验收完成，Unity/Docker 验收待补”。

## 下一步

1. 以管理员身份启用 WSL2 和 Virtual Machine Platform，重启后启动 Docker Desktop。
2. Docker 引擎可用时执行 `docker compose up -d mysql`，检查容器健康和端口 3306。
3. 关闭当前 Unity 编辑器，重新打开项目并运行 `SampleScene`，确认探针显示 API 结果且 Console 无编译错误。
4. 完成以上验收后再进入第二阶段：账号、用户表、题目表和选择题界面。
