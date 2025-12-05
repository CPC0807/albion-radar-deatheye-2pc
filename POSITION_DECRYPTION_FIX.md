# 🔧 玩家位置解密問題診斷與修復

## 問題現象

```
[PlayerPos] ID:231094 Name:MrsBreewc Pos:(0.00,0.00) NewPos:(0.00,0.00) Speed:11.83
```

玩家位置顯示為 `(0.00, 0.00)`，但速度正常，說明：
- ✅ Move 事件正確接收
- ✅ 玩家 ID 和名稱正常
- ❌ 位置數據未正確解密

## 根本原因

在 `Radar/GameObjects/Players/PlayersHandler.cs:157-179`，`UpdatePlayerPosition` 方法**沒有使用 XorCode 解密座標**：

```csharp
// 當前代碼（錯誤）：
Vector2 position = new Vector2(
    BitConverter.ToSingle(positionBytes, 4),
    BitConverter.ToSingle(positionBytes, 0)
);
```

這段代碼嘗試直接讀取未加密的座標，但 Albion Online **仍然使用 XOR 加密**位置數據。

## 診斷步驟

### 第 1 步：檢查是否收到 KeySync

運行程序時，查看控制台輸出：

```bash
# 如果看到這個，表示 KeySync 正常：
[KeySync] XorCode received! Length:8 Bytes:12-34-56-78-9A-BC-DE-F0

# 如果看到這個，表示 KeySync 事件 ID 錯誤：
[KeySync] XorCode is NULL!
```

### 第 2 步：檢查 KeySync 事件 ID

查看 `bin/Debug/jsons/indexes.json`：

```json
{
  "KeySync": 593
}
```

如果沒有收到 KeySync，事件 ID 可能已變更，需要重新搜索。

### 第 3 步：檢查 KeySync 參數偏移

查看 `bin/Debug/jsons/offsets.json`：

```json
{
  "KeySync": [0]
}
```

這表示 XorCode 在 parameters[0] 中。

## 修復方案

### 方案 A：使用現有的 Decrypt 方法（推薦）

修改 `Radar/GameObjects/Players/PlayersHandler.cs` 的 `UpdatePlayerPosition` 方法：

```csharp
public void UpdatePlayerPosition(int id, byte[] positionBytes, byte[] newPositionBytes, float speed, DateTime time)
{
    lock (playersList)
    {
        // 使用 Decrypt 方法解密座標
        float[] pos = Decrypt(positionBytes);
        float[] newPos = Decrypt(newPositionBytes);

        Vector2 position = new Vector2(pos[0], pos[1]);
        Vector2 newPosition = new Vector2(newPos[0], newPos[1]);

        if (playersList.TryGetValue(id, out Player player))
        {
            player.IsStanding = (player.Position - position).Magnitude() <= 0.05;
            player.Position = position;
            player.Speed = speed;
            player.Time = time;
            player.NewPosition = newPosition;

            // Debug 輸出
            #if DEBUG
            Console.WriteLine($"[PlayerPos] ID:{id} Name:{player.Name} Pos:({position.X:F2},{position.Y:F2}) NewPos:({newPosition.X:F2},{newPosition.Y:F2}) Speed:{speed:F2}");

            // 如果 XorCode 為空，輸出警告
            if (XorCode == null)
            {
                Console.WriteLine($"[XorCode] NULL - Cannot decrypt! Raw bytes: {BitConverter.ToString(positionBytes)}");
            }
            #endif
        }
    }
}
```

### 方案 B：增強診斷輸出

如果方案 A 仍然顯示 (0.00, 0.00)，添加更詳細的調試信息：

```csharp
public void UpdatePlayerPosition(int id, byte[] positionBytes, byte[] newPositionBytes, float speed, DateTime time)
{
    lock (playersList)
    {
        #if DEBUG
        Console.WriteLine($"[DEBUG] XorCode is null: {XorCode == null}");
        Console.WriteLine($"[DEBUG] XorCode length: {XorCode?.Length ?? 0}");
        Console.WriteLine($"[DEBUG] Raw position bytes: {BitConverter.ToString(positionBytes)}");
        Console.WriteLine($"[DEBUG] Raw newPosition bytes: {BitConverter.ToString(newPositionBytes)}");
        #endif

        // 嘗試解密
        float[] pos = Decrypt(positionBytes);
        float[] newPos = Decrypt(newPositionBytes);

        #if DEBUG
        Console.WriteLine($"[DEBUG] Decrypted pos: ({pos[0]:F2}, {pos[1]:F2})");
        Console.WriteLine($"[DEBUG] Decrypted newPos: ({newPos[0]:F2}, {newPos[1]:F2})");
        #endif

        Vector2 position = new Vector2(pos[0], pos[1]);
        Vector2 newPosition = new Vector2(newPos[0], newPos[1]);

        if (playersList.TryGetValue(id, out Player player))
        {
            player.IsStanding = (player.Position - position).Magnitude() <= 0.05;
            player.Position = position;
            player.Speed = speed;
            player.Time = time;
            player.NewPosition = newPosition;

            #if DEBUG
            Console.WriteLine($"[PlayerPos] ID:{id} Name:{player.Name} Pos:({position.X:F2},{position.Y:F2}) NewPos:({newPosition.X:F2},{newPosition.Y:F2}) Speed:{speed:F2}");
            #endif
        }
    }
}
```

## 可能的問題場景

### 場景 1：KeySync 事件 ID 錯誤

**症狀**：
```
[KeySync] XorCode is NULL!
[PlayerPos] Pos:(0.00,0.00)
```

**解決方案**：使用暴力搜索找到新的 KeySync 事件 ID

1. 取消註解 `Radar/Init.cs:131-137` 的暴力搜索代碼：
```csharp
Console.WriteLine("[BruteForce] Registering KeySync scanners for event IDs 500-700...");
for (int i = 500; i <= 700; i++)
{
    builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
}
```

2. 重新編譯並運行
3. 進入遊戲並**切換地圖**
4. 查看控制台輸出：
```
[FOUND!] Event 612 has 8-byte code: 12-34-56-78-9A-BC-DE-F0
```

5. 更新 `bin/Debug/jsons/indexes.json`：
```json
{
  "KeySync": 612
}
```

### 場景 2：KeySync 參數偏移錯誤

**症狀**：
```
[KeySync] XorCode received! Length:0 Bytes:
[PlayerPos] Pos:(0.00,0.00)
```

**解決方案**：KeySync 的 8-byte 數組可能不在 parameter[0]

1. 使用 DebugHandler 記錄事件結構
2. 查看 `event_structures.txt` 找到 KeySync 事件（例如 593）
3. 找到包含 8-byte 數組的參數 key
4. 更新 `jsons/offsets.json`：
```json
{
  "KeySync": [1]  // 如果在 parameter[1]
}
```

### 場景 3：Albion 改變了加密算法

**症狀**：
```
[KeySync] XorCode received! Length:8 Bytes:12-34-56-78-9A-BC-DE-F0
[PlayerPos] Pos:(-874965800.00, 123456789.00)  // 垃圾數值
```

**解決方案**：這是最壞的情況，意味著 XOR 算法已改變

可能需要：
1. 逆向工程新的解密算法
2. 等待社區更新
3. 在 Discord/OwnedCore 論壇尋求幫助

### 場景 4：座標現在完全不加密

**症狀**：
```
[DEBUG] Raw position bytes: 00-00-00-00-00-00-00-00
[DEBUG] Decrypted pos: (0.00, 0.00)
```

**解決方案**：嘗試其他字節序或偏移

```csharp
// 嘗試不同的讀取方式
Vector2 position = new Vector2(
    BitConverter.ToSingle(positionBytes, 0),  // X 在前 4 bytes
    BitConverter.ToSingle(positionBytes, 4)   // Y 在後 4 bytes
);
```

## 快速測試流程

1. **重新編譯**項目（Debug 模式）
2. **啟動** VRise.exe
3. **進入遊戲世界**
4. **觀察控制台輸出**：
   - 是否看到 `[KeySync] XorCode received!`？
   - `[PlayerPos]` 顯示什麼座標？
5. **切換地圖**（觸發 KeySync）
6. **再次觀察**座標是否變化

## 預期正常輸出

```
[KeySync] XorCode received! Length:8 Bytes:12-34-56-78-9A-BC-DE-F0
[PlayerAdd] ID:231094 Name:MrsBreewc Guild:SOME_GUILD Pos:(1234.56,5678.90) Faction:Hostile
[PlayerPos] ID:231094 Name:MrsBreewc Pos:(1234.56,5678.90) NewPos:(1235.00,5680.00) Speed:11.83
```

## 下一步行動

根據你的情況選擇：

### ✅ 優先嘗試方案 A
修改 `UpdatePlayerPosition` 使用 `Decrypt()` 方法

### 🔍 如果還是 (0.00, 0.00)
使用方案 B 添加詳細診斷輸出，找出問題在哪裡

### 🔬 如果沒有收到 KeySync
使用暴力搜索找到新的事件 ID

---

**需要幫助？** 提供以下信息：
1. 控制台完整輸出（特別是 KeySync 和 PlayerPos 行）
2. `event_structures.txt` 中事件 593 的內容
3. `jsons/indexes.json` 和 `jsons/offsets.json` 的內容
