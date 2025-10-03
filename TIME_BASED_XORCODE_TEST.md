# 時間導向 XorCode 假設測試指南

## 核心假設

**XorCode 可能是基於時間動態生成的，而非透過網絡封包傳輸！**

### 為什麼這個假設合理？

1. ✅ 解釋了為什麼找不到 KeySync Event
2. ✅ 解釋了為什麼同一玩家、同一位置會失效
3. ✅ 解釋了為什麼不會造成遊戲同步問題
4. ✅ 符合現代遊戲加密設計（類似 TOTP）

### 可能的實現方式

**方式 A：固定週期**
```
每 30 秒更換一次 XorCode
XorCode = Hash(SessionKey + floor(UnixTime / 30))
```

**方式 B：動態種子**
```
登入時獲得 Seed
XorCode = HMAC(Seed, CurrentTime)
```

**方式 C：雙向同步**
```
客戶端和伺服器都使用相同算法
基於共享密鑰 + 時間戳生成
```

---

## 已實現的追蹤功能

### PlayersHandler.cs 增強

**新增功能**：

1. **記錄 XorCode 恢復時間**
   ```csharp
   xorCodeRecoveredTime = DateTime.UtcNow;
   ```

2. **記錄 XorCode 失效時間**
   ```csharp
   xorCodeExpiredTime = DateTime.UtcNow;
   ```

3. **計算生命週期**
   ```csharp
   double lifetime = (expiredTime - recoveredTime).TotalSeconds;
   ```

4. **統計分析（3 次以上）**
   - 平均壽命
   - 最小/最大值
   - 變異數

5. **自動檢測固定週期**
   - 如果變異數 < 5 秒 → 顯示警告
   - 提示可能是時間導向機制

---

## 測試步驟

### 第 1 步：啟動程式並等待自動恢復

1. 啟動 DEATHEYE.exe
2. 進入遊戲
3. 移動一下（觸發 MoveRequest，獲取 LocalPlayerPosition）
4. 等待朋友站到你旁邊（同一座標）
5. 觀察 Console 輸出

**預期輸出**：
```
[SUCCESS!] XorCode recovered: AA-BB-CC-DD-EE-FF-11-22
[TIME] Recovered at: 14:25:30.123
```

### 第 2 步：保持不動，等待失效

**關鍵**：你和朋友都不要移動！

**預期輸出（失效時）**：
```
[XorCode EXPIRED] Decrypted position is invalid
[LIFETIME] XorCode lasted: 28.47 seconds
[RECOVERED] at 14:25:30.123
[EXPIRED]   at 14:25:58.593
```

### 第 3 步：重複 3-5 次，觀察規律

每次失效後：
1. XorCode 自動重置為 null
2. 下次收到朋友的位置更新時，再次恢復
3. 記錄新的生命週期

**預期輸出（第 3 次失效後）**：
```
[STATISTICS] Count: 3
  Average: 29.34s
  Min: 28.47s, Max: 30.12s
  Variance: 1.65s

[!!!] PATTERN DETECTED: XorCode changes every ~29 seconds!
[!!!] This suggests TIME-BASED XorCode generation!
```

---

## 判斷標準

### 時間導向的證據

**強證據**：
- ✅ 生命週期穩定（變異數 < 5 秒）
- ✅ 平均值接近整數（如 30s, 60s）
- ✅ 不受玩家移動影響
- ✅ 不受網絡封包影響

**弱證據**：
- ⚠️ 生命週期波動較大（5-10 秒）
- ⚠️ 無明顯週期規律

### 封包導向的證據

**如果是封包觸發**：
- ❌ 每次失效前都有特定 Event/Request/Response
- ❌ 生命週期不規律
- ❌ 受玩家動作影響（移動、戰鬥、傳送）

---

## 測試案例

### 案例 1：固定週期（30 秒）

**結果**：
```
Lifetime 1: 30.12s
Lifetime 2: 29.87s
Lifetime 3: 30.05s
Average: 30.01s
Variance: 0.25s
```

**結論**：✅ 高度確認時間導向機制

### 案例 2：不規律

**結果**：
```
Lifetime 1: 15.23s
Lifetime 2: 42.87s
Lifetime 3: 8.91s
Average: 22.34s
Variance: 33.96s
```

**結論**：❌ 可能是事件觸發，繼續追蹤封包

### 案例 3：雙週期（30s 或 60s）

**結果**：
```
Lifetime 1: 30.05s
Lifetime 2: 60.12s
Lifetime 3: 29.98s
Lifetime 4: 59.87s
```

**結論**：⚠️ 可能基於特定條件切換週期（需進一步測試）

---

## 如果確認是時間導向

### 下一步行動

1. **尋找 Seed/SessionKey 來源**
   - 檢查登入 Response (JoinResponse)
   - 尋找初始化 Event
   - 查找 8-byte 或 16-byte 的「種子」參數

2. **反推時間算法**
   ```csharp
   // 已知：
   - XorCode 週期（如 30 秒）
   - 某時刻的有效 XorCode
   - 該時刻的時間戳

   // 目標：
   找出 Hash/HMAC 算法和 Seed
   ```

3. **實現預測機制**
   ```csharp
   // 如果知道算法
   byte[] PredictNextXorCode(DateTime currentTime)
   {
       long timeBlock = currentTime.Ticks / TimeSpan.FromSeconds(30).Ticks;
       return ComputeXorCode(SessionSeed, timeBlock);
   }
   ```

---

## 如果不是時間導向

### 替代假設

1. **封包觸發但我們錯過了**
   - 回到監控 Request 369 Response
   - 檢查其他頻繁 Event (160, 272)

2. **玩家專屬 XorCode**
   - 每個玩家有不同密鑰
   - 需要修改架構支持多密鑰

3. **區域性 XorCode**
   - 不同地圖/區域用不同密鑰
   - 玩家進出時更新

---

## 測試注意事項

### 必須保持條件

1. ✅ **你和朋友站在同一座標**（否則無法反推）
2. ✅ **都不要移動**（避免干擾時間測量）
3. ✅ **記錄完整輸出**（包含時間戳）
4. ✅ **至少測試 3-5 次**（統計顯著性）

### 干擾因素

1. ❌ 網絡延遲（可能影響時間測量）
2. ❌ 其他玩家進入視野（可能觸發額外事件）
3. ❌ 戰鬥/受傷（可能改變加密機制）
4. ❌ 換地圖（會重置 XorCode）

---

## 預期輸出示例

### 成功案例

```
[UpdatePosition] ID:12345 Name:TestPlayer
  RawBytes: 3A-F2-8C-1D-4B-9E-27-C5-...
  [BruteForce] Attempting to recover XorCode...
  [MyPosition] (33.94, 28.00)
  [SUCCESS!] XorCode recovered: AA-BB-CC-DD-EE-FF-11-22
  [TIME] Recovered at: 14:25:30.123
  [Decrypted] (33.94, 28.00)

... (29 秒後) ...

[UpdatePosition] ID:12345 Name:TestPlayer
  RawBytes: 7D-A1-3F-8B-...
  [Decrypted] (-3472789000000000000.00, 0.00)
  [XorCode EXPIRED] Decrypted position is invalid
  [LIFETIME] XorCode lasted: 29.47 seconds
  [RECOVERED] at 14:25:30.123
  [EXPIRED]   at 14:25:59.593

... (再次恢復) ...

[SUCCESS!] XorCode recovered: 11-22-33-44-55-66-77-88
[TIME] Recovered at: 14:26:00.012

... (30 秒後再次失效) ...

[XorCode EXPIRED] Decrypted position is invalid
[LIFETIME] XorCode lasted: 30.15 seconds
[STATISTICS] Count: 2
  Average: 29.81s
  Min: 29.47s, Max: 30.15s
  Variance: 0.68s

... (第 3 次) ...

[STATISTICS] Count: 3
  Average: 29.92s
  Min: 29.47s, Max: 30.15s
  Variance: 0.68s

[!!!] PATTERN DETECTED: XorCode changes every ~30 seconds!
[!!!] This suggests TIME-BASED XorCode generation!
```

---

## 總結

**測試目標**：確認 XorCode 是否基於時間動態生成

**關鍵指標**：生命週期的穩定性和規律性

**成功標準**：變異數 < 5 秒，且接近整數週期

**失敗標準**：生命週期完全不規律，或與特定封包強相關

---

## 快速檢查清單

執行測試前：
- [ ] 已編譯最新版本
- [ ] 朋友已上線並準備合作
- [ ] 選擇安靜的測試地點（避免其他玩家）
- [ ] Console 輸出可見（或記錄到文件）

執行測試：
- [ ] 移動觸發 MoveRequest
- [ ] 朋友站到同一座標
- [ ] 觀察第一次恢復
- [ ] 保持不動等待失效
- [ ] 重複至少 3 次
- [ ] 記錄所有時間數據

分析結果：
- [ ] 計算平均生命週期
- [ ] 檢查變異數
- [ ] 判斷是否有規律
- [ ] 決定下一步行動
