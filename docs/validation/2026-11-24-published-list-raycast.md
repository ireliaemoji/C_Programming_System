# PublishedList 射线与班级时间考试复验

日期：2026-11-24

## 已执行

- 检查 PC/手机 `UiRoot.prefab`，确认两套 `PublishedList` 视口 Image 原为 `raycastTarget=1`。
- 将两套 `PublishedList` 视口 Image 改为 `raycastTarget=0`，并同步修改 `ExamUiPrefabBuilder.ScrollContent`。
- 使用本机 API 的 `teacher / teacher123`、班级 ID 1 和题目 ID 1 创建带 UTC 开始/结束时间的考试，接口返回考试 ID 4。
- `dotnet build server/CExam.Api/CExam.Api.csproj --no-restore` 通过，0 警告，0 错误。

## 尚未执行

- Unity 重新导入后的真实鼠标点击、客户端班级时间考试表单提交及学生端开考仍需 PlayMode 复验。
