# XorCode 樣本分析與 SessionKey 反推

## 收集到的 XorCode 樣本

從 exp1.txt 中提取的 8 個 XorCode（每個間隔約 10 秒）：

```
樣本 1: 86-BA-A9-C5-D7-77-CD-21  (07:03:37.646)
樣本 2: 62-31-DC-45-91-A8-6B-97  (07:03:38.026) - 0.38s 後
樣本 3: D6-86-0E-0D-A9-38-78-EA  (07:03:48.009) - 9.88s 後
樣本 4: A8-0B-7F-17-F5-51-4B-05  (07:03:58.007) - 9.99s 後
樣本 5: 5F-8D-24-0B-EC-6E-07-41  (07:04:08.009) - 10.00s 後
樣本 6: A1-FE-B7-A6-66-CB-0F-1E  (07:04:17.998) - 9.99s 後
樣本 7: E9-84-E2-58-E4-12-C4-66  (07:04:28.058) - 10.01s 後
樣本 8: 60-48-64-1A-4B-B4-73-A5  (07:04:38.052) - 9.99s 後
```

## 分析方法

### 假設 1：簡單 XOR 加密

如果 XorCode 的生成算法是：
```
XorCode = SessionKey XOR TimeBlock
```

那麼：
```
SessionKey = XorCode_1 XOR TimeBlock_1
           = XorCode_2 XOR TimeBlock_2
           = ...
```

**問題**：我們不知道精確的 TimeBlock 值（伺服器時間）

### 假設 2：HMAC 或 Hash

如果使用 HMAC-SHA256 或類似算法：
```
XorCode = HMAC(SessionKey, TimeBlock)[:8]
```

**問題**：無法直接反推（單向函數）

### 假設 3：基於遊戲內時間

如果 TimeBlock 基於遊戲內時間（不是真實世界時間）：
```
TimeBlock = GameTime / 10_seconds
```

**可能性**：需要找到遊戲內時間的來源

---

## 觀察 XorCode 的規律

### 十六進制分析

```
Byte Position:  0    1    2    3    4    5    6    7
樣本 1:        86   BA   A9   C5   D7   77   CD   21
樣本 2:        62   31   DC   45   91   A8   6B   97
樣本 3:        D6   86   0E   0D   A9   38   78   EA
樣本 4:        A8   0B   7F   17   F5   51   4B   05
樣本 5:        5F   8D   24   0B   EC   6E   07   41
樣本 6:        A1   FE   B7   A6   66   CB   0F   1E
樣本 7:        E9   84   E2   58   E4   12   C4   66
樣本 8:        60   48   64   1A   4B   B4   73   A5
```

### 觀察

1. **看起來完全隨機**：沒有明顯的遞增或遞減模式
2. **高熵值**：每個 byte 都在變化
3. **沒有重複**：8 個樣本都不同

**結論**：很可能使用了強加密算法（HMAC/SHA256）

---

## 嘗試 XOR 差分分析

如果是簡單的 XOR，相鄰的 XorCode 之間應該有規律：

```
XorCode_2 XOR XorCode_1 = (SessionKey XOR TimeBlock_2) XOR (SessionKey XOR TimeBlock_1)
                        = TimeBlock_2 XOR TimeBlock_1
```

### 計算相鄰 XorCode 的 XOR 差值

```python
def xor_bytes(a, b):
    return bytes([x ^ y for x, y in zip(a, b)])

xor_1_2 = XorCode_2 XOR XorCode_1
xor_2_3 = XorCode_3 XOR XorCode_2
...
```

**如果這些差值有規律（例如遞增 1），則可能是簡單 XOR。**

---

## SessionKey 可能的位置

### 位置 1：登入時的 JoinResponse (Response 2)

**可能性**：⭐⭐⭐⭐⭐ 最高

- 登入時伺服器發送 SessionKey
- 客戶端存儲用於整個遊戲會話
- 可能是 8-byte 或 16-byte 的參數

**如何驗證**：
1. 重新登入遊戲
2. 捕獲 Response 2 (JoinResponse)
3. 尋找 8-byte 或 16-byte 陣列
4. 測試是否為 SessionKey

### 位置 2：初始化 Event

**可能性**：⭐⭐⭐⭐

- 進入地圖時的某個 Event
- 可能包含地圖專屬的 SessionKey

**如何驗證**：
1. 記錄進入地圖時的所有 Event
2. 尋找包含 8-byte 陣列的 Event
3. 測試是否為 SessionKey

### 位置 3：NewCharacterEvent (玩家出現)

**可能性**：⭐⭐⭐

- 每個玩家可能有專屬的 SessionKey
- 在 NewCharacterEvent 中傳輸

**如何驗證**：
1. 檢查 NewCharacterEvent 的所有參數
2. 尋找 8-byte 陣列
3. 測試是否能用來生成 XorCode

### 位置 4：隱藏在封包的固定位置

**可能性**：⭐⭐

- SessionKey 可能在每個 MoveEvent 的固定位置
- 但會被混淆或編碼

**觀察**：你之前看到的封包 pattern 變化
```
格式 A: XX-XX-D6-46-XX-XX-1F-F8
格式 B: XX-XX-C2-0C-XX-XX-54-92
格式 C: XX-XX-EA-D3-XX-XX-E5-4C
```

後 4 bytes 可能包含時間戳或密鑰相關信息

### 位置 5：客戶端硬編碼

**可能性**：⭐

- SessionKey 可能在遊戲程式中硬編碼
- 所有客戶端使用相同的 SessionKey

**如何驗證**：
1. 反編譯遊戲客戶端
2. 尋找 XorCode 相關代碼
3. 提取 SessionKey

---

## 反推 SessionKey 的實驗計劃

### 實驗 A：時間戳測試

**假設**：SessionKey 是固定的，TimeBlock 是連續的

```python
# 如果 XorCode = SessionKey XOR TimeBlock
# 且 TimeBlock 每 10 秒遞增 1

TimeBlock_1 = N
TimeBlock_2 = N + 1
TimeBlock_3 = N + 2
...

SessionKey = XorCode_1 XOR N
           = XorCode_2 XOR (N + 1)
           = XorCode_3 XOR (N + 2)
```

**測試**：
1. 假設 N = 0, 1, 2, ... 1000
2. 對每個 N，計算 `SK = XorCode_1 XOR N`
3. 驗證 `XorCode_2 == SK XOR (N+1)` 是否成立
4. 如果成立，找到了 SessionKey 和起始 TimeBlock

### 實驗 B：捕獲 JoinResponse

**步驟**：
1. 重新啟動遊戲並登入
2. 捕獲所有初始 Response 和 Event
3. 尋找 8-byte 陣列
4. 測試這些陣列是否為 SessionKey

### 實驗 C：封包 Pattern 分析

**觀察後 4 bytes 的規律**：
```
格式 A: 1F-F8
格式 B: 54-92
格式 C: E5-4C
格式 D: 95-49
```

測試這些值與 TimeBlock 的關係

---

## 下一步行動

### 優先級 1：嘗試簡單 XOR 反推

編寫程式測試 XorCode 是否使用簡單 XOR：

```csharp
// 測試是否 XorCode = SessionKey XOR TimeBlock
for (int baseTime = 0; baseTime < 10000; baseTime++)
{
    byte[] candidateKey = XOR(xorCode1, BitConverter.GetBytes(baseTime));

    bool allMatch = true;
    for (int i = 1; i < xorCodes.Count; i++)
    {
        byte[] expected = XOR(candidateKey, BitConverter.GetBytes(baseTime + i));
        if (!expected.SequenceEqual(xorCodes[i]))
        {
            allMatch = false;
            break;
        }
    }

    if (allMatch)
    {
        Console.WriteLine($"Found SessionKey: {BitConverter.ToString(candidateKey)}");
        Console.WriteLine($"Starting TimeBlock: {baseTime}");
        return;
    }
}
```

### 優先級 2：捕獲初始化封包

重新登入並記錄：
- Response 2 (JoinResponse)
- 前 10 個 Event
- 所有包含 8-byte 陣列的封包

### 優先級 3：分析封包 Pattern

研究後 4 bytes 的規律，可能包含時間信息

---

## 總結

**我們已知**：
- ✅ XorCode 每 10 秒更新一次
- ✅ 有 8 個連續的 XorCode 樣本
- ❓ SessionKey 的位置未知

**推薦行動**：
1. 先嘗試簡單 XOR 反推（最快）
2. 捕獲 JoinResponse（最可能）
3. 分析封包 Pattern（深入研究）

**最可能的位置**：
- JoinResponse (Response 2) ⭐⭐⭐⭐⭐
- 初始化 Event ⭐⭐⭐⭐
- NewCharacterEvent ⭐⭐⭐
