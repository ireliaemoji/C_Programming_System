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
| API 无数据库行为 | 已验证 | 先前数据库未启动时返回 503，明确报告 `database: unavailable`；当前正常连接时返回 200 |
| MySQL 连接 | 已通过 | MySQL 8.4.9 本地实例运行于 `127.0.0.1:3306`，`/health` 返回 200 |
| Docker 安装 | 已通过 | Docker Desktop 4.99.0 已安装 |
| Docker 引擎 | 待重启验证 | 用户已执行 VirtualMachinePlatform 和 WSL 功能启用命令，尚未重启；引擎和容器启动待验收 |
| Unity 实际请求 | 已通过（用户反馈） | 用户提供 `API: {"status":"ok","database":"ok"}` 和 `ApiHealthProbe.cs:20` 成功日志，证明修复后的脚本实际运行 |
| Unity 批处理验证 | 未完成 | 现有 Unity 实例占用项目，批处理启动被拒绝 |
| GitHub 推送 | 已通过 | `main` 已与远程提交同步 |

## 阶段结论

原始三项功能验收（API 启动、MySQL 连接、Unity 请求健康检查）已通过；Unity 结果来自用户实际运行日志。环境安装要求中的 Docker 仍待重启后的运行验证，批处理编译检查尚未补做，因此不能宣告所有环境验收结束。状态为“本地功能闭环通过，Docker 环境待验收”。

## 下一步

1. 保存工作并重启 Windows，验证 Docker 引擎；功能启用命令已由用户执行。
2. 当前采用 A 开发、B 独立验收，各自运行本机 API 和 Docker MySQL。上一版共享 B 数据库的安排仅保留为后续部署选项，见 [多电脑方案](multi-computer-deployment.md)。
3. 先按 [下一轮交接任务书](tasks/next-implementation.md) 收尾：调整 Docker 默认环境工具、统一数据库版本、处理端口冲突、机密配置和启动验证；两机不迁移整库、不开放远程访问。
4. A 提交可复验版本，B 按 [验收模板](validation/template.md) 独立验证并记录问题。A 的成功结果不等于 B 已通过。
5. Docker 环境收尾完成后进入第二阶段，先实现账号、题库和权限的最小闭环，具体范围另行确认。
