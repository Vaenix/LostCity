# UISystem

## 职责

Unity UI 提供克制灰盒反馈与控制，玩家文字使用简体中文。
战斗界面继续负责 HUD、生命条、迷你地图、集中提示、证据、推理及升级奖励。
Phase 6 大厅仅消费 meta 服务，不自行保存进度或计算解锁规则。

## 大厅组件与 API

| 组件 | 接口与职责 |
| --- | --- |
| `HubController` | `Refresh()`；`ShowCases()`、`ShowArchive()`、`ShowBestiary()` 切换面板；`RetrySave()` 显式重试存档 |
| `HubCaseRow` | `Bind(CaseRegistryEntry, Action<CaseDefinition>)`；`SetSaveAllowed(bool)`；`BindHeading(string)` |
| `InvestigationArchiveUI` | `Refresh()`，完成摘要及所有已发现线索的只读视图 |
| `BestiaryUI` | `Refresh()`，目录定义与遭遇 ID 驱动的只读视图 |
| `HubViewSupport` | 内部 helper：服务重绑、事件生命周期、存档状态与运行时中文字体 |

`HubController` 的序列化引用严格使用：
`currentChapterText`、`statusText`、`caseListRoot`、`rowTemplate`、`archiveButton`、
`bestiaryButton`、`casesButton`、`casesPanel`、`archiveUI`、`bestiaryUI`、`retrySaveButton`。
行模板引用 `caseNameText`、`descriptionText`、`stateText`、`startButton`、`startButtonText`。
档案与图鉴各引用 `documentText`。

## 状态与启动

- 按 `CaseRegistry.GetEntries()` 的章节顺序展示，未分章条目显示在“未归档案件”。
- 当前章显示名称与完成数；行状态为“未解锁”“已解锁”“已完成”。
- metadata-only 标为“开发中”且不可启动；已完成可玩案件显示“重新调查”。
- `CanStart` 与 `StartBlockReason` 完全来自 registry，界面不复制核心规则。
- 无服务、存档只读、未保存变更或 `LastError` 非空时，显示中文状态并禁止启动。
- 仅存储异常时显示“重试存档”，与进入案件按钮分离；存储正常或服务未就绪本身不触发显示。
- 显式点击重试时：只读存档调用 `Reload()`，否则调用 `SaveNow()`，随后刷新状态、按钮与案件行。
- 未来版本存档仍由 Save 服务保护，重试不会覆盖或清除只读标记；故障未解除时保留重试入口。
- 权限或空间恢复后可直接点击重试，无需退出游戏；没有自动 IO 重试或后台重试循环。
- 点击时重新校验案件，并调用 `SaveNow()` 检查实际写入路径；存档状态正常后才调用 `SceneFlow.StartCase()`。
- 场景启动失败恢复按钮并显示中文提示；同次切场景期间拒绝重复启动。
- 不直接展示可能为英文的底层异常，不以界面绕过只读保护。

## 刷新与事件

所有可见 meta 视图订阅 `SaveGameManager.DataChanged`；禁用时解绑，启用时立即刷新。
隐藏面板不要求先执行 `Start`，不会漏掉首次数据。
runtime 延迟就绪、替换或存档状态变化时补刷新，不每帧重建列表。
按钮使用命名回调，绑定行前移除自身旧回调，禁用时移除，启用时恢复；不清空其他拥有者的事件。

## 场景生成

```csharp
LostCity.CombatSandbox.EditorTools.HubSceneBuilder.CreateHubScene(); // void
```

Editor 菜单：`Tools > Lost City > Create Hub Scene`。
保存路径常量：`HubSceneBuilder.ScenePath = "Assets/_Project/Scenes/Hub.unity"`。
方法要求退出 Play Mode 且已打开场景无未保存改动，然后用新的单场景替换当前场景并保存。
该方法不生成章节/案件/叙事、不修改 Build Settings，也不调用 `CombatSandboxCreator`。
主生成流程可在已有场景保存后调用它；场景与 `.meta` 由 Unity 正常生成。

生成内容包含一个 `MainCamera`、一个 `EventSystem`、`InputSystemUIInputModule`、
`Canvas`、`GraphicRaycaster`、三个带滚动条的 `ScrollRect`、禁用行模板、默认隐藏的重试存档按钮，以及全部序列化引用。
输入优先引用项目 UI action；项目尚无 UI map 时使用 Input System 包内默认 action。
仅使用 importer 生成的持久化 `InputActionReference` 子资源，不将临时 action 引用写进场景。
输入引用丢失已修复：输入模块的隐藏字段通过单次 `SerializedObject` 赋值并 Apply、标脏后持久化。
生成器保存后立即重开 Hub，检查动作资源及六个 UI action 引用仍是持久化对象且属于同一资源；失败直接抛出诊断异常。
原生 `GenerateAndValidate` 已验证修复通过：Hub 与 CombatSandbox 保存重开后输入引用非空且可解析，场景引用及 meta 数据契约检查通过。
装饰背景和文字 `raycastTarget=false`；按钮、滚动条与透明 Viewport 接收自身交互。

## 中文字体

场景仅序列化内置 `LegacyRuntime.ttf`。`HubController.Awake()` 在运行时查找系统安装的
`PingFang SC`、`Microsoft YaHei`、`Noto Sans CJK SC`、`Source Han Sans SC`、`SimHei`，
创建动态字体并应用到 Canvas 下全部 `Text`，包含隐藏面板和禁用行模板。
找不到时明确警告并回退内置字体，可能缺少中文字形。
不添加字体文件、美术或本地化框架，不序列化运行时创建的 Font。

## 其他界面

现有 `EvidenceJournal.Hide()`、`DeductionBoard.SetAvailable(bool)`、`DeductionBoard.Hide()`
和 `UpgradeSelectionController.IsShowing` 保持各自职责。
奖励与完成界面的泛化及自动返回属于调查流程实现，不由大厅视图接管。

## 验证边界

原生生成与验证已通过（`GenerateAndValidate` exit 0）：证据为
`/private/tmp/lost-city-phase6-generate-recheck.log` 第 840 行场景验证通过、第 850 行 meta 契约验证通过。
保存重开通过不等于真实交互通过；手动点击尚未测试，点击/键盘导航、三类长列表滚动、隐藏面板首次刷新、
只读/保存失败禁止开局、故障解除后的显式重试、未来版本保护、完成案件重玩、未知图鉴条目与中文字体。
以上交互仍需验收。`MetaHubUITests` 包含临时目录模拟的写入失败、读取失败和未来版本重试回归；最终全套 EditMode 结果待主任务汇总。
