# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DEATHEYE is a radar application for Albion Online designed to run on a separate device from the game client. It's a WPF application (.NET Framework 4.8, C#) that captures game network traffic to display real-time information about players, resources, mobs, and dungeons on an overlay.

**Important Changes in This Version:**
- ✅ **No Cryptonite required** - Uses direct packet forwarding instead
- ✅ **No hosts file modification** - More secure setup
- ✅ **Dual mode support** - Can use either npcap (local) or TCP receiver (remote)

This tool is detected by BattlEye anti-cheat and is intended for 2-device setups only.

## Build Commands

### Building the Project
```bash
# Open solution in Visual Studio or use MSBuild
msbuild DEATHEYE.sln /p:Configuration=Release /p:Platform=AnyCPU
```

### Build Output
- Debug: `bin\Debug\`
- Release: `bin\Release\`
- The AfterBuild target automatically copies `ITEMS\` and `ao-bin-dumps\` directories to output

### Prerequisites
1. .NET Framework 4.8 SDK
2. npcap (for packet capture)
3. Required external data:
   - `ITEMS\` directory with item images (not in repo, download separately)
   - `ao-bin-dumps\` directory with XML data files (not in repo, download from ao-data/ao-bin-dumps)

## Architecture

### Core Components

**Radar.Init** (`Radar\Init.cs`): Main initialization hub that:
- Loads JSON configuration files (`jsons/indexes.json`, `jsons/offsets.json`, `jsons/clusters.json`)
- Loads XML game data (`ao-bin-dumps/items.xml`, `harvestables.xml`, `mobs.xml`)
- Creates all handler instances (LocalPlayerHandler, PlayersHandler, HarvestablesHandler, MobsHandler, DungeonsHandler, etc.)
- Builds the Photon packet receiver with all event/request/response handlers
- Spawns threads for packet sniffing, global timer, and three overlays (radar, items, info)

**PacketDeviceSelector** (`Radar\Packets\Sniffer\PacketDeviceSelector.cs`): Network capture using SharpPcap/npcap to intercept Albion Online traffic locally. Used in "local" mode.

**NetworkPacketReceiver** (`Radar\Packets\Sniffer\NetworkPacketReceiver.cs`): TCP server that receives packets from remote packet sender (e.g., Ubuntu VM). Used in "remote" mode. This replaces the need for Cryptonite and hosts file modification.

**NetworkConfig** (`Radar\Packets\Sniffer\NetworkConfig.cs`): Configuration system for packet capture mode selection and network settings. Loads from `network_config.json`.

**Photon Protocol**: Uses Albion.Network and PhotonPackageParser libraries to decode Photon protocol packets. Packet handlers are in `Radar\Packets\Handlers\` and follow naming pattern `[EventName]Event.cs` + `[EventName]EventHandler.cs`.

### Handler System

Each game object type has a dedicated handler class:
- **LocalPlayerHandler**: Tracks the player's position, cluster (zone), and uses cluster data for map rendering
- **PlayersHandler**: Manages other players' positions, equipment, health, flagging status, faction state
- **HarvestablesHandler**: Tracks resource nodes (trees, ore, fiber, etc.) with tier and enchantment info
- **MobsHandler**: Tracks world mobs, corrupt mobs, mist mobs, drones, event mobs
- **DungeonsHandler**: Tracks dungeon entrances with type and rarity
- **FishNodesHandler**: Tracks fishing zones
- **GatedWispsHandler**: Tracks mist wisps and portals
- **LootChestsHandler**: Tracks treasure chests

Handlers receive parsed packet data and maintain collections of active game objects, cleaning up stale entries via GlobalTimer.

### Overlay Rendering

Three separate overlay threads use GameOverlay library (SharpDX/Direct2D):

1. **RadarOverlay** (`Radar\Drawing\Overlays\RadarOverlay.cs`): Main map overlay showing all objects
   - Uses "Drawer" classes for each object type (`PlayersDrawerer`, `HarvestablesDrawerer`, `MobsDrawerer`, etc.)
   - Settings controlled via `RadarOverlaySettings` and `RadarOverlayBrushesDictionary`

2. **ItemsOverlay** (`Radar\Drawing\Overlays\ItemsOverlay.cs`): Shows player equipment/inventory
   - Uses item images from `ITEMS\` directory
   - Controlled by `ItemsOverlaySettings` and `ItemsOverlayBrushesDictionary`

3. **InfoOverlay** (`Radar\Drawing\Overlays\InfoOverlay.cs`): Displays cluster/zone information

### Configuration System

**ConfigHandler** (`Settings\ConfigHandler.cs`): Singleton that manages `.cfg` files (JSON format)
- Loads/saves settings for ESP, resources, mobs, dungeons, styling, addons
- Creates default configs on first run (Config 1.cfg, Config 2.cfg, Config 3.cfg)
- Settings include: player ESP toggles, resource filters, mob filters, radar styling, overlay positions

**MainWindow** (`MainWindow.xaml.cs`): WPF UI with navigation between configuration pages
- Pages: PlayersPage, HarvestablePage, MobsPage, DungeonsPage, StylePage, MapPage, ItemsPage, MistsPage, SupportPage, ConfigPage

### Data Models

**XML Data Loading**:
- `HarvestableData.Load()` - Parses harvestables.xml for resource node data
- `ItemData.Load()` - Parses items.xml for equipment/item info
- `MobData.Load()` - Parses mobs.xml for mob type data

**JSON Packet Configuration**:
- `jsons/indexes.json` - Maps packet event names to numeric IDs (e.g., "NewCharacter": 29)
- `jsons/offsets.json` - Contains byte offsets for parsing packet data structures
- `jsons/clusters.json` - Map/cluster metadata

## Key Implementation Details

### Packet Flow

**Local Mode (npcap):**
1. PacketDeviceSelector captures raw network packets via npcap
2. SharpPcap extracts UDP payload on configured game_port
3. PhotonPackageParser decodes Photon protocol layer
4. Albion.Network maps to Albion-specific packet types
5. ReceiverBuilder dispatches to appropriate handler (Event/Request/Response)
6. Handler updates game object collections
7. Overlay threads read from collections and render at ~60 FPS

**Remote Mode (TCP receiver):**
1. Ubuntu VM sender captures packets using scapy on UDP port 5050
2. Sender forwards packets via TCP to NetworkPacketReceiver
3. NetworkPacketReceiver receives packet data on configured remote_port
4. Passes packet payload to IPhotonReceiver.ReceivePacket()
5. PhotonPackageParser decodes Photon protocol layer
6. Albion.Network maps to Albion-specific packet types
7. ReceiverBuilder dispatches to appropriate handler (Event/Request/Response)
8. Handler updates game object collections
9. Overlay threads read from collections and render at ~60 FPS

### Network Configuration

Configuration is loaded from `network_config.json` at startup:

```json
{
  "mode": "local",        // or "remote"
  "remote_port": 9999,    // TCP port for remote mode
  "game_port": 5050       // UDP port for game traffic
}
```

Mode selection in `Init.cs`:
- **local**: Uses PacketDeviceSelector with npcap
- **remote**: Uses NetworkPacketReceiver with TCP server

### Threading Model
- UI Thread: WPF MainWindow and configuration pages
- Packet Capture Thread:
  - Local mode: PacketDeviceSelector with SharpPcap event callbacks
  - Remote mode: NetworkPacketReceiver with async TCP server (Task-based)
- GlobalTimer Thread: Periodic cleanup of expired game objects
- Three Overlay Threads: Independent rendering loops with high-precision timers

### Coordinates and Positioning
- Albion uses 2D float coordinates
- LocalPlayer position is reference point for radar centering
- Zoom level and offset settings in StylePage control radar view
- Each drawer calculates screen position from world position relative to local player

## Common Gotchas

- **Missing ITEMS directory**: Application checks for `ITEMS\T1_TRASH.png` at startup and shows fatal error if missing
- **Missing ao-bin-dumps**: XML loading failures will cause startup crash with diagnostic message
- **Packet indexes/offsets**: These JSON files must match current Albion Online version; outdated values cause parsing errors
- **Thread safety**: Handlers use concurrent collections or locks; be careful when adding new shared state
- **AllowUnsafeBlocks**: Project uses unsafe code for performance-critical operations
- **Network mode configuration**: Must have valid `network_config.json` - if missing, defaults are created automatically
- **Port conflicts**: Remote mode TCP port (default 9999) must not be in use by other applications
- **Sender compatibility**: packet_sender.py must use matching game_port (default 5050)

## Development Notes

### Adding New Packet Capture Methods

To add a new packet capture method:

1. Create new class implementing packet forwarding to `IPhotonReceiver`
2. Add configuration option to `NetworkConfig.cs`
3. Update `Init.cs` to instantiate based on config mode
4. Update `Init.Start()` to start appropriate capture method

### Testing Different Modes

**Test Local Mode:**
```json
{ "mode": "local", "remote_port": 9999, "game_port": 5050 }
```
Requires npcap and game running on same PC.

**Test Remote Mode:**
```json
{ "mode": "remote", "remote_port": 9999, "game_port": 5050 }
```
Requires sender running on separate device/VM.

### Debugging Packet Flow

Add logging in NetworkPacketReceiver.cs to see packet reception:
```csharp
Console.WriteLine($"[DEBUG] Received {packetData.Length} bytes");
```

Add logging in sender to see packet capture:
```python
logger.setLevel(logging.DEBUG)
```

### Port Configuration

Game uses different ports depending on region:
- Port 5050: Most common (used by default)
- Port 5056: Alternative in some setups
- Check actual traffic with Wireshark to confirm

If packets not captured, change `game_port` in config and sender filter.
