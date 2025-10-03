# 捕獲 Response 2 (JoinResponse) 完整指南

## 問題分析

經過詳細分析，發現：

### 1. JoinResponse 的雙重機制
- **Event 2**: 觸發事件（已捕獲，但無參數）
- **Response 2**: 實際的加入數據（**從未捕獲**）← 這才是我們需要的！

### 2. 當前捕獲狀態
你的 `event_structures.txt` 顯示：
```
✓ Event 2: 已捕獲（但無參數）
✗ Response 2: 從未捕獲 ← 問題所在！
```

捕獲到的 Responses：344, 18, 364, 211, 194, 16, 190
**缺少 Response 2！**

### 3. 根本原因
**你在已經登入遊戲後才啟動 Radar**，錯過了登入序列中的 Response 2！

Response 2 (JoinResponse) 只在以下時機觸發：
- ✓ 登入遊戲進入地圖
- ✓ 切換地圖/區域
- ✗ 已經在地圖內移動（不會再次觸發）

---

## 解決方案：捕獲完整登入序列

### 步驟 1：準備環境

1. **關閉 Albion Online**（完全登出）
2. **關閉 Radar**
3. **刪除舊的 log**：
   ```
   刪除 bin\Debug\event_structures.txt
   ```

### 步驟 2：正確的啟動順序

**關鍵：先啟動 Radar，再登入遊戲！**

1. **先啟動 Radar**
   - 執行 `bin\Debug\DEATHEYE.exe`
   - 等待看到訊息：
     ```
     [Init] Starting packet capture on game port 5056
     [DebugHandler] Initialized - logging to event_structures.txt
     ```

2. **然後啟動 Albion Online**
   - 登入帳號
   - 選擇角色
   - 進入遊戲地圖

3. **等待進入地圖**
   - 當角色完全載入到地圖後
   - 等待 5-10 秒

4. **檢查捕獲結果**
   - 查看 Radar console 是否顯示：
     ```
     !!! RESPONSE 2 (JoinResponse) CAPTURED !!!
     ```
   - 如果看到這個訊息 → 成功！

### 步驟 3：分析 Response 2

打開 `bin\Debug\event_structures.txt`，搜尋：
```
[ResponseStructure] Response 2:
```

檢查是否有以下參數：
- Key 0, 2, 8, 9, 48, 57, 77（根據 offsets.json）
- **尋找任何 byte[8] 陣列** ← 這可能是 XorCode！
- **尋找任何 byte[4-32] 陣列** ← 潛在的加密密鑰

---

## 端口配置（已修正）

目前 Radar 已設定監聽多個端口：
```csharp
// PacketDeviceSelector.cs 已修改為：
device.Filter = "udp and (port 5056 or port 5055 or port 4535)";
```

這將捕獲所有三個 Albion Online 相關端口：
- **5056** - 主要遊戲數據
- **5055** - 備用/聊天頻道
- **4535** - 登入/認證服務器

---

## 檢查清單

### 啟動前
- [ ] Albion Online 完全關閉
- [ ] Radar 完全關閉
- [ ] 刪除 `bin\Debug\event_structures.txt`

### 啟動時
- [ ] 先啟動 Radar（看到初始化訊息）
- [ ] 再啟動 Albion Online
- [ ] 登入並進入地圖

### 檢查結果
- [ ] Console 顯示 "!!! RESPONSE 2 (JoinResponse) CAPTURED !!!"
- [ ] `event_structures.txt` 包含 `[ResponseStructure] Response 2:`
- [ ] Response 2 有多個參數（不只是 Event ID）
- [ ] 檢查是否有 byte[8] 陣列

---

## 預期結果

如果成功捕獲 Response 2，你應該看到類似：
```
[ResponseStructure] Response 2:
  Key 0: Int32 = [玩家ID]
  Key 2: String = [公會名稱]
  Key 8: String = [地圖名稱]
  Key 9: Single[] = [位置座標]
  Key 48: Byte = [陣營]
  Key 57: String = [聯盟名稱]
  Key 77: String = [其他資訊]
  Key [?]: byte[8] = XX-XX-XX-XX-XX-XX-XX-XX...  ← 可能的 XorCode！
```

---

## 故障排除

### 問題 1：還是沒有 Response 2
**可能原因**：
- 登入序列發生在 4535 端口（已解決，現在會監聽）
- Photon 協議版本變更

**解決方法**：
1. 確認 Radar console 沒有錯誤訊息
2. 嘗試「切換地圖」來重新觸發 JoinResponse
3. 檢查 Windows 防火牆是否阻擋 npcap

### 問題 2：Event 2 有了，但 Response 2 還是沒有
**可能原因**：
- Albion 可能不再使用 Response 2，改用其他機制

**下一步**：
1. 分析 **Event 140**（看起來像完整的地圖進入數據）
2. 檢查 Event 140 的 Dictionary 參數（Key 33, 34, 35）
3. Event 140 可能包含加密密鑰

---

## 編譯修改後的代碼

如果需要重新編譯：
```cmd
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" DEATHEYE.csproj /p:Configuration=Debug /nologo /v:minimal
```

---

## 重要提醒

**Response 2 是唯一沒有被捕獲的 Response**

所有其他 Responses 都已捕獲：
- ✓ Response 16 (遊戲伺服器地址)
- ✓ Response 18, 190, 194, 211, 344, 364
- ✗ **Response 2 (JoinResponse)** ← 最關鍵的遺失封包！

**如果 Response 2 包含 XorCode，這將解決所有玩家位置問題！**
