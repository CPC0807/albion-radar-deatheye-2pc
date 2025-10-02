# 尋找正確的 KeySync 事件 ID - 操作指南

## 📊 當前狀況

### ✅ 已確認的事實：
1. **你的 Photon 封包解析正常** - 能看到 60+ 種事件（Event 11-601）
2. **不需要 Cryptonite** - 你已經能捕獲並解析封包
3. **Event 593 (KeySync) 從未觸發** - 在 479 行事件日誌中完全不存在
4. **玩家裝備可見，座標不可見** - 因為缺少 XorCode 解密金鑰

### ❌ 問題根源：
**indexes.json 中的 KeySync ID (593) 已經過時！**

---

## 🎯 下一步操作

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

KeySync 事件只在**切換區域**時觸發，請依序測試：

#### ✨ 最可能觸發的動作：
1. **從城市傳送到野外**
   - 使用傳送門離開城市
   - 進入 Yellow/Red/Black Zone

2. **進入地下城（Dungeon）**
   - 找到地下城入口
   - 進入地下城

3. **進入迷霧（Mists）**
   - 找到迷霧入口
   - 進入迷霧區域

4. **PVP 區域切換**
   - 從 Blue Zone → Yellow Zone
   - 從 Yellow Zone → Red Zone

### 步驟 5：查看 Console 輸出

尋找以下訊息：

```
[EventStructure] Event 612 logged
  <<< 8-BYTE ARRAY >>> Event 612 Key 0 - byte[8]
```

或

```
  <<< POTENTIAL KEY >>> Event 587 Key 1 - byte[12]
```

**記下所有顯示 `<<< 8-BYTE ARRAY >>>` 或 `<<< POTENTIAL KEY >>>` 的事件ID！**

### 步驟 6：分析 event_structures.txt

1. 打開 `bin\Debug\event_structures.txt`
2. 搜尋 `byte[8]`（最可能是 XorCode）
3. 搜尋 `byte[4]` 到 `byte[32]`（其他可能的金鑰長度）
4. 記錄所有包含 byte 陣列的事件ID

---

## 📋 候選事件記錄表

請把找到的事件記錄在這裡：

### 包含 byte[8] 的事件：
```
Event ID: _______  Key: _____  值: ___________________
Event ID: _______  Key: _____  值: ___________________
Event ID: _______  Key: _____  值: ___________________
```

### 包含其他長度 byte[] 的事件：
```
Event ID: _______  Key: _____  長度: ___  值: ___________________
Event ID: _______  Key: _____  長度: ___  值: ___________________
```

---

## 🔍 特別注意

### 觸發時機很重要！
- KeySync 通常在**剛進入新區域的瞬間**觸發
- 注意 Console 在你**傳送/進門**瞬間的輸出
- 可能需要嘗試多次不同的區域切換

### 如何確認找到了 KeySync？
1. 事件在切換區域時觸發
2. 包含一個 byte[] 陣列（通常 4-8 bytes）
3. 數值看起來是隨機的（不是全 0）
4. 每次進入新區域時都會觸發

---

## 🚀 找到候選事件後...

把事件ID告訴我，我會幫你：
1. 修改 `indexes.json` 測試新的 KeySync ID
2. 創建測試代碼驗證座標是否能正確解密
3. 如果成功，玩家位置將正常顯示！

---

## 💡 如果沒有找到任何 byte[] 陣列...

可能性：
1. 需要測試更多不同類型的區域切換
2. KeySync 可能已經不存在（Albion 改用其他加密方式）
3. 需要使用暴力搜尋法（測試所有可能的事件ID）

## 📞 需要幫助？

把你的發現告訴我：
- event_structures.txt 的內容
- Console 輸出的截圖
- 你執行了哪些觸發動作

我會幫你分析下一步！
