# ✅ 暴力破解代碼已移除

## 已完成的更改

### 1. 從 Init.cs 移除暴力搜索代碼
**文件**：`Radar\Init.cs`

**移除的代碼**（第 130-138 行）：
```csharp
#if DEBUG
// 暴力搜索 KeySync 事件ID (500-700)
Console.WriteLine("[BruteForce] Registering KeySync scanners for event IDs 500-700...");
for (int i = 500; i <= 700; i++)
{
    builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
}
Console.WriteLine("[BruteForce] Registration complete. Switch maps to trigger KeySync.");
#endif
```

### 2. 從項目文件移除引用
**文件**：`DEATHEYE.csproj`

**移除的引用**：
```xml
<Compile Include="Radar\Packets\Handlers\DebugAllEventsHandler.cs" />
<Compile Include="Radar\Packets\Handlers\BruteForceKeySyncEvent.cs" />
<Compile Include="Radar\Packets\Handlers\BruteForceKeySyncHandler.cs" />
```

## 📝 保留的文件

以下暴力破解相關文件仍然保留在硬碟上（但不在項目中）：
- `Radar\Packets\Handlers\BruteForceKeySyncEvent.cs`
- `Radar\Packets\Handlers\BruteForceKeySyncHandler.cs`
- `Radar\Packets\Handlers\DebugAllEventsHandler.cs`

如果將來需要重新搜索 KeySync，可以再次添加這些文件到項目中。

## 🚀 現在可以正常編譯運行

重新編譯：
```bash
msbuild DEATHEYE.sln /t:Rebuild /p:Configuration=Debug /p:Platform=AnyCPU
```

運行時**不會再看到**：
```
[BruteForce] Registering KeySync scanners for event IDs 500-700...
[BruteForce] Registration complete. Switch maps to trigger KeySync.
```

## ⚠️ 重要提醒

如果你還沒有找到正確的 KeySync 事件 ID，需要：

1. **確保 `jsons/indexes.json` 中的 KeySync ID 正確**
2. **進入遊戲並切換地圖**
3. **觀察是否收到 KeySync**：
   ```
   [KeySync] XorCode received! Length:8 Bytes:12-34-56-78-9A-BC-DE-F0
   ```

如果仍然看到：
```
[XorCode] WARNING: XorCode is NULL! Cannot decrypt positions.
```

那麼需要重新啟用暴力搜索來找到新的 KeySync ID。

## 🔄 如何重新啟用暴力搜索

如果將來需要再次搜索 KeySync：

### 1. 編輯 `DEATHEYE.csproj`
添加：
```xml
<Compile Include="Radar\Packets\Handlers\BruteForceKeySyncEvent.cs" />
<Compile Include="Radar\Packets\Handlers\BruteForceKeySyncHandler.cs" />
```

### 2. 編輯 `Radar\Init.cs`
在 `builder.AddEventHandler(new KeySyncEventHandler(playersHandler));` 之後添加：
```csharp
#if DEBUG
Console.WriteLine("[BruteForce] Registering KeySync scanners for event IDs 500-700...");
for (int i = 500; i <= 700; i++)
{
    builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
}
Console.WriteLine("[BruteForce] Registration complete. Switch maps to trigger KeySync.");
#endif
```

### 3. 重新編譯並搜索

---

**暴力破解代碼已成功移除！** ✅
