# 玩家座標Debug測試指南

## 已添加的Debug功能

### 1. MoveEvent.cs
添加了原始 bytes 輸出：
```
[MoveEvent] ID:{玩家ID} RawPosBytes: XX-XX-XX-XX-XX-XX-XX-XX Flags:{移動標記}
[MoveEvent] ID:{玩家ID} RawNewPosBytes: XX-XX-XX-XX-XX-XX-XX-XX
```

### 2. PlayersHandler.cs
測試4種不同的座標解析方法：
- **Method1**: Y,X from offset 4,0（當前使用，和怪物相同）
- **Method2**: X,Y from offset 0,4（反向）
- **Method3**: Decrypt XY（使用 XorCode 解密）
- **Method4**: Decrypt YX（解密後反向）

## 測試步驟

### 1. 啟動測試
```bash
cd c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc\bin\Debug
DEATHEYE.exe
```

### 2. 進入遊戲
- 啟動 Albion Online
- 進入遊戲地圖
- **等待看到其他玩家移動**

### 3. 觀察Console輸出

你會看到類似以下的輸出：
```
[MoveEvent] ID:1234567 RawPosBytes: AA-BB-CC-DD-EE-FF-00-11 Flags:Speed, NewPosition
[MoveEvent] ID:1234567 RawNewPosBytes: 11-22-33-44-55-66-77-88
[PlayerPos Debug] ID:1234567 Name:TestPlayer
  Method1 (Y,X from offset 4,0): (123.45, 456.78)
  Method2 (X,Y from offset 0,4): (456.78, 123.45)
  Method3 (Decrypt XY): (789.01, 234.56) [XorCode: NULL]
  Method4 (Decrypt YX): (234.56, 789.01)
```

### 4. 分析結果

#### 情況 A：所有方法都顯示 (0.00, 0.00) 或異常值
**原因**:
- 座標 bytes 可能是空的或全為0
- 封包解析的 index 計算錯誤

**解決方案**:
- 檢查 RawPosBytes 是否為 `00-00-00-00-00-00-00-00`
- 如果是，問題在於 `MoveEvent.cs` 的 `Array.Copy` index

#### 情況 B：Method1/Method2 顯示合理座標（例如：100-2000範圍）
**原因**:
- 座標沒有加密！
- Albion Online 已經改為明文傳輸座標

**解決方案**:
- 檢查哪個方法的值看起來正確（通常 Method1）
- 移除所有 XorCode 相關代碼

#### 情況 C：Method3/Method4 顯示合理座標
**原因**:
- 座標仍然加密
- 但 XorCode 不是來自 KeySync Event 593

**解決方案**:
- 執行方案 C（暴力測試所有 byte arrays）
- XorCode 可能在 Event 140 或其他隱藏位置

#### 情況 D：所有方法都顯示巨大的科學記數值（如 1e+38）
**原因**:
- 座標加密且 XorCode 為 NULL
- bytes 被錯誤解釋為 float

**解決方案**:
- 必須找到正確的 XorCode
- 執行方案 C

## 預期的正確座標範圍

Albion Online 的座標通常在以下範圍：
- **城市內**: 0 - 500
- **野外地圖**: 0 - 3000
- **巨大地圖**: 0 - 10000

如果你看到：
- **合理值**: 123.45, 456.78（可能正確！）
- **零值**: 0.00, 0.00（有問題）
- **巨大值**: 8749658.00（加密問題）
- **負值**: -8779551.00（加密問題）

## 比較怪物座標

同時觀察怪物的位置是否正常：
```
怪物位置正常 + 玩家位置錯誤 = 玩家座標需要特殊處理（可能加密）
怪物位置錯誤 + 玩家位置錯誤 = MoveEvent 解析問題
```

## 下一步

根據測試結果：
1. **如果 Method1 或 Method2 顯示合理值** → 移除 Decrypt 代碼，直接使用
2. **如果所有方法都失敗** → 檢查 RawPosBytes 確認 bytes 是否正確提取
3. **如果 Method3/4 顯示合理值但 XorCode 是 NULL** → 需要找到 XorCode 來源

## 儲存測試結果

請將 Console 輸出複製到文件：
```bash
DEATHEYE.exe > debug_output.txt 2>&1
```

然後分享前 50 行給我分析。
