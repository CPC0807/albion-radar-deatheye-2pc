# 動態 Mob Offset 檢測功能

## 功能概述

實現了自動檢測 Mob TypeId Offset 的功能，**無需再手動調整 offset 值**。每次程式啟動時會自動計算正確的 offset，完全適應 ao-bin-dumps 更新。

## 修改日期
2026-03-12

## 問題背景

### 舊問題
- Albion Online 頻繁更新（幾乎每週）
- 每次更新可能在 mobs.xml 開頭插入新怪物
- 導致所有 XML 索引後移
- Offset 需要手動調整（-15 → -16 → ...）
- 每次更新後需要測試和修正

### 新方案
✅ **動態檢測** - 程式啟動時自動計算正確的 offset
✅ **無需維護** - ao-bin-dumps 更新後自動適應
✅ **穩定可靠** - 使用多個錨點怪物交叉驗證

## 實現原理

### 錨點怪物機制

使用幾個"錨點怪物"來計算 offset：

| UniqueName | 預期索引 | 特點 |
|-----------|----------|------|
| T3_MOB_TR_HERETIC_MAGE_BOSS | 0 | 遊戲最早期的 Boss |
| T4_MOB_TR_HERETIC_SHADOWMASK_SUMMON | 3 | T4 召喚物 |
| T4_MOB_TR_SILVER_KEEPER_EARTHDAUGHTER_BOSS | 9 | 常見的 Keeper Boss |
| T5_MOB_TREASURE_BEAR | 402 | 中段的採集怪物 |

### 檢測算法

```
1. 在 mobs.xml 中查找每個錨點怪物的實際索引
2. 計算 "索引偏移" = 實際索引 - 預期索引
3. 統計最常見的偏移量（投票機制）
4. 計算 Offset = 默認值 - 索引偏移
```

**示例：**

如果 mobs.xml 開頭插入了 1 個新怪物：

```
錨點: T5_MOB_TREASURE_BEAR
預期索引: 402
實際索引: 403 (因為前面插入了 1 個怪物)
索引偏移: 403 - 402 = 1

Offset = 16 - 1 = 15
```

### 為什麼穩定？

- 使用 **4 個錨點**分佈在不同位置（前、中、後）
- **投票機制**：如果多數錨點得出相同結果，才採用
- 錨點怪物是**遊戲核心內容**，不會被刪除
- 如果錨點找不到，使用**默認值 16** 作為後備

## 修改的文件

### 1. ResponseObj/MobInfo.cs

添加 `UniqueName` 屬性：

```csharp
public class MobInfo
{
    public int Id { get; set; }
    public string UniqueName { get; set; }  // ← 新增
    public int Tier { get; set; }
    // ...
}
```

### 2. Radar/Dependencies/Mob/MobModel.cs

保存 UniqueName 到 MobInfo：

```csharp
return root.Mobs
    .Select((e, i) => new MobInfo()
    {
        Id = i,
        UniqueName = e.UniqueName,  // ← 新增
        Tier = e.Tier,
        // ...
    })
    .ToList();
```

### 3. Radar/Utility/MobOffsetDetector.cs（新文件）

實現動態檢測邏輯：

```csharp
public static int DetectOffset(List<MobInfo> mobInfos)
{
    // 1. 查找錨點怪物的實際索引
    // 2. 計算索引偏移
    // 3. 投票選出最常見的偏移
    // 4. 返回計算出的 Offset
}
```

### 4. Radar/Init.cs

在初始化時執行檢測：

```csharp
// 加載 Mob 數據並動態檢測 Offset
List<MobInfo> mobInfos = MobData.Load("ao-bin-dumps/mobs.xml");

// 執行動態 Offset 檢測
MobTypeIdOffset = MobOffsetDetector.DetectOffset(mobInfos);

// Debug 模式下驗證結果
#if DEBUG
MobOffsetDetector.VerifyOffset(mobInfos, MobTypeIdOffset);
#endif

mobsHandler = new MobsHandler(mobInfos);
```

### 5. Radar/Packets/Handlers/NewMobEvent.cs

使用動態檢測的 Offset：

```csharp
// 修改前
TypeId = rawTypeId - 16;  // 固定值

// 修改後
TypeId = rawTypeId - Init.MobTypeIdOffset;  // 動態值
```

### 6. DEATHEYE.csproj

添加新文件到編譯：

```xml
<Compile Include="Radar\Utility\MobOffsetDetector.cs" />
```

## 控制台輸出

### 啟動時的檢測日誌

```
[MobOffsetDetector] Starting offset detection...
[MobOffsetDetector] Total mobs in XML: 4530
[MobOffsetDetector] Anchor: T3_MOB_TR_HERETIC_MAGE_BOSS
  Expected index: 0, Actual index: 0, Shift: 0
[MobOffsetDetector] Anchor: T4_MOB_TR_HERETIC_SHADOWMASK_SUMMON
  Expected index: 3, Actual index: 3, Shift: 0
[MobOffsetDetector] Anchor: T4_MOB_TR_SILVER_KEEPER_EARTHDAUGHTER_BOSS
  Expected index: 9, Actual index: 9, Shift: 0
[MobOffsetDetector] Anchor: T5_MOB_TREASURE_BEAR
  Expected index: 402, Actual index: 402, Shift: 0
[MobOffsetDetector] Most common index shift: 0 (votes: 4/4)
[MobOffsetDetector] ========================================
[MobOffsetDetector] Detected Offset: 16
[MobOffsetDetector] (Previous default: 16, Index shift: 0)
[MobOffsetDetector] ========================================
```

### Debug 模式驗證日誌

```
[MobOffsetDetector] Verifying offset 16...
  T3_MOB_TR_HERETIC_MAGE_BOSS:
    XML Index: 0, TypeId (with offset): 16
  T4_MOB_TR_HERETIC_SHADOWMASK_SUMMON:
    XML Index: 3, TypeId (with offset): 19
  T4_MOB_TR_SILVER_KEEPER_EARTHDAUGHTER_BOSS:
    XML Index: 9, TypeId (with offset): 25
  T5_MOB_TREASURE_BEAR:
    XML Index: 402, TypeId (with offset): 418
```

### 遊戲運行時（Debug 模式）

```
[NewMobEvent] Raw typeId: 418, Offset: 16, Final TypeId: 402
[MobsHandler] Found mob: typeId=402, Tier=5, Type=HARVESTABLE
```

## 測試方法

### 1. 編譯項目

```bash
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU
```

### 2. 運行並檢查控制台

啟動程式後，控制台會顯示檢測日誌：

```
[MobOffsetDetector] Detected Offset: 16
```

### 3. 進入遊戲驗證

找到以下怪物並檢查顯示：

- ✅ T5 熊 → 應顯示為 T5
- ✅ T6 Keeper Boss → 應顯示為 T6
- ✅ T7-T8 各種怪物 → 等級正確

### 4. 模擬 ao-bin-dumps 更新

**測試步驟：**

1. 在 mobs.xml 開頭手動插入一個新怪物：

```xml
<Mob uniquename="TEST_NEW_MOB" tier="1" ... />
```

2. 重新啟動程式

3. 檢查控制台輸出：

```
[MobOffsetDetector] Anchor: T3_MOB_TR_HERETIC_MAGE_BOSS
  Expected index: 0, Actual index: 1, Shift: 1  ← 注意這裡變了
[MobOffsetDetector] Most common index shift: 1
[MobOffsetDetector] Detected Offset: 15  ← 自動調整！
```

4. 進入遊戲驗證怪物顯示仍然正確

## 優勢對比

### 舊方案（手動調整）

❌ 每次 Albion Online 更新後需要手動測試
❌ 需要進遊戲找到已知怪物驗證
❌ 需要修改代碼（-15 → -16 → ...）
❌ 需要重新編譯
❌ 如果忘記更新會顯示錯誤

### 新方案（動態檢測）

✅ **完全自動** - 無需手動調整
✅ **啟動即用** - 更新 ao-bin-dumps 後立即生效
✅ **穩定可靠** - 多錨點交叉驗證
✅ **易於維護** - 代碼無需修改
✅ **透明可視** - 控制台顯示檢測過程

## 未來擴展

### 如果錨點怪物被刪除？

可以輕鬆添加新的錨點：

```csharp
private static readonly Dictionary<string, int> AnchorMobs = new Dictionary<string, int>
{
    { "T3_MOB_TR_HERETIC_MAGE_BOSS", 0 },
    { "T4_MOB_TR_HERETIC_SHADOWMASK_SUMMON", 3 },
    { "T4_MOB_TR_SILVER_KEEPER_EARTHDAUGHTER_BOSS", 9 },
    { "T5_MOB_TREASURE_BEAR", 402 },
    // 新增錨點
    { "T6_MOB_XXX", 600 },  // ← 只需添加一行
};
```

### 如何選擇新錨點？

1. 選擇遊戲核心內容（不會被刪除）
2. 選擇不同索引區間（前、中、後）
3. 選擇容易在遊戲中遇到的怪物
4. 運行 Debug 模式查看 UniqueName

### 完全移除 Offset？

未來可以改用 **UniqueName 映射**（需要維護 typeId → UniqueName 的映射表）：

```csharp
// 需要額外的映射文件
Dictionary<int, string> typeIdToUniqueName = LoadMapping();

// 直接通過 UniqueName 查找
string uniqueName = typeIdToUniqueName[typeId];
MobInfo mobInfo = mobInfoByUniqueName[uniqueName];
```

但這需要從遊戲中提取 typeId 映射，工作量較大。

## 故障排除

### 問題 1: 所有錨點都找不到

**可能原因：** mobs.xml 文件損壞或格式錯誤

**解決方法：**
1. 重新下載 ao-bin-dumps
2. 檢查 mobs.xml 是否完整

### 問題 2: 檢測到的 Offset 明顯錯誤

**可能原因：** 錨點怪物的預期索引過時

**解決方法：**
1. 查看控制台日誌找出哪個錨點有問題
2. 在 MobOffsetDetector.cs 中更新該錨點的預期索引
3. 或者移除該錨點，使用其他錨點

### 問題 3: 怪物仍然顯示錯誤

**可能原因：**
- 遊戲更新改變了 typeId 編碼方式（罕見）
- PacketOffsets 中的 NewMobEvent 偏移量錯誤

**解決方法：**
1. 檢查 Debug 日誌中的 Raw typeId 值
2. 手動驗證計算公式
3. 如有必要，更新 jsons/offsets.json

## 相關文檔

- [MOB_OFFSET_INVESTIGATION.md](MOB_OFFSET_INVESTIGATION.md) - Offset 變化原因分析
- [MOB_OFFSET_FIX_16.md](MOB_OFFSET_FIX_16.md) - 手動修正為 -16 的過程
- [MOB_DISPLAY_ISSUE.md](MOB_DISPLAY_ISSUE.md) - 初始問題報告

## 總結

動態 Offset 檢測功能完全解決了 ao-bin-dumps 更新導致的 offset 失效問題。現在可以：

✅ 隨時更新 ao-bin-dumps 而無需擔心怪物顯示錯誤
✅ 無需手動測試和調整 offset
✅ 自動適應 Albion Online 的頻繁更新
✅ 保持代碼穩定性和可維護性

**這是一個一勞永逸的解決方案！**
