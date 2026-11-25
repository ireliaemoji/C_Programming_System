# UI 预制体基准布局

日期：2026-11-24。依据：用户要求完整页面预制体、采用已确认的 ArtAssets、先排好基准布局再做分辨率适配；页面语义沿用 [v3 线框](ui-layout-detailed-v3.svg)。本记录替代此前“根 prefab 加运行时控件工厂”的实现方式。

## 工程结构

运行入口仍为 `SampleScene` 上的 `ExamClientDemo`，只实例化 `Resources/UI/UiRoot.prefab`。预制体保存 Canvas、背景、安全区、PC/手机两套完整视图、EventSystem，以及各页面的固定控件。`ExamClientDemo` 为这些引用绑定回调、文本和业务数据，不再创建 GameObject、Text、Button 或 InputField。

每套视图包含登录、学生首页、考试列表、考试作答、成绩、教师总览、班级成员、教师题库、组卷发布、设置和交卷确认框。考试、成绩、班级、题库和题号列表克隆预制体内部的 `ItemTemplate`；空状态插画、条目布局、按钮背景也在模板中保存。未实现的自主练习、学生题库入口保持禁用，不扩展相关 API。

`Assets/Editor/ExamUiPrefabBuilder.cs` 是编辑器制作/重建工具，不会进入运行时。正常调整可直接在 Prefab Mode 修改控件；手动执行重建菜单会按源码中的尺寸重新生成预制体，因此自定义过 prefab 后不要无意覆盖。`Assets/Editor/ExamUiValidation.cs` 提供可重复的离屏渲染和引用/边界检查。

## 基准尺寸

| 区域 | PC：2560×1440 | 手机：1080×2160 |
|---|---|---|
| Header | 144 高，Logo、标题、账号和头像 | 160 高，Logo、标题和头像 |
| 导航 | 左侧 340 宽，固定六个入口 | 底部 152 高，固定六个入口 |
| 页面区 | x=410、y=200，2070×1140 | x=40、y=210，1000×1700 |
| 学生首页 | 横向主卡片、四个并列快捷入口 | 主卡片、两列快捷入口 |
| 考试 | 题号侧栏与题目卡分栏 | 题号抽屉、单列题目与选项 |
| 教师表单 | 输入与操作并列，发布表单两列 | 输入与操作分行，发布表单单列可滚动 |

固定控件的锚点、尺寸、字号和位置保存在 prefab 中。`CanvasScaler` 使用 `ScaleWithScreenSize / Expand`；`UiDesignFit` 参考 Solitaire `TaskBarWindowLayout` 的安全区内统一缩放思路，按 `min(可用宽/设计宽, 可用高/设计高)` 缩放完整设计区，保留初始布局和宽高比。背景独立铺满屏幕；窄屏和超宽屏可以留白，不挤压文字或改变按钮比例。`UiSafeArea` 使用归一化安全区锚点，两个视图共用同一份业务状态。切换布局复制表单输入，题号/已选答案仍由业务控制器持有。

布局选择以 Unity 当前画面的宽高为准。高大于宽时使用手机布局，宽大于或等于高时使用 PC 布局。移动端启动时请求竖屏。Windows 项目设置关闭了手动拖动窗口大小。

## 素材绑定

- PC / 手机背景：`ui_background_pc_preview` / `ui_background_mobile`。PC 仍沿用已确认可用于开发的 1672×941 图，不宣称它已替换为最终 2560×1440 原图。
- Header / 登录：`ui_logo_c`、学生/教师默认头像。
- 白色卡片与主卡片：`ui_panel_white` / `ui_panel_blue`。
- 主/次/危险按钮：`ui_button_primary` / `ui_button_secondary` / `ui_button_danger`。
- 导航：学生图集切为 6 个 Sprite；教师图集切为 8 个 Sprite，按当前角色替换图标。
- 空列表：无考试/题库插画。
- 面板 Border=64、按钮 Border=48，使用 `Image.Type.Sliced`，不拉伸四角。PNG 内容保持原样。

验证结果与限制见 [基准布局验收](../validation/2026-11-24-ui-reference-layout.md)。离屏截图使用演示数据，不代表真实数据库/客户端端到端业务验收。
