# Free DEATHEYE Radar for Albion Online (2 device version - No Cryptonite Required)

## Discord

- Link: [https://discord.gg/Jhr5Y7qrCY](https://discord.gg/Jhr5Y7qrCY)

## ⚠️ Important Changes

**This modified version does NOT require:**
- ❌ Cryptonite tool
- ❌ Modifying hosts file
- ❌ DNS redirection

**Instead it uses:**
- ✅ Direct packet forwarding from Ubuntu VM
- ✅ Simple TCP network connection
- ✅ More secure and transparent setup

## Warning

This software is **DETECTED** by **BattlEye** and not intended to be used on a single PC with game client.

## Features
- Players (**current position**, items, detection sound, name, guild, alliance, distance, colors, etc.)
- Resources and resource mobs
- World mobs
- Dungeons (any type, **chest type and rarity** for current floor)
- Customizable map (size, position, colors, etc.)

## How it Works

You need two devices:

1. **Device 1: Windows PC (or VM)** - Runs the DEATHEYE radar
2. **Device 2: Ubuntu VM in VMware** - Captures game packets and forwards them

### Architecture

```
Albion Online (any device)
    ↓ (game traffic on UDP port 5050)
Ubuntu VM (VMware with Bridge mode)
    ↓ (packet_sender.py captures packets)
    ↓ (TCP connection on port 9999)
Windows PC (DEATHEYE radar)
    ↓ (NetworkPacketReceiver processes packets)
    ↓ (Display on radar overlay)
```

## Setup Instructions

### Step 1: Windows PC Setup (Radar Device)

1. **Install Prerequisites:**
   - Install npcap: https://npcap.com/dist/npcap-1.80.exe
   - Install .NET Framework 4.8 Runtime: https://dotnet.microsoft.com/download/dotnet-framework/net48

2. **Download DEATHEYE:**
   - Download the latest build from [Releases](https://github.com/pxlbit228/albion-radar-deatheye-2pc/releases)
   - Extract to any directory

3. **Download Required Assets:**

   ⚠️ Download and install [font from repository](https://github.com/pxlbit228/albion-radar-deatheye-2pc/raw/refs/heads/master/Design/Font/DEATHEYE.ttf)

   - **Items Images:** Download from https://drive.google.com/file/d/1Egji6ceOt3eBh6yE9-zxB4OPtoWMYSby/view?usp=sharing
     - Extract `ITEMS` directory to radar root directory

   - **Game Data:** Download ao-bin-dumps from https://github.com/ao-data/ao-bin-dumps/archive/refs/heads/master.zip
     - Extract contents of `ao-bin-dumps-master` into `ao-bin-dumps` directory in radar root

4. **Configure Network Mode:**
   - Open `network_config.json` in the radar directory
   - Set mode to `"remote"`:
     ```json
     {
       "mode": "remote",
       "remote_port": 9999,
       "game_port": 5050
     }
     ```
   - Save the file

5. **Find Your PC IP Address:**
   - Open Command Prompt
   - Run: `ipconfig`
   - Note your local IP address (e.g., 192.168.1.100)

### Step 2: Ubuntu VM Setup (Packet Forwarder)

1. **Create Ubuntu VM in VMware:**
   - Install Ubuntu (any recent version)
   - Set network adapter to **Bridge mode** (important!)
   - Ensure VM can ping your Windows PC

2. **Install Python Dependencies:**
   ```bash
   sudo apt update
   sudo apt install python3 python3-pip
   sudo pip3 install scapy
   ```

3. **Download packet_sender.py:**
   - Copy `sender/packet_sender.py` from this repository to your Ubuntu VM

4. **Configure Sender:**
   - Edit the sender script or use command line arguments
   - Set Windows PC IP address (from Step 1.5)

### Step 3: Run the System

**Order is important:**

1. **On Windows PC:** Run `X975.exe` (DEATHEYE radar)
   - It will start listening on port 9999
   - Wait for "Waiting for sender connection..." message

2. **On Ubuntu VM:** Run packet sender (as root for packet capture):
   ```bash
   sudo python3 packet_sender.py <WINDOWS_IP> 9999 <NETWORK_INTERFACE>
   ```

   Example:
   ```bash
   # Auto-detect configuration
   sudo python3 packet_sender.py

   # Manual configuration
   sudo python3 packet_sender.py 192.168.1.100 9999 ens33
   ```

3. **On Any Device:** Run Albion Online
   - Play normally
   - The radar should start showing data

### Troubleshooting

**No connection between sender and radar:**
- Check that both devices are on the same network
- Verify firewall allows TCP port 9999
- Ensure Ubuntu VM uses Bridge mode (not NAT)
- Ping Windows PC from Ubuntu VM to test connectivity

**No packets captured:**
- Verify Albion Online is running and in-game
- Check network interface name: `ip addr show`
- Try different interface names (ens33, ens32, eth0, enp0s3)
- Ensure you run sender with sudo

**Radar shows no data:**
- Check that packets are being sent (sender console shows "Sent packet: X bytes")
- Verify `network_config.json` mode is set to "remote"
- Check that game_port is 5050 in config
- Ensure ITEMS and ao-bin-dumps directories exist

## Configuration Files

### network_config.json

Located in radar root directory:

```json
{
  "mode": "remote",        // "local" for npcap, "remote" for TCP receiver
  "remote_port": 9999,     // Port to listen on for incoming packets
  "game_port": 5050        // Albion Online game port (usually 5050)
}
```

### Switching to Local Mode (npcap)

If you want to use traditional npcap capture (single PC setup):

1. Edit `network_config.json`:
   ```json
   {
     "mode": "local",
     "remote_port": 9999,
     "game_port": 5050
   }
   ```

2. Restart radar - it will use npcap instead

## How to Build from Source

1. Install prerequisites:
   - .NET Framework 4.8 SDK
   - npcap
   - Visual Studio 2019 or later

2. Clone repository:
   ```bash
   git clone https://github.com/pxlbit228/albion-radar-deatheye-2pc.git
   cd albion-radar-deatheye-2pc
   ```

3. Download external assets (as described in Setup Step 1.3)

4. Open `DEATHEYE.sln` in Visual Studio

5. Build solution (Ctrl+Shift+B)

## Advantages Over Original

✅ **No Cryptonite required** - Open source and transparent
✅ **No hosts file modification** - More secure, no system changes
✅ **No DNS hijacking** - Cleaner network setup
✅ **Easier to set up** - Just run two scripts
✅ **More portable** - Works with any packet capture setup

## Source

Original repository: [W4RPWISH/AlbionRadar-DEATHEYE_2pc](https://github.com/W4RPWISH/AlbionRadar-DEATHEYE_2pc)

Modified version: This repository

## License

Use at your own risk. This is for educational purposes only.
