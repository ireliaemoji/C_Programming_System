# ArtAssets 第二批审查结果

日期：2026-11-23

审查目录：`client/Assets/ArtAssets/`

## 筛选结论

本轮采用新生成的独立资源，删除旧的重复图集、旧中文命名资源和预览拼图。当前目录保留 22 个资源：

| 资源 | 尺寸 | 结论 | Unity 用途 |
|---|---:|---|---|
| `ui_background_mobile.png` | 1080×2160 | 可用 | 手机纯背景；不包含 Header、BottomNav 和文字 |
| `ui_background_pc_preview.png` | 1672×941 | 暂作开发占位 | 比例符合 16:9，但低于 PC 2560×1440 基准；最终版需重新生成 2560×1440 |
| `ui_panel_white.png` | 512×512 | 可用 | 白色九宫格面板 |
| `ui_panel_blue.png` | 512×512 | 可用 | 蓝色九宫格面板 |
| `ui_panel_success.png` | 512×512 | 可用 | 成功状态面板 |
| `ui_panel_warning.png` | 512×512 | 可用 | 提醒/待评分面板 |
| `ui_panel_disabled.png` | 512×512 | 可用 | 禁用状态面板 |
| `ui_button_primary.png` | 512×256 | 可用 | 主按钮背景 |
| `ui_button_secondary.png` | 512×256 | 可用 | 次按钮背景 |
| `ui_button_danger.png` | 512×256 | 可用 | 删除/危险操作按钮背景 |
| `ui_icons_student.png` | 2172×724 | 可用，需切片 | 学生端图标图集 |
| `ui_icons_teacher.png` | 2172×724 | 可用，需切片 | 教师端图标图集 |
| `ui_icons_common.png` | 2172×724 | 可用，需切片 | 搜索、返回、筛选、上传等通用图标 |
| `ui_icons_status.png` | 2172×724 | 可用，需切片 | 考试状态图标 |
| `ui_icons_question_state.png` | 1774×887 | 可用，需切片 | 当前题、已答、未答、标记题 |
| `ui_empty_question_bank.png` | 768×512 | 可用 | 无题库空状态 |
| `ui_empty_no_exam.png` | 768×512 | 可用 | 无考试空状态 |
| `ui_empty_network_error.png` | 768×512 | 可用 | 网络错误空状态 |
| `ui_code_editor_background.png` | 1024×512 | 可用 | 代码编辑区底图；代码和行号由组件生成 |
| `ui_avatar_student_default.png` | 512×512 | 可用 | 学生默认头像 |
| `ui_avatar_teacher_default.png` | 512×512 | 可用 | 教师默认头像 |
| `ui_logo_c.png` | 512×512 | 可用 | Logo 和应用图标 |

## 已删除资源

本轮删除以下旧资源：

- 旧中文命名的 Logo、头像、透明插画；
- 旧学生、教师、学习和时间图标图集；
- `unity_exam_ui_assets_16-19_preview.jpg` 预览拼图；
- 旧背景、固定结构代码编辑器图和旧卡片/按钮图已在上一轮删除。

## 需要后续处理

1. 将 `ui_background_pc_preview.png` 重新生成 2560×1440，并改名为 `ui_background_pc.png` 后再作为最终 PC 背景。
2. 对五张图标图集设置 `Sprite Mode = Multiple`，使用 Sprite Editor 切片；不要把图集整张放入 Image。
3. 对五张面板和三张按钮设置 Sprite Editor Border，使用 `Image Type = Sliced`。
4. 新资源目前没有对应 `.meta`，Unity 重新导入后需要检查平台导入设置；不能在未导入 Unity 的情况下宣称九宫格和移动端压缩已验证。
5. `ui_code_editor_background.png` 只负责底色和边框，代码、行号、评测结果、运行按钮和提交按钮必须由 Unity 控件生成。

## 运行时边界

本轮只完成静态文件筛选、重命名和删除，没有将资源接入当前 `ExamClientDemo`，也没有执行 Unity 导入、Sprite 切片、九宫格预览或 Android/iOS/PC PlayMode 验收。

## 2026-11-24 实施更新

以上筛选与未接入状态为 11 月 23 日的历史记录。当前已制作完整双端页面 prefab 并绑定背景、Logo、学生/教师头像、白/蓝面板、按钮和无考试/无题库插画；面板与按钮已设置九宫格，学生/教师图集完成切片。PNG 内容保持原样。其他图集切片、最终 PC 背景原图和移动发布导入验收仍按需要继续。当前实现与验证见 [基准布局](ui-prefab-reference-layout.md) 与 [验收](../validation/2026-11-24-ui-reference-layout.md)。
