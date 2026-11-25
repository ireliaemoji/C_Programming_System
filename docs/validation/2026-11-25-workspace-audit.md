# 2026-11-25 工作区改动审查

## 审查基线

- 仓库分支为 `main`，HEAD 为 `1c6958631a3f45c2645f9a53f0364eaaadab7396`，远端为 `origin/main`。
- 当前工作区没有形成包含本轮系统开发的提交。`git status` 同时显示基线文件删除、已跟踪文件修改和大量新文件。
- A 机 API 当前健康检查返回 200。A 机已完成 API 编译、Unity 脚本编译和到期自动交卷接口验证。
- B 机尚未使用本轮系统开发版本完成独立数据库、断网重启和截止前/截止后恢复验收。

## 工作区改动分类

### 1. 仓库结构迁移

- 旧的顶层 Unity 工程路径 `Assets/`、`Packages/` 和 `ProjectSettings/` 被标记删除。
- 当前 Unity 工程位于 `client/`，包含 `Assets/`、`Packages/`、`ProjectSettings/`、PC/手机预制体、ArtAssets、TextMesh Pro 资源和场景。
- 旧的 `Server/CExam.Api/` 是基线已跟踪路径，当前业务服务端代码位于 `server/CExam.Api/`。Windows 不区分大小写，`Server/` 与 `server/` 在当前工作区形成路径碰撞，提交前必须用 Git 的临时路径重命名方式整理。
- `README.md`、`.gitignore`、启动脚本和设计文档已经按 `client/`、`server/` 新结构更新。

### 2. 环境和部署

- Docker Compose 固定 MySQL 8.4.9，仅绑定回环地址。
- `tools/check-environment.ps1` 和 `tools/start-local.ps1` 增加 Unity、.NET、Docker、MySQL 和本机 API 检查及启动逻辑。
- 增加独立业务边界检查脚本 `tools/test-business-boundaries.ps1`。
- `.env`、`.local/`、Unity Library/Temp/Obj/Logs 和 `.NET` 构建目录已经加入忽略规则。当前 `server/CExam.Api/artifacts/` 仍出现在未跟踪列表，提交前应清理或加入忽略规则。

### 3. 服务端业务

- ASP.NET Core API 增加账号注册、登录、内存会话和角色授权。
- 增加题目创建、编辑、CSV/XLSX 兼容导入、可见性和题目分值。
- 增加班级、成员、考试发布、学生分配、时间窗口和考试会话。
- 增加逐题保存、交卷、客观题评分、成绩查询和时间边界检查。
- 增加教师查看答卷、单选题人工给 0 分/满分、成绩开放和学生查看本人错题。
- 增加 `grades_published`、`exam_grade_overrides`、题目分值和答案 `revision` 结构升级。
- 增加 `ExamDeadlineWorker`，服务端按数据库 UTC 时间自动结算到期且仍为 `InProgress` 的答卷。
- 增加服务端时间、答卷状态查询、截止前答案版本校验和截止后写入拒绝。

### 4. Unity 客户端和 UI

- `client/Assets/Resources/UI/UiRoot.prefab` 保存 PC 2560×1440 和手机 1080×2160 两套完整页面。
- 增加 SafeArea、等比设计区缩放、横竖屏布局切换、手机固定竖屏和数据列表模板。
- 增加教师班级、成员、题库、组卷、考试、答卷查看和单选题批改入口。
- 增加学生考试列表、倒计时、逐题作答、成绩和错题回看。
- 增加 `ExamDraftFile`，使用 `Application.persistentDataPath/ExamDrafts` 保存待同步答案。
- 增加服务端时间偏移、答案 revision、请求超时、1/2/5/10/30/60 秒退避补传和截止后状态查询。

### 5. 设计、验收和毕业设计资料

- `docs/design/` 保存系统业务切片、UI 线框、预制体基准和素材审查。
- `docs/tasks/` 保存阶段任务、UI 交接和 B 机考试收尾任务。
- `docs/validation/` 保存环境、业务、UI、批改和考试收尾记录。
- `docs/graduation/` 新增审校后的毕业设计任务书和英文节选翻译文档。
- 这些内容中同时存在本轮业务开发、此前 UI/文档迁移和毕业设计资料交付，提交时需要按目标拆分，避免把无关内容混入收尾提交。

## 当前阶段结论

| 范围 | 状态 | 依据 |
|---|---|---|
| A 机服务端到期自动交卷 | A 机已验证 | 4 秒考试等待 7 秒后，教师答卷列表显示 `Submitted` |
| A 机客户端编译 | 已验证 | Unity 2022.3.62f3c1 batchmode 导入和脚本编译通过 |
| A 机 API 编译 | 已验证 | `dotnet build` 0 警告、0 错误 |
| A 机物理断网重启 | 未执行 | 当前只有代码、编译和 API 级验证 |
| B 机独立数据库迁移 | 未执行 | B 尚未拿到本轮可定位提交 |
| B 机断网、截止前恢复、截止后恢复 | 未执行 | 待 B 机现场验收 |
| 阶段整体收尾 | 待 B 机通过 | A 机结果不能替代 B 机验收 |

## B 机无需远端推送的交付方式

A 机可以先整理并创建本地提交，再使用 Git bundle 交给 B 机。流程如下。

1. A 机把本轮目标文件整理到专用分支或提交，确认 `git show --stat` 只包含本轮目标。
2. A 机执行 `git bundle create CExam-closeout.bundle origin/main..本地分支`。
3. 通过 U 盘、受控共享目录或其他已批准的文件通道把 bundle 和验收任务交给 B 机。
4. B 机从 bundle 创建本地分支，按 `docs/tasks/2026-11-25-B-exam-offline-closeout.md` 重建自己的 Docker MySQL 和 API。
5. B 机把提交号、环境版本、脱敏日志和结果写入新的 B 验收记录。

Git bundle 保留提交和父提交关系，B 机可以审查精确版本。数据库使用 B 机自己的 Docker volume，通过结构迁移和脱敏种子建立，不复制 A 机 `.env`、数据库 volume 或真实答卷。

## 下一阶段目标

B 机通过本阶段后，进入正式身份和账户可靠性阶段。

- 持久化登录会话，支持 API 重启后的重新认证和答卷恢复。
- 本地开发邮件验证替身，覆盖注册验证、找回流程和验证码失效。
- 保留现有用户 ID、角色、考试分配和答卷归属。
- 完成 B 机复验后再扩展填空题、程序设计题、程序评测沙箱和更完整的成绩流程。

当前审查未执行提交、推送、工作区重置或数据库迁移。
