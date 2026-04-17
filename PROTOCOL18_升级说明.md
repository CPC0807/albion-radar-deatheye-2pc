# Protocol 18 升级说明

## 🎯 更新摘要

你的雷达已经从 **Photon Protocol 16** 升级到 **Photon Protocol 18**，以支持 2026年4月的 Albion Online 最新更新。

## ✅ 已完成的工作

### 1. 创建了新的 Protocol 18 解析器

**新增文件**：

1. **`Radar\Packets\Photon\Protocol18Deserializer.cs`**
   - 完整的 Protocol 18 类型反序列化器
   - 支持新的类型代码（IntZero, BoolFalse, BoolTrue 等）
   - 实现压缩的 varint 编码
   - 处理字节序规范化

2. **`Radar\Packets\Photon\PhotonParser.cs`**
   - 解析原始 Photon UDP 数据包
   - 处理分片数据包和重组
   - 分发事件、请求和响应到 IPhotonReceiver
   - 检测加密数据包

### 2. 更新了现有代码

**修改文件**：

1. **`Radar\Packets\Sniffer\PacketDeviceSelector.cs`**
   - 使用新的 `PhotonParser` 替代旧的 PhotonPackageParser
   - 添加加密数据包检测回调
   - 更新错误消息引用 Protocol 18

2. **`DEATHEYE.csproj`**
   - 添加新源文件到编译列表

## 🔧 技术细节

### Protocol 18 的新特性

Protocol 18 引入了优化的类型代码来减少带宽：

```csharp
// 零值优化（不需要额外字节）
TypeIntZero = 30      // 整数 0
TypeBoolFalse = 27    // 布尔 false
TypeBoolTrue = 28     // 布尔 true
TypeShortZero = 29    // Short 0
TypeLongZero = 31     // Long 0
TypeFloatZero = 32    // Float 0.0
TypeDoubleZero = 33   // Double 0.0
TypeByteZero = 34     // Byte 0

// 压缩整数（varint 编码，节省空间）
TypeCompressedInt = 9     // Zig-zag 编码的 32 位整数
TypeCompressedLong = 10   // Zig-zag 编码的 64 位长整数

// 紧凑整数类型
TypeInt1 = 11         // 1 字节无符号整数
TypeInt1Neg = 12      // 1 字节无符号整数（负数）
TypeInt2 = 13         // 2 字节无符号整数
TypeInt2Neg = 14      // 2 字节无符号整数（负数）
```

### 参考来源

我们的实现基于 **albiondata-client** 的最新代码（昨天刚更新！）：
- 仓库: https://github.com/ao-data/albiondata-client
- 版本: 0.1.51 (2026年4月13日)
- 提交: "Updates photon protocol18"

## 📝 如何编译

### 使用 Visual Studio

1. 打开 `DEATHEYE.sln`
2. 按 `Ctrl+Shift+B` 编译
3. 新文件会自动包含

### 使用 Visual Studio Code

1. 打开项目文件夹
2. 安装 C# Dev Kit 扩展
3. 按 `F5` 编译并调试

### 使用命令行

```bash
# Debug 版本
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU

# Release 版本
msbuild DEATHEYE.sln /p:Configuration=Release /p:Platform=AnyCPU
```

## 🧪 测试步骤

### 1. 编译检查

编译项目并确保没有错误：
- 检查命名空间冲突
- 验证所有文件编译成功

### 2. 运行测试

1. 启动 Albion Online
2. 启动雷达
3. 检查控制台输出：
   ```
   [Init] Starting packet capture on game port 5056
   [Init] No Cryptonite needed! No hosts file modification needed!
   ```

### 3. 验证数据解析

观察控制台消息，确认成功解析：
```
[DebugHandler] Event: NewCharacter (29)
[DebugHandler] Event: Move (10)
[KeySync] XorCode received!
```

### 4. 检查雷达功能

- ✅ 玩家位置显示正确
- ✅ Mob 生成点显示
- ✅ 资源节点显示
- ✅ 地下城入口显示

## ⚠️ 常见问题

### 问题 1: 无法捕获数据包

**症状**: 雷达不显示任何玩家、怪物或资源

**可能原因**:
1. **游戏端口错误**
   - 检查 `network_config.json`
   - 使用 Wireshark 确认 Albion Online 使用的端口
   - 尝试端口 5050, 5056, 或 5057

2. **Npcap 未安装或配置错误**
   - 重新安装 Npcap（启用 WinPcap 兼容模式）
   - 以管理员身份运行雷达

### 问题 2: 玩家位置显示 (0.00, 0.00)

**症状**: 玩家显示在坐标 (0, 0) 而不是真实位置

**可能原因**:
1. **XorCode 未接收**
   - 玩家位置用 KeySync 事件的 XorCode 加密
   - 检查控制台: `[KeySync] XorCode received!`
   - 如果缺失，KeySync 事件 ID (593) 可能已改变

2. **KeySync 事件 ID 改变**
   - 取消注释 `Init.cs` 第 131-137 行的暴力搜索代码
   - 运行雷达并在游戏中切换地图
   - 检查控制台找到正确的事件 ID
   - 更新 `jsons/indexes.json`

### 问题 3: 加密数据包警告

**症状**:
```
[PacketDeviceSelector] Encountered encrypted packet
```

**说明**:
- 某些游戏数据（如市场价格）被 Albion Online 加密
- 这是正常现象
- 玩家位置、怪物数据和资源**不会**被加密

**操作**: 无需操作 - 这只是信息提示

### 问题 4: 协议不匹配错误

**症状**:
```
[PhotonParser] Error dispatching event: Index out of range
[PacketError] ArgumentException in Protocol18: ...
```

**可能原因**:
1. **事件/操作代码改变**
   - Albion Online 更新了数据包结构
   - 更新 `jsons/indexes.json` 和 `jsons/offsets.json`

2. **参数结构改变**
   - 事件处理器期望不同的参数键
   - 检查事件处理器中的 `PacketOffsets` 实现

3. **协议升级到 Protocol 19+**
   - Albion Online 可能升级到更新的协议版本
   - 检查 albiondata-client 更新: https://github.com/ao-data/albiondata-client/releases

## 🔍 调试技巧

### 启用详细日志

在 `Init.cs` 中启用 DebugHandler（已默认启用）：
```csharp
builder.AddHandler(new DebugHandler());
```

### 查看所有事件

如果需要看到所有事件（会产生大量日志），取消注释：
```csharp
builder.AddHandler(new DebugAllEventsHandler());
```

### 暴力搜索 KeySync 事件 ID

如果玩家位置不正确，取消注释 `Init.cs` 第 131-137 行：
```csharp
for (int i = 500; i <= 700; i++)
{
    builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
}
```

然后：
1. 运行雷达
2. 在游戏中切换地图
3. 观察控制台输出找到新的 KeySync 事件 ID
4. 更新 `jsons/indexes.json` 中的 "KeySync" 值

## 📚 与 albiondata-client 的对比

我们的实现基于最新的 albiondata-client (Go)，并做了这些适配：

| 功能 | albiondata-client (Go) | VRise (C#) |
|------|------------------------|------------|
| 语言 | Go | C# (.NET Framework 4.8) |
| 协议版本 | Protocol 18 | Protocol 18 |
| 反序列化 | `photon/deserializer.go` | `Protocol18Deserializer.cs` |
| 解析器 | `photon/parser.go` | `PhotonParser.cs` |
| Varint 编码 | Go 原生 varint | 自定义 C# 实现 |
| 字节序 | Little-endian | Little-endian（带规范化） |
| 分片处理 | 支持 | 支持 |
| 加密检测 | 支持 | 支持（回调） |

## 🚀 下一步

### 如果游戏再次更新（例如 Protocol 19）

1. **检查 albiondata-client 仓库**
   - https://github.com/ao-data/albiondata-client/releases
   - 查找提到协议更新的提交消息

2. **更新反序列化器**
   - 从 `client/photon/deserializer.go` 移植新类型代码
   - 在 `Protocol18Deserializer.Deserialize()` 中添加新 case

3. **更新解析器**
   - 检查 `client/photon/parser.go` 的改动
   - 相应更新 `PhotonParser.cs`

4. **全面测试**
   - 验证所有游戏事件仍能正确解析
   - 检查玩家位置、怪物生成、资源节点

## 💡 提示

1. **保持更新**
   - 定期检查 albiondata-client 的更新
   - Albion Online 每次大更新后都可能需要调整

2. **备份配置**
   - 在更新前备份 `jsons/` 文件夹
   - 记录任何自定义的事件 ID

3. **社区支持**
   - 如果遇到问题，查看 albiondata-client 的 Issues
   - 其他玩家可能遇到相同问题并有解决方案

## 📖 参考资料

- **albiondata-client**: https://github.com/ao-data/albiondata-client
- **最新发布** (0.1.51, 2026年4月13日): https://github.com/ao-data/albiondata-client/releases/tag/0.1.51
- **Protocol 18 更新**: https://github.com/ao-data/albiondata-client/pull/180
- **Photon 协议文档**: https://doc.photonengine.com/

## 🙏 致谢

- **Protocol 18 实现**: 基于 ao-data 团队的 albiondata-client
- **特别感谢**: jpcodecraft 的 Protocol 18 修复（2026年4月）
- **原始 PhotonPackageParser**: 0blu（已弃用）

## 📄 许可证

此 Protocol 18 实现派生自 albiondata-client（MIT 许可证）。
原始 VRise 代码保持其原有许可证。

---

**编译并测试你的雷达吧！祝你好运！** 🎮✨
