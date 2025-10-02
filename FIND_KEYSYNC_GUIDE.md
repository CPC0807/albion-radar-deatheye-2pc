# 🔍 手動尋找 KeySync 封包ID 指南

## 方法：使用現有的 DebugHandler

由於無法找到最新的公開封包定義，我們需要手動找出正確的 KeySync 事件ID。

## 步驟

### 1. 啟用完整封包記錄

修改 `Radar/Packets/Handlers/DebugHandler.cs`：

```csharp
public class DebugHandler : PacketHandler<object>
{
    private static System.IO.StreamWriter logFile;
    private static HashSet<byte> loggedEvents = new HashSet<byte>();

    static DebugHandler()
    {
        logFile = new System.IO.StreamWriter("packet_log.txt", false);
        logFile.AutoFlush = true;
    }

    protected override Task OnHandleAsync(object packet)
    {
        if (packet is EventPacket @event)
        {
            // 記錄所有事件的所有參數
            if (!loggedEvents.Contains(@event.Parameters.ContainsKey(252) ? (byte)@event.Parameters[252] : (byte)0))
            {
                byte eventCode = @event.Parameters.ContainsKey(252) ? (byte)@event.Parameters[252] : (byte)0;
                loggedEvents.Add(eventCode);

                logFile.WriteLine($"=== Event Code: {eventCode} ===");
                foreach (var kvp in @event.Parameters)
                {
                    string valueInfo = "";
                    if (kvp.Value is byte[] byteArray)
                    {
                        valueInfo = $"byte[{byteArray.Length}]: {BitConverter.ToString(byteArray).Substring(0, Math.Min(50, BitConverter.ToString(byteArray).Length))}...";
                    }
                    else
                    {
                        valueInfo = $"{kvp.Value?.GetType().Name}: {kvp.Value}";
                    }
                    logFile.WriteLine($"  Key {kvp.Key}: {valueInfo}");
                }
                logFile.WriteLine();
            }
        }

        return Task.CompletedTask;
    }
}
```

### 2. 啟用 DebugHandler

在 `Radar/Init.cs` 中取消註解：

```csharp
ReceiverBuilder builder = ReceiverBuilder.Create();

#if DEBUG
builder.AddHandler(new DebugHandler());
#endif
```

### 3. 運行並收集數據

1. 以 **DEBUG** 模式編譯
2. 啟動 DEATHEYE
3. 啟動 Albion Online
4. **進入遊戲世界**
5. **切換地圖/區域** (這通常會觸發 KeySync)
6. 關閉程序

### 4. 分析日誌

查看 `packet_log.txt`，尋找：

**KeySync 的特徵**：
- 包含 8-byte 的 byte[] 數組
- 通常在進入新區域時觸發
- 參數key可能是 0, 1, 或其他小數字
- 只觸發一次或很少觸發

**示例**：
```
=== Event Code: 123 ===
  Key 0: byte[8]: AA-BB-CC-DD-EE-FF-00-11...
  Key 1: Int32: 12345

=== Event Code: 593 ===
  Key 0: byte[8]: 12-34-56-78-9A-BC-DE-F0...
```

### 5. 更新 indexes.json

找到後，更新 `bin/Debug/jsons/indexes.json`：

```json
{
  "KeySync": 新找到的事件ID
}
```

### 6. 測試

重新編譯並測試玩家位置是否正確顯示。

## 進階：暴力搜索法

如果上述方法沒有結果，使用暴力搜索：

### 創建 BruteForceKeySyncHandler.cs

```csharp
using Albion.Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using X975.Radar.GameObjects.Players;

namespace X975.Radar.Packets.Handlers
{
    public class BruteForceKeySyncHandler : EventPacketHandler<BruteForceKeySyncEvent>
    {
        private readonly PlayersHandler playersHandler;
        private readonly int testEventCode;

        public BruteForceKeySyncHandler(PlayersHandler playersHandler, int eventCode)
            : base(eventCode)
        {
            this.playersHandler = playersHandler;
            this.testEventCode = eventCode;
        }

        protected override Task OnActionAsync(BruteForceKeySyncEvent value)
        {
            if (value.Code != null && value.Code.Length == 8)
            {
                Console.WriteLine($"[FOUND!] Event {testEventCode} has 8-byte code: {BitConverter.ToString(value.Code)}");
                playersHandler.XorCode = value.Code;
            }
            return Task.CompletedTask;
        }
    }

    public class BruteForceKeySyncEvent : BaseEvent
    {
        public BruteForceKeySyncEvent(Dictionary<byte, object> parameters) : base(parameters)
        {
            Code = parameters.ContainsKey(0) ? parameters[0] as byte[] : null;
        }

        public byte[] Code { get; }
    }
}
```

### 在 Init.cs 中註冊

```csharp
// 測試事件ID 500-700
for (int i = 500; i < 700; i++)
{
    builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
}
```

運行後，Console會輸出：
```
[FOUND!] Event 612 has 8-byte code: 12-34-56-78-9A-BC-DE-F0
```

## 替代方案

### 選項 B：加入社區

1. **Discord/論壇**：
   - 搜索 "Albion Online Radar Discord"
   - 詢問其他開發者是否有最新的封包定義

2. **GitHub Issues**：
   - 在相關項目的 Issues 中搜索 "KeySync" 或 "packet"
   - 查看是否有人分享了更新的定義

3. **OwnedCore 論壇**：
   - https://www.ownedcore.com/forums/mmo/albion-online-exploits-hacks/
   - 這裡有很多 Albion 相關的技術討論

## 預期結果

找到正確的 KeySync ID後，你應該會看到：

```
[KeySync] XorCode received! Length:8 Bytes:12-34-56-78-9A-BC-DE-F0
[PlayerPos] ID:12345 Name:Player1 Pos:(123.45,456.78) NewPos:(125.00,460.00) Speed:11.00
```

而不是：
```
[XorCode] NULL - Cannot decrypt!
[PlayerPos] ID:12345 Name:Player1 Pos:(0.00,-874965800.00) NewPos:(0.00,-887795100.00) Speed:11.00
```

---
**祝你好運！找到後請更新這個文檔，幫助其他人。**
