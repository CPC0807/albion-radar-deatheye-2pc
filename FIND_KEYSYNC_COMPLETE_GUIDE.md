# 🔍 尋找 KeySync - 完整掃描指南（更新版）

## 📊 重要更新

### 🆕 現在會掃描三種封包類型：

1. **Event（事件）** - 伺服器主動推送
2. **Response（回應）** - 伺服器對客戶端請求的回應
3. **Request（請求）** - 客戶端發送的請求

### ❓ 為什麼要掃描 Response 和 Request？

**KeySync 可能不再是 Event！**

理論：
- Albion Online 可能改變了加密機制
- KeySync 可能變成 **Response** 型別（客戶端請求 → 伺服器回應 XorCode）
- 或者合併到其他封包中

---

## 🎯 完整測試步驟

### 步驟 1：重新編譯程序（DEBUG 模式）

1. 在 Visual Studio 中選擇 **Debug** 配置
2. 按 **F6** 或點擊「建置」→「建置方案」
3. 確認編譯成功

### 步驟 2：關閉當前運行的程序

1. 關閉當前的 Radar 程序
2. 刪除或重命名 `bin\Debug\event_structures.txt`
   ```cmd
   ren "bin\Debug\event_structures.txt" "event_structures_old.txt"
   ```

### 步驟 3：啟動程序並準備測試

1. 運行 `bin\Debug\X975.exe`
2. 確認 Console 顯示：
   ```
   [DebugHandler] Initialized - logging to event_structures.txt
   ```
3. 啟動 Albion Online

### 步驟 4：執行觸發動作（最重要！）

KeySync 可能在**切換區域**或**首次進入遊戲**時觸發：

#### ✨ 最可能觸發的動作：

1. **剛進入遊戲（最優先！）**
   - 在登入畫面時啟動 Radar
   - 觀察登入過程的所有封包
   - KeySync 可能在連線初始化時就發送

2. **從城市傳送到野外**
   - 使用傳送門離開城市
   - 進入 Yellow/Red/Black Zone

3. **進入地下城（Dungeon）**
   - 找到地下城入口
   - 進入地下城

4. **進入迷霧（Mists）**
   - 找到迷霧入口
   - 進入迷霧區域

5. **PVP 區域切換**
   - 從 Blue Zone → Yellow Zone
   - 從 Yellow Zone → Red Zone

### 步驟 5：查看 Console 輸出

尋找三種類型的訊息：

#### Event:
```
[EventStructure] Event 612 logged
  <<< 8-BYTE ARRAY >>> Event 612 Key 0 - byte[8]
```

#### Response:
```
[ResponseStructure] Response 2 logged
  <<< 8-BYTE ARRAY >>> Response 2 Key 5 - byte[8]
```

#### Request:
```
[RequestStructure] Request 21 logged
  <<< POTENTIAL KEY >>> Request 21 Key 3 - byte[12]
```

**記下所有顯示 `<<< 8-BYTE ARRAY >>>` 或 `<<< POTENTIAL KEY >>>` 的封包！**

### 步驟 6：分析 event_structures.txt

打開 `bin\Debug\event_structures.txt`，搜尋：

1. **`[EventStructure]`** - 所有事件
2. **`[ResponseStructure]`** - 所有回應
3. **`[RequestStructure]`** - 所有請求
4. **`byte[8]`** - 最可能是 XorCode
5. **`byte[4]` 到 `byte[32]`** - 其他可能的金鑰長度

---

## 📋 候選封包記錄表

### 包含 byte[8] 的 Events：
```
Event ID: _______  Key: _____  值: ___________________
Event ID: _______  Key: _____  值: ___________________
```

### 包含 byte[8] 的 Responses：
```
Response ID: _______  Key: _____  值: ___________________
Response ID: _______  Key: _____  值: ___________________
```

### 包含 byte[8] 的 Requests：
```
Request ID: _______  Key: _____  值: ___________________
Request ID: _______  Key: _____  值: ___________________
```

---

## 🔍 如何判斷是否為 KeySync？

### ✅ 可能的特徵：

1. **觸發時機**
   - 在進入新區域時觸發
   - 或在遊戲啟動/登入時觸發

2. **包含 byte[] 陣列**
   - 通常是 4-8 bytes
   - 數值看起來是隨機的（不全是 0）

3. **每次切換區域都觸發**
   - 重複進出同一地點
   - 每次都能看到這個封包

4. **Response 2 (JoinResponse) 是重點！**
   - 這是客戶端加入地圖的回應
   - 最可能包含初始化資訊（包括 XorCode）

---

## 🚀 找到候選封包後的測試步驟

### 假設找到 Response 2 包含 byte[8]：

1. **檢查是否有對應的 Handler**
   - 查看 `Init.cs` 是否已註冊 `JoinResponseOperationHandler`
   - 檢查該 Handler 是否處理 XorCode

2. **修改 Handler 測試**
   - 如果是 Response，修改對應的 `ResponseHandler`
   - 如果是 Event，修改 `indexes.json`

3. **驗證座標解密**
   - 重新編譯並運行
   - 查看玩家位置是否正常顯示

---

## 💡 特別注意事項

### 🎯 重點關注 Response 2 (JoinResponse)

根據代碼分析，`JoinResponseOperationHandler` 已經存在，這可能就是 KeySync 的新載體！

檢查方法：
1. 在 `event_structures.txt` 搜尋 `[ResponseStructure] Response 2`
2. 查看是否包含 byte[] 陣列
3. 這可能就是新的 XorCode 來源！

### 🔄 如果沒有找到任何 byte[] 陣列

可能性：
1. 需要更多不同類型的區域切換
2. KeySync 機制已完全改變（不再使用 XOR）
3. 需要 Cryptonite 或其他解密工具

---

## 📞 回報發現

找到候選封包後，請記錄：

1. **封包類型**（Event / Response / Request）
2. **封包 ID**
3. **Key 編號**
4. **byte[] 長度**
5. **完整的值**（從 event_structures.txt 複製）
6. **觸發條件**（何時出現）

範例：
```
類型: Response
ID: 2
Key: 5
長度: byte[8]
值: 12-34-56-78-9A-BC-DE-F0
觸發: 每次登入遊戲時
```

---

## 🎯 下一步計劃

找到候選後，我會幫你：
1. 分析封包結構
2. 修改對應的 Handler
3. 測試座標解密
4. 驗證是否成功

加油！這次會掃描所有類型的封包，成功率大幅提高！ 🚀
