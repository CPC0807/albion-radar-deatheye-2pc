# 🛠️ VRise 錯誤修復總結

## 修復的問題

### 1. ✅ System.FormatException 崩潰問題

**問題**：應用程序啟動後立即崩潰，錯誤訊息：
```
於 System.FormatException 擲回例外狀況: 'mscorlib.dll'
Input string was not in a correct format.
```

**原因**：
- `StyleSettings` 配置數組包含混合類型（字符串顏色代碼 + 整數）
- 例如：`["#FFFF0100", "#FFFF0100", 0, "#FFFF0100", 0, ...]`
- 代碼嘗試使用 `Convert.ToInt32()` 轉換顏色字符串 `"#FFFF0100"` 為整數時失敗

**修復方案**：
1. 在 `Settings/ConfigHandler.cs` 添加安全轉換方法 `SafeConvertToInt32()`
2. 該方法能夠：
   - 處理 int, long, double, float 類型
   - 嘗試解析字符串數字
   - 對於顏色代碼等非數字字符串返回默認值（0）
   - 捕獲所有異常並返回安全的默認值

3. 更新所有使用 `Convert.ToInt32()` 的文件：
   - `Radar/Drawing/Drawers/HudDrawerer.cs`
   - `Radar/Drawing/Drawers/PlayersDrawerer.cs`
   - `Design/Pages/PlayersPage.xaml.cs`
   - `Design/Pages/DungeonsPage.xaml.cs`
   - `Design/Pages/MobsPage.xaml.cs`
   - `Radar/Packets/Handlers/DebugHandler.cs`

**修改的文件**：
```
Settings/ConfigHandler.cs               - 添加 SafeConvertToInt32() 方法
Radar/Drawing/Drawers/HudDrawerer.cs    - 替換所有 Convert.ToInt32()
Radar/Drawing/Drawers/PlayersDrawerer.cs - 替換所有 Convert.ToInt32()
Design/Pages/PlayersPage.xaml.cs        - 替換所有 Convert.ToInt32()
Design/Pages/DungeonsPage.xaml.cs       - 替換所有 Convert.ToInt32()
Design/Pages/MobsPage.xaml.cs           - 替換所有 Convert.ToInt32()
Radar/Packets/Handlers/DebugHandler.cs  - 添加異常處理
```

---

### 2. ✅ 玩家位置顯示 (0.00, 0.00) 問題

**問題**：
```
[PlayerPos] ID:231094 Name:MrsBreewc Pos:(0.00,0.00) NewPos:(0.00,0.00) Speed:11.83
```

玩家位置始終顯示為 (0.00, 0.00)，但速度正常。

**原因**：
- `PlayersHandler.UpdatePlayerPosition()` 方法**沒有使用 XorCode 解密座標**
- 代碼直接讀取加密的位置字節，未調用 `Decrypt()` 方法
- Albion Online 仍然使用 XOR 加密位置數據

**原始錯誤代碼**：
```csharp
// ❌ 錯誤：直接讀取加密數據
Vector2 position = new Vector2(
    BitConverter.ToSingle(positionBytes, 4),
    BitConverter.ToSingle(positionBytes, 0)
);
```

**修復方案**：
```csharp
// ✅ 正確：使用 Decrypt 方法解密
float[] pos = Decrypt(positionBytes);
float[] newPos = Decrypt(newPositionBytes);

Vector2 position = new Vector2(pos[0], pos[1]);
Vector2 newPosition = new Vector2(newPos[0], newPos[1]);
```

**增強的診斷功能**：
- 檢查 XorCode 是否為 NULL
- 檢查 XorCode 長度是否正確（應為 8 字節）
- 當位置仍為 (0.00, 0.00) 時輸出原始字節數據
- 提供清晰的警告訊息指導用戶排查問題

**修改的文件**：
```
Radar/GameObjects/Players/PlayersHandler.cs - 修復 UpdatePlayerPosition() 方法
```

---

### 3. ✅ InvalidCastException 錯誤

**問題**：
```
於 System.InvalidCastException 擲回例外狀況: 'VRise.exe'
Unable to cast object of type 'System.Byte' to type 'System.Single[]'.
```

**原因**：
- `Decrypt()` 方法缺少輸入驗證
- 當座標數據異常時（NULL、長度不足等），LINQ 操作或類型轉換失敗
- 沒有適當的異常處理機制

**修復方案**：

1. **增強 `Decrypt()` 方法**：
   - 添加 NULL 檢查
   - 驗證字節數組長度（必須 >= offset + 8）
   - 驗證 Skip/Take 操作後的數組長度
   - 包裝所有操作在 try-catch 中
   - 返回安全的默認值 (0, 0) 而不是崩潰

2. **增強 `UpdatePlayerPosition()` 方法**：
   - 驗證參數不為 NULL
   - 檢查參數長度是否正確
   - 捕獲 InvalidCastException 並輸出診斷信息
   - 發生錯誤時優雅返回而不是崩潰

**修改的文件**：
```
Radar/GameObjects/Players/PlayersHandler.cs - Decrypt() 和 UpdatePlayerPosition() 方法
```

**診斷輸出**：
現在當發生錯誤時，會看到清晰的錯誤訊息：
```
[Decrypt] ERROR: coordinates is NULL!
[Decrypt] ERROR: coordinates length 4 is too short for offset 0!
[UpdatePlayerPosition] InvalidCastException: Unable to cast...
[UpdatePlayerPosition] positionBytes type: Byte[]
```

---

## 診斷與故障排除

### 檢查 KeySync 是否正常

KeySync 是位置解密的關鍵。運行程序時，觀察控制台：

#### ✅ 正常情況：
```
[KeySync] XorCode received! Length:8 Bytes:12-34-56-78-9A-BC-DE-F0
[PlayerAdd] ID:231094 Name:MrsBreewc Pos:(1234.56,5678.90)
[PlayerPos] ID:231094 Name:MrsBreewc Pos:(1234.56,5678.90) NewPos:(1235.00,5680.00) Speed:11.83
```

#### ❌ KeySync 未觸發：
```
[XorCode] WARNING: XorCode is NULL! Cannot decrypt positions.
[XorCode] Make sure KeySync event (ID 593) is triggered when entering a new zone.
[PlayerPos] ID:231094 Name:MrsBreewc Pos:(0.00,0.00) NewPos:(0.00,0.00) Speed:11.83
```

**解決方案**：
1. 確保在遊戲中**切換地圖/區域**（這會觸發 KeySync）
2. 檢查 `bin/Debug/jsons/indexes.json` 中 KeySync 事件 ID 是否正確（當前為 593）
3. 如果仍未收到，KeySync 事件 ID 可能已更改，需要使用暴力搜索

### 如何暴力搜索 KeySync 事件 ID

如果 KeySync 始終為 NULL：

1. 在 `Radar/Init.cs` 中找到第 131-137 行
2. 取消註解暴力搜索代碼：
```csharp
Console.WriteLine("[BruteForce] Registering KeySync scanners for event IDs 500-700...");
for (int i = 500; i <= 700; i++)
{
    builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
}
```

3. 重新編譯（Debug 模式）
4. 運行並進入遊戲
5. **切換地圖**
6. 查看控制台輸出：
```
[FOUND!] Event 612 has 8-byte code: 12-34-56-78-9A-BC-DE-F0
```

7. 更新 `bin/Debug/jsons/indexes.json`：
```json
{
  "KeySync": 612
}
```

8. 註釋掉暴力搜索代碼，重新編譯測試

---

## 重新編譯和測試

### 1. 在 Visual Studio 中編譯

```
1. 打開 DEATHEYE.sln
2. 選擇 Debug 配置
3. 選擇 AnyCPU 或 x64 平台
4. 點擊 Build > Rebuild Solution
```

### 2. 或使用命令行（Windows）

```cmd
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU /t:Rebuild
```

### 3. 運行測試

```
1. 啟動 bin\Debug\VRise.exe
2. 觀察控制台輸出
3. 啟動 Albion Online
4. 進入遊戲世界
5. 切換地圖（觸發 KeySync）
6. 查看玩家位置是否正確顯示
```

---

## 預期結果

修復後，你應該看到：

```
[KeySync] XorCode received! Length:8 Bytes:AA-BB-CC-DD-EE-FF-00-11
[EventStructure] Event 44 logged
  <<< POTENTIAL KEY >>> Event 44 Key 14 - byte[16]
[PlayerAdd] ID:231094 Name:MrsBreewc Guild:SOME_GUILD Pos:(1523.45,2847.92) Faction:Hostile
[PlayerPos] ID:231094 Name:MrsBreewc Pos:(1523.45,2847.92) NewPos:(1524.50,2849.00) Speed:11.83
```

**注意**：
- 位置應該是合理的數值（通常在 0-10000 範圍內）
- 不再顯示 (0.00, 0.00)
- 不再有 FormatException 崩潰

---

## 其他參考文檔

- **[POSITION_DECRYPTION_FIX.md](POSITION_DECRYPTION_FIX.md)** - 位置解密問題詳細診斷指南
- **[FIND_KEYSYNC_GUIDE.md](FIND_KEYSYNC_GUIDE.md)** - 手動尋找 KeySync 封包 ID 指南
- **[CLAUDE.md](CLAUDE.md)** - 項目架構和開發指南

---

## 需要進一步幫助？

提供以下信息以便診斷：

1. **控制台完整輸出**（特別是 KeySync、PlayerPos、XorCode 相關行）
2. **event_structures.txt** 的內容（如果存在）
3. **jsons/indexes.json** 和 **jsons/offsets.json** 的內容
4. **錯誤訊息截圖**（如果仍有崩潰）

---

**祝你好運！** 🎮
