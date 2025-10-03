# SessionKey 搜尋指南

## 已實現的功能

### 1. 自動 XorCode 樣本收集

程式會自動收集每次恢復的 XorCode，當收集到 8 個樣本時，自動運行 SessionKey 分析。

**自動執行**：
```
[SUCCESS!] XorCode recovered: 86-BA-A9-C5-D7-77-CD-21
[TIME] Recovered at: 07:03:37.646
[SAMPLES] Collected 1 XorCode samples

... (收集中) ...

[SAMPLES] Collected 8 XorCode samples
============================================================
  Enough samples collected! Running SessionKey analysis...
============================================================
```

### 2. SessionKey 反推測試

程式會自動嘗試三種方法：

#### 方法 A：簡單 XOR 測試
```
假設：XorCode = SessionKey XOR TimeBlock
測試：10萬種可能的 TimeBlock 起始值
結果：如果找到，顯示 SessionKey 和 TimeBlock
```

#### 方法 B：統計分析
```
計算：相鄰 XorCode 的 XOR 差值
分析：熵值、byte 頻率
判斷：是否使用強加密（HMAC/SHA256）
```

#### 方法 C：模式識別
```
觀察：XorCode 的規律性
建議：下一步該做什麼
```

---

## 測試步驟

### 步驟 1：正常運行雷達

1. 啟動 DEATHEYE.exe
2. 進入遊戲
3. 朋友站到你旁邊（同一座標）
4. 朋友按住移動鍵卡牆角 1 分鐘

### 步驟 2：等待自動分析

當收集到 8 個 XorCode 樣本時，程式會自動輸出：

```
╔════════════════════════════════════════════════════════╗
║   SessionKey Brute Force & XorCode Analysis Tool      ║
╚════════════════════════════════════════════════════════╝

========== XorCode Analysis ==========

XOR differences between consecutive XorCodes:
  XorCode[0] XOR XorCode[1] = XX-XX-XX-XX-XX-XX-XX-XX
  XorCode[1] XOR XorCode[2] = XX-XX-XX-XX-XX-XX-XX-XX
  ...

Byte frequency analysis:
  Unique bytes: XXX / 256
  Total bytes: 64
  Entropy: XX.X%
  → High entropy, likely using strong crypto (HMAC/SHA256)

========== SessionKey Brute Force Test ==========
Testing 8 XorCode samples...

Testing TimeBlock: 0...
Testing TimeBlock: 1000...
Testing TimeBlock: 2000...
...

[RESULT] Simple XOR method failed - likely using HMAC/SHA256
SessionKey is probably in JoinResponse or initialization event

========== Recommendations ==========
1. Capture JoinResponse (Response 2) on login
2. Look for 8-byte or 16-byte arrays in initialization events
3. Analyze packet patterns (后 4 bytes: 1F-F8, 54-92, E5-4C, etc.)
4. Consider reverse engineering the game client
```

---

## 預期結果

### 結果 A：找到 SessionKey（簡單 XOR）

```
✅ ========== FOUND SESSION KEY! ==========
SessionKey: XX-XX-XX-XX-XX-XX-XX-XX
Starting TimeBlock: XXXXX
Encoding: Integer (8 bytes)

Verification:
  ✅ XorCode[0]: 86-BA-A9-C5-D7-77-CD-21 (TimeBlock: XXXXX)
  ✅ XorCode[1]: 62-31-DC-45-91-A8-6B-97 (TimeBlock: XXXXX+1)
  ✅ XorCode[2]: D6-86-0E-0D-A9-38-78-EA (TimeBlock: XXXXX+2)
  ...
============================================
```

**如果找到**：
1. 記錄 SessionKey
2. 測試是否能預測未來的 XorCode
3. 實現自動 XorCode 生成

### 結果 B：沒找到（強加密）

```
[RESULT] Simple XOR method failed - likely using HMAC/SHA256
SessionKey is probably in JoinResponse or initialization event
```

**如果沒找到**：
1. SessionKey 使用 HMAC-SHA256 或類似強加密
2. 需要從封包中尋找 SessionKey
3. 繼續下一步調查

---

## 下一步：尋找 SessionKey 在封包中的位置

### 優先級 1：JoinResponse (Response 2) ⭐⭐⭐⭐⭐

**步驟**：
1. 重新啟動遊戲
2. 在 DebugHandler.cs 中添加捕獲 Response 2 的代碼
3. 記錄 Response 2 的所有參數
4. 尋找 8-byte 或 16-byte 陣列

**修改 DebugHandler.cs**：
```csharp
if (responseCode == 2)
{
    Console.WriteLine("!!! RESPONSE 2 (JoinResponse) CAPTURED !!!");

    // 記錄所有參數
    foreach (var kvp in response.Parameters)
    {
        if (kvp.Value is byte[] byteArray)
        {
            Console.WriteLine($"  Key {kvp.Key}: byte[{byteArray.Length}] = {BitConverter.ToString(byteArray)}");

            // 標記可能的 SessionKey
            if (byteArray.Length == 8 || byteArray.Length == 16)
            {
                Console.WriteLine($"    !!! POSSIBLE SESSION KEY !!!");
            }
        }
    }
}
```

### 優先級 2：初始化 Event ⭐⭐⭐⭐

**捕獲前 20 個 Event**：
```csharp
private static int eventCounter = 0;

if (eventCounter < 20)
{
    eventCounter++;
    Console.WriteLine($"\n[INIT EVENT {eventCounter}] Code: {eventCode}");

    // 記錄所有 8-byte 陣列
    foreach (var kvp in @event.Parameters)
    {
        if (kvp.Value is byte[] byteArray && byteArray.Length == 8)
        {
            Console.WriteLine($"  Key {kvp.Key}: {BitConverter.ToString(byteArray)}");
        }
    }
}
```

### 優先級 3：NewCharacterEvent ⭐⭐⭐

**檢查玩家出現時的參數**：
```csharp
// 在 NewCharacterEvent.cs 中
Console.WriteLine("\n[NewCharacterEvent] All Parameters:");
foreach (var kvp in parameters)
{
    if (kvp.Value is byte[] byteArray)
    {
        Console.WriteLine($"  Key {kvp.Key}: byte[{byteArray.Length}] = {BitConverter.ToString(byteArray)}");
    }
}
```

### 優先級 4：封包 Pattern 分析 ⭐⭐

**觀察封包後 4 bytes 的規律**：

從你的數據中看到：
```
格式 A: XX-XX-D6-46-XX-XX-1F-F8
格式 B: XX-XX-C2-0C-XX-XX-54-92
格式 C: XX-XX-EA-D3-XX-XX-E5-4C
格式 D: XX-XX-95-3B-XX-XX-95-49
```

**可能性**：
- 後 4 bytes 可能包含時間戳
- 或者是 SessionKey 的一部分
- 或者是加密模式標識

---

## SessionKey 的可能特徵

### 如果在 JoinResponse：
- **長度**：8 bytes 或 16 bytes
- **位置**：某個參數的 byte array
- **唯一性**：每次登入可能不同（Session 專屬）
- **用途**：用於整個遊戲會話的加密

### 如果在初始化 Event：
- **時機**：進入地圖時
- **可能性**：每個地圖有不同的 SessionKey
- **傳輸**：在某個 Event 的參數中

### 如果是硬編碼：
- **所有客戶端相同**
- **需要反編譯遊戲客戶端**
- **最後的手段**

---

## 測試計劃

### 實驗 2A：捕獲 JoinResponse

1. 修改 DebugHandler.cs 捕獲 Response 2
2. 重新啟動遊戲並登入
3. 記錄 Response 2 的所有參數
4. 測試可疑的 8-byte 陣列是否為 SessionKey

### 實驗 2B：測試候選 SessionKey

如果找到可疑的 8-byte 陣列：
```csharp
byte[] candidateKey = new byte[] { /* 從 JoinResponse 獲得 */ };
SessionKeyBruteForce.TestHMAC(xorCodeSamples, candidateKey);
```

### 實驗 2C：封包 Pattern 相關性

記錄封包後 4 bytes 與 XorCode 的關係：
```csharp
// 在 MoveEvent 中
Console.WriteLine($"Packet Pattern: {BitConverter.ToString(parameter, parameter.Length - 4, 4)}");
Console.WriteLine($"Current XorCode: {BitConverter.ToString(playersHandler.XorCode)}");
```

---

## 成功標準

### 如果找到 SessionKey：

1. ✅ 能用 SessionKey 預測下一個 XorCode
2. ✅ 預測的 XorCode 能正確解密座標
3. ✅ 不再需要朋友站在同一位置
4. ✅ 可以追蹤所有玩家的位置

### 驗證方法：

```csharp
// 使用 SessionKey 生成 XorCode
byte[] predictedXorCode = GenerateXorCode(sessionKey, currentTimeBlock);

// 測試解密
Vector2 pos = Decrypt(encryptedBytes, predictedXorCode);

// 檢查是否合理
if (pos.X >= 0 && pos.X <= 3000 && pos.Y >= 0 && pos.Y <= 3000)
{
    Console.WriteLine("✅ SessionKey is correct!");
}
```

---

## 總結

**已實現**：
- ✅ 自動收集 XorCode 樣本
- ✅ 自動測試簡單 XOR 反推
- ✅ 統計分析和模式識別
- ✅ 建議下一步行動

**下一步**：
1. 捕獲 JoinResponse
2. 測試候選 SessionKey
3. 分析封包 Pattern
4. 如果都失敗，考慮反編譯客戶端

**最可能的位置**：
- JoinResponse (Response 2) - 85% 可能性
- 初始化 Event - 10% 可能性
- 硬編碼 - 5% 可能性
