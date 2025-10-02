# 🔍 玩家位置顯示問題 - 完整分析報告

## 問題現象
- ✅ 可以看到其他玩家的裝備
- ❌ 玩家位置全部顯示在 (0, 0) 附近
- ❌ 玩家圖標抖動（移動幾步就跳回原點）
- ✅ 怪物位置顯示正常

## 根本原因

### 1. 座標加密機制
Albion Online使用XOR加密來保護玩家移動座標：

**怪物 (正確)**：
- `NewMobEvent` → 位置是 `float[]` 數組 → 未加密
- `MobsHandler.UpdateMobPosition()` → 直接用 `BitConverter.ToSingle()` 解析

**玩家 (錯誤)**：
- `NewCharacterEvent` → 位置是加密的 `byte[]` → 需要XOR解密
- `MoveEvent` → 位置是加密的 `byte[]` → 需要XOR解密
- `PlayersHandler.UpdatePlayerPosition()` → 需要 `Decrypt(XorCode)` 解密

### 2. XorCode缺失
```
XorCode = null
```

**原因**：`KeySyncEvent` (封包ID: 593) 沒有被觸發

**證據**：
1. Console輸出顯示 `[XorCode] NULL - Cannot decrypt!`
2. 沒有看到 `[KeySync]` 的調試日誌
3. 封包掃描沒有發現包含8-byte數組的事件

### 3. 座標解析流程

#### MoveEvent 數據結構
```
parameters[offsets[0]] = 玩家ID
parameters[offsets[1]] = byte[] parameter (包含位置、速度等數據)
    ↓
parameter[0] = Flags
parameter[9-16] = PositionBytes (8 bytes, 加密的)
parameter[18+] = Speed (如果有Flag)
parameter[22+] = NewPositionBytes (8 bytes, 加密的, 如果有Flag)
```

#### 解密失敗的結果
```csharp
// 沒有XorCode時，Decrypt()直接返回未解密的bytes
var pos = Decrypt(positionBytes);  // 返回加密的垃圾數據
position = new Vector2(pos[1], pos[0]);  // 解析成巨大的錯誤數字

實際輸出：Pos:(0.00,-874965800.00)
```

## 測試記錄

### 測試1：直接使用BitConverter (失敗)
```csharp
Vector2 position = new Vector2(
    BitConverter.ToSingle(positionBytes, 4),
    BitConverter.ToSingle(positionBytes, 0)
);
```
**結果**：`Pos:(0.00,-874965800.00)` - X始終為0，Y是巨大錯誤值

### 測試2：使用Decrypt但XorCode為null (失敗)
```csharp
var pos = Decrypt(positionBytes);  // XorCode == null
position = new Vector2(pos[1], pos[0]);
```
**結果**：`Pos:(1894675000.00,-1962.46)` - 兩個座標都是錯誤值

### 測試3：封包掃描尋找XorCode (失敗)
- 掃描所有事件，尋找包含8-byte數組的封包
- **結果**：沒有發現任何8-byte數組
- **結論**：KeySyncEvent確實沒有被觸發

## 可能的解決方案

### 方案1：更新封包ID（最可能）⭐
**問題**：`indexes.json` 中的 `KeySync: 593` 可能過時

**解決步驟**：
1. 使用網絡抓包工具（如Wireshark）分析Albion Online的封包
2. 找到包含XOR密鑰的新封包ID
3. 更新 `jsons/indexes.json` 中的 `KeySync` 值

**風險**：需要逆向工程技能

### 方案2：使用社區更新的封包定義
**解決步驟**：
1. 搜索GitHub上其他Albion Online雷達項目
2. 查看他們最新的 `indexes.json` 和 `offsets.json`
3. 更新我們的JSON文件

**推薦項目**：
- https://github.com/broderickhyman/ao-bin-dumps
- 其他活躍的Albion雷達項目

### 方案3：暴力搜索KeySync ID
**解決步驟**：
1. 修改 `KeySyncEventHandler` 監聽所有可能的事件ID (500-700)
2. 檢查哪個事件包含8-byte的密鑰數據
3. 更新 `KeySync` ID

**代碼示例**：
```csharp
// 在Init.cs中添加
for (int i = 500; i < 700; i++)
{
    builder.AddEventHandler(new TestKeySyncHandler(playersHandler, i));
}
```

### 方案4：移除加密（不推薦）❌
**問題**：Albion可能檢測到未加密的座標訪問並封禁賬號

## 文件修改記錄

### 已修改的文件
1. `Radar/GameObjects/Players/PlayersHandler.cs`
   - Line 157-179: `UpdatePlayerPosition()` 方法
   - 嘗試了多種座標解析方式

2. `Radar/Packets/Handlers/KeySyncEventHandler.cs`
   - 添加了調試日誌

3. `Radar/Packets/Handlers/DebugHandler.cs`
   - 添加了封包掃描功能

### 配置文件
1. `bin/Debug/jsons/indexes.json`
   - `KeySync: 593` ← **可能需要更新**

2. `bin/Debug/jsons/offsets.json`
   - `KeySync: [0]` ← 從參數0讀取

## 下一步建議

### 立即執行 (最重要) ⭐⭐⭐
1. **更新封包定義文件**
   - 訪問 https://github.com/broderickhyman/ao-bin-dumps
   - 下載最新的 indexes 和 offsets
   - 替換 `bin/Debug/jsons/` 中的文件
   - 重新測試

### 備選方案
2. **手動搜索KeySync事件ID**
   - 使用暴力搜索方法
   - 測試每個可能的事件ID

3. **社區求助**
   - 在Albion雷達相關Discord/論壇發問
   - 尋找其他使用者的解決方案

## 技術細節

### Decrypt() 方法工作原理
```csharp
public float[] Decrypt(byte[] coordinates, int offset = 0)
{
    var code = XorCode;
    if (code == null)
    {
        // ❌ 沒有密鑰：直接返回錯誤數據
        return new[] {
            BitConverter.ToSingle(coordinates, offset),
            BitConverter.ToSingle(coordinates, offset + 4)
        };
    }

    // ✅ 有密鑰：XOR解密
    var x = coordinates.Skip(offset).Take(4).ToArray();
    var y = coordinates.Skip(offset + 4).Take(4).ToArray();

    Decrypt(x, code, 0);  // XOR操作
    Decrypt(y, code, 4);

    return new[] {
        BitConverter.ToSingle(x, 0),
        BitConverter.ToSingle(y, 0)
    };
}
```

### 為什麼怪物位置正確？
怪物使用不同的事件和數據結構：
```csharp
// NewMobEvent: 位置在參數7，是float[]數組
var location = parameters[7] as Array;
float posX = float.Parse(location.GetValue(0).ToString());
float posY = float.Parse(location.GetValue(1).ToString());
```

## 結論
問題的根本原因是 **KeySyncEvent沒有被觸發，導致XorCode為null，無法解密玩家移動座標**。

最可能的解決方案是**更新 `indexes.json` 中的 KeySync 事件ID**，因為Albion Online更新後封包ID可能已經改變。

---
**日期**：2025-10-02
**分析者**：Claude Code
**狀態**：待解決 - 需要更新封包定義
