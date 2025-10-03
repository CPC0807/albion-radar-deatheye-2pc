# XorCode 真實生命週期發現

## 重大發現

**之前記錄的生命週期（53秒、36秒）是錯誤的！**

XorCode 實際上在更早的時候就已經改變，但因為檢測不夠嚴格，沒有被標記為失效。

---

## 問題分析

### 舊的失效檢測邏輯

```csharp
bool isInvalid = Math.Abs(position.X) > 10000f ||
                 Math.Abs(position.Y) > 10000f ||
                 float.IsNaN(position.X) ||
                 float.IsNaN(position.Y) ||
                 float.IsInfinity(position.X) ||
                 float.IsInfinity(position.Y);
```

**問題**：只檢測極端值（超過 10000 或 NaN/Infinity）

### 實際發生的情況

```
06:22:43.737 - XorCode B4-B0-5C-74 恢復成功
[Decrypted] (33.94, 28.00)   ← ✅ 正確
[Decrypted] (33.93, 28.01)   ← ✅ 正確
[Decrypted] (0.00, 7.98)     ← ❌ XorCode 已改變！但未檢測到
[Decrypted] (0.00, 7.98)     ← ❌ 繼續用錯誤的 XorCode
[Decrypted] (0.00, 2028.55)  ← ❌ 越來越錯
[Decrypted] (-1483.11, -0.01)← ❌ 嚴重錯誤
...
[Decrypted] (3195594000...00)← ❌ 天文數字，終於觸發失效！

06:23:36.809 - XorCode EXPIRED
記錄的生命週期：53.07 秒
```

**真相**：
- 實際失效時間：可能在恢復後 **1-2 秒**
- 記錄的失效時間：53 秒後（當解密出天文數字時）
- 差距：**51 秒的誤差！**

---

## 為什麼 (0.00, 7.98) 沒有觸發失效？

### 檢查條件

1. ❌ `Math.Abs(0.00) > 10000`？ → No
2. ❌ `Math.Abs(7.98) > 10000`？ → No
3. ❌ `float.IsNaN(0.00)`？ → No
4. ❌ `float.IsInfinity(7.98)`？ → No

**結果**：所有檢查都通過，沒有被標記為失效！

### 但這是明顯的錯誤

**已知事實**：
- 本地玩家在 (33.94, 28.00)
- 朋友站在同一位置（沒有移動）
- XorCode 是用 (33.94, 28.00) 反推的
- **理論上**解密結果應該接近 (33.94, 28.00)

**實際結果**：
- 解密出 (0.00, 7.98)
- 距離本地玩家 ≈ 39 單位

**結論**：XorCode 已經改變了！

---

## 新的失效檢測邏輯

### 增強檢測 #1：極端值（保留）

```csharp
if (Math.Abs(position.X) > 10000f || Math.Abs(position.Y) > 10000f ||
    float.IsNaN(position.X) || float.IsNaN(position.Y) ||
    float.IsInfinity(position.X) || float.IsInfinity(position.Y))
{
    isInvalid = true;
}
```

### 增強檢測 #2：距離警告

```csharp
float distanceToLocal = Vector2.Distance(position, LocalPlayerPosition.Value);

if (distanceToLocal > 5000f)
{
    Console.WriteLine($"[WARNING] Decrypted position too far: {distanceToLocal:F2} units");
    // 暫時不標記為失效（可能是遠處的玩家）
}
```

### 增強檢測 #3：近距離異常檢測 ⭐

```csharp
// 如果朋友應該在你旁邊（用你的座標反推的 XorCode）
// 但解密出來距離很遠 → XorCode 肯定錯了
if (distanceToLocal > 10f)  // 超過 10 單位就懷疑
{
    Console.WriteLine($"[SUSPICIOUS] Distance: {distanceToLocal:F2} units");
    Console.WriteLine($"[XorCode LIKELY CHANGED] Marking as expired");
    isInvalid = true;
}
```

**核心邏輯**：
- XorCode 是用你的座標 (33.94, 28.00) 反推的
- 朋友站在你旁邊
- 如果解密出 (0.00, 7.98)，距離 39 單位
- **距離 > 10 單位** → XorCode 肯定錯了！

---

## 預期改進效果

### 舊版本

```
[Decrypted] (33.94, 28.00)  ← 成功
[Decrypted] (0.00, 7.98)    ← 沒有檢測到失效
[Decrypted] (0.00, 2028.55) ← 沒有檢測到失效
...
[Decrypted] (天文數字)       ← 53 秒後才檢測到

記錄生命週期：53 秒 ❌ 錯誤
```

### 新版本

```
[Decrypted] (33.94, 28.00)  ← 成功
[Decrypted] (0.00, 7.98)    ← ⚠️ 觸發！
  [SUSPICIOUS] Distance: 39.05 units
  [XorCode LIKELY CHANGED] Marking as expired
  [LIFETIME] XorCode lasted: 1.23 seconds ✅

記錄生命週期：1-2 秒 ✅ 正確
```

---

## 測試期望

### 期望結果 1：發現真實週期

```
[LIFETIME] XorCode lasted: 1.47 seconds
[LIFETIME] XorCode lasted: 1.52 seconds
[LIFETIME] XorCode lasted: 1.49 seconds

[STATISTICS] Count: 3
  Average: 1.49s
  Min: 1.47s, Max: 1.52s
  Variance: 0.05s

[!!!] PATTERN DETECTED: XorCode changes every ~1 seconds!
```

### 期望結果 2：發現固定週期

```
[LIFETIME] XorCode lasted: 30.02 seconds
[LIFETIME] XorCode lasted: 29.98 seconds
[LIFETIME] XorCode lasted: 30.01 seconds

[!!!] PATTERN DETECTED: XorCode changes every ~30 seconds!
[!!!] This suggests TIME-BASED XorCode generation!
```

---

## 關鍵洞察

### 1. 為什麼會誤判？

**根本原因**：用錯誤的 XorCode 解密，仍可能得到「看起來合理」的座標

```
正確：
  加密座標 XOR 正確XorCode = (33.94, 28.00) ✅

錯誤但沒被發現：
  加密座標 XOR 錯誤XorCode = (0.00, 7.98)  ← 看起來「合理」
  - 不是 NaN
  - 不是 Infinity
  - 不超過 10000
  - ❌ 但距離本地玩家很遠！
```

### 2. 為什麼距離檢測有效？

**假設前提**：
- 你和朋友站在同一位置
- XorCode 是用你的座標反推的

**推論**：
- 如果 XorCode 正確 → 解密出來的座標 ≈ 你的座標
- 如果 XorCode 錯誤 → 解密出來的座標 ≠ 你的座標
- **距離檢測可以立即發現 XorCode 錯誤！**

### 3. 限制條件

**此方法僅適用於**：
- 朋友站在你旁邊（同一座標）
- XorCode 用你的座標反推

**不適用於**：
- 正常遊戲（其他玩家在遠處）
- XorCode 通過其他方式獲得

---

## 下一步測試

1. **重新測試生命週期**
   - 用新的檢測邏輯
   - 記錄真實的 XorCode 變化時間

2. **驗證週期規律**
   - 是否為固定週期（1秒、30秒、60秒？）
   - 變異數是否 < 5 秒

3. **尋找觸發條件**
   - 記錄失效前的 Request/Response/Event
   - 確認是時間導向還是事件觸發

---

## 結論

**重要發現**：
1. ❌ 之前記錄的生命週期（53秒、36秒）是錯誤的
2. ✅ 真實生命週期可能只有 1-2 秒（需要重新測試確認）
3. ✅ 新的距離檢測可以立即發現 XorCode 改變
4. ⏳ 需要重新測試以獲得準確的生命週期數據

**期待**：新版本將揭示 XorCode 的真實變化規律！
