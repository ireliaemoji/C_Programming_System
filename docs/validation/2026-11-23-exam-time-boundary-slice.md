
## 2026-11-23 考试时间边界补强（A 端代码验证）

- 工作区：`C:\Users\hello\Documents\ChatGPT\explore\C_Programming_System`
- 机器：A；本轮未改变数据库和运行中的 API 进程。
- 变更：`server/CExam.Api/ExamApi.cs` 的开考、取题、保存答案、交卷窗口检查；重复题目和时间顺序输入校验。
- 验证：`dotnet build server/CExam.Api/CExam.Api.csproj --nologo -p:UseAppHost=false -o server/CExam.Api/artifacts/build-check`，0 警告、0 错误；独立输出目录随后清理。
- 未执行：过期/未开始/跨班级真实 HTTP 负向测试、Unity UI。
