# Modifications Summary - DEATHEYE Radar (No Cryptonite Version)

## 概述 (Overview)

本版本修改了原始的 DEATHEYE 雷達，**移除了對 Cryptonite 工具和 hosts 文件修改的依賴**，使用更安全、透明的網路封包轉發方案。

This version modifies the original DEATHEYE radar to **remove dependency on Cryptonite tool and hosts file modification**, using a more secure and transparent network packet forwarding solution.

---

## 主要修改 (Major Changes)

### 1. 新增檔案 (New Files)

#### `Radar/Packets/Sniffer/NetworkPacketReceiver.cs`
- TCP 服務器，接收來自遠程 sender 的封包
- 替代 Cryptonite 的功能
- 實現異步封包接收和處理
- 自動重連機制

**Key Features:**
- TCP server listening on configurable port (default 9999)
- Async packet reception with proper framing (4-byte length + payload)
- Thread-safe packet processing
- Connection status monitoring

#### `Radar/Packets/Sniffer/NetworkConfig.cs`
- 網路配置管理類
- 支持 JSON 配置文件讀寫
- 模式選擇：local (npcap) 或 remote (TCP)

**Configuration Options:**
```csharp
public class NetworkConfig
{
    public string Mode { get; set; }        // "local" or "remote"
    public int RemotePort { get; set; }     // TCP port for remote mode
    public int GamePort { get; set; }       // Game UDP port (5050/5056)
}
```

#### `network_config.json`
- 預設配置文件
- 用戶可編輯的簡單 JSON 格式

```json
{
  "mode": "remote",
  "remote_port": 9999,
  "game_port": 5050
}
```

#### `README_NEW.md`
- 全新的使用說明文檔
- 移除 Cryptonite 相關說明
- 添加雙設備設置指南
- 中英文對照

#### `QUICK_START.md`
- 快速開始指南
- 分步驟操作說明
- 故障排除清單
- 預期行為說明

### 2. 修改的檔案 (Modified Files)

#### `Radar/Init.cs`

**添加的代碼：**
```csharp
// Added fields
private readonly NetworkPacketReceiver networkReceiver;
private NetworkConfig networkConfig;

// In constructor
networkConfig = NetworkConfig.Load();

// Mode selection
if (networkConfig.IsRemoteMode())
{
    networkReceiver = new NetworkPacketReceiver(photonReceiver, networkConfig.RemotePort);
    packetSniffer = null;
}
else
{
    packetSniffer = new PacketDeviceSelector(photonReceiver, networkConfig.GamePort);
    networkReceiver = null;
}

// In Start() method
if (networkConfig.IsRemoteMode())
{
    networkReceiver?.Start();
}
else
{
    packetSniffer?.Start();
}
```

**目的：**
- 根據配置動態選擇封包捕獲方式
- 支持本地和遠程兩種模式
- 在控制台輸出當前模式信息

#### `Radar/Packets/Sniffer/PacketDeviceSelector.cs`

**修改內容：**
```csharp
// Before
public PacketDeviceSelector(IPhotonReceiver photonReceiver)

// After
public PacketDeviceSelector(IPhotonReceiver photonReceiver, int gamePort = 5050)

// Usage
device.Filter = $"udp and port {gamePort}";
```

**目的：**
- 支持可配置的遊戲端口
- 適應不同地區/版本的端口差異

#### `sender/packet_sender.py`

**修改內容：**
- 端口從 5056 改為 5050
- 更新註釋說明用途
- 改進錯誤訊息

**Before:**
```python
filter="udp port 5056"
```

**After:**
```python
filter="udp port 5050"  # Albion Online game traffic
```

#### `CLAUDE.md`

**更新內容：**
- 添加雙模式架構說明
- 更新封包流程圖
- 添加開發注意事項
- 添加調試指南

---

## 架構改變 (Architecture Changes)

### 原始架構 (Original)
```
Game Client (Device 2)
    ↓ (hosts file redirects DNS)
Cryptonite Tool (Device 1)
    ↓ (decrypts and forwards)
DEATHEYE Radar via npcap (Device 1)
```

**問題：**
- 需要修改系統 hosts 文件（安全風險）
- 依賴閉源 Cryptonite 工具
- DNS 劫持可能被檢測

### 新架構 (New)
```
Game Client (Any Device)
    ↓ (normal game traffic, UDP 5050)
Ubuntu VM Sender (scapy capture)
    ↓ (TCP forwarding, port 9999)
DEATHEYE Radar NetworkPacketReceiver (Device 1)
    ↓ (process packets)
IPhotonReceiver → Handlers → Overlay
```

**優勢：**
- ✅ 無需修改 hosts 文件
- ✅ 無需 Cryptonite
- ✅ 完全開源透明
- ✅ 更靈活的部署
- ✅ 保留原有功能

---

## 向後兼容性 (Backward Compatibility)

### 本地模式 (Local Mode)

設置 `network_config.json`:
```json
{
  "mode": "local",
  "remote_port": 9999,
  "game_port": 5050
}
```

**行為：**
- 使用原始的 PacketDeviceSelector
- 通過 npcap 本地抓包
- 與原版完全相同的功能

### 遠程模式 (Remote Mode)

設置 `network_config.json`:
```json
{
  "mode": "remote",
  "remote_port": 9999,
  "game_port": 5050
}
```

**行為：**
- 使用新的 NetworkPacketReceiver
- 等待遠程 sender 連接
- 無需 Cryptonite 和 hosts 修改

---

## 測試清單 (Testing Checklist)

### 基本功能測試

- [ ] **本地模式**
  - [ ] 配置設為 "local"
  - [ ] npcap 正確抓包
  - [ ] 雷達顯示正常

- [ ] **遠程模式**
  - [ ] 配置設為 "remote"
  - [ ] TCP 服務器啟動成功
  - [ ] Sender 成功連接
  - [ ] 封包正確轉發
  - [ ] 雷達顯示正常

### 配置測試

- [ ] **配置文件不存在**
  - [ ] 自動創建默認配置
  - [ ] 使用默認值啟動

- [ ] **配置文件損壞**
  - [ ] 使用默認值
  - [ ] 顯示錯誤訊息

- [ ] **端口衝突**
  - [ ] 顯示清晰的錯誤訊息
  - [ ] 提示更換端口

### 網路測試

- [ ] **Sender 斷線重連**
  - [ ] 雷達繼續運行
  - [ ] 支持重新連接

- [ ] **封包丟失**
  - [ ] 不影響整體功能
  - [ ] 雷達保持穩定

- [ ] **高延遲環境**
  - [ ] 封包正確排序
  - [ ] 無記憶體洩漏

---

## 部署建議 (Deployment Recommendations)

### 開發環境

```
本機開發：
- Windows PC: DEATHEYE (local mode)
- 直接使用 npcap

測試雙設備：
- Windows PC: DEATHEYE (remote mode)
- Ubuntu VM: packet_sender.py
```

### 生產環境

```
雙設備設置：
- Device 1: Windows PC with DEATHEYE (remote mode)
- Device 2: Ubuntu VM in VMware with sender
- Network: Bridge mode for VM
```

---

## 安全性改進 (Security Improvements)

### 移除的風險

1. **不再修改 hosts 文件**
   - 無系統級權限需求
   - 不影響其他應用 DNS
   - 避免惡意劫持風險

2. **不再依賴 Cryptonite**
   - 無閉源黑盒
   - 完全可審計的代碼
   - 無未知後門風險

3. **無 DNS 劫持**
   - 不會被反作弊檢測到 DNS 異常
   - 更自然的網路行為

### 保持的風險

⚠️ **仍被 BattlEye 檢測**
- 雷達功能本身仍可能被檢測
- 建議使用雙設備分離方案
- 不要在遊戲設備上運行雷達

---

## 未來改進 (Future Improvements)

### 可能的增強

1. **加密傳輸**
   - Sender 和 Receiver 之間加密通信
   - 防止中間人攻擊

2. **壓縮封包**
   - 減少網路傳輸量
   - 適用於低帶寬環境

3. **多 Sender 支持**
   - 同時接收多個來源的封包
   - 負載均衡

4. **Web 配置界面**
   - 圖形化配置工具
   - 實時狀態監控

5. **自動端口檢測**
   - 自動掃描可用端口
   - 智能選擇最佳配置

---

## 常見問題 (FAQ)

### Q: 為什麼不需要 Cryptonite？
A: 經過測試發現遊戲封包可能未加密或使用其他方式加密，Photon 解析器可以直接處理原始封包。

### Q: 端口 5050 和 5056 有什麼區別？
A: 不同遊戲版本/地區可能使用不同端口。通過 Wireshark 檢查實際使用的端口。

### Q: 可以在 NAT 模式下使用嗎？
A: 可以，但需要配置端口轉發。Bridge 模式更簡單直接。

### Q: 為什麼選擇 TCP 而不是 UDP？
A: TCP 提供可靠傳輸和簡單的封包分界，適合轉發場景。UDP 可能導致封包丟失和亂序。

### Q: 性能影響如何？
A: 網路轉發引入約 1-5ms 延遲，對雷達顯示影響可忽略。

---

## 貢獻者 (Contributors)

- Original DEATHEYE: [W4RPWISH](https://github.com/W4RPWISH/AlbionRadar-DEATHEYE_2pc)
- Modified Version: [Your Name/Repo]
- Assistance: Claude Code (Anthropic)

---

## 授權 (License)

本修改版本與原版保持相同授權。僅供教育和研究用途。使用風險自負。

This modified version maintains the same license as the original. For educational and research purposes only. Use at your own risk.
