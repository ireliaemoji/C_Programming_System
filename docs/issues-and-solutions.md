# 问题与解决方案记录

更新日期：2026-11-19

## ISSUE-001：本地项目路径与用户描述存在空格歧义

- 现象：用户描述为 `C:\UnityProject C_Client`。
- 原因：实际目录是 `C:\UnityProject\C_Client`。
- 处理：使用实际存在的 Unity 项目目录作为 Git 仓库根目录，并保留该路径记录。
- 状态：已解决。

## ISSUE-002：Unity 项目原本不是 Git 仓库

- 现象：项目目录没有 `.git`，远程仓库只有初始 README 和 `.gitignore`。
- 处理：在 Unity 项目根目录初始化 Git，关联远程 `main`，提交 Unity 项目、API、Docker 配置和文档后推送。
- 状态：已解决。

## ISSUE-003：Docker 与 MySQL 未安装或未加入命令行

- 现象：`docker --version` 和 `mysql --version` 均无法执行。
- 影响：无法启动本地 MySQL，也无法完成 `/health` 返回 200 的数据库验收。
- 处理：本阶段只提交 `docker-compose.yml` 和连接配置，没有擅自安装系统软件。
- 下一步：由用户确认后安装 Docker Desktop（推荐），或安装 MySQL 8.0。
- 状态：待用户决定安装方式。

## ISSUE-004：Unity 批处理验证被已有 Unity 实例占用

- 现象：Unity batchmode 返回“Project already open in another instance”。
- 处理：未强制关闭用户正在使用的 Unity 编辑器，避免丢失未保存内容。
- 下一步：关闭当前 Unity 编辑器后重新执行批处理验证。
- 状态：待复验。

## ISSUE-005：MySqlConnector 2.4.0 NuGet 安全告警

- 现象：`dotnet build` 报告 `NU1902`，对应 GitHub Advisory `GHSA-473q-m89c-ghf8`。
- 风险：公告描述在零配置 TLS 且未验证证书时，攻击者可能诱导连接先发送明文数据库密码。使用本机回环连接、配置 CA 并进行证书校验可避免公告描述的条件，但不应把旧版本带入正式环境。
- 处理：升级到 `MySqlConnector 2.6.1`，重新还原并编译。
- 结果：当前 `dotnet build` 为 0 警告、0 错误。
- 参考：[GitHub Advisory GHSA-473q-m89c-ghf8](https://github.com/advisories/GHSA-473q-m89c-ghf8)、[NuGet MySqlConnector](https://www.nuget.org/packages/MySqlConnector)。
- 状态：已解决；正式环境仍需配置 CA/证书校验和安全连接参数。

## ISSUE-006：第一阶段完成状态容易被误判

- 现象：代码已经提交，但本机依赖和 Unity 运行验收未完成。
- 处理：将状态拆分为“实施完成，环境验收待补”，并在 `phase1-implementation.md` 中记录证据和停止条件。
- 状态：已解决。

## ISSUE-007：Docker Desktop 已安装但引擎无法启动

- 现象：Docker CLI 29.9.3 和 Compose 插件已安装，但 `docker desktop status` 显示 `stopped`，`docker info` 报 Docker Desktop unable to start。
- 原因：当前 PowerShell 不是管理员会话，WSL2 和 Virtual Machine Platform 的状态无法读取或启用；Docker Desktop 的 Linux 后端尚未就绪。
- 处理：保留 Docker Desktop 安装结果；没有强制修改 Windows 可选功能或重启机器。
- 下一步：管理员 PowerShell 执行 `wsl --install --no-distribution` 或启用对应 Windows 功能，重启后再启动 Docker Desktop。
- 后续：用户已执行 VirtualMachinePlatform 和 WSL 功能启用命令，尚未重启；不能据此确认 Docker 已恢复。
- 状态：待重启和引擎/容器验证。

## ISSUE-008：MySQL 安装后未注册 Windows 服务

- 现象：MySQL Community Server 8.4.9 文件已安装，但当前用户没有对应 Windows 服务。
- 原因：安装器在非管理员会话中无法完成服务注册。
- 处理：使用 `C:\Users\hello\AppData\Local\CExam\mysql-data` 初始化用户拥有的开发数据目录，并启动本地 `mysqld`；创建 `c_exam` 数据库和 `exam_app` 开发账号。
- 验证：API `/health` 返回 HTTP 200，响应为 `{"status":"ok","database":"ok"}`。
- 状态：开发环境已可用；如需开机自动启动或系统服务，需要管理员权限单独配置。

## ISSUE-009：Unity 健康探针 meta GUID 无效

- 现象：Unity 日志报告 `ApiHealthProbe.cs.meta` 的 GUID 无效，并将场景中的 `ApiHealthProbe` 组件识别为 Missing Script。
- 原因：前次手写 meta 文件时 GUID 少了 1 位，只有 31 位十六进制字符。
- 处理：生成 32 位 GUID，并同步更新 `SampleScene.unity` 的脚本引用。
- 复验：用户已提供 `ApiHealthProbe.cs:20` 的 `API: {"status":"ok","database":"ok"}` 成功日志。
- 状态：实际场景请求已通过；独立批处理验证仍记录于阶段文档。

## ISSUE-010：Unity 报告 API unavailable

- 现象：`ApiHealthProbe` 输出 `Cannot connect to destination host`。
- 原因：探针请求的是 `http://localhost:5000/health`，但当时 ASP.NET Core API 未启动；MySQL 3306 端口本身正常。
- 处理：启动 `dotnet run --project Server/CExam.Api --urls http://localhost:5000`，随后验证 `/health` 返回 HTTP 200 和 `database: ok`。
- 结论：这是本地 API 进程生命周期问题，不是 Unity 请求代码或 Docker 引擎问题。
- 状态：已解决；每次运行 Unity 前需保持 API 进程运行。

## ISSUE-011：本地 MySQL X Protocol 暴露到所有网卡

- 现象：初始开发实例除了 `127.0.0.1:3306` 外，还监听 `0.0.0.0:33060` 和 `[::]:33060`。
- 原因：MySQL 8.4 默认启用了 X Plugin；即使普通 SQL 连接绑定到回环地址，X Protocol 仍可能监听所有网卡。
- 处理：安全停止开发实例后，以 `--bind-address=127.0.0.1 --mysqlx=0` 重启；同时更新环境工具，后续启动不会再开放 33060。
- 结果：当前只保留 `127.0.0.1:3306`，API `/health` 仍返回 200。
- 状态：已解决。

## ISSUE-012：换电脑后 Docker 不会自动共享数据

- 原因：容器和 named volume 属于各自宿主机，相同 Compose 文件、卷名或 Git 仓库不会同步实际数据库。
- 决定：未来以另一台电脑 B 的 Docker MySQL 为唯一数据源，本机 A 通过 B 的 API 访问；首次迁移后避免两边独立写入。
- 安全边界：个人电脑同样可能暴露端口；当前 `3306:3306` 和示例密码不适合直接共享部署。仅开放受限 API 访问，数据库只在 B 内部可用。
- 状态：方案已记录，尚未部署或开放网络访问。迁移、版本差异、工具调整与验收见 [多电脑部署文档](multi-computer-deployment.md)。
