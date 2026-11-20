# 实施日志

更新日期：2026-11-19
本文件从建立项目协作规则这一轮开始记录；此前的安装、健康检查、GUID 修复等历史见 [第一阶段记录](phase1-implementation.md) 和 [问题记录](issues-and-solutions.md)，不补造未执行的验收。

## 2026-11-19：建立项目级 AGENTS 与文档同步规则

- 需求：为本项目后续对话建立规则，讨论确认内容及实际实施必须记录并同步文档。
- 实施基线：`97cbd71`；本轮交付提交可通过本文件的 Git 历史定位。
- 变更：新增根目录 `AGENTS.md`，规定开始前读取、文档分工、状态/证据、敏感信息与 Git 边界；新增决定记录与本日志；README 和下一轮任务书增加规则入口。
- 相关文件：`AGENTS.md`、`docs/decisions.md`、`docs/implementation-log.md`、`README.md`、`docs/tasks/next-implementation.md`。
- 验证结果：本轮文档差异检查通过；9 份 Markdown 文档的本地相对链接均可解析；规则与现有 A 开发、B 独立验收方案一致。未运行无关业务测试。
- 状态：文档规则已实施；不包含代码、数据库或 Docker 环境变更。
- 遗留：新对话需在本仓库启动并读取规则；仓库外对话需显式读取。不把 A 的历史验证结果记成 B 已完成。
- 保护边界：用户已有 `ProjectSettings/ProjectSettings.asset` 修改保留，本轮不提交。

## 2026-11-19：第一阶段环境收尾与独立验收交接

- 需求：按 `docs/tasks/next-implementation.md` 完成 Docker 默认环境、共享本机配置、启动检查与独立验收交接；不开始登录、题库或考试业务。
- 实施：Compose 固定 `mysql:8.4.9`，只绑定 `127.0.0.1`，增加容器健康检查；新增 `.env.example` 并忽略 `.env`、`.local/`；API 503 响应移除异常详情并保留服务端告警日志；重写 `tools/check-environment.ps1` 为只读环境检查/显式安装，新增 `tools/start-local.ps1` 负责加载配置、等待认证 `SELECT 1`、处理端口冲突和启动/停止 API。
- 文档：同步 README、`docs/multi-computer-deployment.md`、`docs/phase1-implementation.md`、`docs/decisions.md`、`docs/issues-and-solutions.md`、`docs/tasks/next-implementation.md` 和 A 机验收记录。
- 数据边界：未操作父目录中用户未跟踪资料；未修改或提交 `ProjectSettings/ProjectSettings.asset` 的用户改动；未删除 Docker 卷、原生 MySQL 数据或强制关闭已有进程。
- 验证：静态 PowerShell 语法检查、`dotnet build --no-restore`、API 503 脱敏响应检查和文档链接检查；Docker/Unity 实际运行受本机缺少 Docker CLI/Unity 可探测安装阻塞，详见验收记录。
- 状态：代码与文档已实施，提交/推送后交 B 独立验收；B 记录不得由 A 结果代填。

## 2026-11-19：补充本机 MySQL 端口冲突提示

- 实施：`tools/start-local.ps1` 在启动 Compose 前只读检查宿主机端口；发现不是项目 MySQL 容器占用时立即提示修改 `MYSQL_HOST_PORT`，不停止未知进程。
- 验证：PowerShell 解析通过，后端重新编译 0 警告、0 错误；Docker 引擎不可用，真实端口占用分支待现场执行。
- 提交：代码提交 `31227c7`；本记录随后同步。

## 2026-11-20：A 机 Docker 环境现场验证

- 环境：执行 `wsl --update`、`wsl --shutdown` 成功；重启 Docker Desktop 后 `docker info` 返回 `29.9.3`。
- 实际验证：生成未跟踪的本机 `.env`，使用 `MYSQL_HOST_PORT=3307`；`mysql:8.4.9` 容器健康，认证 `SELECT 1` 成功；临时持久化记录在 `docker compose restart mysql` 后仍存在并已删除；API `/health` 返回 HTTP 200 和 `database: ok`。
- 代码修复：发现 Docker Compose 的 stderr 进度和 PowerShell 原生退出码处理会让启动脚本误报失败，已在 `30bc13b` 修复并重新通过脚本解析。
- 未执行：Unity SampleScene 实际请求；当前 `C:\UnityProject\C_Client` 已有 Unity 编辑器实例，未强制关闭或批处理启动。
- 状态：A Docker/API/持久化已验证；Unity 和 B 机验收待完成。

## 2026-11-20：A 机 Unity 健康请求验收

- 用户反馈：在 Unity `SampleScene` 运行后，Console 输出 `API: {"status":"ok","database":"ok"}`，调用位置为 `Assets/Scripts/Infrastructure/ApiHealthProbe.cs:20`。
- 结论：A 机第一阶段的 API、Docker MySQL、持久化和 Unity 健康请求均有证据；B 机仍必须使用自己的环境独立复验。
