# ArchiveSystem

## 职责与数据源

`LostCity.Meta.InvestigationArchiveUI` 是只读档案视图，不修改存档、不决定案件解锁。
通过 `GameRuntime.Instance.CaseRegistry.GetEntries()` 获取章节顺序的案件、状态与定义，
通过 `GameRuntime.Instance.SaveGame.DiscoveredClueIds` 筛选线索。

- 已完成案件展示 `CaseDefinition.CaseSummary` 与已发现线索；空摘要显示“暂无案件摘要”。
- 未完成案件的已发现线索显示在“调查中的已发现线索”，不泄露完成摘要。
- catalog 中未归入有效案件的已发现线索显示在“未归档线索”。
- 未发现线索不展示；线索文字来自 `Name`、`Category`、`JournalText`，不生成叙事内容。
- 线索按稳定 ID 匹配，同一案件中重复引用的线索只展示一次。

## API 与引用

```csharp
public void Refresh();
```

唯一序列化引用 `Text documentText` 由 `HubSceneBuilder` 赋值。
其所在面板拥有 `ScrollRect`，文档处于自适应高度的内容布局中。
`HubController.ShowArchive()` 激活面板；`ShowCases()`、`ShowBestiary()` 将其隐藏。

## 刷新与生命周期

`OnEnable` 创建共享 `HubViewSupport`、订阅 `SaveGameManager.DataChanged` 并立即刷新，
因此首次开启一直隐藏的面板不依赖 `Start` 或旧事件。
`OnDisable` 解绑；再次显示会读取最新数据。
视图启用时检测 runtime 引用变化和存档错误状态，服务延迟就绪后会补刷新。

## 常见问题与验证

- 必须由 runtime 初始化 registry 和存档；视图不会自行创建或初始化服务。
- 玩家发现线索但未完成案件时，也必须能在大厅档案中回看。
- 存档残留但 catalog 已移除的 ID 不猜测内容，不展示已不存在的定义。
- 长列表依赖内容高度布局、Viewport 裁切与滚轮输入，不使用固定高度正文。
- Unity 未启动时只能验证 C# 与静态契约；中文字体、长文滚动和隐藏面板首次刷新需要运行验收。
