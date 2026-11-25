# 2026-11-23 系统级仓库结构验收

## 目标

确认系统级仓库、客户端、服务端、仓库级工具和文档的目录边界清晰，且迁移后启动路径可用。

## 结果

| 检查项 | 实际结果 | 状态 |
|---|---|---|
| 仓库目录 | 本地目录为 `C_Programming_System`，远端仓库名称一致 | 通过 |
| 客户端 | Unity 工程位于 `client/Assets`、`client/Packages`、`client/ProjectSettings` | 通过 |
| 服务端 | ASP.NET Core 位于同级 `server/CExam.Api` | 通过 |
| 仓库级内容 | `tools/`、`docs/`、`docker-compose.yml` 位于仓库根级 | 通过 |
| 毕设文档 | 任务书和翻译位于 `docs/graduation/` | 通过 |
| 环境检查 | 正确发现 `client/ProjectSettings/ProjectVersion.txt`、.NET 10、Docker 引擎 | 通过 |
| API 启动 | 新目录启动 MySQL 和 API，登录及题目查询通过 | 通过 |
| 旧副本 | `C:\UnityProject\C_Client` 和旧本地 `C_Client` 已删除 | 通过 |

## 结论

仓库已按系统级项目整理，客户端和服务端为同级产品目录，工具、文档和基础设施配置保持仓库级归属。
