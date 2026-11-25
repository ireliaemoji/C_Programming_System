# 2026-11-23 A 端系统开发首个客户端闭环验收

## 范围

验证当前开发切片：账号注册/登录、会话、已发布单选题查询、答案提交、学生访问教师接口的权限边界，以及 API 编译和 Unity 脚本编译。

## 环境

- 机器：A，本机工作区 `C:\Users\hello\Documents\ChatGPT\explore\C_Programming_System`。
- Unity：2022.3.62f3c1。
- .NET SDK：10.x；MySQL Docker 镜像：8.4.9；Docker Engine：29.9.3。
- MySQL：`127.0.0.1:3307`，API：`http://127.0.0.1:5000`。

## 结果

| 检查项 | 实际结果 | 状态 |
|---|---|---|
| API 编译 | `dotnet build server/CExam.Api/CExam.Api.csproj --no-restore`，0 警告、0 错误 | 通过 |
| 数据库/API 启动 | `tools/start-local.ps1` 等待认证 `SELECT 1` 并启动 API | 通过 |
| 演示账号登录 | `student` 登录成功，返回 Student 会话令牌；令牌长度仅记录为64，不记录内容 | 通过 |
| 题目查询 | Bearer 令牌返回3道已发布单选题，未返回标准答案 | 通过 |
| 答案提交 | 第1题选择2，服务端返回 `isCorrect=true` 并写入答案 | 通过 |
| 错误密码 | 返回 HTTP 401 | 通过 |
| 无效令牌 | 返回 HTTP 401 | 通过 |
| Student→Teacher | `/api/teacher/questions` 返回 HTTP 403 | 通过 |
| Unity 脚本编译 | Unity 编辑器日志记录脚本编译完成且无本轮脚本错误 | 通过 |
| Unity PlayMode | 尚未完成完整的登录、题目展示、选择和提交 UI 操作 | 待执行 |
| B 端复验 | 尚未在修复提交上执行 | 待执行 |

## 结论

A 端服务端最小业务闭环已通过实际接口验证；客户端 UI 代码已落地并完成编辑器编译，但 PlayMode 端到端操作仍需补验。当前结果支持继续完善 UI 和后续业务设计，不代表整个毕业设计系统已完成。
