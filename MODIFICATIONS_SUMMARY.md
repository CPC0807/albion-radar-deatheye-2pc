# 座標Debug修改總結

## 目的
測試玩家座標是否需要 XorCode 解密，以及找出正確的解析方式。

## 已修改的文件

### 1. `Radar\Packets\Handlers\MoveEvent.cs`

**位置**: 第 27-30 行和第 47-49 行

**修改內容**:
```csharp
#if DEBUG
// Debug: 輸出原始位置 bytes
Console.WriteLine($"[MoveEvent] ID:{Id} RawPosBytes: {BitConverter.ToString(PositionBytes)} Flags:{flags}");
#endif

#if DEBUG
Console.WriteLine($"[MoveEvent] ID:{Id} RawNewPosBytes: {BitConverter.ToString(NewPositionBytes)}");
#endif
```

**作用**: 輸出從封包中提取的原始座標 bytes，用於診斷是否正確提取數據。

---

### 2. `Radar\GameObjects\Players\PlayersHandler.cs`

**位置**: 第 161-179 行（UpdatePlayerPosition 方法開頭）

**修改內容**:
```csharp
#if DEBUG
// 測試多種解析方式
Vector2 method1 = new Vector2(BitConverter.ToSingle(positionBytes, 4), BitConverter.ToSingle(positionBytes, 0));
Vector2 method2 = new Vector2(BitConverter.ToSingle(positionBytes, 0), BitConverter.ToSingle(positionBytes, 4));

// 使用 Decrypt 方法（如果 XorCode 存在）
float[] decrypted = Decrypt(positionBytes, 0);
Vector2 method3 = new Vector2(decrypted[0], decrypted[1]);
Vector2 method4 = new Vector2(decrypted[1], decrypted[0]);

if (playersList.TryGetValue(id, out Player player))
{
    Console.WriteLine($"[PlayerPos Debug] ID:{id} Name:{player.Name}");
    Console.WriteLine($"  Method1 (Y,X from offset 4,0): ({method1.X:F2}, {method1.Y:F2})");
    Console.WriteLine($"  Method2 (X,Y from offset 0,4): ({method2.X:F2}, {method2.Y:F2})");
    Console.WriteLine($"  Method3 (Decrypt XY): ({method3.X:F2}, {method3.Y:F2}) [XorCode: {(XorCode != null ? "YES" : "NULL")}]");
    Console.WriteLine($"  Method4 (Decrypt YX): ({method4.X:F2}, {method4.Y:F2})");
}
#endif
```

**作用**: 測試4種不同的座標解析方式，找出哪一種產生合理的座標值。

---

## 編譯指令

### 方法 1：使用 Visual Studio
1. 打開 `DEATHEYE.sln`
2. 切換到 **Debug** 配置
3. 按 F5 編譯並運行

### 方法 2：使用命令行
```cmd
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" DEATHEYE.csproj /p:Configuration=Debug /nologo
```

### 方法 3：使用 PowerShell
```powershell
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" DEATHEYE.csproj /p:Configuration=Debug
```

---

## 測試步驟

1. **編譯成功後**，執行 `bin\Debug\DEATHEYE.exe`

2. **啟動 Albion Online** 並進入遊戲

3. **等待看到其他玩家移動**

4. **觀察 Console 輸出**，尋找類似：
   ```
   [MoveEvent] ID:1234567 RawPosBytes: AA-BB-CC-DD-EE-FF-00-11 Flags:Speed, NewPosition
   [PlayerPos Debug] ID:1234567 Name:PlayerName
     Method1 (Y,X from offset 4,0): (123.45, 456.78)
     Method2 (X,Y from offset 0,4): (456.78, 123.45)
     Method3 (Decrypt XY): (789.01, 234.56) [XorCode: NULL]
     Method4 (Decrypt YX): (234.56, 789.01)
   ```

---

## 如何判斷哪個方法正確

### 正確座標的特徵：
- ✅ **範圍**: 0 到 3000（大部分地圖）或 0 到 10000（巨大地圖）
- ✅ **穩定性**: 隨著玩家移動而逐漸改變（不是跳躍）
- ✅ **合理性**: 不會突然從 100 跳到 100000

### 錯誤座標的特徵：
- ❌ **零值**: (0.00, 0.00) - 沒有正確提取
- ❌ **巨大值**: (8749658.00, -8779551.00) - 加密未解
- ❌ **科學記數**: (1.23e+38, -5.67e+38) - 完全錯誤

---

## 根據測試結果的下一步

### 場景 A：Method1 或 Method2 顯示合理值
**結論**: 座標沒有加密！

**下一步**:
1. 移除 `Decrypt()` 調用
2. 使用正確的 Method（可能是 Method1）
3. 移除所有 XorCode 相關代碼

### 場景 B：Method3 或 Method4 顯示合理值（且 XorCode 不是 NULL）
**結論**: 座標加密，但 XorCode 來源已找到

**下一步**:
1. 使用正確的 Decrypt 方法
2. 確認 XorCode 來源

### 場景 C：Method3 或 Method4 顯示合理值（但 XorCode 是 NULL）
**結論**: 這不可能發生，因為 Decrypt 沒有 XorCode 時等同於 Method1/2

**下一步**:
1. 檢查邏輯錯誤

### 場景 D：所有方法都顯示異常值
**結論**: 問題在於 bytes 提取或封包結構變化

**下一步**:
1. 檢查 RawPosBytes 輸出
2. 可能需要調整 `MoveEvent.cs` 的 index 計算（第 23 行：`int index = 9`）
3. 嘗試不同的 index 值（如 1, 5, 9, 13）

---

## 重要發現

**怪物和玩家使用相同的 MoveEvent 封包！**

查看 `MoveEventHandler.cs`:
```csharp
protected override Task OnActionAsync(MoveEvent value)
{
    playerHandler.UpdatePlayerPosition(value.Id, value.PositionBytes, value.NewPositionBytes, value.Speed, value.Time);
    mobHandler.UpdateMobPosition(value.Id, value.PositionBytes, value.NewPositionBytes, value.Speed, value.Time);
    return Task.CompletedTask;
}
```

這意味著：
- 如果怪物位置正確 → 封包提取是對的
- 如果玩家位置錯誤 → 問題在於 PlayersHandler 的座標解析
- 如果兩者都錯誤 → 問題在於 MoveEvent 的 bytes 提取

---

## 故障排除

### 問題 1：編譯錯誤
**錯誤**: "找不到 .NETFramework,Version=v4.8"

**解決**:
- 安裝 .NET Framework 4.8 Developer Pack
- 或使用完整 Visual Studio（不是 VS Code）

### 問題 2：沒有任何 Debug 輸出
**原因**:
- 沒有以 Debug 模式編譯
- 可能以 Release 模式運行

**解決**:
- 確認編譯配置為 Debug
- 檢查 `bin\Debug` 文件夾（不是 `bin\Release`）

### 問題 3：Console 視窗立即關閉
**原因**: 程序崩潰或快速結束

**解決**:
```cmd
cd bin\Debug
DEATHEYE.exe
pause
```

---

## 聯絡支援

如果測試後需要分析，請提供：
1. Console 輸出的前 100 行
2. 螢幕截圖顯示 Radar 上玩家的位置
3. 遊戲內其他玩家的實際位置

將這些信息貼上後，我可以確定正確的座標解析方式。
