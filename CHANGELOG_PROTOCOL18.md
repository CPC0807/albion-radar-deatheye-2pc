# Changelog - Protocol 18 Upgrade

## [Unreleased] - 2026-04-14

### Added

#### New Files
- `Radar\Packets\Photon\Protocol18Deserializer.cs` - Complete Protocol 18 deserializer implementation
  - Supports all Protocol 18 type codes (30+ types)
  - Varint encoding/decoding for compressed integers
  - Typed array deserialization with optimizations
  - Dictionary and custom type support
  - Zero-value optimizations (IntZero, BoolFalse, etc.)

- `Radar\Packets\Photon\PhotonParser.cs` - Raw Photon packet parser
  - Command-level packet parsing (SendReliable, SendUnreliable, SendFragment)
  - Fragment reassembly for large packets
  - Event/Request/Response dispatching
  - Encrypted packet detection

#### Documentation
- `PROTOCOL18_UPGRADE.md` - Comprehensive upgrade guide (English)
- `PROTOCOL18_升级说明.md` - 升级指南（中文）
- `PROTOCOL18_SUMMARY.md` - Quick reference summary
- `CHANGELOG_PROTOCOL18.md` - This file

### Changed

#### Modified Files
- `Radar\Packets\Sniffer\PacketDeviceSelector.cs`
  - Added `PhotonParser` instance field
  - Replaced direct `photonReceiver.ReceivePacket()` call with `photonParser.ReceivePacket()`
  - Added encrypted packet detection callback
  - Updated error messages to reference Protocol 18

- `DEATHEYE.csproj`
  - Added `Protocol18Deserializer.cs` to compilation
  - Added `PhotonParser.cs` to compilation

### Deprecated

- **PhotonPackageParser 4.1.0** (from NuGet)
  - Still referenced but no longer used
  - Can be safely removed in future update

- **Protocol16.dll**
  - Still referenced but no longer used
  - Can be safely removed in future update

### Technical Details

#### Protocol 18 Features Implemented

1. **Type System**
   - All 30+ Protocol 18 type codes
   - Zero-value sentinels (IntZero, BoolFalse, etc.)
   - Compressed integers with zig-zag encoding
   - Compact integer types (Int1, Int2, etc.)

2. **Encoding**
   - Varint (variable-length integer) encoding
   - Little-endian byte order with normalization
   - Efficient array packing (especially for booleans)

3. **Packet Handling**
   - 12-byte Photon packet header parsing
   - 12-byte command header parsing
   - Fragment reassembly with sequence tracking
   - Multiple commands per packet

4. **Error Handling**
   - Graceful degradation on parse errors
   - Encrypted packet detection (doesn't crash)
   - Console logging for debugging

#### Performance Considerations

- Minimal allocations for common types
- Reuses byte arrays where possible
- Efficient varint decoding algorithm
- Fragment buffer cleanup

#### Compatibility

- **Maintains backward compatibility** with existing `IPhotonReceiver` interface
- **No changes required** to event handlers
- **Drop-in replacement** for old PhotonPackageParser

### Testing

#### Verified Functionality
- ✅ Packet capture from game
- ✅ Event deserialization (NewCharacter, Move, etc.)
- ✅ Request deserialization (MoveRequest, etc.)
- ✅ Response deserialization (JoinResponse, ChangeCluster, etc.)
- ✅ KeySync event parsing for position decryption
- ✅ Fragment reassembly for large packets

#### Known Limitations
- Debug messages in responses may not always parse (gracefully skipped)
- Encrypted packets are detected but not decrypted (expected behavior)
- Some market data may be encrypted by Albion Online

### Migration Notes

#### For Users
1. **No configuration changes required**
   - Existing `network_config.json` works as-is
   - Existing `jsons/indexes.json` and `jsons/offsets.json` compatible

2. **Building**
   - Clean build recommended: `msbuild DEATHEYE.sln /t:Clean,Build`
   - New files will be automatically compiled

3. **Testing**
   - Verify radar shows players, mobs, resources
   - Check console for `[KeySync] XorCode received!`
   - Player positions should be accurate (not 0, 0)

#### For Developers
1. **New API**
   - `PhotonParser` class can be instantiated with any `IPhotonReceiver`
   - `Protocol18Deserializer` is static and can be used independently
   - `OnEncrypted` callback for encrypted packet notification

2. **Extensibility**
   - Easy to add new Protocol 18 type codes
   - Deserializer is self-contained and testable
   - Parser handles packet structure independently

3. **Debugging**
   - Console logging at key points
   - Error messages include context
   - Can enable `DebugHandler` for verbose output

### References

- **Based on**: [albiondata-client](https://github.com/ao-data/albiondata-client) v0.1.51
- **Protocol 18 Commit**: https://github.com/ao-data/albiondata-client/pull/180
- **Release Date**: April 13, 2026
- **Credit**: jpcodecraft for Protocol 18 implementation

### Future Work

#### Planned
- [ ] Remove deprecated PhotonPackageParser dependency
- [ ] Remove deprecated Protocol16.dll dependency
- [ ] Add unit tests for Protocol18Deserializer
- [ ] Add unit tests for PhotonParser

#### Nice to Have
- [ ] Performance benchmarking vs old parser
- [ ] Packet recording/replay for testing
- [ ] Protocol version detection (auto-detect 16 vs 18)

---

**Version**: Protocol 18 Implementation (Initial)
**Date**: 2026-04-14
**Author**: Based on albiondata-client by ao-data team
**License**: Derived from MIT-licensed albiondata-client
