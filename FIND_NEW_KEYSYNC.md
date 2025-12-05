# 🔍 尋找新的 KeySync 事件 ID

## 問題確認

根據診斷輸出：

```
XorCode: NULL
XorCode Length: 0
[XorCode] WARNING: XorCode is NULL! Cannot decrypt positions.
```

**結論：KeySync 事件 ID 593 已失效！** 需要找到新的事件 ID。

## 🎯 暴力搜索已啟用

我已經在 `Radar/Init.cs` 中啟用了暴力搜索（500-700 範圍）。

## 📋 操作步驟

### 1. 重新編譯項目

```bash
# Windows Command Prompt 或 PowerShell
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU /t:Rebuild
```

或在 Visual Studio 中：
1. 選擇 **Debug** 配置
2. 點擊 **Build > Rebuild Solution**

### 2. 運行應用程序

```bash
cd bin\Debug
VRise.exe
```

啟動時應該看到：
```
[BruteForce] Registering KeySync scanners for event IDs 500-700...
[BruteForce] Registration complete. Switch maps to trigger KeySync.
```

### 3. 進入遊戲並切換地圖

**重要**：KeySync 只在**進入新地圖/區域時觸發**。

操作：
1. 啟動 Albion Online
2. 進入遊戲世界
3. **切換到另一個地圖**（走過傳送門、進入城市、進入副本等）
4. 觀察控制台輸出

### 4. 查看結果

**成功找到 KeySync**：
```
[FOUND!] Event 612 has 8-byte code: 12-34-56-78-9A-BC-DE-F0
```

記下這個事件 ID（例如 612）！

**其他可能的輸出**：
```
[DEBUG] Event 645 has 16-byte array
[DEBUG] Event 521 has 4-byte array
```

如果看到 8-byte 數組，那就是 KeySync！

### 5. 更新配置

找到正確的事件 ID 後，更新 `bin/Debug/jsons/indexes.json`：

```json
{
  "KeySync": 612
}
```

（將 612 替換為你找到的實際事件 ID）

### 6. 禁用暴力搜索並重新編譯

在 `Radar/Init.cs:130-138` 註釋掉暴力搜索代碼：

```csharp
// #if DEBUG
// 暴力搜索 KeySync 事件ID (500-700)
// Console.WriteLine("[BruteForce] Registering KeySync scanners for event IDs 500-700...");
// for (int i = 500; i <= 700; i++)
// {
//     builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
// }
// Console.WriteLine("[BruteForce] Registration complete. Switch maps to trigger KeySync.");
// #endif
```

重新編譯並測試。

### 7. 驗證修復

重新運行後，切換地圖時應該看到：

```
[KeySync] XorCode received! Length:8 Bytes:12-34-56-78-9A-BC-DE-F0
[PlayerPos] ID:243438 Name:SomePlayer Pos:(1234.56,5678.90) NewPos:(1235.00,5680.00) Speed:11.83
```

**不應該再有**：
- ❌ `XorCode: NULL`
- ❌ 巨大的負數位置
- ❌ `[ABNORMAL POSITION DETECTED!]`

## ⚠️ 注意事項

### 性能影響

暴力搜索會註冊 200 個事件處理器（500-700），這會：
- ✅ 幫助找到正確的 KeySync 事件 ID
- ⚠️ 略微影響性能
- ⚠️ 產生大量控制台輸出

**因此找到後務必禁用暴力搜索！**

### 多次切換地圖

如果第一次切換沒有輸出 `[FOUND!]`：
1. 再次切換到不同的地圖
2. 嘗試進入副本
3. 嘗試進入城市
4. 嘗試切換到不同的區域類型

KeySync 可能只在特定的地圖轉換時觸發。

### 如果沒有找到 8-byte 數組

查看是否有其他長度的數組：

```
[DEBUG] Event 645 has 16-byte array
```

如果 KeySync 現在使用 16-byte 密鑰，需要修改：

1. **BruteForceKeySyncHandler.cs**：
```csharp
if (value.Code != null && value.Code.Length == 16)  // 改為 16
{
    Console.WriteLine($"[FOUND!] Event {testEventCode} has 16-byte code: {BitConverter.ToString(value.Code)}");
    playersHandler.XorCode = value.Code;
}
```

2. **PlayersHandler.cs** 的 `Decrypt()` 方法需要調整以支持 16-byte 密鑰。

## 🔧 故障排除

### 問題：沒有任何輸出

**檢查**：
- 是否在 Debug 模式下編譯？
- 是否看到 `[BruteForce] Registering...` 訊息？
- 是否真的切換了地圖？

### 問題：太多輸出，看不清

**解決**：
1. 將輸出重定向到文件：
```bash
VRise.exe > output.log 2>&1
```

2. 在另一個終端查看：
```bash
findstr /i "FOUND DEBUG" output.log
```

### 問題：找到多個候選事件

如果看到多個 8-byte 數組：
```
[DEBUG] Event 612 has 8-byte array
[DEBUG] Event 645 has 8-byte array
[DEBUG] Event 678 has 8-byte array
```

**判斷方法**：
1. 選擇在**每次切換地圖時都出現**的事件
2. KeySync 通常在玩家進入新區域時**只觸發一次**
3. 嘗試每個候選 ID，看哪個能正確解密位置

## 📊 預期時間線

- ⏱️ 重新編譯：30 秒
- ⏱️ 進入遊戲：1-2 分鐘
- ⏱️ 切換地圖：30 秒
- ⏱️ 找到 KeySync：立即（如果成功）

**總計：約 3-5 分鐘**

---

**現在開始搜索！記得切換地圖來觸發 KeySync 事件。** 🎮
