
# LeereRiss

独立制作的迷你游戏项目（半成品，很多没实现）。

---

## 说明

- 名称：LeereRiss

- 作者：dieWehmut

- 仓库（源码）：[GitHub - dieWehmut/LeereRiss](https://github.com/dieWehmut/LeereRiss)

- 在线试玩：[LeereRiss](https://leereriss.netlify.app)
- 演示视频: [Bilibili - LeereRiss 游戏演示](https://www.bilibili.com/video/BV1YFyfBYELt)


## Scripts 目录概要

- `Assets/Scripts/GameScene/`
	- `Maze/`：迷宫生成与可视化逻辑，主要脚本示例：`MazeGenerator.cs`、`Maze.cs`、`MazeData.cs`、`MazeVisualizer.cs`、`GenerateLib.cs`、`SpawnLib.cs`。子目录 `Algorithm/` 包含多种生成与生成点分配算法（`GenerateAlgorithm0..n.cs`、`SpawnAlgorithm0..n.cs`）。
	- `NPC/`：NPC 行为体系，分为 `Attack/`、`Concrete/`（具体类型实现，如 `Chaser.cs`、`Ghost.cs`）、`Cooperation/`（合作算法）、`Core/`（`NPCBase.cs`、`NPCManager.cs`、`NPCAnimation.cs`）、`Movement/`（`MovementLib.cs`、移动算法如 `AstarMovement.cs`）、`Perception/`、`Resource/` 等。
	- `Player/`：玩家相关（`Player.cs`、`PlayerInput.cs`、`PlayerMovement.cs`、`PlayerShoot.cs`、`PlayerResource.cs` 等）并含 `Guide/` 目录用于路径/指引算法（如 `AutoController.cs`、`GuideLib.cs`）。
	- `Utility/`：局部游戏内工具脚本（如 `GameOverController.cs`、`HealthBar.cs`、`PauseMenuController.cs` 等）。

- `Assets/Scripts/MainMenu/`：主菜单逻辑与 UI 控制器（`MainMenuController.cs`、`MainMenuStyler.cs`、`MazeSettings.cs`、`ButtonHover.cs`）。

- `Assets/Scripts/Utility/`：跨场景共享工具（例如 `GameOverController.cs`、`InputBlocker.cs` 等），供主流程和场景复用。

### Windows 可执行

1. 双击可执行文件启动游戏。

2. 若遇到缺少运行库，请安装 Microsoft Visual C++ 运行库并以管理员权限运行。

## 已知问题

- 游戏仍存在若干已知 BUG，尤其是程序生成的地图在极少数情况下可能出现布局错乱或显示异常。

- 临时处理：出现地图错乱时，右键调出游戏内菜单，选择 `Restart` 重启当前地图；或选择 `MainMenu` 回到主菜单后调整 `Difficulty`中的数值再重试。

- 若问题频繁发生，请在仓库提交 Issue 并附上日志或截图，便于排查。

## 许可（License）

- 本项目代码采用 MIT 许可。

- 第三方素材归原作者所有（[fab.com](https://www.fab.com/)）。

## 反馈与贡献

- 如有 BUG 报告或建议，请在 GitHub 仓库提交 Issue： [https://github.com/dieWehmut/LeereRiss](https://github.com/dieWehmut/LeereRiss)

- 若要贡献代码或提出改进，请 Fork 仓库后提交 Pull Request。

---

感谢试用与支持！


