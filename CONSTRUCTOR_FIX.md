# EventPacket Constructor Fix

## Problem

Protocol 18 parsing was working correctly:
```
[Protocol18Bridge] Event 3 with 3 parameters
```

But handler invocation was failing:
```
於 System.MissingMethodException 擲回例外狀況: 'mscorlib.dll'
[Protocol18Bridge] Error invoking event handler: Constructor on type 'Albion.Network.EventPacket' not found.
```

## Root Cause

The reflection code was trying to create `EventPacket` with this call:
```csharp
var eventPacket = Activator.CreateInstance(eventPacketType, parameters);
```

This assumed EventPacket has a constructor like:
```csharp
public EventPacket(Dictionary<byte, object> parameters)
```

But Albion.Network's EventPacket class doesn't have this constructor.

## Solution

Try multiple approaches to create the packet objects:

### Approach 1: Constructor with Dictionary
```csharp
try {
    eventPacket = Activator.CreateInstance(eventPacketType, parameters);
}
```

### Approach 2: Parameterless Constructor + Property Setter
```csharp
catch {
    try {
        eventPacket = Activator.CreateInstance(eventPacketType);
        var parametersProperty = eventPacketType.GetProperty("Parameters");
        if (parametersProperty != null && parametersProperty.CanWrite) {
            parametersProperty.SetValue(eventPacket, parameters);
        }
    }
}
```

### Approach 3: Parameterless Constructor + Field Setter
```csharp
catch {
    eventPacket = Activator.CreateInstance(eventPacketType);
    var parametersField = eventPacketType.GetField("Parameters",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (parametersField != null) {
        parametersField.SetValue(eventPacket, parameters);
    }
}
```

## Implementation

Updated [PacketDeviceSelector.cs](Radar/Packets/Sniffer/PacketDeviceSelector.cs) to use this fallback approach for all three packet types:
- EventPacket (OnEvent callback)
- RequestPacket (OnRequest callback)
- ResponsePacket (OnResponse callback)

## Testing

Run `rebuild-quick.bat` and watch for:

### ✅ SUCCESS
```
[Protocol18Bridge] Event 3 with 3 parameters
[DebugHandler] OnHandleAsync called - packet type: EventPacket
[DebugHandler] Event: 3
```

### ⚠️ STILL FAILING
```
[Protocol18Bridge] Event 3 with 3 parameters
[Protocol18Bridge] Error invoking event handler: <different error>
```

If still failing, it means:
1. EventPacket class structure is different than expected
2. Need to inspect Albion.Network.dll with ILSpy
3. Consider alternative approaches (direct handler registry, bypass Albion.Network)

## Files Modified

- [Radar/Packets/Sniffer/PacketDeviceSelector.cs](Radar/Packets/Sniffer/PacketDeviceSelector.cs)
  - Lines 105-165: OnEvent callback with fallback constructors
  - Lines 167-222: OnRequest callback with fallback constructors
  - Lines 224-279: OnResponse callback with fallback constructors

## Next Steps

1. **Build:** Run `rebuild-quick.bat`
2. **Test:** Launch Albion Online, then VRise.exe
3. **Verify:** Look for `[DebugHandler] Event: N` messages
4. **If successful:** Objects should appear on radar!
5. **If still failing:** Check console for new error messages and inspect Albion.Network.dll

## Expected Timeline

- **If this fix works:** Protocol 18 is fully functional, radar will detect objects ✅
- **If this fix doesn't work:** Need to inspect Albion.Network.dll to understand packet class structure (15-30 minutes)

## Alternative Approaches (if this fails)

### Option A: Direct Handler Registry
Bypass ReceiverBuilder entirely, invoke handlers directly from callbacks.

### Option B: Inspect Albion.Network.dll
Use ILSpy to see exact EventPacket/RequestPacket/ResponsePacket structure:
```bash
ilspycli packages/Albion.Network.5.0.1/lib/netstandard2.0/Albion.Network.dll
```

### Option C: Completely Bypass Albion.Network
Create new handler system that works directly with Protocol 18 data.
