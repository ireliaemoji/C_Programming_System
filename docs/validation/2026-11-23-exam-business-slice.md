# 2026-11-23 A 端题库组卷与客观题考试业务层验证

## 环境

- 工作区：`C:\Users\hello\Documents\ChatGPT\explore\C_Programming_System`
- Docker MySQL：8.4.9，`127.0.0.1:3307`；API：`http://127.0.0.1:5000`。
- API 编译：`dotnet build server/CExam.Api/CExam.Api.csproj --no-restore`，0 警告、0 错误。

## 实际验证

| 项目 | 结果 | 状态 |
|---|---|---|
| 教师登录 | `teacher` 账号登录为 Teacher | 通过 |
| 教师题库 | 查询原有3题，新增第4题成功 | 通过 |
| 教师组卷 | 创建包含4题的已发布考试成功 | 通过 |
| 学生考试列表 | 学生可获取1场考试 | 通过 |
| 开始考试 | 创建唯一 InProgress 会话，返回4题 | 通过 |
| 保存答案 | 4道答案均可保存，重复保存使用同一题目键 | 通过 |
| 交卷评分 | 交卷返回总分1，重复交卷不再重复提交 | 通过 |
| 题目隔离 | 教师私有题不出现在学生公开题目列表，教师可在自己的题库看到 | 通过 |
| 考试分配 | 未分配学生看不到考试；分配 student 后可看到并开考 | 通过 |
| 导入 | 上传 `docs/templates/question-bank-import.csv`，事务导入2题，默认写入 TeacherPrivate | 通过 |
| Unity UI | 本轮未扩展 UI；业务接口待后续接入 Canvas | 未执行 |

## 范围限制

本轮完成客观题考试业务层、题目可见性、学生分配和 Excel 兼容 CSV 导入，不包含倒计时、断线恢复、程序评测、成绩查询和正式教师界面。测试使用本机开发数据库中的演示账号和题目，不代表 B 端验收。
