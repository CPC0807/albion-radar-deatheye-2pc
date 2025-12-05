# 🔴 巨大負數位置問題診斷

## 問題現象

```
[PlayerPos] ID:239589 Name:Draking12 Pos:(0.00,-8039084000000.00) NewPos:(0.00,-7323678000000.00) Speed:11.83
```

位置值為 **-8039084000000**（負 8 兆），這是**完全錯誤**的數值！

## 可能的原因

### 1. ❌ XorCode 錯誤或為 NULL

**症狀**：使用錯誤的密鑰解密會產生垃圾數據。

**檢查方法**：
- 查看是否有 `[KeySync] XorCode received!` 訊息
- 確認 XorCode 長度為 8 字節

**如果 KeySync 為 NULL**：
```
[XorCode] WARNING: XorCode is NULL! Cannot decrypt positions.
```
這意味著未收到 KeySync 事件，需要：
1. 切換遊戲地圖/區域
2. 確認 KeySync 事件 ID（當前為 593）是否正確

### 2. ❌ 位置數據格式已改變

Albion Online 可能更改了位置數據的格式：

**可能的變化**：
- ✅ X/Y 字節順序對調（原本 Y 在前 4 bytes，X 在後 4 bytes）
- ✅ 座標不再加密（直接存儲浮點數）
- ✅ 使用了新的加密算法
- ✅ 增加了額外的偏移或填充字節

### 3. ❌ 解密算法不正確

當前解密邏輯：
```csharp
var x = coordinates.Skip(offset).Take(4).ToArray();  // 前 4 bytes
var y = coordinates.Skip(offset + 4).Take(4).ToArray();  // 後 4 bytes

Decrypt(x, code, 0);  // 用 XorCode[0-3] 解密 X
Decrypt(y, code, 4);  // 用 XorCode[4-7] 解密 Y

return new[] { BitConverter.ToSingle(x, 0), BitConverter.ToSingle(y, 0) };
```

如果算法已改變，這個解密會失敗。

## 診斷工具

我已經添加了**異常位置檢測**，當位置值超過 ±100000 時會輸出詳細信息：

```
========== [ABNORMAL POSITION DETECTED!] ==========
  Player ID: 239589
  Decrypted Position: (0.00, -8039084000000.00)
  Raw positionBytes: 12-34-56-78-9A-BC-DE-F0
  XorCode: AA-BB-CC-DD-EE-FF-00-11
  XorCode Length: 8
  Direct read (X at 0, Y at 4): (1234.56, 5678.90)
  Reverse read (X at 4, Y at 0): (5678.90, 1234.56)
==================================================
```

## 測試方案

### 方案 A：嘗試不解密（座標可能不再加密）

修改 `Decrypt()` 方法，臨時禁用解密：

```csharp
public float[] Decrypt(byte[] coordinates, int offset = 0)
{
    // 測試：直接讀取不解密
    try
    {
        float x = BitConverter.ToSingle(coordinates, offset);
        float y = BitConverter.ToSingle(coordinates, offset + 4);
        Console.WriteLine($"[TEST] Unencrypted read: ({x:F2}, {y:F2})");
        return new[] { x, y };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TEST] Failed: {ex.Message}");
        return new float[] { 0f, 0f };
    }
}
```

### 方案 B：嘗試反向字節順序

修改 `UpdatePlayerPosition()` 測試反向讀取：

```csharp
// 測試：X 和 Y 順序對調
Vector2 position = new Vector2(pos[1], pos[0]);  // 反過來
Vector2 newPosition = new Vector2(newPos[1], newPos[0]);
```

### 方案 C：使用暴力搜索找到正確的 KeySync

如果 KeySync 事件 ID 變了：

1. 在 `Radar/Init.cs:131-137` 取消註解暴力搜索
2. 重新編譯並運行
3. 進入遊戲並切換地圖
4. 查看控制台：
```
[FOUND!] Event 612 has 8-byte code: 12-34-56-78-9A-BC-DE-F0
```

### 方案 D：檢查 Move 事件的偏移配置

查看 `jsons/offsets.json`：

```json
{
  "Move": [0, 1, 2]
}
```

確認位置數據提取是否正確（`MoveEvent.cs:23-25`）：
```csharp
int index = 9;  // 位置從第 9 字節開始
PositionBytes = new byte[8];
Array.Copy(parameter, index, PositionBytes, 0, 8);
```

如果這個 `index = 9` 不正確，位置數據會錯誤。

## 立即行動步驟

### 步驟 1：重新編譯並運行

```bash
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU /t:Rebuild
cd bin\Debug
VRise.exe
```

### 步驟 2：觀察控制台輸出

當看到異常位置時，會自動輸出詳細診斷信息。

**重點觀察**：
- ✅ `Direct read` 的值是否合理（通常 0-10000）？
- ✅ `Reverse read` 的值是否合理？
- ✅ XorCode 是否為 NULL？
- ✅ Raw positionBytes 是什麼？

### 步驟 3：根據診斷結果調整

#### 如果 Direct read 的值正常：
```
Direct read (X at 0, Y at 4): (1234.56, 5678.90)  ← 正常值！
```

**結論**：座標**不再加密**，直接使用即可。

**修復**：在 `UpdatePlayerPosition()` 中改為：
```csharp
// 不解密，直接讀取
Vector2 position = new Vector2(
    BitConverter.ToSingle(positionBytes, 0),
    BitConverter.ToSingle(positionBytes, 4)
);
```

#### 如果 Reverse read 的值正常：
```
Reverse read (X at 4, Y at 0): (1234.56, 5678.90)  ← 正常值！
```

**結論**：X 和 Y 的位置**對調了**。

**修復**：調整讀取順序：
```csharp
Vector2 position = new Vector2(pos[1], pos[0]);  // 反向
```

#### 如果兩者都不正常：

**結論**：加密算法已改變或 Move 事件的偏移錯誤。

**下一步**：
1. 使用 DebugHandler 記錄 Move 事件的完整結構
2. 查看社區/Discord 是否有更新的解密算法
3. 嘗試逆向工程新算法

## 快速測試腳本

在 `UpdatePlayerPosition()` 開頭添加：

```csharp
#if DEBUG
// 快速測試：嘗試所有可能的讀取方式
if (positionBytes != null && positionBytes.Length == 8)
{
    Console.WriteLine($"\n=== Testing position decode for ID {id} ===");

    // 方法 1：直接讀取 (X at 0, Y at 4)
    float x1 = BitConverter.ToSingle(positionBytes, 0);
    float y1 = BitConverter.ToSingle(positionBytes, 4);
    Console.WriteLine($"  Method 1 (X@0, Y@4): ({x1:F2}, {y1:F2})");

    // 方法 2：反向讀取 (X at 4, Y at 0)
    float x2 = BitConverter.ToSingle(positionBytes, 4);
    float y2 = BitConverter.ToSingle(positionBytes, 0);
    Console.WriteLine($"  Method 2 (X@4, Y@0): ({x2:F2}, {y2:F2})");

    // 方法 3：使用解密
    float[] decrypted = Decrypt(positionBytes);
    Console.WriteLine($"  Method 3 (Decrypt): ({decrypted[0]:F2}, {decrypted[1]:F2})");

    Console.WriteLine($"======================================\n");
}
#endif
```

## 預期結果

找到正確的讀取方法後：

```
========== [Testing] ==========
  Method 1 (X@0, Y@4): (1234.56, 5678.90)  ← 這個正常！
  Method 2 (X@4, Y@0): (5678.90, 1234.56)
  Method 3 (Decrypt): (0.00, -8039084000000.00)
===============================

[PlayerPos] ID:239589 Name:Draking12 Pos:(1234.56,5678.90) NewPos:(1235.00,5680.00) Speed:11.83
```

---

**現在重新編譯並運行，查看異常位置診斷輸出！**
