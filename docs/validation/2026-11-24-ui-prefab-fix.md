# UI 根预制体与空引用修复验收记录

## 验收对象

- 机器代号：A（开发）
- 验收日期：2026-11-24
- 功能/任务：UiRoot 空引用修复和根预制体接入
- 被验证的完整 Git 提交号：工作区未提交；基线 `main`
- 工作区是否干净：否；保留仓库已有未提交修改，本轮相关文件为 `client/Assets/Scripts/UI/UiRoot.cs`、`client/Assets/Resources/UI/UiRoot.prefab` 及对应文档
- Unity：2022.3.62f3c1

## 验收结果

| 测试项 | 操作/命令 | 预期 | 实际结果 | 通过/失败/未执行 |
|---|---|---|---|---|
| 预制体生成 | Unity batchmode 执行 `UiPrefabBuilder.Build` | 生成 `Resources/UI/UiRoot.prefab` 且无空引用异常 | 生成成功，日志出现 `UiRoot prefab generated`，返回码 0 | 通过 |
| 脚本编译 | Unity batchmode 导入隔离客户端 | 无新的 C# 编译错误 | 编译返回码 0；未出现 `error CS` 或 `NullReferenceException` | 通过 |
| 预制体运行时 smoke test | Unity batchmode 执行 `UiPrefabSmokeTest.Run`，加载 prefab 后调用 `SetPage`、`RefreshLayout` | 根节点绑定、导航实例化和布局刷新不抛异常 | 日志出现 `UiRoot prefab smoke test passed`，返回码 0 | 通过 |
| 正式 SampleScene | 在已有 Unity 编辑器中运行 PlayMode | 登录页和页面切换正常 | 未执行；正式编辑器窗口当前不可激活 | 未执行 |

## 本轮结论

部分通过：空引用根因已修复，UI 根结构已改为预制体并完成隔离编译；正式工程 PlayMode 仍待人工复验。
