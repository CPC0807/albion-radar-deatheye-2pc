# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VRise (formerly DEATHEYE) is a radar application for Albion Online designed to run on a separate device from the game client. It's a WPF application (.NET Framework 4.8, C#) that captures game network traffic to display real-time information about players, resources, mobs, and dungeons on an overlay.

**Important:**
- ✅ **Simplified setup** - No Cryptonite required, no hosts file modification needed
- ✅ **Configurable game port** - Easy port configuration via `network_config.json`
- ⚠️ This tool is detected by BattlEye anti-cheat and is intended for 2-device setups only

## Build Commands

### Building the Project
```bash
# Using MSBuild
msbuild DEATHEYE.sln /p:Configuration=Release /p:Platform=AnyCPU

# Or for specific platforms
msbuild DEATHEYE.sln /p:Configuration=Release /p:Platform=x64
msbuild DEATHEYE.sln /p:Configuration=Debug /p:Platform=AnyCPU
```

### Build Output
- Debug: `bin\Debug\`
- Release: `bin\Release\`
- x64: `bin\x64\Release\` or `bin\x64\Debug\`
- The AfterBuild target automatically copies `ITEMS\` and `ao-bin-dumps\` directories to output

### Prerequisites
1. .NET Framework 4.8 SDK
2. npcap (for packet capture) - https://npcap.com/
3. Required external data (not in repository):
   - `ITEMS\` directory with item images
   - `ao-bin-dumps\` directory with XML data files from https://github.com/ao-data/ao-bin-dumps
   - DEATHEYE.ttf font (for overlay icons)

## Architecture

### Core Components

**Radar.Init** ([Radar\Init.cs](Radar/Init.cs)): Main initialization hub
- Loads JSON configuration files (`jsons/indexes.json`, `jsons/offsets.json`, `jsons/clusters.json`)
- Loads XML game data (`ao-bin-dumps/items.xml`, `harvestables.xml`, `mobs.xml`)
- Creates all handler instances (LocalPlayerHandler, PlayersHandler, HarvestablesHandler, MobsHandler, DungeonsHandler, etc.)
- Builds the Photon packet receiver with all event/request/response handlers
- Spawns threads for packet sniffing and three overlay renderers (radar, items, info)

**PacketDeviceSelector** ([Radar\Packets\Sniffer\PacketDeviceSelector.cs](Radar/Packets/Sniffer/PacketDeviceSelector.cs)): Network packet capture
- Uses SharpPcap/npcap to intercept Albion Online UDP traffic
- Listens on configurable game port (default 5056, can be 5050)
- Extracts UDP payload and forwards to PhotonReceiver

**NetworkConfig** ([Radar\Packets\Sniffer\NetworkConfig.cs](Radar/Packets/Sniffer/NetworkConfig.cs)): Simple port configuration
- Loads from `network_config.json`
- Only configures `game_port` (5056 or 5050 depending on region)
- Auto-creates default config if missing

**Photon Protocol**: Uses Albion.Network (v5.0.1) and PhotonPackageParser (v4.1.0) libraries to decode Photon protocol packets. Packet handlers follow naming pattern `[EventName]Event.cs` + `[EventName]EventHandler.cs` in `Radar\Packets\Handlers\`.

### Handler System

Each game object type has a dedicated handler class that processes network events:

- **LocalPlayerHandler**: Tracks player position, cluster (zone), mist dungeon state, and cluster metadata for map rendering
- **PlayersHandler**: Manages other players' positions, equipment, health, flagging status, faction, mounts, KeySync data
- **HarvestablesHandler**: Tracks resource nodes (trees, ore, fiber, hide, stone, fish) with tier and enchantment info
- **MobsHandler**: Tracks world mobs, corrupt mobs, mist mobs (wisps, spiders, dragons, griffins), drones, event mobs
- **DungeonsHandler**: Tracks dungeon entrances with type and rarity
- **FishNodesHandler**: Tracks fishing zones
- **GatedWispsHandler**: Tracks mist wisps and portals
- **LootChestsHandler**: Tracks treasure chests

Handlers process packets from ReceiverBuilder, update game object collections, and expose data to overlay renderers. GlobalTimer (currently disabled) was used for periodic cleanup of stale objects.

### Overlay Rendering

Three independent overlay threads use GameOverlay library (SharpDX/Direct2D) for hardware-accelerated rendering:

1. **RadarOverlay** ([Radar\Drawing\Overlays\RadarOverlay.cs](Radar/Drawing/Overlays/RadarOverlay.cs)): Main map overlay (30 FPS)
   - Uses dedicated "Drawer" classes: `PlayersDrawerer`, `HarvestablesDrawerer`, `MobsDrawerer`, `DungeonsDrawerer`, `FishNodesDrawerer`, `GatedWispsDrawerer`, `LootChestsDrawerer`, `HudDrawerer`
   - Settings managed by `RadarOverlaySettings` and `RadarOverlayBrushesDictionary`
   - Fullscreen transparent overlay

2. **ItemsOverlay** ([Radar\Drawing\Overlays\ItemsOverlay.cs](Radar/Drawing/Overlays/ItemsOverlay.cs)): Player equipment/inventory display
   - Loads item images from `ITEMS\` directory
   - Controlled by `ItemsOverlaySettings` and `ItemsOverlayBrushesDictionary`

3. **InfoOverlay** ([Radar\Drawing\Overlays\InfoOverlay.cs](Radar/Drawing/Overlays/InfoOverlay.cs)): Cluster/zone information HUD
   - Displays current zone, mist dungeon info, etc.

### Configuration System

**ConfigHandler** ([Settings\ConfigHandler.cs](Settings/ConfigHandler.cs)): Singleton configuration manager
- Manages `.cfg` files in JSON format (Config 1.cfg, Config 2.cfg, Config 3.cfg)
- Auto-creates default configs on first run
- Settings categories:
  - Player ESP toggles (show players, equipment, health, distance, etc.)
  - Resource filters (tier, enchantment, type)
  - Mob filters (corrupt, mist, world, event)
  - Dungeon filters
  - Radar styling (zoom, colors, position, fonts)
  - Overlay positions and visibility
  - Language (en-US, ru-RU)
  - Hotkeys (Toggle Menu: INSERT, Toggle Radar: END, Close: HOME)

**MainWindow** ([MainWindow.xaml.cs](MainWindow.xaml.cs)): WPF UI with page navigation
- Pages: PlayersPage, HarvestablePage, MobsPage, DungeonsPage, StylePage, MapPage, ItemsPage, MistsPage, SupportPage, ConfigPage
- Uses Wpf.Ui library for modern UI components

### Data Models

**XML Data Loading** (from ao-bin-dumps):
- `HarvestableData.Load("ao-bin-dumps/harvestables.xml")` - Resource node definitions
- `ItemData.Load("ao-bin-dumps/items.xml")` - Equipment/item metadata
- `MobData.Load("ao-bin-dumps/mobs.xml")` - Mob type definitions

**JSON Packet Configuration**:
- `jsons/indexes.json` - Maps packet event names to numeric IDs (e.g., "NewCharacter": 29, "Leave": 46)
- `jsons/offsets.json` - Byte offsets for parsing packet data structures
- `jsons/clusters.json` - Map/cluster metadata (display names, colors, types)

## Key Implementation Details

### Packet Flow

1. **PacketDeviceSelector** captures raw UDP packets on configured `game_port` via npcap
2. **SharpPcap** extracts UDP payload from captured packets
3. **PhotonPackageParser** decodes Photon protocol layer (handles fragmentation, reassembly)
4. **Albion.Network** (IPhotonReceiver) maps to Albion-specific packet types
5. **ReceiverBuilder** dispatches to registered handlers based on packet type (Event/Request/Response)
6. **Handlers** extract game object data from Dictionary<byte, object> parameters and update collections
7. **Overlay threads** read from handler collections and render at 30 FPS

### Network Configuration

Configuration is loaded from `network_config.json`:

```json
{
  "game_port": 5056
}
```

**Port Configuration:**
- Port 5056: Default (most common)
- Port 5050: Alternative in some setups
- Use Wireshark to determine which port your game client uses
- Edit `network_config.json` and restart application to change port

### Threading Model
- **UI Thread**: WPF MainWindow and configuration pages (STA thread)
- **Packet Capture Thread**: PacketDeviceSelector with SharpPcap event callbacks
- **GlobalTimer Thread**: Currently disabled (line 194 in Init.cs is commented out)
  - Was used for: position syncing, health regeneration, stale object cleanup
- **Three Overlay Threads**: Independent rendering loops with PrecisionTimer (high-precision timing)
  - radarOverlay: Main map rendering
  - itemsOverlay: Equipment display
  - infoOverlay: Zone info HUD

### Coordinates and Positioning
- Albion uses 2D float coordinates (Vector2)
- LocalPlayer position is reference point for radar centering
- Zoom level and offset settings in StylePage control radar view
- Each drawer calculates screen position from world position relative to local player
- Position syncing between network updates uses interpolation (when GlobalTimer is enabled)

## Common Gotchas

- **Missing ITEMS directory**: Application checks for `ITEMS\T1_TRASH.png` at startup and shows fatal error if missing
- **Missing ao-bin-dumps**: XML loading failures cause startup crash with diagnostic message from Diagnostics.DoVital()
- **Packet indexes/offsets**: JSON files must match current Albion Online version; outdated values cause parsing errors or no data
- **Thread safety**: Handlers use concurrent collections or locks; be careful when adding new shared state
- **AllowUnsafeBlocks**: Project uses unsafe code for performance-critical operations (enabled in all configurations)
- **Network config**: If `network_config.json` is missing, default config with port 5056 is auto-created
- **Wrong port**: If no packets are captured, check which port your game uses with Wireshark and update `game_port`
- **Npcap not installed**: Application will show error message and exit if npcap is not available
- **GlobalTimer disabled**: Position syncing and object cleanup are currently disabled (Init.cs:194)

## Development Notes

### Adding New Event Handlers

To add support for a new packet type:

1. Create event model class in `Radar\Packets\Handlers\` (e.g., `NewFooEvent.cs`)
2. Create handler class implementing `EventHandler<T>` (e.g., `NewFooEventHandler.cs`)
3. Register handler in `Init.cs` ReceiverBuilder section:
   ```csharp
   builder.AddEventHandler(new NewFooEventHandler(fooHandler));
   ```
4. Add packet ID mapping to `jsons/indexes.json` if needed
5. Add byte offset definitions to `jsons/offsets.json` if needed

### Adding New Game Object Types

1. Create game object class in `Radar\GameObjects\{Type}\` (e.g., `Foo.cs`)
2. Create handler class (e.g., `FooHandler.cs`) with collection management
3. Create drawer class in `Radar\Drawing\Drawers\` (e.g., `FooDrawerer.cs`)
4. Register handler in `Init.cs` constructor
5. Add drawer to `RadarOverlay.cs` constructor and DrawAsync() method
6. Add configuration options to `Config.cs` and appropriate UI page

### Debugging Packet Capture

Enable debug output in Init.cs (lines 97-98):
```csharp
#if DEBUG
builder.AddHandler(new DebugHandler());
#endif
```

Use `DebugAllEventsHandler` to log all packet types:
```csharp
builder.AddHandler(new DebugAllEventsHandler());
```

### KeySync Brute-Force (Development)

Lines 131-137 in Init.cs contain commented-out code for brute-forcing KeySync event IDs. This is used when packet structure changes in new game versions:

```csharp
// for (int i = 500; i <= 700; i++)
// {
//     builder.AddEventHandler(new BruteForceKeySyncHandler(playersHandler, i));
// }
```

### Project Naming

- Assembly name: `VRise` (namespace: `VRise`)
- Previous name: DEATHEYE (still used in solution file name)
- Recent rename in commit 3424cf8

### Resource Icons

Images in `Design\Images\`:
- `Mobs\Corrupt\` - Corrupt mob icons (GLUE, HOOK, KNOCKBACK, LAVA, SILENCE)
- `Mobs\Mist\` - Mist mob icons (CRYSTALSPIDER, DRAGON, GRIFFIN, SPIDER, WISP, MIST_PORTAL)
- `Mobs\Other\` - Special icons (EVENT, TREASURE, DRONE, CHEST)
- `Resources\` - Resource icons (fiber, hide, ore, rock, wood)

Item images in `ITEMS\` directory use naming pattern: `{ItemID}.png` (e.g., `T1_TRASH.png`, `T8_HEAD_PLATE_HELL@3.png`)

### Language Support

Supports English (en-US) and Russian (ru-RU) via resource dictionaries:
- `Design\Lang\lang.xaml` - English (default)
- `Design\Lang\lang.ru-RU.xaml` - Russian

## Recent Bug Fixes

### FormatException in Config Loading

**Issue**: Application crashed on startup with `System.FormatException` when trying to convert color strings in `StyleSettings` array to integers.

**Fix**: Added `ConfigHandler.SafeConvertToInt32()` helper method that safely handles:
- Mixed type arrays (strings, ints, longs, doubles)
- Color code strings (returns default value instead of throwing)
- Null values

Updated all files using `Convert.ToInt32()` on config values to use the safe version.

### Player Position Shows (0.00, 0.00)

**Issue**: Player positions displayed as `(0.00, 0.00)` even though player data was received correctly.

**Cause**: `PlayersHandler.UpdatePlayerPosition()` was not using the `Decrypt()` method to decrypt position bytes with XorCode.

**Fix**: Modified the method to:
1. Call `Decrypt(positionBytes)` to decrypt coordinates using XorCode
2. Add diagnostic warnings when XorCode is NULL or invalid
3. Output raw bytes when position is still (0.00, 0.00) for debugging

**KeySync Troubleshooting**: If positions are still (0.00, 0.00):
1. Ensure KeySync event is triggered (switch maps/zones in game)
2. Check console for `[KeySync] XorCode received!` message
3. If KeySync is NULL, event ID 593 may have changed - use brute-force search in Init.cs lines 131-137
4. See [POSITION_DECRYPTION_FIX.md](POSITION_DECRYPTION_FIX.md) and [FIXES_SUMMARY_ZH.md](FIXES_SUMMARY_ZH.md) for detailed troubleshooting
