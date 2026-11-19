# 第一阶段实施与验收记录

更新日期：2026-11-19（第一阶段收尾）
来源文档：`C语言考试系统开发文档.md` 第 10 节“第 1 阶段：基础环境”，以及需求分析与子系统设计文档的已确认规则。

## 目标

第一阶段要求建立 Unity 客户端、ASP.NET Core 服务、Git 仓库和本地 MySQL 开发环境，并达到以下验收标准：

- ASP.NET Core 可以启动；
- Unity 可以访问健康检查接口；
- MySQL 可以连接。

## 已实施

### 环境收尾（本轮）

- Compose 固定使用 `mysql:8.4.9`，宿主机端口只绑定 `127.0.0.1`，不发布 33060。
- 新增 `.env.example`；本机 `.env` 由使用者生成并被 Git 忽略，Compose 与 API 启动脚本使用同一套本地变量。
- `tools/check-environment.ps1` 改为默认只读检查实际 Unity、目标 .NET SDK、Docker CLI/Compose/引擎和项目容器状态；`-InstallMissing` 不再安装原生 MySQL。
- 新增 `tools/start-local.ps1`，按超时等待容器内应用账号认证查询，检查 API 已运行/端口冲突后再启动本机 API；`-Stop` 只停止该脚本记录的 API 和 Compose 服务，不删除卷。
- `/health` 的 503 响应不再返回内部异常详情，服务端日志保留诊断信息。

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

### 本轮 A 机收尾检查

详见 [A 机验收记录](validation/2026-11-19-A-0bb9dbc.md)。本机可探测到 Docker CLI/Compose 和 Unity 编辑器，但 Docker 引擎不可用；因此 Docker 容器、容器认证持久化和 Unity 实际场景请求标为未执行，不能用历史原生 MySQL/Unity 用户反馈替代。

## 阶段结论

历史原始三项功能验收（API 启动、原生 MySQL 连接、Unity 请求健康检查）已通过；本轮 Docker 默认环境和失败响应代码已实施，但本机 Docker/Unity 现场验收与 B 机独立验收尚未完成。状态为“环境收尾已提交，A Docker/Unity 与 B 独立验收待执行”。

## 下一步

1. A 机人工安装/启动 Docker Desktop 和对应 Unity 版本后，按验收记录补做 Docker、持久化和 SampleScene 请求。
2. B 按同一提交和 [验收模板](validation/template.md) 独立启动 `.env`、容器、API 与 Unity，记录实际结果；A 的结果不等于 B 已通过。
3. Docker 环境收尾和 B 验收完成后，再另行确认第二阶段账号、题库和权限的最小闭环。
