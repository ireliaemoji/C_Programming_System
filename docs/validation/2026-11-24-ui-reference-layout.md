# 完整 UI 预制体基准布局验收

- 日期：2026-11-24；机器：A，Windows / Unity 2022.3.62f3c1。
- Git 基线：`1c6958631a3f45c2645f9a53f0364eaaadab7396`，`main`；工作区包含既有未提交内容，本轮未提交/推送。
- 范围：完整 `UiRoot.prefab`、共享业务绑定、PC/手机设计区适配、ArtAssets 导入与引用。设计见 [基准布局](../design/ui-prefab-reference-layout.md)。
- 为保留用户已有 Unity 编辑器实例，在 `C:/Users/hello/AppData/Local/Temp/CExamClientUiCompile2` 验证。交付前已对实际工作区脚本和 prefab 与隔离副本做 SHA256 一致性检查。

## 结果

| 检查 | 实际操作与结果 | 状态 |
|---|---|---|
| 编辑器脚本与 prefab | batchmode 执行 `ExamUiPrefabBuilder.Build`；生成完整两套页面，编译成功 | 通过 |
| 序列化引用 | `ExamUiValidation.Run` 检查两套 `UiLayoutView` 所有序列化对象引用，模板和按钮不缺失 | 通过 |
| 分辨率与边界 | 2560×1440、1920×1080、1440×900、1280×720、2560×1080、1080×2160、390×844、360×800、430×932；设计区和固定按钮未越出屏幕 | 通过 |
| 离屏渲染 | 26 张 Unity Camera/RenderTexture 截图；首页覆盖九种尺寸，登录/考试列表/作答/成绩/教师/班级/题库/发布覆盖 PC 与手机，并补测手机上下 inset | 通过 |
| 人工视觉复核 | 查看 PC/手机首页、登录、考试、教师/班级和手机发布的实际渲染图，复核字体、卡片、输入框、导航与素材；修正了登录卡片底部留白、手机题号抽屉 | 通过 |
| Windows 隔离播放器 | Mono Development Player 完成初始化、导航、答案选择、交卷确认与取消、下一题、切换 390×844、草稿及答案保留、题号抽屉、教师导航与退出；14 个断言通过，日志无 UI 空引用 | 通过 |
| 正式 SampleScene + API | 未在用户已打开的 Unity 实例上执行完整登录/上传/组卷/交卷接口流程；隔离播放器未请求业务 API | 未执行 |
| Android/iOS、软键盘、真机安全区 | 当前只有分辨率/inset 模拟与 Windows 播放器证据 | 未执行 |
| B 独立验收 | 无 B 端本轮证据 | 未执行 |

隔离副本最初构建播放器时缺少 Burst 的 `bcl.exe`，第一次构建失败；复验仅通过命令行 `--burst-disable-compilation` 关闭该次临时构建的 Burst，未改正式工程的包或编译配置。UI 不依赖 Burst。后续生成、渲染、构建与播放器检查通过。

证据摘要：[构建与渲染](2026-11-24-final-build-validation-player.txt)、[播放器检查](2026-11-24-player-check.txt)。可重复的渲染/引用/边界检查保留在 `Assets/Editor/ExamUiValidation.cs`，菜单为 `CExam/UI/Validate and render reference layouts`。

## 预览

| 页面 | PC | 手机 |
|---|---|---|
| 学生首页 | [1920×1080](ui-previews/StudentHome-1920x1080.png) | [390×844](ui-previews/StudentHome-390x844.png) |
| 考试 | [1920×1080](ui-previews/Exam-1920x1080.png) | [390×844](ui-previews/Exam-390x844.png) |
| 教师首页 | [1920×1080](ui-previews/TeacherHome-1920x1080.png) | [390×844](ui-previews/TeacherHome-390x844.png) |
| 登录 / 安全区 | [PC 登录](ui-previews/Login-1920x1080.png) | [手机安全区](ui-previews/StudentHome-390x844-safe.png) |

截图里的列表使用演示数据，图片不是烘焙 UI。实际控件、交互、数据和状态仍由 Unity 与业务代码控制。

本轮结论：基准布局、素材引用、分辨率渲染和隔离播放器交互通过。真实服务端端到端操作、移动设备与 B 端独立验收仍开放。
