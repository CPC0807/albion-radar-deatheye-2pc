# 尋找 XorCode 更新事件的調查計劃

## 問題分析

**觀察**：
1. XorCode 會定期改變（不是換地圖，而是時間觸發）
2. 客戶端必定收到新的 XorCode（否則無法解密座標）
3. 原本的 KeySyncEvent (593) 從未觸發

**結論**：官方改變了 XorCode 傳輸機制，但仍在發送！

---

## 調查方法

### 方法 1：監控 XorCode 失效時刻的所有 Event

**步驟**：

1. 記錄 XorCode 成功解密的時間點
2. 記錄 XorCode 失效的時間點
3. 提取失效前後 10 秒內的所有 Event/Response/Request
4. 對比差異，找出只在失效時刻出現的 Event

**實現**：添加時間戳記錄

```csharp
// 在 PlayersHandler.cs 中
private static DateTime? lastValidXorCodeTime = null;
private static DateTime? xorCodeExpiredTime = null;

// 當 XorCode 成功解密時
if (isValidDecryption)
{
    lastValidXorCodeTime = DateTime.UtcNow;
}

// 當檢測到 XorCode 失效時
if (isInvalid)
{
    xorCodeExpiredTime = DateTime.UtcNow;
    Console.WriteLine($"[XorCode EXPIRED] at {xorCodeExpiredTime}");
    Console.WriteLine($"  Last valid: {lastValidXorCodeTime}");
    Console.WriteLine($"  Duration: {(xorCodeExpiredTime.Value - lastValidXorCodeTime.Value).TotalSeconds:F1} seconds");
}
```

### 方法 2：監控所有含 8-byte 陣列的 Event

**原理**：XorCode 是 8 bytes，可能隱藏在某個參數中

**已實現**：DebugHandler.cs 已經在檢測 8-byte 陣列

**優化**：記錄 8-byte 陣列的內容變化

```csharp
private static Dictionary<int, byte[]> last8ByteArrays = new Dictionary<int, byte[]>();

if (byteArray.Length == 8)
{
    string key = $"{eventCode}-{kvp.Key}";
    if (last8ByteArrays.ContainsKey(eventCode))
    {
        if (!byteArray.SequenceEqual(last8ByteArrays[eventCode]))
        {
            Console.WriteLine($"[8-BYTE CHANGED] Event {eventCode} Key {kvp.Key}");
            Console.WriteLine($"  Old: {BitConverter.ToString(last8ByteArrays[eventCode])}");
            Console.WriteLine($"  New: {BitConverter.ToString(byteArray)}");
        }
    }
    last8ByteArrays[eventCode] = byteArray;
}
```

### 方法 3：嘗試所有 Event ID (0-1000)

**原理**：暴力掃描所有可能的 Event ID

**風險**：可能觸發遊戲反作弊

### 方法 4：分析封包時間戳

**工具**：Wireshark + npcap

**步驟**：
1. 記錄 XorCode 失效的精確時間
2. 在 Wireshark 中過濾該時間前後的 UDP 封包
3. 查看哪些封包在該時刻發送
4. 反向推導 Event ID

---

## 重點 Event 候選

從你的日誌中，這些 Event 出現在 XorCode 變化附近：

### Event 160
- 出現頻率：經常
- 可能性：地圖/區域變化

### Event 272
- 出現頻率：中等
- 可能性：玩家狀態更新

### Event 402
- 出現頻率：較少
- 可能性：**可能是 XorCode 更新！**

### Event 73
- 出現頻率：較少
- 可能性：特殊事件

---

## 建議的測試步驟

### 測試 1：Event 402 監控

在 DebugHandler.cs 中添加：

```csharp
if (eventCode == 402)
{
    Console.WriteLine("\n!!! EVENT 402 CAPTURED !!!");
    Console.WriteLine($"Time: {DateTime.UtcNow:HH:mm:ss.fff}");

    // 記錄所有參數
    foreach (var kvp in @event.Parameters)
    {
        if (kvp.Value is byte[] byteArray && byteArray.Length == 8)
        {
            Console.WriteLine($"  FOUND 8-BYTE ARRAY in Key {kvp.Key}: {BitConverter.ToString(byteArray)}");

            // 立即測試這是否是新的 XorCode
            var testPos = XorCodeBruteForce.DecryptPosition(currentPlayerPositionBytes, byteArray);
            if (testPos.HasValue)
            {
                Console.WriteLine($"    TEST DECRYPT: ({testPos.Value.X:F2}, {testPos.Value.Y:F2})");
            }
        }
    }
}
```

### 測試 2：Response 監控

檢查是否有 Response 包含 XorCode：

```csharp
protected override void OnResponse(ResponsePacket response)
{
    if (response.Parameters.TryGetValue(253, out var code))
    {
        int responseCode = Convert.ToInt32(code);

        // 記錄所有 Response 的 8-byte 陣列
        foreach (var kvp in response.Parameters)
        {
            if (kvp.Value is byte[] byteArray && byteArray.Length == 8)
            {
                Console.WriteLine($"[Response {responseCode}] 8-byte Key {kvp.Key}: {BitConverter.ToString(byteArray)}");
            }
        }
    }
}
```

### 測試 3：時間相關性分析

記錄 XorCode 生命週期：

```csharp
private static DateTime xorCodeRecoveredTime;
private static int xorCodeLifetimeSeconds = 0;

// 當恢復 XorCode 時
xorCodeRecoveredTime = DateTime.UtcNow;

// 當失效時
var lifetime = (DateTime.UtcNow - xorCodeRecoveredTime).TotalSeconds;
Console.WriteLine($"[XorCode Lifetime] {lifetime:F1} seconds");
xorCodeLifetimeSeconds = (int)lifetime;
```

---

## 可能的發現

### 假設 1：固定時間間隔

如果 XorCode 每隔固定時間更新（例如 60 秒），那麼：
- Event 可能是週期性的
- 可能與心跳包 (heartbeat) 相關

### 假設 2：事件觸發

如果 XorCode 在特定事件時更新：
- 玩家進入戰鬥
- 玩家離開安全區
- 玩家使用傳送
- 特定技能使用

### 假設 3：隱藏參數

XorCode 可能不是獨立 Event，而是：
- MoveEvent 的隱藏參數
- NewCharacterEvent 的一部分
- 某個 Response 的附加數據

---

## 下一步行動

### 立即可做：

1. ✅ **監控 Event 402**：在 XorCode 失效時特別關注
2. ✅ **記錄 8-byte 陣列變化**：追蹤所有 8-byte 陣列的內容
3. ✅ **時間戳記錄**：精確記錄 XorCode 失效時間

### 需要協助：

4. ⏳ **提供更多日誌**：
   - XorCode 失效前後 30 秒的完整日誌
   - 包含所有 Event/Response/Request
   - 記錄遊戲中的動作（移動、戰鬥、傳送等）

5. ⏳ **Wireshark 抓包**：
   - 記錄 XorCode 失效時刻的網絡封包
   - 過濾 UDP 端口 5056, 5055, 4535
   - 導出為 .pcap 文件供分析

---

## 技術細節

### XorCode 特徵

1. **長度**：8 bytes
2. **熵值**：高隨機性（不全是 0 或 0xFF）
3. **變化**：每次都不同
4. **用途**：解密玩家座標

### 可能的傳輸方式

**方式 A：明文**
```
Event XXX, Parameter YY = [8 bytes XorCode]
```

**方式 B：Base64**
```
Event XXX, Parameter YY = "base64_encoded_xorcode"
```

**方式 C：分段**
```
Event XXX, Parameter YY = [前4 bytes]
Event XXX, Parameter ZZ = [後4 bytes]
```

**方式 D：加密**
```
Event XXX, Parameter YY = [encrypted XorCode]
需要另一把 key 解密
```

---

## 成功指標

如果找到正確的 Event/Response：

1. ✅ 能在 XorCode 失效前捕獲新的 8-byte 數據
2. ✅ 用新數據解密座標成功
3. ✅ 解密結果與實際位置一致
4. ✅ 不再需要本地玩家座標反推

---

## 參考資料

### 相關文件位置

- Event 定義：`Radar/Packets/Handlers/`
- Event 記錄：`event_structures.txt`
- Debug 輸出：Console 或日誌文件

### 已知的 Event 類型

- NewCharacterEvent
- MoveEvent
- ChangeClusterEvent
- KeySyncEvent (593, 已失效)
- Event 160, 272, 402, 73 (未知用途)

---

## 🔍 調查進度

### 已確認資訊

1. **XorCode 會定期改變**（不只是換地圖時）
2. **Request 369 與 XorCode 失效有時間關聯**：
   - 觀察到 Request 369 連續出現兩次後，XorCode 立即失效
   - Request 369 = **MoveRequest**（本地玩家移動請求）
3. **Request 369 持續發送機制** ✅：
   - **確認：即使玩家不移動，Request 369 仍會持續發送**
   - 推論：Request 369 不只是移動請求，可能用於**狀態同步**或**加密密鑰更新機制**
   - 意義：XorCode 可能在 Request 369 的 Response 中定期更新

### 已實現的監控

**DebugHandler.cs 增強**（最新版本）：
1. ✅ 追蹤 Request 369 的發送時間
2. ✅ 檢測連續的 Request 369（< 1 秒間隔）
3. ✅ 監控 Request 369 後的 Response（2 秒內）
4. ✅ 自動掃描 Response 中的 8-byte 陣列
5. ✅ 測試 8-byte 陣列是否為 XorCode

**監控邏輯**：
```csharp
// Request 369 發送時
if (requestCode == 369)
{
    waitingForResponse369 = true;
    lastRequest369Time = DateTime.UtcNow;

    // 偵測連續 Request（可能觸發 XorCode 更新）
    if (timeSinceLastRequest < 1000ms)
        Console.WriteLine("CONSECUTIVE REQUEST 369 - XorCode may change!");
}

// Response 接收時
if (waitingForResponse369 && timeSinceRequest < 2s)
{
    // 掃描所有 8-byte 陣列
    // 測試是否為 XorCode
}
```

---

## 總結

**目標**：找到新的 XorCode 傳輸機制

**方法**：
1. 時間戳分析
2. 8-byte 陣列追蹤
3. Event 相關性分析
4. 封包抓取分析
5. **Request 369 Response 監控** ← 當前重點

**預期**：
- 找到替代 KeySyncEvent 的新機制
- 實現主動獲取 XorCode（而非被動反推）
- 提升穩定性和準確度

**下一步**：
1. 運行增強版 DebugHandler
2. 觀察 Request 369 的 Response 是否包含 8-byte 陣列
3. 測試該 8-byte 陣列是否能解密玩家座標
4. 記錄 XorCode 更新的精確時間點與觸發條件
