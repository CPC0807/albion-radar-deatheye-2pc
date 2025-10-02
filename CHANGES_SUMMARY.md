# 修改總結 - DEATHEYE 雷達（簡化版）

## 🎯 針對你的架構優化

### 你的設置
```
主電腦 (Albion Online - UDP 5056)
    ↓
Windows VM (VMware Bridge 模式)
    ↓ npcap 本地抓包
DEATHEYE 雷達
```

### 關鍵發現
- ✅ Windows VM 的 Bridge 模式**可以**抓取主電腦封包
- ✅ 遊戲使用端口 **5056** 和 **5055**（不是預設的 5050）
- ❌ **不需要** sender/receiver 架構
- ❌ **不需要** remote 模式

---

## 📝 實際修改內容

### 1. 新增文件

#### `Radar/Packets/Sniffer/NetworkConfig.cs` ✨
- **目的：** 簡單的端口配置類
- **功能：** 讀取 JSON 配置文件，允許配置遊戲端口
- **好處：** 不需重新編譯就能改端口

```csharp
public class NetworkConfig
{
    public int GamePort { get; set; } = 5056;

    public static NetworkConfig Load(string configPath = "network_config.json")
    {
        // 從 JSON 讀取配置
    }
}
```

---

### 2. 修改的文件

#### `Radar/Init.cs` ✏️

**刪除：**
```csharp
- private readonly NetworkPacketReceiver networkReceiver;  // 不需要
- private NetworkConfig networkConfig;                     // 改為只用端口配置
```

**簡化：**
```csharp
// 之前：複雜的模式選擇邏輯
if (networkConfig.IsRemoteMode()) {
    networkReceiver = new NetworkPacketReceiver(...);
} else {
    packetSniffer = new PacketDeviceSelector(...);
}

// 現在：直接使用 local 模式
networkConfig = NetworkConfig.Load();
packetSniffer = new PacketDeviceSelector(photonReceiver, networkConfig.GamePort);
```

#### `Radar/Packets/Sniffer/PacketDeviceSelector.cs` ✏️

**已在之前修改：**
```csharp
// 支持可配置端口
public PacketDeviceSelector(IPhotonReceiver photonReceiver, int gamePort = 5050)
{
    this.gamePort = gamePort;
}

// 使用配置的端口
device.Filter = $"udp and port {gamePort}";
```

#### `network_config.json` ✏️

**簡化為：**
```json
{
  "game_port": 5056
}
```

---

### 3. 刪除的文件

#### `Radar/Packets/Sniffer/NetworkPacketReceiver.cs` ❌
- 之前創建用於 remote 模式
- 你的場景不需要
- 已刪除

---

## ✅ 修改成果

### 保留的功能
1. ✅ **可配置端口** - 輕鬆改變遊戲端口（5056/5055/5050）
2. ✅ **無需 Cryptonite** - 不需要閉源解密工具
3. ✅ **無需修改 hosts** - 不需要系統級修改
4. ✅ **簡單配置** - 只有一個 JSON 文件

### 移除的複雜性
1. ❌ 移除 remote 模式（你不需要）
2. ❌ 移除 TCP 接收器（你不需要）
3. ❌ 移除模式選擇邏輯（簡化代碼）
4. ❌ 移除 sender/receiver 架構（你不需要）

---

## 🔧 使用方式

### 編譯步驟

1. **打開 Visual Studio**
   ```cmd
   start DEATHEYE.sln
   ```

2. **添加 NetworkConfig.cs 到項目**
   - 右鍵 `Sniffer` 資料夾 → Add → Existing Item
   - 選擇 `NetworkConfig.cs`

3. **編譯**
   ```
   Build → Build Solution (Ctrl+Shift+B)
   ```

4. **複製資源到輸出目錄**
   ```cmd
   xcopy ITEMS bin\Release\ITEMS /E /I /Y
   xcopy ao-bin-dumps bin\Release\ao-bin-dumps /E /I /Y
   xcopy jsons bin\Release\jsons /E /I /Y
   copy network_config.json bin\Release\
   ```

5. **運行**
   ```cmd
   cd bin\Release
   右鍵 X975.exe → 以管理員身份運行
   ```

### 配置端口

如果需要更改端口（如改為 5050）：

1. 編輯 `network_config.json`
   ```json
   {
     "game_port": 5050
   }
   ```

2. 重啟雷達（無需重新編譯）

---

## 📊 對比：修改前 vs 修改後

| 項目 | 原版 DEATHEYE | 你的修改版 |
|------|--------------|-----------|
| 需要 Cryptonite | ✅ 是 | ❌ 否 |
| 修改 hosts 文件 | ✅ 是 | ❌ 否 |
| 可配置端口 | ❌ 否（硬編碼 5050） | ✅ 是（JSON 配置） |
| 支持 remote 模式 | ❌ 否 | ❌ 否（不需要） |
| 代碼複雜度 | 中 | 低（簡化） |
| 設置難度 | 高 | 低 |

---

## 🎓 學到的東西

### 關於 Bridge 模式

**為什麼 Ubuntu VM 能抓包？**
- scapy 自動啟用混雜模式
- Linux 網路堆疊對 Bridge 支持好

**為什麼 Windows VM 也能抓包？**
- npcap 啟用了混雜模式
- VMware Bridge 模式配置正確
- 你的網路環境支持（可能是 WiFi 或特殊交換機配置）

### 關於端口

**遊戲端口：**
- 5056 - 主要遊戲流量
- 5055 - 輔助服務
- 5050 - 舊版本或其他地區

**配置靈活性：**
- 現在可以輕鬆測試不同端口
- 不需要重新編譯

---

## 📂 文件結構

```
albion-radar-deatheye-2pc/
├── Radar/
│   ├── Init.cs                              ✏️ 修改：簡化邏輯
│   └── Packets/
│       └── Sniffer/
│           ├── PacketDeviceSelector.cs      ✏️ 修改：支持配置端口
│           └── NetworkConfig.cs             ✨ 新增：端口配置類
├── network_config.json                      ✏️ 修改：簡化為只有端口
├── BUILD_AND_RUN.md                         ✨ 新增：編譯運行指南
└── CHANGES_SUMMARY.md                       ✨ 新增：本文件
```

---

## ⚠️ 注意事項

### 必須以管理員身份運行

**原因：** npcap 需要管理員權限才能抓包

**如何：**
```
右鍵 X975.exe → 以管理員身份運行
```

### 遊戲必須進入世界

**不會工作：**
- ❌ 角色選擇畫面
- ❌ 登入畫面

**會工作：**
- ✅ 實際進入遊戲地圖
- ✅ 角色可以移動

### 端口可能因地區而異

**測試方法：**
1. 用 Wireshark 抓包
2. 過濾器：`udp`
3. 啟動遊戲
4. 查看實際使用的端口
5. 更新 network_config.json

---

## 🚀 下一步

### 立即可做
1. ✅ 編譯項目（按照 BUILD_AND_RUN.md）
2. ✅ 運行測試
3. ✅ 享受雷達功能

### 如果遇到問題
1. 📖 查看 BUILD_AND_RUN.md 的故障排除部分
2. 🔍 檢查控制台錯誤訊息
3. 🔧 用 Wireshark 驗證能否抓包

---

## 📚 相關文檔

- `BUILD_AND_RUN.md` - 詳細的編譯和運行指南
- `network_config.json` - 配置文件
- `START_HERE.md` - 快速開始指南
- `test_npcap.bat` - npcap 測試腳本

---

**修改完成日期：** 2025-10-02
**修改類型：** 簡化版（僅 local 模式）
**適用場景：** Windows VM Bridge 模式可抓包
