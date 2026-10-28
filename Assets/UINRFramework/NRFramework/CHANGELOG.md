# 更新日志

本文件记录每个版本的变化。版本号规则遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [1.0.13] - 2026-10-28

### 修复

- `UIRoot.ClosePanel` / `DestroyPanel`：关闭后只把状态写成 `Hidden` 而没有移除 key，导致面板关掉再开会被当成重复打开、界面出不来。改为直接移除占位。
- `UIPanel.PlayCloseAnim`：判断的是 `openAnimPlayMode`、调用的也是 `PlayOpenAnim`，有关闭动画的弹窗关闭时会再播一遍打开动画。
- `UIRoot.SetPanelVisible`：原来只有 `Debug.Assert` 保护，该断言在 Release 包里会被剥离，之后 `panelDict[panelId]` 直接抛 `KeyNotFoundException`。改为 `TryGetValue` + 警告日志。
- `UIRoot.GetPanel`：面板未打开时抛 `KeyNotFoundException`，改为返回 `null`。
- `UIRoot.FindPanelComponent`：缺少面板存在性判断，面板已关闭时抛 `KeyNotFoundException`。
- `UIRoot.CreatePanelAsync`：加载中被关闭后立刻重新打开时，先返回的那次加载会抢占 `panelDict`，而真正发起第二次打开的调用方反而收到失败回调。增加加载流水号核验身份。
- `UIRoot.CreatePanelAsync`：层级溢出原来只有 `Debug.Assert`，Release 下静默失效，面板会串到相邻层级。改为 `LogError` 并提示该调整哪里。
- `UIManager.SetBackgroundAndFocus`：焦点收集在命中独占面板（`GetFocusType.Get`）后没有停止，其下方面板会被错误地继续加入焦点列表。
- `UIManager.Awake`：启动校验失败后直接 return，三个集合保持 `null`，后续每次 `CreateRoot` 都抛空引用、把真正的错误信息刷没。初始化已提前。
- `UIManager.Awake`：增加 `UICanvas` / `UICamera` 上组件缺失的校验。
- `UIManager.GetFocusingPanels`：`UIPanel` 是普通 C# 对象，销毁后引用依然非 `null`，会把已销毁的面板返回给调用方。
- `UIBlocker`：面板销毁时无条件 `Unbind()`，而带关闭动画的面板是在背景重新绑定之后才真正销毁的，会把遮罩从别的面板身上扯走、导致背景凭空消失。新增 `UnbindFrom(parent)`，只解绑挂在自己身上的。
- `UIPanel.Close`：关闭动画播放期间面板仍可点击，连点会触发本不该发生的业务逻辑。关闭期间禁用射线。
- `UIPanel.PlayCloseAnim`：增加重入保护，避免 `onFinish` 二次触发导致重复销毁。
- `UIPanelBehaviour.ExistValidAnimator`：只判断组件存在且启用，挂了 Animator 却没配 Controller 时会空引用崩溃。增加 Controller 判空。
- `UIPanelBehaviour.DoAfterSeconds`：用的是 `WaitForSeconds`，`Time.timeScale = 0` 时计时停住，正在播关闭动画的面板永远走不到销毁、卡死在屏幕上。改用 `WaitForSecondsRealtime`。
- `UIView.CreateAsync`：预制体根节点缺少 `UIViewBehaviour` 时只有 `Debug.Assert`，Release 下紧接着空引用。改为明确报错并释放已加载的资源。

### 新增

- `DelayUIResLoader`：延迟若干帧回调的开发期假异步加载器。编辑器默认加载器是同步立即回调，框架中的异步分支（`Loading` 占位、加载中取消、加载流水号）在它下面不会被执行；接入真实加载器前建议先用它验证时序。

### 变更

- Inspector 标签全面中文化，枚举成员加 `[InspectorName]`（只影响显示，不影响序列化）。
- 修正枚举拼写：`DontRespone` → `DontRespond`，`DestorySelf` → `DestroySelf`。枚举按 int 存盘，已有 prefab 配置不受影响。
- `Config.kDefaultPanelThickness` 由 `10` 改为 `5`。
- `CreatePanelAsync` 的重复打开警告按真实状态区分，面板处于隐藏态时会提示改用 `SetPanelVisible(true)`。

## [1.0.12] 及更早

见 git 提交历史。
