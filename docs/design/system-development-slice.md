# 系统开发首个客户端闭环设计

日期：2026-11-23

## 目标

在现有 SampleScene 上跑通一条可演示的客户端业务链：注册或使用演示账号登录，服务端创建/验证会话，客户端获取已发布单选题，显示题干和四个选项，提交答案并显示判题结果。

毕业设计任务书仍以整个 C 语言考试系统为范围；本文只描述当前开发批次的落地切片，不缩减任务书中的正式考试、程序评测和成绩管理目标。

## 当前实现

### 服务端

- `POST /api/auth/register`：创建 Student 账号，密码使用 PBKDF2-SHA256 哈希保存。
- `POST /api/auth/login`：验证账号并返回 8 小时有效的内存会话令牌。
- `GET /api/questions`：要求 Bearer 令牌，只返回已发布题目的题干和选项，不返回标准答案。
- `POST /api/questions/{id}/answer`：要求 Bearer 令牌，服务端判断选项并以幂等键保存答案。
- `GET /api/teacher/questions`：保留角色边界检查，Student 返回 403，Teacher 才能进入后续教师功能。
- `POST/PUT /api/teacher/questions`：教师创建和修改单选题，服务端校验正确选项和发布状态。
- `POST /api/teacher/questions/import`：教师上传 Excel 兼容 CSV，服务端先完整解析和校验，再以事务写入教师私有题库。
- `POST /api/teacher/exams`：教师用已发布题目组建并发布最小考试。
- `GET /api/exams`、`POST /api/exams/{id}/start`：学生查看考试并创建唯一考试会话。
- `GET /api/exam-attempts/{id}`：学生查询答卷状态和服务端时间，用于截止后的重连确认。
- `GET /api/exam-attempts/{id}/questions`、`POST /api/exam-attempts/{id}/answers`、`POST /api/exam-attempts/{id}/submit`：学生获取题目、保存答案和一次性交卷，服务端完成客观题评分。
- `POST/GET /api/teacher/classes`、`POST /api/teacher/classes/{id}/members`：教师创建班级并维护学生成员。
- `GET /api/teacher/exams/{id}/attempts`、`GET /api/teacher/attempts/{id}/answers`：教师查看自己发布考试的学生答卷列表和逐题答案，服务端校验考试归属。
- 考试可绑定班级或学生，并设置 UTC 开始/结束时间；服务端在开考、保存答案和交卷时检查时间窗口。
- 学生只可通过 `GET /api/grades` 及其答卷明细接口查看教师已开放的本人结果。
- 教师按考试归属查看和批改已交卷答卷，人工分值与总分在同一事务写入。题库分值在组卷时保存到试卷。开放成绩由服务端按数据库时间检查截止，开放后拒绝继续改分。细节见 [D-020](../decisions.md)。
- 首次访问业务接口时创建 `app_users`、`questions`、`question_answers` 表并写入三道演示单选题；对应的首个结构迁移保存在 `server/CExam.Api/Database/001_exam_core.sql`。演示账号为 `student / student123`，仅用于本地开发。

### Unity 客户端

`ExamClientDemo` 挂载在 `SampleScene`，实例化 `Resources/UI/UiRoot.prefab`。完整 PC/手机页面、导航、表单、题目选项、确认框和列表模板都已保存在预制体中；运行时只绑定事件、文本和业务数据，不动态拼装固定控件。使用 Unity 内置 Text/InputField，TMP Essentials 不作为运行前置条件。

当前客户端覆盖角色登录/注册、教师班级/成员/题库上传/组卷/学生答卷查看、学生考试列表/时间窗口/倒计时/逐题作答、自动保存、交卷和成绩。独立 UI 层与实际素材、缩放规则详见 [完整预制体基准布局](ui-prefab-reference-layout.md)。

## 数据和安全边界

- Unity 只保存短期会话令牌，不连接 MySQL。
- 标准答案只在服务端查询，题目接口不下发答案。
- 题目通过 `visibility` 区分 `TeacherPrivate`、`ClassOnly` 和 `PublicPractice`；公开练习接口只返回 `PublicPractice`。
- 考试必须创建学生分配关系；学生只能看到分配给自己的考试，不能通过公开题库读取教师私有题。
- 注册账号统一为 Student；Teacher 权限不由客户端选择产生。
- 会话当前保存在 API 进程内存，重启 API 会使令牌失效。正式身份框架仍待后续阶段。
- 考试答案在服务端按 revision 保存，客户端在 Application.persistentDataPath 保存待同步草稿。服务端后台结算截止答卷，截止后的写入会被拒绝。
- 本地演示密码只写在开发 UI 默认值和文档中，禁止作为生产凭据使用。
- 导入模板位于 `docs/templates/question-bank-import.csv`，Excel 可直接打开和另存；教师工作台的文件路径和上传按钮是可选项，通过 multipart/form-data 上传到服务端。服务端按扩展名解析 Excel 兼容 CSV 或 `.xlsx` 第一个工作表。

## 实施顺序

1. 在 A 端完成 API 编译和数据库连接验证。
2. 启动 API 与 MySQL，验证注册、登录、题目查询、答案提交和 Student→Teacher 403。
3. 在 Unity SampleScene 验证登录、教师/学生工作台、考试、自动保存和成绩页面。
4. 用 `tools/test-business-boundaries.ps1` 验证过期、未开始、跨班级和角色越权边界。
5. 后续再按任务书扩展程序评测和持久化身份会话。

## 预期效果

A 端可从空数据库启动服务，完成注册/登录、教师班级与组卷、学生开始考试、服务端时间校准、自动保存、断线待同步、到期交卷、交卷评分和成绩查看；服务端数据可在 MySQL 中复查，客户端无法直接修改标准答案或成绩。程序评测和正式身份持久化仍属于后续范围。B 机断网恢复仍待独立验收。

## 当前限制

本轮代码已完成并通过 API 编译；A 端 Docker/MySQL/API 已完成教师建题、组卷、学生开考、保存答案、到期交卷和交卷评分验证。UI 完整预制体已完成九种尺寸渲染与隔离 Windows 播放器的控件/答案/切屏状态检查；物理断网、移动真机和 B 端仍待独立复验，详见 [UI 验收](../validation/2026-11-24-ui-reference-layout.md) 与 [考试收尾验收](../validation/2026-11-25-exam-offline-closeout.md)。

## 双端 UI 适配基线与首版实现

本项目客户端只面向 PC 与手机。PC 设计基准为 2560×1440，手机设计基准为 1080×2160 且固定竖屏；Android 与 iOS 共用业务状态和页面语义。其他窄屏、宽屏和不同 DPI 设备按统一缩放比例适配，通过锚点、安全边距和滚动容器避免拉伸或裁切，不为每种物理分辨率制作一套独立页面。

UI 层采用工程内独立实现，不引入 UnityProject 的框架包。`UiRoot` 只实例化/切换已制作的两套布局，`UiLayoutView` 保存固定页面引用，`UiList`/`UiListItem` 绑定模板条目，`UiSafeArea` 和 `UiDesignFit` 完成安全区及完整设计区的统一缩放；ArtAssets Sprite 直接序列化到 prefab 中。此前运行时 `UiFactory` 方案已替换并移除。

横屏/竖屏选择 PC/手机分支，使用 2560×1440 / 1080×2160 的完整初始布局；按安全区可用宽高的较小比例等比适配，背景单独铺满，九宫格保留面板四角。考试作答状态共用，切换布局复制输入框草稿。当前禁用尚无业务实现的学生题库/自主练习入口。

页面适配建议如下：

| 页面/流程 | PC | 手机竖屏 |
|---|---|---|
| 登录、绑定、注册 | 居中卡片，辅助说明可并列 | 单列卡片，表单分步，输入框和主按钮占满可用宽度 |
| 学生首页 | 左侧导航或顶部导航，继续练习和考试信息可两栏 | 单列信息流，底部导航承载首页、题库、练习、成绩、设置 |
| 题库/练习 | 搜索、筛选、列表和详情可左右分栏 | 搜索栏固定在顶部，列表卡片，详情进入独立页面 |
| 考试客观题 | 题干区 + 题号侧栏；题号列表使用左侧滑入抽屉 | 顶部显示题目/计时，题号使用底部或全屏抽屉，题目单列滚动 |
| 程序题 | 题面、代码编辑器、评测结果三栏或上下分区 | 题面、编辑器、结果分步切换；键盘弹出时编辑区仍可滚动 |
| 成绩与错题 | 总分、分题结果、评语和统计可并列 | 总分摘要优先，分题详情和评语折叠展开 |
| 教师工作台 | 导航 + 数据表格/筛选 + 详情面板 | 卡片列表、筛选抽屉、编辑页面；批量操作降低优先级 |

所有端统一处理 SafeArea、滚动容器、加载、错误重试、空状态、待评分和提交确认。考试倒计时、自动保存、交卷和服务端判分属于共享业务状态；视觉布局可以变化，状态机和接口契约不应在端上分叉。

页面级当前线框见 [`ui-layout-detailed-v3.svg`](ui-layout-detailed-v3.svg)，覆盖登录、学生首页、题库/练习、考试、成绩和教师工作台及教师关键子页面。此前的 `ui-layout-detailed.svg` 和 `ui-layout-sketch.svg` 保留为历史讨论稿，不能作为当前分辨率基线。组件尺寸、颜色、字体和交互动画仍待视觉规范确认。
