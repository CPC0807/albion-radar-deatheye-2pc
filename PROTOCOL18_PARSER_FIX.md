# Protocol 18 Parser Fix

## 问题描述 (Problem Description)

### 症状 (Symptoms)
应用程序运行时出现大量 "Type code not implemented" 错误：

```
[PacketError] ArgumentException in Protocol18:
  Message: Type code: 217 not implemented.
  at Protocol16.Protocol16Deserializer.Deserialize(Protocol16Stream input, Byte typeCode)

Type code: 74 not implemented
Type code: 8 not implemented
Type code: 26 not implemented
Type code: 16 not implemented
Type code: 17 not implemented
... (many more type codes)
```

### 根本原因 (Root Cause)

Albion Online 游戏服务器现在使用 **Photon Protocol 18** 发送网络数据包，但项目依赖的 `Albion.Network` 库内部使用的是 **Protocol 16 反序列化器**。

Protocol 18 引入了新的类型代码 (type codes)，例如：
- Type 217 (0xD9) - Protocol 18 特有类型
- Type 74 (0x4A) - Protocol 18 特有类型
- Type 8 - Null type in Protocol 18
- Type 9 - Compressed Int
- Type 10 - Compressed Long
- Type 11-18 - Various optimized integer types

Protocol 16 的反序列化器无法识别这些新类型代码，导致抛出 `ArgumentException`。

## 解决方案 (Solution)

### 1. 使用自定义 Protocol 18 解析器

项目已经包含了完整的 Protocol 18 实现：
- `Radar/Packets/Photon/PhotonParser.cs` - Protocol 18 packet parser
- `Radar/Packets/Photon/Protocol18Deserializer.cs` - Protocol 18 deserializer

但之前的代码直接调用 `photonReceiver.ReceivePacket()`，这会路由到 `Albion.Network` 库的 Protocol 16 解析器。

### 2. 修改 PacketDeviceSelector.cs

**修改前 (Before):**
```csharp
private void Device_OnPacketArrival(object sender, PacketCapture e)
{
    var packet = Packet.ParsePacket(...).Extract<UdpPacket>();

    if (packet != null && packet.PayloadData != null)
    {
        // ❌ 错误：直接调用 Protocol 16 解析器
        photonReceiver.ReceivePacket(packet.PayloadData);
    }
}
```

**修改后 (After):**
```csharp
private PhotonParser photonParser;

public PacketDeviceSelector(IPhotonReceiver photonReceiver, int gamePort = 5050)
{
    this.photonReceiver = photonReceiver;
    this.gamePort = gamePort;
    this.photonParser = new PhotonParser(photonReceiver);  // ✅ 初始化 Protocol 18 解析器

    Console.WriteLine("[PacketDeviceSelector] Using Protocol 18 PhotonParser");
}

private void Device_OnPacketArrival(object sender, PacketCapture e)
{
    var packet = Packet.ParsePacket(...).Extract<UdpPacket>();

    if (packet != null && packet.PayloadData != null)
    {
        // ✅ 正确：使用我们的 Protocol 18 解析器
        photonParser.ReceivePacket(packet.PayloadData);
    }
}
```

### 3. Protocol 18 数据流 (Data Flow)

```
Network Packet (UDP)
    ↓
PacketDeviceSelector.Device_OnPacketArrival()
    ↓
PhotonParser.ReceivePacket(payload)  ← Protocol 18 解析
    ↓
PhotonParser.HandleCommand()  ← 处理 fragments, reliable, unreliable
    ↓
PhotonParser.HandleSendReliable()
    ↓
Protocol18Deserializer.DeserializeParameterTable()  ← 使用 Protocol 18 类型代码
    ↓
PhotonParser.ReconstructPacketForReceiver()  ← 重构为兼容格式
    ↓
photonReceiver.ReceivePacket()  ← 传递给 Albion.Network handlers
    ↓
Event/Request/Response Handlers
```

## Protocol 18 vs Protocol 16 类型代码对比

| Type Code | Protocol 16 | Protocol 18 | 说明 |
|-----------|-------------|-------------|------|
| 0 | Unknown | Unknown | 未知类型 |
| 2 | Boolean | Boolean | 布尔值 |
| 3 | Byte | Byte | 字节 |
| 4 | Short | Short | 短整型 |
| 5 | - | Float | 浮点数 |
| 6 | - | Double | 双精度浮点数 |
| 7 | - | String | 字符串 |
| 8 | - | **Null** | **P18 新增：空值** |
| 9 | - | **Compressed Int** | **P18 新增：压缩整型** |
| 10 | - | **Compressed Long** | **P18 新增：压缩长整型** |
| 11-18 | - | **Optimized Ints/Longs** | **P18 新增：优化的整型编码** |
| 19 | - | Custom | 自定义类型 |
| 20 | - | Dictionary | 字典 |
| 21 | - | Hashtable | 哈希表 |
| 23 | - | Object Array | 对象数组 |
| 27-34 | - | **Zero Values** | **P18 新增：零值优化** |
| 0x40 | - | Typed Array | 类型化数组 |
| 0x80+ | - | **Slim Custom** | **P18 新增：精简自定义类型** |
| 74 (0x4A) | - | **P18 Special** | **P18 特有类型** |
| 217 (0xD9) | - | **P18 Special** | **P18 特有类型** |

## Protocol 18 新特性

### 1. 压缩整型 (Compressed Integers)
使用 zig-zag 编码和变长编码 (varint) 来减少小整数的传输字节数。

```csharp
// ReadCompressedInt32 - zig-zag decode
int ReadCompressedInt32(Stream stream)
{
    uint value = ReadCount(stream);  // varint
    return (int)((value >> 1) ^ (-(int)(value & 1)));  // zig-zag
}
```

### 2. 零值优化 (Zero Value Optimization)
为常用的零值类型提供单字节编码：
- Type 27: BoolFalse
- Type 28: BoolTrue
- Type 29: ShortZero
- Type 30: IntZero
- Type 31: LongZero
- Type 32: FloatZero
- Type 33: DoubleZero
- Type 34: ByteZero

### 3. 精简自定义类型 (Slim Custom Types)
类型代码 >= 0x80 表示精简自定义类型，customId 直接编码在类型字节中：
```csharp
if (typeCode >= 0x80)
{
    customId = (byte)(typeCode & 0x7F);  // 低 7 位是 customId
}
```

### 4. 优化整型编码 (Optimized Integer Encoding)
- Type 11: Int1 (1-byte unsigned int)
- Type 12: Int1Neg (1-byte unsigned int, negated)
- Type 13: Int2 (2-byte unsigned int)
- Type 14: Int2Neg (2-byte unsigned int, negated)
- Type 15-18: 对应的 Long 版本

## 测试验证 (Testing)

### 预期行为
修复后应该**不再看到** "Type code not implemented" 错误。

### 测试步骤
1. 启动 Albion Online 游戏
2. 运行 `bin\Debug\VRise.exe`
3. 选择网络适配器
4. 观察控制台输出：

**✅ 正确输出 (Correct Output):**
```
[PacketDeviceSelector] Using Protocol 18 PhotonParser
[DebugHandler] OnHandleAsync called - packet type: EventPacket
[DebugHandler] Event: 29
[DebugHandler] Event: 46
[DebugHandler] Event: 593  ← KeySync event
```

**❌ 错误输出 (Wrong Output - 修复前):**
```
Type code: 217 not implemented
Type code: 74 not implemented
ArgumentException in Protocol18
```

### 功能测试
1. **地图切换**: 在游戏中切换地图区域
   - 应能正确显示新地图名称
   - 雷达应重置并显示新区域的实体

2. **资源显示**: 资源节点应该显示在雷达上
   - 树木、矿石、纤维等
   - 带有正确的等级和附魔信息

3. **怪物显示**: 怪物应该显示在雷达上
   - 普通怪物、精英怪物
   - 迷雾怪物（蜘蛛、龙、狮鹫等）

4. **玩家追踪**: 其他玩家应该显示
   - 正确的位置
   - 装备信息（如果启用）

## 技术细节

### PhotonParser 工作原理

1. **接收原始 UDP payload**:
   ```csharp
   public bool ReceivePacket(byte[] payload)
   {
       // 跳过 peerId (2 bytes)
       byte flags = payload[2];
       int commandCount = payload[3];

       for (int i = 0; i < commandCount; i++)
       {
           HandleCommand(payload, offset);
       }
   }
   ```

2. **处理 Photon 命令**:
   - CmdSendReliable (6) - 可靠消息
   - CmdSendUnreliable (7) - 不可靠消息
   - CmdSendFragment (8) - 分片消息
   - CmdDisconnect (4) - 断开连接

3. **解析消息类型**:
   - MsgRequest (2) - 客户端请求
   - MsgResponse (3) - 服务器响应
   - MsgEvent (4) - 服务器事件
   - MsgEncrypted (131) - 加密消息

4. **Protocol 18 反序列化**:
   ```csharp
   var parameters = Protocol18Deserializer.DeserializeParameterTable(data);
   ```

5. **重构为兼容格式**:
   ```csharp
   byte[] reconstructedPacket = ReconstructPacketForReceiver(msgType, data);
   photonReceiver.ReceivePacket(reconstructedPacket);
   ```

### 为什么需要重构？

`PhotonParser` 使用 Protocol 18 解析原始数据，但项目的事件处理器（handlers）期望 Albion.Network 的数据格式。`ReconstructPacketForReceiver()` 方法将 Protocol 18 解析的参数重新打包成 Albion.Network 兼容的二进制格式。

这种方法的优点：
- ✅ 使用 Protocol 18 正确解析游戏数据包
- ✅ 保持与现有 handler 代码的兼容性
- ✅ 不需要重写所有 event/request/response handlers
- ✅ 支持所有 Protocol 18 特性（压缩、优化编码等）

## 相关文件

### 核心文件
- `Radar/Packets/Sniffer/PacketDeviceSelector.cs` - **已修改**：使用 PhotonParser
- `Radar/Packets/Photon/PhotonParser.cs` - Protocol 18 解析器
- `Radar/Packets/Photon/Protocol18Deserializer.cs` - Protocol 18 反序列化
- `Radar/Packets/Photon/PhotonProtocolHelper.cs` - 辅助函数（鲁棒性修复）

### 配套文档
- `PROTOCOL18_ROBUSTNESS_FIXES.md` - Protocol 18 鲁棒性修复详解
- `PACKET_HANDLING_FIX.md` - EventPacket 构造函数错误修复
- `COMPILE_FIX_NET48.md` - .NET Framework 4.8 兼容性修复
- `QUICK_FIX_SUMMARY.md` - 所有修复的快速参考

## 故障排除

### 如果仍然看到 "Type code not implemented" 错误

1. **检查构建是否成功**:
   ```bash
   ls -lh bin/Debug/VRise.exe
   ```
   应该显示最新的时间戳。

2. **检查控制台启动消息**:
   ```
   [PacketDeviceSelector] Using Protocol 18 PhotonParser
   ```
   如果看到这条消息，说明修复已应用。

3. **检查是否使用了旧的 exe**:
   确保运行的是 `bin\Debug\VRise.exe` 而不是其他位置的旧版本。

4. **清理并重新构建**:
   ```bash
   cmd /c rebuild-quick.bat
   ```

### 如果雷达仍然不显示实体

Protocol 18 修复只解决了**数据包解析错误**。如果雷达不显示实体，可能是其他问题：

1. **KeySync 未触发**: 玩家位置解密需要 KeySync 事件
   - 在游戏中切换地图区域来触发 KeySync
   - 查找控制台消息: `[KeySync] XorCode received!`

2. **Packet offsets 过期**: `jsons/offsets.json` 可能不匹配当前游戏版本
   - 参考 albiondata-client 的最新 offsets

3. **Packet indexes 过期**: `jsons/indexes.json` 可能不匹配当前游戏版本
   - 某些事件代码可能已更改（参考 `NEW_EVENT_CODES.md`）

## 总结

此修复通过使用自定义的 Protocol 18 解析器替换了 Albion.Network 库的 Protocol 16 解析器，解决了大量 "Type code not implemented" 错误。

**关键变更**:
- 在 `PacketDeviceSelector` 构造函数中初始化 `PhotonParser`
- 使用 `photonParser.ReceivePacket()` 代替 `photonReceiver.ReceivePacket()`
- Protocol 18 解析器正确处理所有新类型代码
- 重构后的数据包保持与现有 handlers 的兼容性

**修复日期**: 2026-04-16
**相关 Issue**: Protocol 16/18 type code mismatch
**测试状态**: 待用户验证

---

*参考: albiondata-client commits from 2026-04-13/14 with "Updates photon protocol18" changes*
