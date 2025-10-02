# 🔧 玩家位置顯示Bug修復報告

## 問題描述
其他玩家的位置無法正確顯示在雷達上。

### 用戶觀察到的現象
1. ✅ **可以看到其他玩家的裝備** → 證明封包接收正常
2. ❌ **所有玩家都顯示在 (0,0) 附近**
3. ❌ **玩家圖標向右下角走幾步就抖動回原點**

## 原因分析

### 發現的根本問題
在 `PlayersHandler.cs` 的 `UpdatePlayerPosition()` 方法中**誤用了加密解析方法**！

**原始代碼（錯誤）**：
```csharp
// ❌ 問題：對MoveEvent的未加密座標錯誤地使用了Decrypt()！
var pos = Decrypt(positionBytes);
Vector2 position = new Vector2(pos[1], pos[0]);

var newPos = Decrypt(newPositionBytes);
Vector2 newPosition = new Vector2(newPos[1], newPos[0]);
```

### 為什麼這是一個Bug？

**關鍵發現**：Albion Online的不同封包事件使用**不同的座標編碼方式**！

#### 1. **NewCharacterEvent** - 使用**加密座標**
```csharp
// NewCharacterEventHandler.cs:36-39
if (playerHandler.XorCode != null && value.EncryptedPosition != null)
{
    var coords = playerHandler.Decrypt(value.EncryptedPosition);  // ✅ 需要XOR解密
    pos = new Vector2(coords[1], coords[0]);
}
```

#### 2. **MoveEvent** - 使用**未加密座標**
```csharp
// MobsHandler.cs:38-39 (正確的參考實現)
var position = new Vector2(
    BitConverter.ToSingle(positionBytes, 4),  // ✅ 直接解析，不需要解密
    BitConverter.ToSingle(positionBytes, 0)
);
```

#### 3. **問題所在**
- `UpdatePlayerPosition()` 錯誤地對 `MoveEvent` 的**未加密座標**調用了 `Decrypt()`
- `Decrypt()` 期望處理8字節的加密數據，但收到的是未加密的座標
- 導致座標解析錯誤，玩家移動時位置計算錯誤
- 結果：玩家圖標抖動、回到原點

## 修復方案

### 修改文件
- **文件**: `Radar/GameObjects/Players/PlayersHandler.cs`
- **方法**: `UpdatePlayerPosition()` (line 157-180)
- **修改**: `AddPlayer()` (line 52-66) - 添加調試日誌

### 修復後的代碼
```csharp
public void UpdatePlayerPosition(int id, byte[] positionBytes, byte[] newPositionBytes, float speed, DateTime time)
{
    lock (playersList)
    {
        // ✅ 修復：MoveEvent的座標是未加密的，直接解析（與MobsHandler相同）
        // 注意：byte[]中座標順序是[X(0-3), Y(4-7)]，但Vector2構造需要(Y, X)
        Vector2 position = new Vector2(
            BitConverter.ToSingle(positionBytes, 4),    // Y座標
            BitConverter.ToSingle(positionBytes, 0)     // X座標
        );
        Vector2 newPosition = new Vector2(
            BitConverter.ToSingle(newPositionBytes, 4), // Y座標
            BitConverter.ToSingle(newPositionBytes, 0)  // X座標
        );

        if (playersList.TryGetValue(id, out Player player))
        {
            player.IsStanding = (player.Position - position).Magnitude() <= 0.05;
            player.Position = position;
            player.Speed = speed;
            player.Time = time;
            player.NewPosition = newPosition;

            // Debug日誌（僅在DEBUG模式下輸出）
            #if DEBUG
            Console.WriteLine($"[PlayerPos] ID:{id} Name:{player.Name} Pos:({position.X:F2},{position.Y:F2}) NewPos:({newPosition.X:F2},{newPosition.Y:F2}) Speed:{speed:F2}");
            #endif
        }
    }
}
```

## 添加的調試功能

為了方便驗證修復效果，添加了DEBUG模式下的日誌輸出：

1. **玩家加入日誌**：
   ```
   [PlayerAdd] ID:12345 Name:PlayerName Guild:GuildName Pos:(100.50,200.75) Faction:PVP
   ```

2. **玩家移動日誌**：
   ```
   [PlayerPos] ID:12345 Name:PlayerName Pos:(100.50,200.75) NewPos:(105.20,205.30) Speed:7.50
   ```

### 如何查看調試日誌
在Visual Studio中：
1. 以 **Debug** 模式編譯項目
2. 運行程序
3. 查看 **Output** 窗口中的 Console 輸出

## 測試步驟

1. **重新編譯項目**：
   - 在Visual Studio中打開項目
   - 選擇 Build → Rebuild Solution

2. **運行測試**：
   - 啟動DEATHEYE
   - 啟動Albion Online
   - 進入遊戲世界
   - 觀察其他玩家是否正確顯示在雷達上

3. **驗證修復**：
   - 檢查其他玩家的圖標是否出現在正確位置
   - 確認玩家移動時圖標是否跟隨移動
   - 在DEBUG模式下查看Console輸出，確認座標不為(0,0)

## 封包→解析→繪製完整流程

### 流程圖
```
封包接收
  ↓
┌──────────────────────────────────────┐
│ 1. NewCharacterEvent / MoveEvent     │  接收Photon封包
│    - 提取EncryptedPosition (byte[])  │
└──────────────────────────────────────┘
  ↓
┌──────────────────────────────────────┐
│ 2. PlayersHandler.Decrypt()          │  座標解密
│    - XOR解密（如果有密鑰）           │
│    - 返回 [x, y] float數組           │
└──────────────────────────────────────┘
  ↓
┌──────────────────────────────────────┐
│ 3. 座標轉換                          │  X/Y互換
│    new Vector2(pos[1], pos[0])       │
└──────────────────────────────────────┘
  ↓
┌──────────────────────────────────────┐
│ 4. PlayersHandler.AddPlayer()        │  添加到玩家列表
│    或 UpdatePlayerPosition()         │
└──────────────────────────────────────┘
  ↓
┌──────────────────────────────────────┐
│ 5. PlayersDrawerer.DrawAsync()       │  繪製玩家
│    - 計算相對位置                    │
│    - 旋轉座標(-45度)                 │
│    - 繪製到雷達                      │
└──────────────────────────────────────┘
```

### 關鍵文件
1. **封包處理**:
   - `Radar/Packets/Handlers/NewCharacterEvent.cs` - 新玩家加入
   - `Radar/Packets/Handlers/MoveEvent.cs` - 玩家移動

2. **座標解析**:
   - `Radar/GameObjects/Players/PlayersHandler.cs` - 解密和管理玩家數據
   - `Radar/Utility/Additions.cs` - 座標轉換工具

3. **繪製渲染**:
   - `Radar/Drawing/Drawers/PlayersDrawerer.cs` - 繪製玩家圖標

## 座標系統說明

### 座標轉換順序
1. **封包座標** (byte[]) → XOR解密 → `[x, y]` float數組
2. **X/Y互換**: `new Vector2(pos[1], pos[0])` → `Vector2(y, x)`
3. **相對座標**: `(玩家位置 - 本地玩家位置)`
4. **旋轉變換**: 旋轉-45度用於雷達顯示

### 為什麼要X/Y互換？
- Albion Online的網絡協議使用特定的座標順序
- 遊戲內部座標系統與網絡傳輸的座標順序不同
- `fromFArray()` 和手動創建 `Vector2(pos[1], pos[0])` 都執行相同的轉換

## 後續工作

如果修復有效且不再需要調試日誌，可以：
1. 移除 `#if DEBUG` 區塊中的 `Console.WriteLine` 語句
2. 或保留它們以便未來調試

## 修復日期
2025-10-02

---
**修復作者**: Claude Code
**影響範圍**: 其他玩家位置顯示
**風險等級**: 低（僅修改座標更新邏輯，不影響其他功能）
