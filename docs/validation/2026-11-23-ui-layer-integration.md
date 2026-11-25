# UI 层接入首版验收记录

## 验收对象

- 机器代号：A（开发）
- 验收日期：2026-11-23
- 功能/任务：工程内独立 UI 层首版接入
- 被验证的完整 Git 提交号：工作区未提交；基线 `main`，实际修改保留在本地审阅
- 工作区是否干净：否；仓库已有大量客户端/服务端/文档未提交修改，本轮相关文件为 `client/Assets/Scripts/Infrastructure/ExamClientDemo.cs`、`client/Assets/Scripts/UI/` 及 UI 文档记录
- 操作系统、Unity、.NET SDK、Docker/Compose、MySQL 镜像版本：Windows；Unity 2022.3.62f3c1；其余与本轮 UI 编译无关
- 数据库结构迁移版本/种子数据版本：不适用
- 测试数据来源：不适用

## 验收结果

| 测试项 | 操作/命令 | 预期 | 实际结果与脱敏证据 | 通过/失败/未执行 |
|---|---|---|---|---|
| UI 脚本隔离编译 | 将最小客户端复制到系统临时目录后运行 Unity batchmode 导入 | 脚本导入完成且无新的 C# 编译错误 | Unity 2022.3.62f3c1 完成资源导入和脚本编译，日志未出现 `error CS`、`Scripts have compiler errors` 或新的编译失败记录 | 通过 |
| 正式 SampleScene 编译 | 使用当前已打开的正式 Unity 工程 | 可在同一工程取得最新编译日志 | 未执行；正式工程被已有 Unity 编辑器实例占用，未关闭或抢占实例 | 未执行 |
| Student/Teacher PlayMode | 登录、页面切换、考试和教师工作台操作 | 页面壳、导航、业务状态正常 | 未执行 | 未执行 |
| PC/手机适配 | 按任务书尺寸切换横屏/竖屏 | SafeArea、滚动内容和导航不裁切 | 未执行 | 未执行 |

## 剩余人工事项

- 在当前 Unity 编辑器中等待脚本重新导入后运行 `SampleScene`；检查 Console、登录页文字、页面切换和 Canvas rebuild 警告。
- 用至少一组带安全区 inset 的手机模拟器或真机复核 1080×2160 基准下的底部导航与输入框。

## 本轮结论

部分通过：独立 UI 层代码已接入并取得隔离 Unity 编译证据；正式工程 PlayMode、分辨率和 SafeArea 仍未验收。
