# 簡化的座標Debug指南

## ✅ 已修正的問題

1. ❌ 移除了不存在的 `XorCodeScanner` 類
2. ❌ 修正了 `PlayersHandler.cs` 中重複的變量聲明
3. ✅ 增強了 `MoveEvent.cs` 的 debug 輸出
4. ✅ 簡化了 `PlayersHandler.cs` 的 debug 邏輯

## 📋 現在的 Debug 輸出

### MoveEvent.cs 會顯示：

```
[MoveEvent] ID:123456 ParamLen:42 Index:9
  Current (index=9): C0-E1-77-A5-BF-FA-F2-C1
  [MATCH!] index=1: (123.45, 456.78)  ← 如果找到合理的座標
  [MATCH!] index=5: (200.50, 350.25)
```

這會告訴我們：
- **parameter 陣列的總長度**
- **當前使用的 index (9)**
- **測試其他 index 值找到的合理座標**

### PlayersHandler.cs 會顯示：

```
[Position Debug] ID:123456 Name:PlayerName
  RawBytes: C0-E1-77-A5-BF-FA-F2-C1
  Bytes[0-3] (C0-E1-77-A5): 0.00
  Bytes[4-7] (BF-FA-F2-C1): -30.37
  Option1 - X=bytes[0-3], Y=bytes[4-7]: (0.00, -30.37)
  Option2 - X=bytes[4-7], Y=bytes[0-3]: (-30.37, 0.00)
  Decrypted: N/A [XorCode is NULL]
```

這會告訴我們：
- **bytes[0-3] 解析成什麼 float 值**
- **bytes[4-7] 解析成什麼 float 值**
- **兩種組合的結果**
- **是否有 XorCode**

---

## 🚀 測試步驟

### 1. 編譯（已完成）

```cmd
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" DEATHEYE.csproj /p:Configuration=Debug
```

### 2. 運行

```cmd
cd bin\Debug
DEATHEYE.exe
```

### 3. 進入遊戲

1. 啟動 Albion Online
2. 進入遊戲地圖
3. 等待其他玩家移動

### 4. 觀察輸出

重點關注：

#### 場景 A：MoveEvent 找到 [MATCH!]

```
[MoveEvent] ID:123456 ParamLen:42 Index:9
  [MATCH!] index=5: (123.45, 456.78)
```

**這很重要！** 這說明：
- 正確的 index 不是 9，而是 5（或其他值）
- 座標沒有加密，只是提取位置錯誤

**解決方案**：將 `MoveEvent.cs` 第 23 行改為：
```csharp
int index = 5;  // 改成正確的值
```

#### 場景 B：Bytes[0-3] 不是 0.00

```
[Position Debug] ID:123456 Name:PlayerName
  Bytes[0-3] (C0-E1-77-A5): -187.50
  Bytes[4-7] (BF-FA-F2-C1): -30.37
```

**這說明**：
- 兩個 bytes 都有有效值
- 可能需要調整解析順序

**檢查**：Option1 或 Option2 哪個看起來正確？

#### 場景 C：值仍然異常

```
[Position Debug] ID:123456 Name:PlayerName
  Bytes[0-3] (4F-2E-42-2A): -293740800000000000000000.00
  Bytes[4-7] (E0-CE-78-E6): -293547400000000000000000.00
```

**這說明**：
- 座標確實加密了
- 需要找到 XorCode

---

## 🎯 預期結果

### 最佳情況

```
[MoveEvent] ID:123456 ParamLen:42 Index:9
  [MATCH!] index=1: (123.45, 456.78)

[Position Debug] ID:123456 Name:PlayerName
  RawBytes: XX-XX-XX-XX-XX-XX-XX-XX
  Bytes[0-3] (...): 123.45
  Bytes[4-7] (...): 456.78
  Option1 - X=bytes[0-3], Y=bytes[4-7]: (123.45, 456.78) ← 合理！
```

✅ **找到了！** index=1 是正確的，座標沒有加密。

### 需要調查

```
[MoveEvent] ID:123456 ParamLen:42 Index:9
  Current (index=9): C0-E1-77-A5-BF-FA-F2-C1
  （沒有 [MATCH!]）

[Position Debug] ID:123456 Name:PlayerName
  Bytes[0-3] (C0-E1-77-A5): 0.00  ← 奇怪
  Bytes[4-7] (BF-FA-F2-C1): -30.37
```

⚠️ **問題**：bytes[0-3] 無法正確解析成 float

**可能原因**：
1. 這 4 個 bytes 不是 float 編碼
2. Endianness 問題（大端/小端）
3. 需要特殊解密

---

## 📊 診斷流程

根據輸出結果：

### 步驟 1：檢查 MoveEvent 輸出

- **有 [MATCH!]** → 修改 index 值，重新編譯測試
- **無 [MATCH!]** → 繼續看 PlayersHandler 輸出

### 步驟 2：檢查 PlayersHandler 輸出

#### Bytes[0-3] 和 Bytes[4-7] 都是合理值？
- **是** → 檢查 Option1 或 Option2 哪個正確
- **否** → 繼續診斷

#### Bytes[0-3] 是 0.00 但 Bytes[4-7] 正常？
- 可能只用了 4 bytes（不是 8 bytes）
- 或者 bytes[0-3] 是其他數據（不是座標）

#### 兩個都是巨大的科學記數值？
- 座標加密了
- 需要找到 XorCode

---

## 🔧 下一步行動

### 如果找到正確的 index

1. 修改 `MoveEvent.cs` 第 23 行：
   ```csharp
   int index = [正確的值];
   ```

2. 重新編譯測試

3. 確認玩家位置正確顯示

### 如果沒有找到 [MATCH!]

1. 將完整的 Console 輸出發給我
2. 特別是：
   - `ParamLen` 的值
   - `Bytes[0-3]` 和 `Bytes[4-7]` 的十六進制和 float 值
   - 任何 `[MATCH!]` 輸出

3. 我會分析並提供下一步建議

---

## 📝 記錄模板

請複製以下信息：

```
=== MoveEvent 輸出 ===
ParamLen: [值]
Current index: [值]
有 [MATCH!]：[是/否]
如果有，index=[值]: (X, Y)

=== PlayersHandler 輸出 ===
RawBytes: [十六進制]
Bytes[0-3]: [十六進制] → [float值]
Bytes[4-7]: [十六進制] → [float值]
Option1: (X, Y)
Option2: (X, Y)
XorCode: [NULL/exists]

=== 遊戲內實際情況 ===
玩家實際位置：[描述]
雷達顯示位置：[描述]
怪物位置：[正常/異常]
```

---

**請運行測試並將結果告訴我！**
