# 🔧 InvalidCastException 修復

## 錯誤訊息

```
於 System.InvalidCastException 擲回例外狀況: 'VRise.exe'
'System.InvalidCastException' 類型的例外狀況發生於 VRise.exe，但使用者程式碼未加以處理
Unable to cast object of type 'System.Byte' to type 'System.Single[]'.
```

## 問題分析

這個錯誤發生在 `PlayersHandler.Decrypt()` 方法中，可能的原因：

1. **參數類型不匹配**：`positionBytes` 或 `newPositionBytes` 不是 `byte[]` 類型
2. **數據長度錯誤**：字節數組長度不足 8 字節
3. **LINQ 操作異常**：`coordinates.Skip(offset).Take(4).ToArray()` 返回了意外的類型

## 修復方案

### 1. 增強 `Decrypt()` 方法的錯誤處理

在 `Radar/GameObjects/Players/PlayersHandler.cs` 中：

```csharp
public float[] Decrypt(byte[] coordinates, int offset = 0)
{
    // ✅ 添加輸入驗證
    if (coordinates == null)
    {
        return new float[] { 0f, 0f };
    }

    if (coordinates.Length < offset + 8)
    {
        Console.WriteLine($"[Decrypt] ERROR: coordinates length {coordinates.Length} is too short!");
        return new float[] { 0f, 0f };
    }

    // ✅ 添加 try-catch 保護
    try
    {
        var x = coordinates.Skip(offset).Take(4).ToArray();
        var y = coordinates.Skip(offset + 4).Take(4).ToArray();

        // 驗證長度
        if (x.Length != 4 || y.Length != 4)
        {
            return new float[] { 0f, 0f };
        }

        // 解密邏輯...
        Decrypt(x, code, 0);
        Decrypt(y, code, 4);

        return new[] { BitConverter.ToSingle(x, 0), BitConverter.ToSingle(y, 0) };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Decrypt] ERROR: {ex.Message}");
        return new float[] { 0f, 0f };
    }
}
```

### 2. 增強 `UpdatePlayerPosition()` 方法的錯誤處理

```csharp
public void UpdatePlayerPosition(int id, byte[] positionBytes, byte[] newPositionBytes, float speed, DateTime time)
{
    lock (playersList)
    {
        // ✅ 驗證參數
        if (positionBytes == null || newPositionBytes == null)
        {
            Console.WriteLine($"[UpdatePlayerPosition] ERROR: NULL parameters!");
            return;
        }

        if (positionBytes.Length != 8 || newPositionBytes.Length != 8)
        {
            Console.WriteLine($"[UpdatePlayerPosition] WARNING: Invalid lengths!");
        }

        // ✅ 添加 try-catch 保護
        try
        {
            float[] pos = Decrypt(positionBytes);
            float[] newPos = Decrypt(newPositionBytes);

            Vector2 position = new Vector2(pos[0], pos[1]);
            Vector2 newPosition = new Vector2(newPos[0], newPos[1]);

            // 更新玩家位置...
        }
        catch (InvalidCastException ex)
        {
            Console.WriteLine($"[UpdatePlayerPosition] InvalidCastException: {ex.Message}");
            Console.WriteLine($"positionBytes type: {positionBytes?.GetType().Name}");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UpdatePlayerPosition] Exception: {ex.Message}");
            return;
        }
    }
}
```

## 診斷步驟

重新編譯並運行後，觀察控制台輸出：

### 場景 1：參數為 NULL

```
[UpdatePlayerPosition] ERROR: positionBytes is NULL!
```

**原因**：MoveEvent 解析失敗，沒有提取到位置數據。

**解決**：檢查 `jsons/offsets.json` 中 Move 事件的偏移配置。

### 場景 2：長度不正確

```
[UpdatePlayerPosition] WARNING: positionBytes length is 4 (expected 8)
[Decrypt] ERROR: coordinates length 4 is too short for offset 0!
```

**原因**：MoveEvent 的位置數據提取邏輯錯誤。

**解決**：檢查 `MoveEvent.cs` 中的 `Array.Copy` 邏輯：
```csharp
// 應該複製 8 字節（X 和 Y 各 4 字節）
PositionBytes = new byte[8];
Array.Copy(parameter, index, PositionBytes, 0, 8);
```

### 場景 3：類型轉換錯誤

```
[UpdatePlayerPosition] InvalidCastException: Unable to cast object...
positionBytes type: Byte[]
```

這表示類型是正確的 `Byte[]`，但內部操作失敗。

### 場景 4：LINQ 操作失敗

```
[Decrypt] ERROR: InvalidCastException - Unable to cast...
[Decrypt] Stack trace: at System.Linq.Enumerable.Skip...
```

**可能原因**：
- `coordinates` 數組損壞
- LINQ 庫版本問題

**替代方案**：使用 `Array.Copy` 代替 LINQ：

```csharp
// 替代 Skip/Take
var x = new byte[4];
var y = new byte[4];
Array.Copy(coordinates, offset, x, 0, 4);
Array.Copy(coordinates, offset + 4, y, 0, 4);
```

## 快速測試

1. **重新編譯**（Debug 模式）
2. **運行並觀察控制台**
3. **進入遊戲**
4. **查看輸出**：
   - 是否有 `[Decrypt] ERROR` 訊息？
   - 是否有 `[UpdatePlayerPosition] ERROR` 訊息？
   - 位置數據的長度是多少？

## 預期正常輸出

修復後應該看到：

```
[KeySync] XorCode received! Length:8 Bytes:12-34-56-78-9A-BC-DE-F0
[PlayerAdd] ID:231094 Name:MrsBreewc Pos:(1234.56,5678.90)
[PlayerPos] ID:231094 Name:MrsBreewc Pos:(1234.56,5678.90) NewPos:(1235.00,5680.00) Speed:11.83
```

**不應該再有**：
- ❌ `InvalidCastException`
- ❌ `FormatException`
- ❌ 應用程序崩潰

## 如果仍然失敗

提供以下診斷信息：

1. **完整的異常堆棧**
2. **控制台輸出**（特別是 ERROR 和 WARNING 行）
3. **`jsons/offsets.json` 中 Move 事件的配置**
4. **是否收到 KeySync**

---

**所有修改已完成，現在重新編譯測試！**
