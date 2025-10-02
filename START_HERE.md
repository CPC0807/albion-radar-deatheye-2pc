# 🚀 DEATHEYE 雷達啟動指南

## 前提條件檢查

### ✅ 已完成

- [x] Npcap 已安裝
- [x] ITEMS 目錄存在
- [x] ao-bin-dumps 目錄存在
- [x] network_config.json 存在

### ⚠️ 需要確認

- [ ] .NET Framework 4.8 已安裝
- [ ] DEATHEYE 字體已安裝（Design/Font/DEATHEYE.ttf）
- [ ] X975.exe 已編譯（或從 Release 下載）

---

## 方案 A: 如果已有 X975.exe（推薦）

### 1. 配置文件

編輯 `network_config.json`:

```json
{
  "mode": "local",
  "remote_port": 9999,
  "game_port": 5050
}
```

### 2. 啟動雷達

**方式 1: 使用批次檔**
```cmd
雙擊 start_local.bat
```

**方式 2: 直接運行**
```cmd
右鍵 X975.exe → 以管理員身份運行
```

### 3. 啟動遊戲

在**主電腦**運行 Albion Online

### 4. 檢查雷達

應該看到：
```
[NetworkConfig] Mode: local, Remote Port: 9999, Game Port: 5050
[Init] Using LOCAL mode (npcap on game port 5050)
```

---

## 方案 B: 如果沒有 X975.exe（需要編譯）

### 1. 安裝 Visual Studio

下載並安裝：
- Visual Studio 2019 或更新版本
- 勾選 ".NET 桌面開發" 工作負載

### 2. 打開項目

```cmd
雙擊 DEATHEYE.sln
```

### 3. 添加新文件到項目

在 Visual Studio 中：

1. 在 Solution Explorer 右鍵點擊 `DEATHEYE` 項目
2. Add → Existing Item
3. 導航到 `Radar/Packets/Sniffer/`
4. 選擇並添加：
   - `NetworkPacketReceiver.cs`
   - `NetworkConfig.cs`

### 4. 恢復 NuGet 包

```
Tools → NuGet Package Manager → Restore
```

### 5. 編譯項目

```
Build → Build Solution (Ctrl+Shift+B)
```

### 6. 找到可執行檔

編譯完成後，可執行檔位於：
```
bin\Debug\X975.exe
或
bin\Release\X975.exe
```

### 7. 複製必要文件到輸出目錄

確保輸出目錄（bin\Debug 或 bin\Release）包含：
- ITEMS\（所有圖片）
- ao-bin-dumps\（XML 文件）
- jsons\（JSON 配置）
- network_config.json

### 8. 運行

```cmd
cd bin\Debug
右鍵 X975.exe → 以管理員身份運行
```

---

## 測試是否能抓到封包

### 使用 Wireshark 驗證

1. **以管理員身份運行 Wireshark**

2. **選擇你的網卡**（通常是 Ethernet 或 WiFi）

3. **開始抓包**

4. **設置過濾器：**
   ```
   udp.port == 5050
   ```

5. **在主電腦啟動 Albion Online**

6. **進入遊戲世界**（不是角色選擇畫面）

7. **檢查 Wireshark：**
   - ✅ 看到 UDP 5050 封包 → 可以抓到，繼續用 local 模式
   - ❌ 沒有封包 → 需要檢查設置或改用 remote 模式

---

## 故障排除

### 問題 1: 找不到 X975.exe

**原因：** 項目未編譯

**解決：** 按照 "方案 B" 編譯項目

---

### 問題 2: 運行時報錯 "找不到 ITEMS\T1_TRASH.png"

**原因：** ITEMS 目錄不在可執行檔同目錄

**解決：**
```cmd
# 複製 ITEMS 目錄到可執行檔目錄
xcopy ITEMS bin\Debug\ITEMS /E /I /Y
xcopy ITEMS bin\Release\ITEMS /E /I /Y
```

---

### 問題 3: 報錯 "找不到 ao-bin-dumps"

**解決：**
```cmd
xcopy ao-bin-dumps bin\Debug\ao-bin-dumps /E /I /Y
xcopy ao-bin-dumps bin\Release\ao-bin-dumps /E /I /Y
```

---

### 問題 4: 雷達啟動但看不到任何東西

**可能原因：**

1. **遊戲未運行或未進入世界**
   - 必須實際進入遊戲地圖，不能只在角色選擇

2. **抓不到主電腦的封包**
   - 用 Wireshark 測試（見上面）
   - 如果抓不到，需要改用 remote 模式

3. **字體未安裝**
   - 雙擊 `Design\Font\DEATHEYE.ttf` 安裝

---

### 問題 5: "Install NPCAP" 錯誤

**解決：**
1. 下載 Npcap: https://npcap.com/#download
2. 安裝時勾選：
   - ✅ "Install Npcap in WinPcap API-compatible Mode"
3. 重啟電腦

---

## 進階：如果 local 模式抓不到主電腦封包

### 使用 remote 模式 + Ubuntu VM sender

**1. 在 Windows VM 配置：**

```json
// network_config.json
{
  "mode": "remote",
  "remote_port": 9999,
  "game_port": 5050
}
```

**2. 在 Ubuntu VM 運行 sender：**

```bash
cd /path/to/sender
sudo python3 packet_sender.py <Windows_VM_IP> 9999 ens33
```

**3. 在 Windows VM 運行雷達：**

```cmd
start_remote.bat
```

這樣可以利用 Ubuntu VM 能抓包的能力，轉發給 Windows VM 的雷達。

---

## 預期行為

### 成功啟動

**控制台輸出：**
```
[NetworkConfig] Loaded from network_config.json
[NetworkConfig] Mode: local, Remote Port: 9999, Game Port: 5050
[Init] Using LOCAL mode (npcap on game port 5050)
[Init] Starting packet sniffer...
```

**雷達窗口：**
- 應該出現半透明覆蓋層
- 可以看到配置界面

### 抓到封包

**控制台會顯示：**
```
Received event code: NewCharacter with parameters: ...
Received event code: Move with parameters: ...
```

**雷達會顯示：**
- 玩家圖標
- 資源節點
- 怪物
- 地下城入口

---

## 快速命令參考

### 檢查 .NET Framework

```cmd
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release
```

### 檢查 Npcap

```cmd
dir C:\Windows\System32\Npcap\
```

### 以管理員身份運行

```cmd
右鍵程序 → 以管理員身份運行
```

---

## 下一步

1. ✅ 確認所有前提條件
2. ✅ 選擇方案 A 或 B
3. ✅ 啟動雷達
4. ✅ 啟動遊戲
5. ✅ 享受雷達功能！

如果遇到問題，請參考故障排除部分或查看 `QUICK_START.md`。
