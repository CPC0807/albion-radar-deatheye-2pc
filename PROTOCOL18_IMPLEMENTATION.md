# Protocol 18 Implementation Summary

## Overview

This document describes the complete Protocol 18 implementation for VRise Albion Radar, based on the albiondata-client Go implementation (https://github.com/ao-data/albiondata-client).

## Architecture

### Key Components

1. **Protocol18Deserializer.cs** - Complete Protocol 18 type deserializer
   - Supports all 30+ Protocol 18 type codes
   - Handles varint encoding, zig-zag encoding
   - Deserializes arrays, dictionaries, byte arrays, primitives

2. **Protocol18ReceiverBridge.cs** - Bridge between Protocol 18 parser and Albion.Network handlers
   - Parses raw Photon packets using Protocol 18
   - Exposes callbacks for Events, Requests, Responses
   - Bypasses Protocol 16 parsing completely

3. **PacketDeviceSelector.cs** - Modified to use Protocol 18 bridge
   - Uses reflection to invoke handler methods directly
   - Creates EventPacket/RequestPacket/ResponsePacket objects with Protocol 18 data
   - Invokes `ReceiveEvent()`, `ReceiveRequest()`, `ReceiveResponse()` on photonReceiver

## How It Works

```
UDP Packet (Npcap)
    ↓
PacketDeviceSelector.Device_OnPacketArrival()
    ↓
Protocol18ReceiverBridge.ReceivePacket()
    ↓
Protocol18Deserializer.DeserializeParameterTable()
    ↓
Callbacks: OnEvent / OnRequest / OnResponse
    ↓
Reflection: Create EventPacket/RequestPacket/ResponsePacket
    ↓
Invoke photonReceiver.ReceiveEvent/ReceiveRequest/ReceiveResponse()
    ↓
Existing handlers process the data
```

## Key Differences from albiondata-client

| Feature | albiondata-client (Go) | VRise (C#) |
|---------|------------------------|------------|
| Language | Go | C# .NET Framework 4.8 |
| Packet Flow | Callbacks → Direct handler | Callbacks → Reflection → ReceiverBuilder |
| Type System | interface{} | object with Dictionary<byte, object> |
| Handler Registration | Manual callbacks | ReceiverBuilder pattern |
| Integration | Clean callback architecture | Reflection to bypass Protocol 16 |

## Implementation Details

### 1. Protocol18Deserializer.cs

Complete C# port of albiondata-client's Protocol 18 deserializer:

**Supported Type Codes:**
- Primitives: Bool, Byte, Short, Int, Long, Float, Double
- Compact integers: Int1, Int2, Long1, Long2
- Special: Null, BoolTrue (27), BoolFalse (28)
- Zero values: IntZero (30), LongZero (31), etc.
- Collections: Array, Dictionary, ByteArray
- String: UTF-8 encoded with length prefix

**Key Methods:**
- `DeserializeParameterTable(byte[] data)` - Main entry point
- `DeserializeValue(Stream stream, byte typeCode)` - Type-specific deserialization
- `ReadCompressedInt32/Int64()` - Varint with zig-zag encoding

### 2. Protocol18ReceiverBridge.cs

**Key Methods:**
- `ReceivePacket(byte[] payload)` - Main entry point
- `HandleCommand()` - Processes Photon commands
- `HandleSendReliable()` - Processes reliable messages
- `DispatchEvent/Request/Response()` - Deserializes and invokes callbacks

**Callbacks:**
```csharp
public Action<byte, Dictionary<byte, object>> OnEvent { get; set; }
public Action<byte, Dictionary<byte, object>> OnRequest { get; set; }
public Action<byte, short, string, Dictionary<byte, object>> OnResponse { get; set; }
public Action OnEncrypted { get; set; }
```

### 3. PacketDeviceSelector.cs

**Reflection-based Handler Invocation:**

```csharp
// Create EventPacket using reflection
var assembly = typeof(IPhotonReceiver).Assembly;
var eventPacketType = assembly.GetType("Albion.Network.EventPacket");
var eventPacket = Activator.CreateInstance(eventPacketType, parameters);

// Invoke ReceiveEvent method
var receiveEventMethod = photonReceiver.GetType().GetMethod("ReceiveEvent",
    BindingFlags.NonPublic | BindingFlags.Instance);
receiveEventMethod.Invoke(photonReceiver, new[] { eventPacket });
```

This approach:
- Creates EventPacket/RequestPacket/ResponsePacket objects with Protocol 18 data
- Invokes private `ReceiveEvent()`/`ReceiveRequest()`/`ReceiveResponse()` methods
- Allows existing handlers to work without modification

## Testing

### Build and Test

Run `test-protocol18.bat` to build and prepare for testing:

```cmd
test-protocol18.bat
```

### Expected Output

When Protocol 18 parsing works correctly, you should see:

```
[Protocol18Bridge] Event 29 with 15 parameters
[Protocol18Bridge] Request 5 with 3 parameters
[Protocol18Bridge] Response 8 (rc=0) with 7 parameters
```

If handlers are invoked successfully, you should also see:

```
[DebugHandler] Event: 29
[NewCharacterEventHandler] Processing event...
```

### Troubleshooting

**No console output:**
- Check network adapter selection
- Verify game_port in network_config.json (5056 or 5050)
- Run Wireshark to confirm packets are being captured

**Protocol18Bridge messages but no handler output:**
- Reflection may have failed to find methods
- Check console for "Error invoking event handler" messages
- Verify Albion.Network assembly structure hasn't changed

**Build errors:**
- Ensure all three files are in DEATHEYE.csproj
- Check that PhotonPackageParser 4.1.0 is installed
- Verify .NET Framework 4.8 SDK is available

## Files Modified

1. **Radar/Packets/Photon/Protocol18Deserializer.cs** (NEW)
   - Complete Protocol 18 type deserializer

2. **Radar/Packets/Photon/Protocol18ReceiverBridge.cs** (NEW)
   - Bridge with callbacks and packet parsing

3. **Radar/Packets/Sniffer/PacketDeviceSelector.cs** (MODIFIED)
   - Removed PhotonParser (Protocol 16)
   - Added Protocol18ReceiverBridge with reflection-based handler invocation

4. **Radar/Init.cs** (MODIFIED)
   - Added console message confirming Protocol 18 is enabled

5. **DEATHEYE.csproj** (MODIFIED)
   - Added Protocol18Deserializer.cs
   - Added Protocol18ReceiverBridge.cs

6. **test-protocol18.bat** (NEW)
   - Quick build and test script

## Next Steps

If the current implementation doesn't work (handlers not being invoked):

### Option A: Direct Handler Registry

Instead of using reflection, create a direct registry:

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

### Option B: Bypass Albion.Network Completely

Implement handlers that directly accept Protocol 18 data:

```csharp
public interface IProtocol18EventHandler
{
    void OnEvent(byte eventCode, Dictionary<byte, object> parameters);
}
```

### Option C: Create Protocol 18 versions of handlers

Fork the existing handlers and modify them to work with Protocol 18 data directly.

## References

- **albiondata-client**: https://github.com/ao-data/albiondata-client
- **Protocol 18 parser (Go)**: https://github.com/ao-data/albiondata-client/blob/master/client/photon/parser.go
- **Protocol 18 deserializer (Go)**: https://github.com/ao-data/albiondata-client/blob/master/client/photon/deserializer.go
- **Photon Protocol spec**: Proprietary - reverse-engineered

## Version History

- **2026-04-14**: Initial Protocol 18 implementation
  - Protocol18Deserializer.cs complete
  - Protocol18ReceiverBridge.cs with callbacks
  - Reflection-based handler invocation
  - Ready for testing

## Notes

- This implementation maintains compatibility with existing handlers
- No changes required to event handler code
- All Protocol 16 dependencies removed from packet capture path
- Protocol 18 parsing happens before data reaches ReceiverBuilder
