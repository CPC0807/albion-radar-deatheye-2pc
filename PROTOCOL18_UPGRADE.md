# Protocol 18 Upgrade Guide

## Overview

This project has been upgraded from **Photon Protocol 16** to **Photon Protocol 18** to support the latest Albion Online game updates (April 2026).

## What Changed

### Old System (Protocol 16)
- Used `PhotonPackageParser 4.1.0` (deprecated, last updated October 2020)
- Used `Protocol16.dll` for deserialization
- Could not parse Protocol 18 packets from modern Albion Online clients

### New System (Protocol 18)
- Custom implementation based on **albiondata-client** (Go implementation)
- Reference: https://github.com/ao-data/albiondata-client
- Last updated: April 13, 2026 (version 0.1.51)
- Fully supports Protocol 18 type codes and encoding

## Files Added

1. **Radar\Packets\Photon\Protocol18Deserializer.cs**
   - Handles Protocol 18 type deserialization
   - Supports new type codes: IntZero (30), BoolFalse (27), BoolTrue (28), etc.
   - Implements compressed varint encoding
   - Handles byte-order normalization

2. **Radar\Packets\Photon\PhotonParser.cs**
   - Parses raw Photon UDP packets
   - Handles fragmented packets and reassembly
   - Dispatches events, requests, and responses to IPhotonReceiver
   - Detects encrypted packets

## Files Modified

1. **Radar\Packets\Sniffer\PacketDeviceSelector.cs**
   - Now uses `PhotonParser` instead of calling `photonReceiver.ReceivePacket()` directly
   - Added encrypted packet detection callback
   - Updated error messages to reference Protocol 18

2. **DEATHEYE.csproj**
   - Added new source files to compilation

## Technical Details

### Protocol 18 Type Codes

Protocol 18 introduces optimized type codes for common values:

```csharp
// Zero-value optimizations
TypeIntZero = 30      // Encodes integer 0 without additional bytes
TypeBoolFalse = 27    // Boolean false as single byte
TypeBoolTrue = 28     // Boolean true as single byte
TypeShortZero = 29    // Short 0
TypeLongZero = 31     // Long 0
TypeFloatZero = 32    // Float 0.0
TypeDoubleZero = 33   // Double 0.0
TypeByteZero = 34     // Byte 0

// Compressed integers (varint encoding)
TypeCompressedInt = 9     // Zig-zag encoded 32-bit int
TypeCompressedLong = 10   // Zig-zag encoded 64-bit long

// Compact integer types
TypeInt1 = 11         // 1-byte unsigned int
TypeInt1Neg = 12      // 1-byte unsigned int, negated
TypeInt2 = 13         // 2-byte unsigned int
TypeInt2Neg = 14      // 2-byte unsigned int, negated
```

### Varint Encoding

Protocol 18 uses **variable-length integer encoding** (varint) for:
- Parameter counts in tables
- String lengths
- Array sizes
- Dictionary counts

Encoding format:
- Each byte encodes 7 bits of data
- Most significant bit (0x80) indicates "more bytes follow"
- Little-endian bit ordering

Example:
```
300 = 0x12C = 0b100101100
Encoded as: [0xAC, 0x02]
  0xAC = 0b10101100 (bit 7 set, low 7 bits = 0b0101100 = 44)
  0x02 = 0b00000010 (bit 7 clear, low 7 bits = 0b0000010 = 2)
  Result = 44 + (2 << 7) = 44 + 256 = 300
```

### Byte Order Normalization

Some Albion Online builds may send operation/event codes with swapped byte order. The new parser includes normalization logic:

```csharp
// Check if code is recognized
if (IsKnownOperationCode(code))
    return code;

// Try byte-swapped version
uint16 swapped = (code << 8) | (code >> 8);
if (IsKnownOperationCode(swapped))
    return swapped;

// Check for common artifact (e.g., 0xDA00 should be 0x00DA)
if ((code > 0x00FF) && ((code & 0x00FF) == 0))
    return code >> 8;

return code;
```

## Building the Project

### Using Visual Studio

1. Open `DEATHEYE.sln` in Visual Studio 2022
2. Build > Build Solution (Ctrl+Shift+B)
3. The new Protocol 18 files will be automatically included

### Using MSBuild (Command Line)

```bash
# Debug build
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU

# Release build
msbuild DEATHEYE.sln /p:Configuration=Release /p:Platform=AnyCPU
```

### Using Visual Studio Code

1. Open the project folder in VS Code
2. Install C# Dev Kit extension
3. Press F5 to build and debug

## Testing

### Step 1: Verify Compilation

Build the project and ensure there are no errors:
- Check for namespace conflicts
- Verify all files compile successfully

### Step 2: Test Packet Capture

1. Run Albion Online
2. Start the radar application
3. Check console output for:
   ```
   [Init] Starting packet capture on game port 5056
   [Init] No Cryptonite needed! No hosts file modification needed!
   ```

### Step 3: Verify Packet Parsing

Watch for console messages indicating successful parsing:
```
[DebugHandler] Event: NewCharacter (29)
[DebugHandler] Event: Move (10)
[KeySync] XorCode received!
```

### Step 4: Check for Errors

If you see errors like:
```
[PhotonParser] Error parsing packet: ...
[PacketError] ArgumentException in Protocol18: ...
```

This may indicate:
- Game updated to a newer protocol version
- Packet structure changed
- Event/operation codes changed

## Troubleshooting

### No Packets Received

**Symptoms**: Radar shows no players, mobs, or resources

**Possible Causes**:
1. **Wrong game port**
   - Check `network_config.json`
   - Use Wireshark to verify which port Albion Online uses
   - Try ports 5050, 5056, or 5057

2. **Npcap not installed or misconfigured**
   - Reinstall Npcap with WinPcap compatibility mode
   - Run radar as Administrator

3. **Network adapter not selected**
   - Radar should auto-detect adapters
   - Check console for adapter selection messages

### Encrypted Packets

**Symptoms**:
```
[PacketDeviceSelector] Encountered encrypted packet
```

**Explanation**:
- Some game data (e.g., market prices) is encrypted by Albion Online
- This is normal and expected
- Player positions, mob data, and resources are NOT encrypted

**Action**: No action needed - this is informational only

### Player Positions Show (0.00, 0.00)

**Symptoms**: Players appear at coordinates (0, 0) instead of real positions

**Possible Causes**:
1. **XorCode not received**
   - Player positions are encrypted with XorCode from KeySync event
   - Check console for: `[KeySync] XorCode received!`
   - If missing, KeySync event ID (593) may have changed

2. **KeySync event ID changed**
   - Uncomment brute-force search in `Init.cs` lines 131-137
   - Run radar and switch maps in game
   - Check console for correct event ID
   - Update `jsons/indexes.json` with new ID

3. **Position decryption broken**
   - Check `PlayersHandler.UpdatePlayerPosition()` method
   - Verify `Decrypt(positionBytes)` is called with XorCode
   - See `POSITION_DECRYPTION_FIX.md` for details

### Protocol Mismatch Errors

**Symptoms**:
```
[PhotonParser] Error dispatching event: Index out of range
[PacketError] ArgumentException in Protocol18: ...
```

**Possible Causes**:
1. **Event/operation codes changed**
   - Albion Online updated packet structure
   - Update `jsons/indexes.json` and `jsons/offsets.json`

2. **Parameter structure changed**
   - Event handlers expect different parameter keys
   - Check `PacketOffsets` in event handler implementations

3. **Protocol updated beyond Protocol 18**
   - Albion Online may have updated to Protocol 19+
   - Check albiondata-client for updates: https://github.com/ao-data/albiondata-client/releases

## Comparison with albiondata-client

Our implementation is based on the latest albiondata-client (Go), with these adaptations:

| Feature | albiondata-client (Go) | VRise (C#) |
|---------|------------------------|------------|
| Language | Go | C# (.NET Framework 4.8) |
| Protocol Version | Protocol 18 | Protocol 18 |
| Deserialization | `photon/deserializer.go` | `Protocol18Deserializer.cs` |
| Parser | `photon/parser.go` | `PhotonParser.cs` |
| Varint Encoding | Native Go varint | Custom C# implementation |
| Byte Order | Little-endian | Little-endian (with normalization) |
| Fragmentation | Supported | Supported |
| Encrypted Packets | Detected | Detected (callback) |

### Key Differences

1. **Type System**
   - Go uses `interface{}` for dynamic types
   - C# uses `object` with explicit casting

2. **Error Handling**
   - Go returns `(value, error)` tuples
   - C# uses try-catch exceptions

3. **Integration**
   - albiondata-client sends data to external servers
   - VRise processes data locally for overlay rendering

## Future Updates

If Albion Online updates to a newer protocol version (e.g., Protocol 19):

1. **Check albiondata-client repository**
   - https://github.com/ao-data/albiondata-client/releases
   - Look for commit messages mentioning protocol updates

2. **Update deserializer**
   - Port new type codes from `client/photon/deserializer.go`
   - Add new cases to `Protocol18Deserializer.Deserialize()`

3. **Update parser**
   - Check for changes in `client/photon/parser.go`
   - Update `PhotonParser.cs` accordingly

4. **Test thoroughly**
   - Verify all game events still parse correctly
   - Check player positions, mob spawns, resource nodes

## References

- **albiondata-client**: https://github.com/ao-data/albiondata-client
- **Latest Release** (0.1.51, April 13, 2026): https://github.com/ao-data/albiondata-client/releases/tag/0.1.51
- **Protocol 18 Commit**: https://github.com/ao-data/albiondata-client/pull/180
- **Photon Protocol Documentation**: https://doc.photonengine.com/

## Credits

- **Protocol 18 Implementation**: Based on albiondata-client by ao-data team
- **Special Thanks**: jpcodecraft for Protocol 18 fix (April 2026)
- **Original PhotonPackageParser**: 0blu (deprecated)

## License

This Protocol 18 implementation is derived from albiondata-client (MIT License).
Original VRise code remains under its original license.
