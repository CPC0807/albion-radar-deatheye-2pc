# 🔧 快速修復 - 編譯錯誤

## 問題

```
builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
error
```

## 修復 ✅

**問題原因**：`BruteForceKeySyncEvent.cs` 缺少 `using Albion.Network;` 引用。

**已修復**：在 `Radar\Packets\Handlers\BruteForceKeySyncEvent.cs` 添加了缺少的引用。

## 現在可以編譯了！

### 方法 1：使用批處理文件（推薦）

雙擊運行：
```
COMPILE_AND_RUN.bat
```

這個批處理會：
1. 清理之前的編譯
2. 重新編譯項目
3. 自動啟動應用程序

### 方法 2：手動編譯

```bash
# 清理
msbuild DEATHEYE.sln /t:Clean /p:Configuration=Debug /p:Platform=AnyCPU

# 重新編譯
msbuild DEATHEYE.sln /t:Rebuild /p:Configuration=Debug /p:Platform=AnyCPU

# 運行
cd bin\Debug
VRise.exe
```

### 方法 3：Visual Studio

1. 打開 `DEATHEYE.sln`
2. 選擇 **Debug** 配置
3. 點擊 **Build > Rebuild Solution** (Ctrl+Shift+B)
4. 按 F5 運行

## 編譯成功後

啟動時應該看到：
```
[BruteForce] Registering KeySync scanners for event IDs 500-700...
[BruteForce] Registration complete. Switch maps to trigger KeySync.
```

## 下一步

1. **進入 Albion Online**
2. **切換地圖/區域**（重要！）
3. **觀察控制台**，尋找：
   ```
   [FOUND!] Event XXX has 8-byte code: 12-34-56-78-9A-BC-DE-F0
   ```
4. **記下事件 ID**（XXX 的值）
5. **更新配置**：
   - 編輯 `bin\Debug\jsons\indexes.json`
   - 將 `"KeySync": 593` 改為 `"KeySync": XXX`

## 如果還有編譯錯誤

請提供完整的錯誤訊息，包括：
- 錯誤代碼（例如 CS0246, CS0103）
- 錯誤描述
- 文件名和行號

---

**編譯錯誤已修復，現在可以繼續了！** 🚀
