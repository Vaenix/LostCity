# BestiarySystem

## 职责与数据源

`LostCity.Meta.BestiaryUI` 是只读图鉴视图。目录来自
`CaseRegistry.Enemies`、`CaseRegistry.Bosses`，两者由自动生成的 catalog 索引。
发现状态分别来自 `SaveGameManager.EncounteredEnemyIds`、`EncounteredBossIds`。

- 敌人以 `EnemyId` 匹配；首领以 `BossId` 匹配，两种 ID 集合不混用。
- 每个未遇见的有效目录条目显示“未知记录”，不泄露名称和描述。
- 已遇见条目展示定义中的名称与 `BestiaryDescription`，空描述显示“暂无图鉴描述”。
- catalog 中的条目决定列表，不仅遍历存档中的已发现 ID。
- 不生成怪物、叙事或美术，不展示已从目录移除的历史 ID。

## API 与引用

```csharp
public void Refresh();
```

序列化字段 `Text documentText` 由 `HubSceneBuilder` 绑定。
`HubController.ShowBestiary()` 激活图鉴面板，正文通过 `ScrollRect` 与自动高度内容布局滚动。
面板使用“敌人”“首领”中文分组。

## 刷新与生命周期

启用时通过 `HubViewSupport` 立即读取数据、订阅 `SaveGameManager.DataChanged`；
禁用时解绑。一直隐藏到首次打开的面板也会首次刷新。
显示期间新增遭遇记录会自动刷新；隐藏期间发生的变化会在再次打开时体现。
runtime 延迟就绪或被替换时，启用中的视图重新绑定。

## 依赖与验证

- 依赖 `GameRuntime`、`CaseRegistry`、`LostCity.Meta.Persistence.SaveGameManager`、Unity UI。
- 发现 ID 必须与 catalog 定义的稳定 ID 一致。
- `GraphicRaycaster` 和带有效 pointer/click/scroll 动作的 `InputSystemUIInputModule` 保证交互。
- 灰盒背景与正文不拦截按钮射线；透明 Viewport 接收空白区域的拖拽/滚动。
- 需在 Unity 中验证未知条目遮蔽、遭遇后刷新、长列表滚动与中文字体。
