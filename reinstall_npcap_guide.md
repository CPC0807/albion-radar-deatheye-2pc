# Npcap 重新安裝指南

## 問題

DEATHEYE 提示 "Install NPCAP Don't change the checkboxes!"

## 原因

1. Npcap 未正確安裝
2. 缺少 WinPcap 兼容模式
3. 32-bit vs 64-bit 平台不匹配

## 解決步驟

### 步驟 1: 完全卸載現有的 Npcap

1. **打開控制台**
   - Windows 設置 → 應用程式 → 應用程式與功能

2. **找到並卸載：**
   - Npcap
   - WinPcap（如果有）

3. **重啟電腦**

---

### 步驟 2: 下載最新版 Npcap

**下載地址：**
https://npcap.com/dist/npcap-1.80.exe

或

https://npcap.com/#download

---

### 步驟 3: 正確安裝 Npcap

**重要：以管理員身份運行安裝程式**

1. **右鍵安裝程式** → 以系統管理員身分執行

2. **在安裝選項中，必須勾選：**

```
┌─────────────────────────────────────────┐
│ Npcap Setup                             │
├─────────────────────────────────────────┤
│                                         │
│ [x] Install Npcap in WinPcap API-       │  ← 必須勾選！
│     compatible Mode                     │
│                                         │
│ [ ] Support raw 802.11 traffic          │  ← 可選
│                                         │
│ [ ] Restrict Npcap driver access to     │  ← 不要勾選
│     Administrators only                 │
│                                         │
│ [ ] Support loopback traffic capture    │  ← 可選
│     and injection                       │
│                                         │
└─────────────────────────────────────────┘
```

**關鍵選項：**
- ✅ **必須勾選：** "Install Npcap in WinPcap API-compatible Mode"
- ❌ **不要勾選：** "Restrict Npcap driver access to Administrators only"

3. **完成安裝**

4. **重啟電腦**（重要！）

---

### 步驟 4: 驗證安裝

**執行驗證腳本：**
```cmd
check_npcap.bat
```

**預期輸出：**
```
[1] Checking System32\Npcap directory...
   [OK] Directory exists

[2] Checking System32\Npcap\wpcap.dll...
   [OK] wpcap.dll found

[3] Checking System32\Npcap\Packet.dll...
   [OK] Packet.dll found

[5] Checking if Npcap service is running...
   [OK] Npcap service is running
```

---

### 步驟 5: 測試 DEATHEYE

```cmd
cd bin\Release
右鍵 X975.exe → 以管理員身份運行
```

應該不再出現 "Install NPCAP" 錯誤。

---

## 如果還是不行

### 檢查點 1: 平台不匹配

DEATHEYE 可能編譯為 32-bit 但 npcap 只安裝了 64-bit 支持。

**解決：**

在 Visual Studio 中檢查編譯平台：
```
Build → Configuration Manager
```

確認 Platform 為：
- **x64**（推薦）- 64-bit 程序
- 或 **Any CPU**

如果是 x86（32-bit），則需要確保 npcap 安裝了 32-bit 支持。

**重新編譯為 x64：**
```
msbuild DEATHEYE.sln /p:Configuration=Release /p:Platform=x64
```

---

### 檢查點 2: Npcap 服務未啟動

**檢查服務：**
```cmd
sc query npcap
```

**如果未運行，啟動服務：**
```cmd
sc start npcap
```

**設置為自動啟動：**
```cmd
sc config npcap start= auto
```

---

### 檢查點 3: 防毒軟件阻擋

某些防毒軟件會阻止 npcap 驅動。

**暫時停用防毒軟件測試：**
1. 停用防毒軟件
2. 重啟電腦
3. 測試 DEATHEYE
4. 如果可以運行，則需要將 npcap 和 DEATHEYE 加入白名單

---

## 替代方案：使用 WinPcap（舊版）

如果 npcap 真的無法正常工作，可以嘗試舊版 WinPcap：

**下載：**
https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe

**注意：**
- WinPcap 已停止更新
- 可能在新版 Windows 上有問題
- 優先使用 npcap

---

## 常見錯誤訊息

### "Npcap service is not running"

**解決：**
```cmd
sc start npcap
```

### "Access denied"

**解決：**
- 確保以管理員身份運行
- 檢查 npcap 安裝時沒有勾選 "Restrict access to Administrators only"

### "Could not find any devices"

**解決：**
1. 重新安裝 npcap
2. 檢查網卡驅動是否正常
3. 在 Device Manager 中確認網卡狀態

---

## 確認清單

安裝完成後，確認以下所有項目：

- [ ] Npcap 已安裝（控制台 → 程式與功能）
- [ ] 勾選了 "WinPcap API-compatible Mode"
- [ ] 電腦已重啟
- [ ] `C:\Windows\System32\Npcap\wpcap.dll` 存在
- [ ] `C:\Windows\System32\Npcap\Packet.dll` 存在
- [ ] Npcap 服務正在運行 (`sc query npcap`)
- [ ] DEATHEYE 以管理員身份運行
- [ ] check_npcap.bat 所有檢查都通過

---

## 仍然無法解決？

請提供以下信息：

1. **check_npcap.bat 的完整輸出**
2. **Windows 版本**
   ```cmd
   winver
   ```
3. **DEATHEYE 編譯平台**
   - 在 Visual Studio 查看：Build → Configuration Manager
4. **是否使用虛擬機**
   - 如果是，哪個虛擬化軟件（VMware/VirtualBox/Hyper-V）

這些信息可以幫助進一步診斷問題。
