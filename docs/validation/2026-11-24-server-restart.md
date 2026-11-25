# 本地服务端重启验收

- 日期：2026-11-24
- 机器：A
- 操作：启动 Docker Desktop，执行 `powershell -ExecutionPolicy Bypass -File tools/start-local.ps1`
- MySQL：项目容器 `c_program_system-mysql-1`，MySQL 8.4.9，`127.0.0.1:3307`，healthy
- API：`127.0.0.1:5000`，PID 27512
- 结果：`GET /health` 返回 HTTP 200，`{"status":"ok","database":"ok"}`
- 结论：服务端重启通过。客户端若仍显示旧请求失败，需要重新发起请求或重新进入 PlayMode。
