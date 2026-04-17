# 新增事件代码 (New Event Codes)

基于 albiondata-client 2026-04-13/14 更新添加的新事件代码

## 新增事件列表

以下是在 `client/events.go` 中新增的事件代码：

```go
// 战斗相关
evMatchNewCombatRound           // 新战斗轮次
evMatchEndCombatRound           // 结束战斗轮次

// 保护状态
evLeaveProtectionStateUpdate    // 离开保护状态更新

// 传说片段系统 (Lore System)
evNewLoreSnippetObject          // 新传说片段对象
evLoreSnippetObjectStateUpdate  // 传说片段对象状态更新
evLoreSnippedClaimed            // 传说片段已认领
evLoreSnippetStatesChangedByCheat // 传说片段状态通过作弊改变

// 传送节点系统 (Teleporter System)
evNewTeleporterNode             // 新传送节点
evTeleporterNodeStateChanged    // 传送节点状态改变
evTeleporterConnectionsFullStateUpdate // 传送连接完整状态更新
evTeleporterConnectionStateChanged     // 传送连接状态改变

// 可携带物品系统 (Carriable Objects)
evRetrieveCarriableObjectStart    // 开始检索可携带物品
evRetrieveCarriableObjectCancel   // 取消检索可携带物品
evRetrieveCarriableObjectReset    // 重置检索可携带物品
evRetrieveCarriableObjectFinished // 完成检索可携带物品
evLosingCarriableObjectStart      // 开始失去可携带物品
evLosingCarriableObjectFinished   // 完成失去可携带物品
```

## 实现建议

### 如果需要监听这些事件

1. **在 `jsons/indexes.json` 中添加对应的事件 ID**
   ```json
   {
     "MatchNewCombatRound": xxx,
     "MatchEndCombatRound": xxx,
     "LeaveProtectionStateUpdate": xxx,
     ...
   }
   ```

2. **创建对应的 Event 和 Handler 类**
   ```csharp
   // 例如：Radar/Packets/Handlers/MatchNewCombatRoundEvent.cs
   public class MatchNewCombatRoundEvent : BaseEvent
   {
       public MatchNewCombatRoundEvent(Dictionary<byte, object> parameters) : base(parameters)
       {
           // 解析参数
       }
   }

   // Radar/Packets/Handlers/MatchNewCombatRoundEventHandler.cs
   public class MatchNewCombatRoundEventHandler : EventPacketHandler<MatchNewCombatRoundEvent>
   {
       protected override Task OnActionAsync(MatchNewCombatRoundEvent value)
       {
           // 处理事件
           return Task.CompletedTask;
       }
   }
   ```

3. **在 `Init.cs` 的 ReceiverBuilder 中注册**
   ```csharp
   builder.AddEventHandler(new MatchNewCombatRoundEventHandler(...));
   ```

## 注意事项

- **当前本项目可能不需要这些事件**：这些事件主要用于特定游戏功能（战斗、传送、传说系统），如果雷达只关注玩家/资源/怪物位置，可以暂时忽略
- **如果遇到未知事件警告**：如果在 DebugHandler 中看到这些事件 ID，可以参考此文档确认是否需要实现
- **事件 ID 需要逆向工程确定**：albiondata-client 项目使用了逆向工程确定每个事件的 ID，本项目需要通过抓包或暴力破解方式确定

## 相关文件

- **上游参考**：https://github.com/ao-data/albiondata-client/blob/master/client/events.go
- **本项目事件索引**：`jsons/indexes.json`
- **事件处理器**：`Radar/Packets/Handlers/`
