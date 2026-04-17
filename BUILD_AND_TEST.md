# Build and Test Instructions

## ✅ Protocol 18 Implementation Complete

All code has been written and is ready to compile and test.

## 🔧 Building the Project

### Option 1: Visual Studio (Recommended)

1. Open `DEATHEYE.sln` in Visual Studio 2022
2. Press `Ctrl+Shift+B` to build
3. Check the Output window for any errors
4. Press `F5` to run and debug

### Option 2: Visual Studio Code

1. Open the project folder in VS Code
2. Install the C# Dev Kit extension if not already installed
3. Press `Ctrl+Shift+B` to build
4. Select "build" from the tasks menu
5. Press `F5` to run

### Option 3: Command Line

```bash
# Navigate to project directory
cd "c:\test\ubuntu\shared\code\albion-radar-deatheye-2pc"

# Clean and build
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" DEATHEYE.sln /t:Clean,Build /p:Configuration=Debug /p:Platform=AnyCPU

# Or for Release
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" DEATHEYE.sln /t:Clean,Build /p:Configuration=Release /p:Platform=AnyCPU
```

## ✅ What to Check After Building

### 1. Compilation Success

Look for:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 2. Output Files

Check that these files exist:
- `bin\Debug\VRise.exe` (or `bin\Release\VRise.exe`)
- `bin\Debug\Radar\Packets\Photon\Protocol18Deserializer.cs` (compiled into DLL)
- `bin\Debug\Radar\Packets\Photon\PhotonParser.cs` (compiled into DLL)

## 🧪 Testing

### Step 1: Pre-Test Checklist

- [ ] Npcap is installed
- [ ] Albion Online is running
- [ ] You are logged into the game
- [ ] `network_config.json` has correct port (default 5056)

### Step 2: Run the Radar

1. Start `VRise.exe` from `bin\Debug\` or `bin\Release\`
2. Select network adapter when prompted
3. Watch console output

### Step 3: Expected Console Output

✅ **Good signs:**
```
[Init] Starting packet capture on game port 5056
[Init] No Cryptonite needed! No hosts file modification needed!
[DebugHandler] Event: ...
```

After switching maps or zones:
```
[KeySync] XorCode received!
```

⚠️ **Normal (informational):**
```
[PacketDeviceSelector] Encountered encrypted packet
```
This is expected - market data is encrypted.

❌ **Bad signs (need fixing):**
```
[PhotonParser] Error parsing packet: ...
[PhotonParser] Error reconstructing packet: ...
System.NullReferenceException: ...
```

### Step 4: Verify Radar Functionality

- [ ] **Players appear** on radar (not at 0, 0)
- [ ] **Player positions update** when they move
- [ ] **Mobs appear** and move correctly
- [ ] **Resources appear** (trees, ore, fiber, etc.)
- [ ] **Dungeons appear** on the map
- [ ] **Equipment overlay works** (if enabled)

## 🐛 Common Build Errors and Fixes

### Error: "The name 'Protocol18Deserializer' does not exist"

**Fix**: Ensure `Protocol18Deserializer.cs` is in the correct location:
```
Radar\Packets\Photon\Protocol18Deserializer.cs
```

And is listed in `DEATHEYE.csproj`:
```xml
<Compile Include="Radar\Packets\Photon\Protocol18Deserializer.cs" />
```

### Error: "Using directive for 'PhotonPackageParser' is unnecessary"

**Fix**: This is just a warning. The code still works. You can ignore it or remove the `using PhotonPackageParser;` line from PhotonParser.cs (line 2).

### Error: "Could not load file or assembly 'Albion.Network'"

**Fix**: Restore NuGet packages:
```bash
nuget restore DEATHEYE.sln
```

Or in Visual Studio: Right-click solution → Restore NuGet Packages

## 🔍 Debugging Tips

### Enable Verbose Logging

In `Radar\Init.cs`, uncomment line 122:
```csharp
builder.AddHandler(new DebugAllEventsHandler());
```

This will show ALL events in the console (very verbose).

### Check Packet Capture

1. Open Wireshark
2. Filter: `udp.port == 5056` (or your configured port)
3. Verify packets are flowing from Albion Online
4. If no packets, try ports 5050, 5057

### Verify Network Config

Check `network_config.json`:
```json
{
  "game_port": 5056
}
```

Try changing to 5050 if 5056 doesn't work.

### KeySync Not Received

If players show at (0, 0), enable brute-force KeySync search:

In `Radar\Init.cs`, uncomment lines 131-137:
```csharp
for (int i = 500; i <= 700; i++)
{
    builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
}
```

Rebuild, run, and switch maps in game. Check console for the correct KeySync event ID.

## 📋 Test Checklist

Use this checklist to verify everything works:

### Basic Functionality
- [ ] Radar application starts without errors
- [ ] Network adapter selection works
- [ ] Console shows packet capture started
- [ ] No exceptions in console

### Player Tracking
- [ ] Local player position updates
- [ ] Other players appear on radar
- [ ] Player positions are accurate (not 0, 0)
- [ ] Player health bars display correctly
- [ ] Player equipment shows in overlay

### Mob Tracking
- [ ] World mobs appear
- [ ] Corrupt mobs appear (if in corrupted zone)
- [ ] Mist mobs appear (if in mists)
- [ ] Event mobs appear (during events)
- [ ] Mob positions update when they move

### Resource Tracking
- [ ] Trees appear
- [ ] Ore nodes appear
- [ ] Fiber nodes appear
- [ ] Hide nodes appear
- [ ] Stone nodes appear
- [ ] Fish zones appear
- [ ] Resource tiers display correctly
- [ ] Enchantment levels display correctly

### Dungeon Tracking
- [ ] Solo dungeons appear
- [ ] Group dungeons appear
- [ ] Avalonian dungeons appear
- [ ] Dungeon types display correctly

### Advanced Features
- [ ] KeySync receives XorCode
- [ ] Position decryption works
- [ ] Fragment reassembly works (large packets)
- [ ] Encrypted packet detection works
- [ ] Multiple overlays render correctly

## 📊 Performance Check

Monitor these metrics:
- **CPU Usage**: Should be < 10% normally
- **Memory Usage**: Should be < 500MB
- **FPS**: Overlays should render at ~30 FPS
- **Packet Loss**: Should be 0% (check console for errors)

## ✅ Success Criteria

Your radar is working correctly if:
1. ✅ Compiles without errors
2. ✅ Captures packets from Albion Online
3. ✅ Players appear at correct positions
4. ✅ Mobs and resources display
5. ✅ No constant errors in console
6. ✅ Overlays render smoothly

## 🆘 Still Having Problems?

1. **Read the troubleshooting guide**: [`PROTOCOL18_UPGRADE.md`](PROTOCOL18_UPGRADE.md)
2. **Check the summary**: [`PROTOCOL18_SUMMARY.md`](PROTOCOL18_SUMMARY.md)
3. **Review Chinese guide**: [`PROTOCOL18_升级说明.md`](PROTOCOL18_升级说明.md)
4. **Check albiondata-client**: https://github.com/ao-data/albiondata-client/issues

## 📝 Notes

- First build may take longer (compiling all dependencies)
- Subsequent builds are much faster (incremental)
- Clean build if you encounter weird errors: `msbuild /t:Clean`
- Debug builds are larger but easier to debug
- Release builds are optimized for performance

---

**Good luck! Your radar should now support Protocol 18!** 🎮✨
