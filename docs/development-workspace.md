# 唯一开发工作区

更新日期：2026-11-23

## 当前约定

本项目只使用当前远端仓库对应的工作区开发：

- 本机当前工作区：`C:\Users\hello\Documents\ChatGPT\explore\C_Programming_System`
- 远端：`https://github.com/ireliaemoji/C_Programming_System.git`
- 分支：`main`
- 当前基线：`1c6958631a3f45c2645f9a53f0364eaaadab7396`

API、Unity、数据库脚本、任务书和所有后续开发都只在该工作区进行。新的对话开始前先执行 `git rev-parse --show-toplevel` 和 `git remote get-url origin`，确认没有切换到另一份同仓库副本。

## 旧工作区处理

`C:\UnityProject\C_Client` 已按用户要求删除，不再作为开发目录。删除前它位于 `cb29693503ecdbe61552a40b564425b96588ec2f`，同步远端后比当前 `origin/main` 落后11个提交，并有未提交的 `client/ProjectSettings/ProjectSettings.asset` 修改。

不得再创建第二份并行 checkout 或运行另一套 API。若以后发现旧副本备份中有需要取回的改动，先单独比较并人工确认，再以补丁方式迁移到唯一工作区。

## 运行约定

- Unity 项目从当前工作区的 `client/ProjectSettings/ProjectVersion.txt` 打开。
- API 从当前工作区的 `server/CExam.Api` 启动。
- Docker Compose、`.env` 和 MySQL 卷只使用当前工作区的配置；A/B 数据库仍保持独立。
- 任何开发结果先在当前工作区验证，再提交到当前远端分支；不在两个副本之间并行开发。

旧的 `explore\_repo_inspect2` 内容已迁移到当前目录；如系统仍显示同名空目录，属于当前工具的目录句柄尚未释放，不含第二份代码，不得重新使用。

仓库结构约定：`client/` 和 `server/` 是系统级仓库的两个同级产品目录；`tools/`、`docs/`、Docker 配置属于仓库级内容。
