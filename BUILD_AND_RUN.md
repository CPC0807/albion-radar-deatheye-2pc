# DEATHEYE 雷達 - 編譯和運行指南

## 📋 你的設置

```
主電腦 (Albion Online)
    ↓ UDP 5056/5055
Windows VM (Bridge 模式 + npcap)
    ↓ 直接抓取封包
DEATHEYE 雷達
```

**優點：**
- ✅ 無需 Cryptonite
- ✅ 無需修改 hosts 文件
- ✅ 無需 Ubuntu sender
- ✅ 簡單直接

---

## 步驟 1: 檢查前提條件

### 已完成 ✅
- [x] Npcap 已安裝
- [x] Windows VM 使用 Bridge 模式
- [x] Wireshark 可以抓到主電腦的 UDP 5056 封包

### 需要安裝 ⚠️

#### .NET Framework 4.8

檢查是否已安裝：
```cmd
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release
```

如果沒有，下載安裝：
https://dotnet.microsoft.com/download/dotnet-framework/net48

#### Visual Studio 2019 或更新版本

下載安裝：
https://visualstudio.microsoft.com/downloads/

安裝時勾選：
- ✅ .NET 桌面開發

#### DEATHEYE 字體

雙擊安裝：
```
Design\Font\DEATHEYE.ttf
```

---

## 步驟 2: 添加新文件到 Visual Studio 項目

### 2.1 打開項目

```cmd
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc
start DEATHEYE.sln
```

### 2.2 添加 NetworkConfig.cs

在 Visual Studio 中：

1. **展開 Solution Explorer**
2. **找到並展開** `Radar` → `Packets` → `Sniffer` 資料夾
3. **檢查是否已有** `NetworkConfig.cs`
   - 如果沒有，繼續下一步
4. **右鍵點擊** `Sniffer` 資料夾
5. **選擇** Add → Existing Item
6. **瀏覽到** `Radar\Packets\Sniffer\NetworkConfig.cs`
7. **點擊** Add

### 2.3 確認文件已添加

在 Solution Explorer 中應該看到：
```
Radar
  └─ Packets
      └─ Sniffer
          ├─ NetworkConfig.cs        ← 新增
          └─ PacketDeviceSelector.cs ← 已修改
```

---

## 步驟 3: 編譯項目

### 3.1 恢復 NuGet 包

在 Visual Studio 中：
```
Tools → NuGet Package Manager → Restore NuGet Packages
```

### 3.2 編譯

**方式 1: 使用菜單**
```
Build → Build Solution (或按 Ctrl+Shift+B)
```

**方式 2: 使用命令行**
```cmd
msbuild DEATHEYE.sln /p:Configuration=Release
```

### 3.3 檢查編譯結果

**成功：**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**如果有錯誤：**
- 檢查 NetworkConfig.cs 是否正確添加到項目
- 檢查命名空間是否為 `X975.Radar.Sniffer`
- 查看錯誤訊息詳情

---

## 步驟 4: 準備運行環境

### 4.1 找到編譯輸出

編譯後的文件在：
```
bin\Debug\         (Debug 版本)
或
bin\Release\       (Release 版本)
```

### 4.2 複製必要文件到輸出目錄

需要確保以下目錄/文件在輸出目錄中：

```cmd
cd bin\Release

# 檢查必要的目錄和文件
dir ITEMS
dir ao-bin-dumps
dir jsons
dir network_config.json
```

**如果缺少，手動複製：**

```cmd
# 在項目根目錄執行
xcopy ITEMS bin\Release\ITEMS /E /I /Y
xcopy ao-bin-dumps bin\Release\ao-bin-dumps /E /I /Y
xcopy jsons bin\Release\jsons /E /I /Y
copy network_config.json bin\Release\
```

### 4.3 檢查 network_config.json

確認內容為：
```json
{
  "game_port": 5056
}
```

---

## 步驟 5: 運行雷達

### 5.1 以管理員身份運行

**重要：** 必須以管理員身份運行才能使用 npcap！

```
方式 1: 右鍵 X975.exe → 以管理員身份運行

方式 2:
cd bin\Release
右鍵 空白處 → 在終端機開啟（以系統管理員身分）
.\X975.exe
```

### 5.2 預期輸出

控制台應顯示：
```
[NetworkConfig] Loaded from network_config.json
[NetworkConfig] Game Port: 5056
[Init] Starting packet capture on game port 5056
[Init] No Cryptonite needed! No hosts file modification needed!
```

### 5.3 在主電腦啟動遊戲

1. 在**主電腦**啟動 Albion Online
2. 進入遊戲世界（不是角色選擇畫面）
3. 移動角色

### 5.4 檢查雷達是否工作

**應該看到：**
- ✅ 雷達覆蓋層出現
- ✅ 顯示玩家圖標
- ✅ 顯示資源節點
- ✅ 顯示怪物

**控制台會輸出：**
```
Received event code: NewCharacter with parameters: ...
Received event code: Move with parameters: ...
```

---

## 故障排除

### 問題 1: "Install NPCAP" 錯誤

**原因：** 找不到 npcap

**解決：**
1. 重新安裝 Npcap
2. 確保勾選 "Install Npcap in WinPcap API-compatible Mode"
3. 重啟電腦

---

### 問題 2: 找不到 ITEMS 目錄

**原因：** ITEMS 未複製到輸出目錄

**解決：**
```cmd
xcopy ITEMS bin\Release\ITEMS /E /I /Y
```

---

### 問題 3: 雷達啟動但看不到任何東西

**可能原因：**

**A. 遊戲未運行或未進入世界**
- 必須進入實際遊戲地圖

**B. 端口不正確**

用 Wireshark 檢查實際端口：
```
1. 運行 Wireshark
2. 過濾器：udp
3. 在主電腦啟動遊戲
4. 查看實際使用的端口號
5. 更新 network_config.json 中的 game_port
```

**C. 未以管理員身份運行**
- 必須以管理員身份運行 X975.exe

---

### 問題 4: 編譯錯誤 "找不到類型或命名空間"

**原因：** NetworkConfig.cs 未正確添加到項目

**解決：**
1. 在 Solution Explorer 檢查 NetworkConfig.cs 是否存在
2. 如果不存在，按照步驟 2.2 添加
3. 確認命名空間為 `X975.Radar.Sniffer`
4. 重新編譯

---

### 問題 5: 控制台顯示但雷達覆蓋層不出現

**檢查：**
1. 字體是否已安裝（DEATHEYE.ttf）
2. 遊戲是否在運行
3. 查看控制台是否有錯誤訊息

---

## 快速命令參考

### 編譯項目
```cmd
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc
msbuild DEATHEYE.sln /p:Configuration=Release
```

### 複製資源
```cmd
xcopy ITEMS bin\Release\ITEMS /E /I /Y
xcopy ao-bin-dumps bin\Release\ao-bin-dumps /E /I /Y
xcopy jsons bin\Release\jsons /E /I /Y
copy network_config.json bin\Release\
```

### 運行雷達
```cmd
cd bin\Release
右鍵 X975.exe → 以管理員身份運行
```

### 檢查 .NET Framework
```cmd
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release
```

---

## 配置選項

### 更改遊戲端口

如果遊戲使用不同端口（如 5050 或 5055），編輯 `network_config.json`:

```json
{
  "game_port": 5050
}
```

重啟雷達即可，無需重新編譯。

---

## 進階：自動複製資源

在 Visual Studio 中設置自動複製：

1. **右鍵項目** → Properties
2. **Build Events** → Post-build event
3. **添加命令：**
```cmd
xcopy "$(ProjectDir)ITEMS" "$(TargetDir)ITEMS" /E /I /Y
xcopy "$(ProjectDir)ao-bin-dumps" "$(TargetDir)ao-bin-dumps" /E /I /Y
xcopy "$(ProjectDir)jsons" "$(TargetDir)jsons" /E /I /Y
copy "$(ProjectDir)network_config.json" "$(TargetDir)" /Y
```

這樣每次編譯都會自動複製必要文件。

---

## 總結

**你的優勢：**
- ✅ Bridge 模式可以直接抓包
- ✅ 無需複雜的 sender/receiver 架構
- ✅ 無需 Cryptonite
- ✅ 無需修改 hosts 文件

**你只需要：**
1. 編譯 DEATHEYE
2. 以管理員身份運行
3. 啟動遊戲
4. 享受雷達功能

**下次使用：**
直接以管理員身份運行 X975.exe 即可，無需重新編譯！
