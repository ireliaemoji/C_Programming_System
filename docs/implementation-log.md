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
