# Protocol 18 Testing Guide

## Quick Start

1. **Build the project:**
   ```cmd
   test-protocol18.bat
   ```

2. **Run Albion Online** (connect to a server)

3. **Run VRise.exe** from `bin\Debug\`

4. **Watch console output** for Protocol 18 messages

## Expected Console Output

### ✅ Success Indicators

If Protocol 18 is working correctly, you'll see:

```
[PacketDeviceSelector] Using Protocol 18 parser
[Init] Protocol 18 enabled - using direct handler invocation
[Protocol18Bridge] Event 29 with 15 parameters
[Protocol18Bridge] Event 5 with 8 parameters
[DebugHandler] OnHandleAsync called - packet type: EventPacket
[DebugHandler] Event: 29
```

### ⚠️ Warning Signs

**If you only see bridge messages but NO handler messages:**
```
[Protocol18Bridge] Event 29 with 15 parameters
[Protocol18Bridge] Request 5 with 3 parameters
```

This means:
- Protocol 18 parsing is working ✅
- Handler invocation via reflection is failing ❌

**If you see reflection errors:**
```
[Protocol18Bridge] Error invoking event handler: Method 'ReceiveEvent' not found
```

This means:
- Albion.Network internal structure has changed
- Need to adjust reflection code or use alternative approach

**If you see NO messages at all:**
```
[PacketDeviceSelector] Using Protocol 18 parser
[Init] Protocol 18 enabled - using direct handler invocation
```

This means:
- No packets are being captured
- Check network adapter selection
- Check game_port in network_config.json
- Verify Albion Online is running and connected

## Diagnostic Steps

### Step 1: Verify Packet Capture

Use Wireshark to confirm packets are being captured:

1. Open Wireshark
2. Capture filter: `udp port 5056` (or 5050)
3. Start capture
4. Run Albion Online
5. You should see UDP packets

If no packets in Wireshark:
- Wrong network adapter
- Wrong port (check network_config.json)
- Game not connected to server

### Step 2: Check Protocol 18 Parsing

Look for `[Protocol18Bridge]` messages in console:

```
[Protocol18Bridge] Event 29 with 15 parameters
[Protocol18Bridge] Request 5 with 3 parameters
[Protocol18Bridge] Response 8 (rc=0) with 7 parameters
```

If you see these:
- ✅ Packet capture is working
- ✅ Protocol 18 parsing is working
- Move to Step 3

If you don't see these:
- ❌ Protocol 18 parsing is failing
- Check for errors in console
- Check Protocol18Deserializer.cs for exceptions

### Step 3: Check Handler Invocation

Look for `[DebugHandler]` messages in console:

```
[DebugHandler] OnHandleAsync called - packet type: EventPacket
[DebugHandler] Event: 29
```

If you see these:
- ✅ Everything is working!
- ✅ Objects should appear on radar

If you don't see these:
- ❌ Reflection-based handler invocation is failing
- Check for "Error invoking event handler" messages
- Consider alternative approaches (see below)

## Alternative Approaches

If reflection doesn't work, we have three options:

### Option A: Direct Handler Registry

Bypass ReceiverBuilder entirely and create direct handlers:

```csharp
// In Init.cs, create registry instead of ReceiverBuilder
var registry = new Protocol18HandlerRegistry();
registry.RegisterEvent(29, (params) => newCharacterHandler.OnEvent(params));
registry.RegisterEvent(5, (params) => moveHandler.OnEvent(params));
// ... etc

// In PacketDeviceSelector.cs
protocol18Bridge.OnEvent = (code, parameters) =>
{
    registry.DispatchEvent(code, parameters);
};
```

### Option B: Modify Handlers

Create Protocol 18 versions of handlers:

```csharp
public interface IProtocol18Handler
{
    void OnEvent(byte code, Dictionary<byte, object> parameters);
    void OnRequest(byte opCode, Dictionary<byte, object> parameters);
    void OnResponse(byte opCode, short returnCode, Dictionary<byte, object> parameters);
}
```

### Option C: Packet Reconstruction

Instead of reflection, reconstruct Protocol 16 packets from Protocol 18 data:

```csharp
// Create binary packet that Protocol 16 parser can understand
byte[] protocol16Packet = ConvertProtocol18ToProtocol16(msgType, data);
photonReceiver.ReceivePacket(protocol16Packet);
```

This is what PhotonParser.cs tried to do, but it didn't work because Albion.Network still uses Protocol 16 parser internally.

## Performance Testing

Once handlers are working, test performance:

1. **FPS check**: Overlay should run at 30 FPS
2. **CPU usage**: Should be similar to old version
3. **Memory usage**: Watch for memory leaks
4. **Latency**: Objects should appear in real-time

## Debugging Tips

### Enable Verbose Logging

In Protocol18ReceiverBridge.cs, add detailed logging:

```csharp
Console.WriteLine($"[Protocol18Bridge] Event {code} parameters:");
foreach (var kvp in parameters)
{
    Console.WriteLine($"  Key {kvp.Key}: {kvp.Value?.GetType().Name ?? "null"}");
}
```

### Check Parameter Integrity

Verify that Protocol 18 data matches expected format:

```csharp
// In DispatchEvent
if (parameters.ContainsKey(252))
{
    Console.WriteLine($"  Event code from params[252]: {parameters[252]}");
}
else
{
    Console.WriteLine($"  WARNING: params[252] missing!");
}
```

### Monitor Reflection

Add detailed reflection debugging:

```csharp
Console.WriteLine($"Assembly: {assembly.FullName}");
Console.WriteLine($"EventPacket type: {eventPacketType?.FullName ?? "NOT FOUND"}");
Console.WriteLine($"ReceiveEvent method: {receiveEventMethod?.Name ?? "NOT FOUND"}");
```

## Common Issues

### Issue: "Method 'ReceiveEvent' not found"

**Cause:** Albion.Network structure changed or method is named differently

**Fix:** Use ILSpy to inspect Albion.Network.dll and find correct method names:

```cmd
ilspycli packages\Albion.Network.5.0.1\lib\net48\Albion.Network.dll
```

### Issue: "Cannot create instance of EventPacket"

**Cause:** EventPacket constructor signature doesn't match

**Fix:** Check EventPacket constructor parameters:

```csharp
// Try different constructor signatures
var eventPacket = Activator.CreateInstance(eventPacketType, parameters);
// Or
var eventPacket = Activator.CreateInstance(eventPacketType, code, parameters);
```

### Issue: Handlers receive data but objects don't appear

**Cause:** Data format doesn't match handler expectations

**Fix:** Compare Protocol 18 parameter format with Protocol 16:

```csharp
// Log all parameters
foreach (var kvp in parameters)
{
    Console.WriteLine($"  [{kvp.Key}] = {kvp.Value}");
}
```

## Success Criteria

Protocol 18 implementation is successful when:

- ✅ Console shows `[Protocol18Bridge]` messages
- ✅ Console shows `[DebugHandler]` messages
- ✅ Objects appear on radar overlay
- ✅ Player positions update in real-time
- ✅ Resources, mobs, dungeons are detected
- ✅ No crashes or exceptions
- ✅ Performance is acceptable

## Next Steps After Success

Once Protocol 18 is working:

1. **Remove debug logging** - Clean up console output
2. **Performance optimization** - Profile and optimize hot paths
3. **Remove old code** - Delete PhotonParser.cs (Protocol 16 version)
4. **Update documentation** - Update README and CLAUDE.md
5. **Test edge cases** - Fragmented packets, encrypted packets, etc.

## Reporting Issues

If you encounter problems, please report with:

1. Full console output from startup to error
2. Which step in this guide failed
3. Wireshark capture (if packet capture issue)
4. Screenshot of error messages
5. Description of what you were doing in-game

## Contact

For Protocol 18 implementation questions, refer to:
- PROTOCOL18_IMPLEMENTATION.md
- PROTOCOL18_实现说明.md
- albiondata-client source: https://github.com/ao-data/albiondata-client
