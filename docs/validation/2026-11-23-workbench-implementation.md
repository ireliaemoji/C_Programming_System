# 2026-11-23 教师/学生工作台实施验收

## 已实施

- 服务端新增 `GET /api/teacher/exams`，并让学生考试摘要携带 `startAt`、`endAt`。
- Unity `ExamClientDemo` 运行时创建 Canvas/TextMeshPro UI。
- 教师：班级创建、成员加入、题库查看、按学生/班级组卷、考试列表。
- 学生：考试列表、时间窗口显示、倒计时、逐题答题、5 秒自动保存、手动/超时交卷、成绩页面。
- 新增 `tools/test-business-boundaries.ps1`，预期覆盖过期、未开始、跨班级和角色越权。

## 验证状态

| 项目 | 状态 |
|---|---|
| API 构建 | 通过，0 警告、0 错误 |
| Unity Canvas/TMP 代码 | 已实施，待 Unity 编辑器重新导入/PlayMode 验收 |
| 过期/未开始负向 HTTP | 脚本已实施，待重启新 API 后执行 |
| 跨班级/角色越权 HTTP | 脚本已实施，待重启新 API 后执行 |
| A/B 独立现场 | 待执行 |

本记录只说明实施和当前验证边界，不把未执行的运行验收标为通过。

## 2026-11-23 Unity 插值错误复验

- 用户反馈：`ExamClientDemo.cs` 第79、108、140行出现 CS8076、CS8361、CS1003。
- 修复：班级和选项按钮文本先计算 `mark`，再用于字符串插值。
- 复验：Unity 编辑器重新导入脚本；修复后日志未出现新的三类错误。日志中的同类错误均位于修复前记录。
- 其他：API `dotnet build` 0 警告、0 错误；尚未完成 PlayMode 全流程验收。

## 2026-11-23 CS7036 复验补记

- 用户反馈：多处 `Text` 调用触发 CS7036。
- 修复：新增四参数 `Text` 重载。
- 复验：Unity 最新脚本导入记录之后未出现 CS7036 或新的 C# 编译错误。

## 2026-11-23 UI 文字和客户端导入修复

- 用户反馈：TMP Essentials 弹窗未导入时界面无文字；题库导入应为客户端可选上传，不应依赖编辑器导入。
- 修复：运行时改用 Unity 内置 Text/InputField；教师工作台增加可选文件路径和上传按钮，调用 `/api/teacher/questions/import`。
- 当前格式：服务端现有解析器接受 Excel 兼容 CSV；原生 `.xlsx` 尚未实现。
- 复验：Unity 最新导入记录后未出现 `ExamClientDemo.cs` 的 C# 编译错误；上传实际操作待 PlayMode 验收。

## 2026-11-23 客户端文件上传与可选导入

- UI 不再依赖 TextMeshPro Essentials；运行时文字使用 Unity 内置 Text/InputField 和 Arial 内置字体。
- 教师工作台提供可选文件路径和上传按钮，普通建班、组卷流程无需导入文件。
- 服务端按文件名支持 Excel 兼容 CSV 与 `.xlsx`；`.xlsx` 读取第一个工作表并复用九列题库模板校验。
- API 构建：0 警告、0 错误。Unity 最新脚本导入后未出现本脚本 C# 编译错误；真实上传流程待执行。

## 2026-11-23 内置字体错误修复补记

用户反馈 Unity 运行时拒绝 `Arial.ttf`。已改用 `LegacyRuntime.ttf`，保留内置字体方案，不依赖 TMP Essentials 或外部资源。PlayMode 重新运行待执行。

## 2026-11-23 Canvas rebuild 警告修复补记

- 修复动态 UI 同步销毁 Placeholder 导致的 Canvas rebuild loop 警告。
- 页面切换统一经过下一帧队列；PlayMode 交互复验待执行。
