# 🛡️ 裝備顯示錯誤修復指南

## 問題描述
偵測到敵人裝備時，裝備顯示錯誤。

## 🗂️ 相關文件清單

### 1. 裝備數據對照表（最重要！）

**主要對照表**：
```
ao-bin-dumps/items.xml          (9.8 MB) - XML 格式裝備數據
ao-bin-dumps/items.json         (17 MB)  - JSON 格式裝備數據
```

**其他參考資料**：
```
ao-bin-dumps/legendaryitems.xml
ao-bin-dumps/legendaryitems.json
ao-bin-dumps/itemroles.xml
ao-bin-dumps/itemroles.json
ao-bin-dumps/formatted/items.json
```

**這些文件來自**：https://github.com/ao-data/ao-bin-dumps

### 2. 裝備加載和解析代碼

**主要文件**：

#### A. 裝備數據加載器
```
Radar/Dependencies/Item/ItemModel.cs
```
- 負責從 `items.xml` 讀取裝備數據
- 解析裝備名稱、ID、ItemPower
- 處理附魔等級（@1, @2, @3）

#### B. 玩家裝備數據結構
```
ResponseObj/PlayerItems.cs
```
- 定義裝備數據結構：
  - `Id` - 內部 ID
  - `Name` - 裝備唯一名稱（如 "T8_HEAD_PLATE_HELL@3"）
  - `Itempower` - 裝備力量

#### C. 玩家處理器（裝備匹配邏輯）
```
Radar/GameObjects/Players/PlayersHandler.cs
```
- `LoadEquipment(int[] values)` 方法（第 322-347 行）
- 根據遊戲傳來的裝備 ID 陣列匹配對應裝備
- 如果找不到匹配，顯示為 "T1_TRASH"

### 3. 裝備封包處理

#### D. 新角色事件
```
Radar/Packets/Handlers/NewCharacterEvent.cs
Radar/Packets/Handlers/NewCharacterEventHandler.cs
```
- 處理新玩家進入視野時的裝備數據

#### E. 裝備變更事件
```
Radar/Packets/Handlers/CharacterEquipmentChangedEvent.cs
Radar/Packets/Handlers/CharacterEquipmentChangedEventHandler.cs
```
- 處理玩家更換裝備時的事件

### 4. 裝備顯示

#### F. 裝備繪製器
```
Radar/Drawing/Drawers/ItemsDrawerer.cs
```
- 負責在 overlay 上繪製裝備圖標
- 從 `ITEMS/` 目錄加載裝備圖片

#### G. 裝備頁面設定
```
Design/Pages/ItemsPage.xaml.cs
```
- 裝備顯示的 UI 設定頁面

### 5. 初始化
```
Radar/Init.cs
```
- 在啟動時加載裝備數據：
  ```csharp
  List<PlayerItems> itemsList = ItemData.Load("ao-bin-dumps/items.xml");
  ```

## 🔍 問題診斷步驟

### 步驟 1：確認裝備數據是否最新

1. 檢查 `ao-bin-dumps/items.xml` 的日期
2. 如果過舊，從 GitHub 更新：
   ```bash
   # 從 https://github.com/ao-data/ao-bin-dumps 下載最新版本
   # 替換 ao-bin-dumps/items.xml
   ```

### 步驟 2：檢查裝備 ID 映射

**問題可能**：遊戲發送的裝備 ID 和 `items.xml` 中的順序不匹配。

**檢查方法**：
1. 打開 `Radar/Dependencies/Item/ItemModel.cs`
2. 查看第 18-26 行的 ID 分配邏輯：
   ```csharp
   var id = 1;  // 從 1 開始計數

   foreach (XmlNode item in applicableNodes)
   {
       var playerItem = new PlayerItems
       {
           Id = id++,  // 順序分配 ID
           Name = item.Attributes["uniquename"].Value,
           Itempower = ...
       };
   }
   ```

**問題**：
- 如果 Albion Online 改變了裝備 ID 的編碼方式
- `Id` 的順序分配可能不再正確匹配遊戲內的 ID

### 步驟 3：添加調試輸出

在 `PlayersHandler.cs` 的 `LoadEquipment` 方法中添加調試：

```csharp
private Equipment LoadEquipment(int[] values)
{
    Array.Resize(ref values, 8);
    Equipment equipment = new Equipment();

    #if DEBUG
    Console.WriteLine($"[LoadEquipment] Received IDs: {string.Join(", ", values)}");
    #endif

    for (int i = 0; i < values.Length; i++)
    {
        if (itemsList.Exists(x => x.Id == values[i]))
        {
            var foundItem = itemsList.Find(x => x.Id == values[i]);
            #if DEBUG
            Console.WriteLine($"  Slot {i}: ID {values[i]} -> {foundItem.Name} (IP: {foundItem.Itempower})");
            #endif
            equipment.Items.Add(foundItem);
        }
        else if (values[i] == 0 || values[i] == -1)
        {
            #if DEBUG
            Console.WriteLine($"  Slot {i}: ID {values[i]} -> NULL");
            #endif
            equipment.Items.Add(new PlayerItems() { Id = 0, Itempower = 0, Name = "NULL" });
        }
        else
        {
            #if DEBUG
            Console.WriteLine($"  Slot {i}: ID {values[i]} -> NOT FOUND (showing T1_TRASH)");
            #endif
            equipment.Items.Add(new PlayerItems() { Id = 0, Itempower = 0, Name = "T1_TRASH" });
        }
    }

    equipment.AllItemPower = GetItemPower(equipment.Items);
    return equipment.Items.All(x => x.Name == "T1_TRASH" || x.Name == "NULL") || equipment.AllItemPower == 0 ? null : equipment;
}
```

## 🔧 可能的修復方案

### 方案 1：更新裝備數據庫

1. 從 https://github.com/ao-data/ao-bin-dumps 下載最新的 `items.xml`
2. 替換 `ao-bin-dumps/items.xml`
3. 同時也替換 `bin/Debug/ao-bin-dumps/items.xml`
4. 重新編譯並測試

### 方案 2：修改 ID 映射方式

如果遊戲現在使用不同的裝備 ID 系統，你可能需要：

1. **使用裝備名稱而不是順序 ID**
   - 修改數據結構使用裝備唯一名稱作為 key
   - 從封包中提取裝備名稱而不是 ID

2. **使用 ao-bin-dumps 的 JSON 格式**
   - JSON 格式可能包含更直接的 ID 映射
   - 修改 `ItemModel.cs` 改為讀取 `items.json`

### 方案 3：檢查封包解析

如果裝備 ID 本身就錯誤，問題可能在封包解析：

1. 檢查 `NewCharacterEvent.cs` 的裝備提取邏輯
2. 檢查 `jsons/offsets.json` 中的 `NewCharacter` 偏移值
3. 可能需要調整裝備數據在封包中的位置

## 📝 修改建議

### 你應該檢查和可能修改的文件（按優先級）：

1. **ao-bin-dumps/items.xml** - 更新到最新版本
2. **Radar/Dependencies/Item/ItemModel.cs** - 修改 ID 分配邏輯
3. **Radar/GameObjects/Players/PlayersHandler.cs** - 添加調試輸出
4. **Radar/Packets/Handlers/NewCharacterEvent.cs** - 檢查裝備提取邏輯
5. **jsons/offsets.json** - 檢查 NewCharacter 的偏移值

## 🎯 快速測試

添加調試輸出後，重新編譯並觀察：

```
[LoadEquipment] Received IDs: 12345, 67890, 11223, 0, 0, 0, 0, 0
  Slot 0: ID 12345 -> T8_HEAD_PLATE_HELL@3 (IP: 1400)
  Slot 1: ID 67890 -> NOT FOUND (showing T1_TRASH)  ← 問題在這裡！
```

如果看到很多 "NOT FOUND"，說明 ID 映射不正確。

---

**建議**：先更新 `items.xml` 到最新版本，如果還是不行，添加調試輸出找出實際收到的 ID。
