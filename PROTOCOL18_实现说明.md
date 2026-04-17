# Protocol 18 实现说明

## 概述

本文档描述了 VRise Albion 雷达的完整 Protocol 18 实现，基于 albiondata-client Go 实现 (https://github.com/ao-data/albiondata-client)。

## 架构

### 核心组件

1. **Protocol18Deserializer.cs** - 完整的 Protocol 18 类型反序列化器
   - 支持所有 30+ Protocol 18 类型代码
   - 处理 varint 编码、zig-zag 编码
   - 反序列化数组、字典、字节数组、基础类型

2. **Protocol18ReceiverBridge.cs** - Protocol 18 解析器与 Albion.Network 处理器之间的桥接
   - 使用 Protocol 18 解析原始 Photon 包
   - 暴露 Events、Requests、Responses 的回调
   - 完全绕过 Protocol 16 解析

3. **PacketDeviceSelector.cs** - 修改为使用 Protocol 18 桥接
   - 使用反射直接调用处理器方法
   - 使用 Protocol 18 数据创建 EventPacket/RequestPacket/ResponsePacket 对象
   - 在 photonReceiver 上调用 `ReceiveEvent()`、`ReceiveRequest()`、`ReceiveResponse()`

## 工作原理

```
UDP 数据包 (Npcap)
    ↓
PacketDeviceSelector.Device_OnPacketArrival()
    ↓
Protocol18ReceiverBridge.ReceivePacket()
    ↓
Protocol18Deserializer.DeserializeParameterTable()
    ↓
回调: OnEvent / OnRequest / OnResponse
    ↓
反射: 创建 EventPacket/RequestPacket/ResponsePacket
    ↓
调用 photonReceiver.ReceiveEvent/ReceiveRequest/ReceiveResponse()
    ↓
现有处理器处理数据
```

## 与 albiondata-client 的主要区别

| 功能 | albiondata-client (Go) | VRise (C#) |
|---------|------------------------|------------|
| 语言 | Go | C# .NET Framework 4.8 |
| 数据包流 | 回调 → 直接处理器 | 回调 → 反射 → ReceiverBuilder |
| 类型系统 | interface{} | object with Dictionary<byte, object> |
| 处理器注册 | 手动回调 | ReceiverBuilder 模式 |
| 集成 | 清晰的回调架构 | 反射绕过 Protocol 16 |

## 实现细节

### 1. Protocol18Deserializer.cs

albiondata-client 的 Protocol 18 反序列化器的完整 C# 移植:

**支持的类型代码:**
- 基础类型: Bool, Byte, Short, Int, Long, Float, Double
- 紧凑整数: Int1, Int2, Long1, Long2
- 特殊类型: Null, BoolTrue (27), BoolFalse (28)
- 零值: IntZero (30), LongZero (31), 等等
- 集合: Array, Dictionary, ByteArray
- String: UTF-8 编码，带长度前缀

**关键方法:**
- `DeserializeParameterTable(byte[] data)` - 主入口点
- `DeserializeValue(Stream stream, byte typeCode)` - 特定类型的反序列化
- `ReadCompressedInt32/Int64()` - Varint 与 zig-zag 编码

### 2. Protocol18ReceiverBridge.cs

**关键方法:**
- `ReceivePacket(byte[] payload)` - 主入口点
- `HandleCommand()` - 处理 Photon 命令
- `HandleSendReliable()` - 处理可靠消息
- `DispatchEvent/Request/Response()` - 反序列化并调用回调

**回调:**
```csharp
public Action<byte, Dictionary<byte, object>> OnEvent { get; set; }
public Action<byte, Dictionary<byte, object>> OnRequest { get; set; }
public Action<byte, short, string, Dictionary<byte, object>> OnResponse { get; set; }
public Action OnEncrypted { get; set; }
```

### 3. PacketDeviceSelector.cs

**基于反射的处理器调用:**

```csharp
// 使用反射创建 EventPacket
var assembly = typeof(IPhotonReceiver).Assembly;
var eventPacketType = assembly.GetType("Albion.Network.EventPacket");
var eventPacket = Activator.CreateInstance(eventPacketType, parameters);

// 调用 ReceiveEvent 方法
var receiveEventMethod = photonReceiver.GetType().GetMethod("ReceiveEvent",
    BindingFlags.NonPublic | BindingFlags.Instance);
receiveEventMethod.Invoke(photonReceiver, new[] { eventPacket });
```

这种方法:
- 使用 Protocol 18 数据创建 EventPacket/RequestPacket/ResponsePacket 对象
- 调用私有的 `ReceiveEvent()`/`ReceiveRequest()`/`ReceiveResponse()` 方法
- 允许现有处理器无需修改即可工作

## 测试

### 构建和测试

运行 `test-protocol18.bat` 来构建和准备测试:

```cmd
test-protocol18.bat
```

### 预期输出

当 Protocol 18 解析正常工作时，你应该看到:

```
[Protocol18Bridge] Event 29 with 15 parameters
[Protocol18Bridge] Request 5 with 3 parameters
[Protocol18Bridge] Response 8 (rc=0) with 7 parameters
```

如果处理器被成功调用，你还应该看到:

```
[DebugHandler] Event: 29
[NewCharacterEventHandler] Processing event...
```

### 故障排除

**没有控制台输出:**
- 检查网络适配器选择
- 验证 network_config.json 中的 game_port (5056 或 5050)
- 运行 Wireshark 确认数据包正在被捕获

**Protocol18Bridge 消息但没有处理器输出:**
- 反射可能未能找到方法
- 检查控制台中的 "Error invoking event handler" 消息
- 验证 Albion.Network 程序集结构没有变化

**构建错误:**
- 确保所有三个文件都在 DEATHEYE.csproj 中
- 检查 PhotonPackageParser 4.1.0 已安装
- 验证 .NET Framework 4.8 SDK 可用

## 修改的文件

1. **Radar/Packets/Photon/Protocol18Deserializer.cs** (新增)
   - 完整的 Protocol 18 类型反序列化器

2. **Radar/Packets/Photon/Protocol18ReceiverBridge.cs** (新增)
   - 带回调和数据包解析的桥接

3. **Radar/Packets/Sniffer/PacketDeviceSelector.cs** (修改)
   - 移除了 PhotonParser (Protocol 16)
   - 添加了 Protocol18ReceiverBridge，带基于反射的处理器调用

4. **Radar/Init.cs** (修改)
   - 添加了确认 Protocol 18 已启用的控制台消息

5. **DEATHEYE.csproj** (修改)
   - 添加了 Protocol18Deserializer.cs
   - 添加了 Protocol18ReceiverBridge.cs

6. **test-protocol18.bat** (新增)
   - 快速构建和测试脚本

## 下一步

如果当前实现不工作（处理器未被调用）:

### 选项 A: 直接处理器注册表

不使用反射，而是创建直接注册表:

```csharp
public class Protocol18HandlerRegistry
{
    private Dictionary<int, Action<Dictionary<byte, object>>> eventHandlers;

    public void RegisterEventHandler(int eventCode, Action<Dictionary<byte, object>> handler)
    {
        eventHandlers[eventCode] = handler;
    }

    public void DispatchEvent(int eventCode, Dictionary<byte, object> parameters)
    {
        if (eventHandlers.TryGetValue(eventCode, out var handler))
        {
            handler(parameters);
        }
    }
}
```

### 选项 B: 完全绕过 Albion.Network

实现直接接受 Protocol 18 数据的处理器:

```csharp
public interface IProtocol18EventHandler
{
    void OnEvent(byte eventCode, Dictionary<byte, object> parameters);
}
```

### 选项 C: 创建处理器的 Protocol 18 版本

复制现有处理器并修改它们以直接使用 Protocol 18 数据。

## 参考资料

- **albiondata-client**: https://github.com/ao-data/albiondata-client
- **Protocol 18 parser (Go)**: https://github.com/ao-data/albiondata-client/blob/master/client/photon/parser.go
- **Protocol 18 deserializer (Go)**: https://github.com/ao-data/albiondata-client/blob/master/client/photon/deserializer.go
- **Photon 协议规范**: 专有 - 逆向工程

## 版本历史

- **2026-04-14**: 初始 Protocol 18 实现
  - Protocol18Deserializer.cs 完成
  - Protocol18ReceiverBridge.cs 带回调
  - 基于反射的处理器调用
  - 准备测试

## 注意事项

- 此实现保持与现有处理器的兼容性
- 事件处理器代码无需更改
- 所有 Protocol 16 依赖项都已从数据包捕获路径中移除
- Protocol 18 解析在数据到达 ReceiverBuilder 之前发生
