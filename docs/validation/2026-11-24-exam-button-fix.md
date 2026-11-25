# 考试按钮修复与教师账号验证

日期：2026-11-24

机器：A 端本机

## 已执行

- `dotnet build server/CExam.Api/CExam.Api.csproj --no-restore`，通过，0 警告，0 错误。
- 使用 `teacher / teacher123` 调用 `POST /api/auth/login`，返回 `role=Teacher`，令牌长度为 64。
- 使用教师令牌调用 `GET /api/teacher/classes`，请求成功并返回 1 个班级。
- 静态检查 `UiList.Add` 和 `ExamClientDemo.BeginExam`，确认列表按钮会清理旧监听、恢复 `interactable`，点击时显示开考状态，失败后清理临时状态。

## 尚未执行

- Unity Game 窗口内的真实鼠标点击和考试页面跳转仍需人工复验。本记录不把 API 构建或静态检查等同于 PlayMode 通过。
