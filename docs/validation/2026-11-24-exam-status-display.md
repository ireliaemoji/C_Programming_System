# 学生考试状态显示修复

日期：2026-11-24

## 复现

- 学生账号查询到考试 3、4 的结束时间已早于当前 UTC 时间，开考接口返回 404。
- 私有分配考试 2 的开考接口返回 `status=Submitted`，客户端此前仍继续请求题目。

## 修复

- 服务端考试摘要增加 `attemptStatus`。
- 服务端对已提交会话返回已提交状态。
- 客户端显示并禁用已完成、已结束、未开始考试的按钮，仅可开考状态绑定点击回调。

## 验证

- `dotnet build server/CExam.Api/CExam.Api.csproj --no-restore -p:OutputPath=bin\\VerifyBuild\\` 通过，0 警告，0 错误。
- 已重启本机 API（127.0.0.1:5000），正式端口复验返回考试摘要状态：考试 4 为 `attemptStatus=null` 且开考 404，考试 3、2 为 `attemptStatus=Submitted` 且开考返回 200/Submitted。
- Unity 真实 PlayMode 点击复验仍待执行。
