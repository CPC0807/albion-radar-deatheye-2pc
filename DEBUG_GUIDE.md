# 调试指南 - 处理游戏更新后的封包解析问题

## 问题背景

Albion Online 在 2026-04-13 发布了更新，导致封包结构发生变化，出现 `Protocol16.dll ArgumentException` 错误。

## 已实施的调试功能

### 1. 详细错误日志 (PacketDeviceSelector.cs)

现在会捕获并显示所有封包解析错误的详细信息：

```
[PacketError] ArgumentException in Protocol16:
  Message: (错误描述)
  ParamName: (参数名)
  InnerException: (内部错误)
  StackTrace: (调用堆栈)
```

### 2. 事件代码记录 (DebugHandler)

已启用 DebugHandler，会：
- 在控制台显示新发现的事件代码
- 将详细的封包结构写入 `event_structures.txt`

## 使用步骤

### Step 1: 编译程序

```bash
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU
```

或者使用 Release：
```bash
msbuild DEATHEYE.sln /p:Configuration=Release /p:Platform=AnyCPU
```

### Step 2: 运行程序并收集日志

1. 启动编译好的程序
2. 打开 Albion Online 客户端并进入游戏
3. 观察控制台输出：

**成功解析的事件：**
```
[DebugHandler] New Event Code: 29 - Logged to event_structures.txt
[DebugHandler] New Event Code: 123 - Logged to event_structures.txt
```

**解析失败的事件：**
```
[PacketError] ArgumentException in Protocol16:
  Message: Parameter 'key' with value '5' is out of range
  ParamName: key
```

### Step 3: 分析日志

#### 检查 event_structures.txt

该文件包含所有成功解析的事件的详细结构：

```
[EventStructure] Event 29:
  Key 0: Int32 = 123456
  Key 1: String = PlayerName
  Key 2: String = GuildName
  Key 5: byte[8] = 1A-2B-3C-4D-5E-6F-7A-8B
```

#### 对比 jsons/indexes.json

检查哪些事件 ID 可能已改变：

**当前映射：**
```json
{
  "NewCharacter": 29,
  "NewMobEvent": 123,
  "KeySync": 593
}
```

**如果日志显示新的 ID：**
- Event 29 仍然出现 → ID 没变
- Event 123 不再出现，但出现 Event 125 → NewMobEvent ID 变了

### Step 4: 修复配置文件

#### 方法 A: 等待社区更新

检查这些仓库是否已更新：
- https://github.com/ao-data/ao-bin-dumps
- https://github.com/Zeldruck/Albion-Online-ZQRadar

#### 方法 B: 手动修复

1. **更新 indexes.json**
   - 如果某个功能不工作（例如看不到玩家），找到对应的事件
   - 在 event_structures.txt 中找到可能的新 ID
   - 更新 jsons/indexes.json

2. **更新 offsets.json**
   - 如果数据显示错误（位置不对、装备错误等）
   - 查看 event_structures.txt 中的参数键值
   - 调整 jsons/offsets.json 中的字节偏移

3. **更新 ao-bin-dumps/**
   - 从 https://github.com/ao-data/ao-bin-dumps 下载最新版
   - 替换 `items.xml`, `mobs.xml`, `harvestables.xml`

## 常见问题诊断

### 看不到玩家

可能原因：
- `NewCharacter` event ID 变了（当前：29）
- `Move` event ID 变了（当前：3）
- `KeySync` event ID 变了（当前：593）

### 看不到怪物

可能原因：
- `NewMobEvent` event ID 变了（当前：123）
- `MobChangeState` event ID 变了（当前：47）

### 看不到资源

可能原因：
- `NewHarvestableObject` event ID 变了（当前：40）
- `NewHarvestableList` event ID 变了（当前：39）

### 位置显示 (0.00, 0.00)

可能原因：
- `KeySync` event ID 变了
- XorCode 解密失败

解决方法：启用 KeySync 暴力破解（在 Init.cs 第 131-137 行）

## 清理调试代码

问题解决后，可以禁用调试功能：

**Radar/Init.cs (第 119 行):**
```csharp
// 註釋掉這行
// builder.AddHandler(new DebugHandler());
```

或者改回原来的条件编译：
```csharp
#if DEBUG
builder.AddHandler(new DebugHandler());
#endif
```

## 需要帮助？

1. 将 event_structures.txt 内容分享到社区
2. 将控制台错误信息截图
3. 说明哪些功能不工作（玩家/怪物/资源）
