# 第一阶段实施与验收记录

更新日期：2026-11-19
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
- 已推送提交：`14b3b5988c1807f26abacab1419d953d40d15fa3`。

## 验收证据

| 项目 | 当前结果 | 说明 |
|---|---|---|
| API 编译 | 已通过 | `dotnet build Server/CExam.Api/CExam.Api.csproj --no-restore`，0 警告、0 错误 |
| API 启动 | 已通过 | 已在 `http://localhost:5000` 启动并访问 `/health` |
| API 无数据库行为 | 已验证 | 当前返回 503，并明确报告 `database: unavailable` |
| MySQL 连接 | 未完成 | 本机尚未安装/启动 Docker 或 MySQL |
| Unity 批处理验证 | 未完成 | Unity 项目已被另一个 Unity 实例占用，批处理启动被拒绝 |
| GitHub 推送 | 已通过 | `main` 已与远程提交同步 |

## 阶段结论

第一阶段的代码实施已完成，但按原文档的全部验收标准尚未完成，因此当前状态为“实施完成，环境验收待补”。完成 MySQL 连通性和 Unity 实际运行验证后，才能将第一阶段标记为“验收完成”。

## 下一步

1. 用户确认安装方式后安装 Docker Desktop（推荐）或 MySQL 8.0。
2. 执行 `docker compose up -d mysql`，检查容器健康和端口 3306。
3. 启动 API，确认 `/health` 返回 200。
4. 关闭当前 Unity 编辑器，重新打开项目并运行 `SampleScene`，确认探针显示 API 结果且 Console 无编译错误。
5. 完成以上验收后再进入第二阶段：账号、用户表、题目表和选择题界面。
