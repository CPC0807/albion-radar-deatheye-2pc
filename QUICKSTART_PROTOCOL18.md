# Quick Start - Protocol 18 Upgrade

## 🚀 TL;DR

Your Albion Radar now supports **Protocol 18** (latest Albion Online update, April 2026).

## ⚡ Quick Steps

### 1️⃣ Build
```bash
# Open in Visual Studio and press F5
# OR
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU
```

### 2️⃣ Run
1. Start **Albion Online**
2. Start **VRise Radar**
3. Select network adapter (auto-popup)

### 3️⃣ Verify
Check console output for:
```
✅ [Init] Starting packet capture on game port 5056
✅ [KeySync] XorCode received!
✅ [DebugHandler] Event: NewCharacter (29)
```

### 4️⃣ Troubleshoot

| Problem | Quick Fix |
|---------|-----------|
| No packets | Edit `network_config.json` → try port 5050 or 5056 |
| Players at (0, 0) | Uncomment lines 131-137 in `Init.cs` → rebuild |
| Compile error | Check files in `Radar\Packets\Photon\` exist |

## 📁 What Changed

**Added**:
- `Radar\Packets\Photon\Protocol18Deserializer.cs` (455 lines)
- `Radar\Packets\Photon\PhotonParser.cs` (285 lines)

**Modified**:
- `Radar\Packets\Sniffer\PacketDeviceSelector.cs` (uses new parser)
- `DEATHEYE.csproj` (includes new files)

## 🔍 Quick Test

1. **Run radar** with Albion Online open
2. **Move around** in game
3. **Check radar shows**:
   - ✅ Your position (center)
   - ✅ Other players
   - ✅ Mobs
   - ✅ Resources
   - ✅ Dungeons

## ⚠️ Common Issues

### Issue: "No packets received"
**Fix**:
```json
// Edit network_config.json
{
  "game_port": 5056  // Try 5050, 5056, or 5057
}
```

### Issue: "Players show at (0, 0)"
**Fix**:
```csharp
// In Init.cs, uncomment lines 131-137:
for (int i = 500; i <= 700; i++)
{
    builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
}
// Rebuild, run, switch maps in game, check console for new KeySync ID
```

### Issue: "Encrypted packet" warning
**This is normal** - market data is encrypted, but player/mob/resource data is not.

## 📚 Full Documentation

- **English**: [`PROTOCOL18_UPGRADE.md`](PROTOCOL18_UPGRADE.md)
- **中文**: [`PROTOCOL18_升级说明.md`](PROTOCOL18_升级说明.md)
- **Summary**: [`PROTOCOL18_SUMMARY.md`](PROTOCOL18_SUMMARY.md)

## 🆘 Still Having Issues?

1. Check [`PROTOCOL18_UPGRADE.md`](PROTOCOL18_UPGRADE.md) - Troubleshooting section
2. Enable debug: Uncomment `builder.AddHandler(new DebugAllEventsHandler());` in `Init.cs`
3. Check albiondata-client: https://github.com/ao-data/albiondata-client/issues

## 🙏 Credits

Based on **albiondata-client v0.1.51** (April 13, 2026)
- https://github.com/ao-data/albiondata-client

---

**That's it! Happy gaming!** 🎮✨
