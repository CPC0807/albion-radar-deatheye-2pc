# Radiant Wilds 更新導致的問題

## 更新信息

**日期：** 2026年4月13日
**更新名稱：** Radiant Wilds
**影響：** 封包協議變更

## 問題根源

Albion Online 的 Radiant Wilds 更新引入了新的 Photon Protocol16 資料類型碼（Type Codes），而目前的 Protocol16.dll (v4.1.0) 不支援這些新類型。

### 不支援的 Type Codes

```
164, 170, 178, 192, 194, 196, 218, 222, 224, 236, 248, 254
```

### 錯誤信息

```
Type code: XXX not implemented.
at Protocol16.Protocol16Deserializer.Deserialize(Protocol16Stream input, Byte typeCode)
```

## 當前狀態

- ❌ **PhotonPackageParser 4.1.0** - 最後更新：2020年10月11日（過時）
- ❌ **Albion.Network 5.0.1** - 最後更新：2021年11月（可能需要更新）
- ✅ **封包捕獲正常** - SharpPcap 正在接收資料
- ❌ **封包解析失敗** - Protocol16 反序列化器崩潰

## 解決方案

### 方案 A: 等待社區更新（推薦）

監控這些專案是否發布更新：

1. **ao-data/albiondata-client**
   https://github.com/ao-data/albiondata-client

2. **Zeldruck/Albion-Online-ZQRadar**
   https://github.com/Zeldruck/Albion-Online-ZQRadar

3. **0blu/PhotonPackageParser**
   https://github.com/0blu/PhotonPackageParser

### 方案 B: 使用修改版 Protocol16

需要有人編譯支援新 Type Code 的 Protocol16.dll。可能的資料類型對應：

| Type Code | 可能的資料類型 |
|-----------|--------------|
| 164 (0xA4) | ? |
| 170 (0xAA) | ? |
| 178 (0xB2) | ? |
| 192 (0xC0) | ? |
| 194 (0xC2) | ? |
| 196 (0xC4) | ? |
| 218 (0xDA) | ? |
| 222 (0xDE) | ? |
| 224 (0xE0) | ? |
| 236 (0xEC) | ? |
| 248 (0xF8) | ? |
| 254 (0xFE) | ? |

### 方案 C: 臨時繞過（當前可用）

修改代碼跳過無法解析的封包，允許程序繼續運行（但會丟失部分數據）。

## 更新檢查清單

- [ ] 檢查 ao-data GitHub 是否有更新
- [ ] 檢查其他 Radar 專案是否已適配
- [ ] 查看 Albion Online 官方論壇討論
- [ ] 尋找更新的 PhotonPackageParser 或 Protocol16.dll
- [ ] 考慮自行編譯支援新 Type Code 的版本

## 臨時 Workaround

當前程序會捕獲並記錄所有錯誤，但無法處理包含新 Type Code 的封包。這意味著：

- ✅ 程序可以運行
- ⚠️ 部分遊戲數據無法解析
- ❌ 可能看不到玩家/怪物/資源

## 參考資料

- Radiant Wilds 更新公告：https://albiononline.com/update
- Photon Protocol16 文檔：https://doc.photonengine.com/
- PhotonPackageParser GitHub：https://github.com/0blu/PhotonPackageParser

## 社區聯繫

如果你找到解決方案或更新的庫，請分享到：
- Albion Online Discord
- GitHub Issues
- Albion Online 論壇
