# Quick Start Guide - DEATHEYE Radar (No Cryptonite)

## 🚀 快速開始（中文）

### Windows PC (雷達端)

1. **下載並安裝依賴：**
   - Npcap: https://npcap.com/dist/npcap-1.80.exe
   - .NET Framework 4.8: https://dotnet.microsoft.com/download/dotnet-framework/net48
   - 字體文件 DEATHEYE.ttf（必須安裝）

2. **下載雷達和資源：**
   - DEATHEYE 雷達（從 Releases 下載）
   - ITEMS 圖片目錄
   - ao-bin-dumps 數據目錄

3. **配置 network_config.json：**
   ```json
   {
     "mode": "remote",
     "remote_port": 9999,
     "game_port": 5050
   }
   ```

4. **查找本機 IP：**
   ```cmd
   ipconfig
   ```
   記下 IP（例如：192.168.1.100）

5. **啟動雷達：**
   ```cmd
   X975.exe
   ```

### Ubuntu VM (封包轉發端)

1. **安裝依賴：**
   ```bash
   sudo apt update
   sudo apt install python3 python3-pip
   sudo pip3 install scapy
   ```

2. **運行發送器：**
   ```bash
   # 自動檢測配置
   sudo python3 packet_sender.py

   # 或手動指定（推薦）
   sudo python3 packet_sender.py 192.168.1.100 9999 ens33
   ```

### 驗證連接

1. Windows 雷達應顯示：`Waiting for sender connection...`
2. Ubuntu 發送器連接後顯示：`Connected to receiver at ...`
3. 進入遊戲後，雷達應開始顯示數據

---

## 🚀 Quick Start (English)

### Windows PC (Radar)

1. **Install dependencies:**
   - Npcap: https://npcap.com/dist/npcap-1.80.exe
   - .NET Framework 4.8
   - DEATHEYE.ttf font (required)

2. **Download radar + assets:**
   - DEATHEYE radar (from Releases)
   - ITEMS directory
   - ao-bin-dumps directory

3. **Configure network_config.json:**
   ```json
   {
     "mode": "remote",
     "remote_port": 9999,
     "game_port": 5050
   }
   ```

4. **Find your IP:**
   ```cmd
   ipconfig
   ```
   Note your IP (e.g., 192.168.1.100)

5. **Start radar:**
   ```cmd
   X975.exe
   ```

### Ubuntu VM (Packet Forwarder)

1. **Install dependencies:**
   ```bash
   sudo apt update
   sudo apt install python3 python3-pip
   sudo pip3 install scapy
   ```

2. **Run sender:**
   ```bash
   # Auto-detect
   sudo python3 packet_sender.py

   # Manual (recommended)
   sudo python3 packet_sender.py 192.168.1.100 9999 ens33
   ```

### Verify Connection

1. Windows radar shows: `Waiting for sender connection...`
2. Ubuntu sender shows: `Connected to receiver at ...`
3. Start game - radar should display data

---

## 🔧 Troubleshooting

### Connection Failed

**Check network:**
```bash
# From Ubuntu VM, ping Windows PC
ping 192.168.1.100
```

**Check VMware network mode:**
- Must use **Bridge** mode (not NAT)
- VM Settings → Network Adapter → Bridge

**Check Windows firewall:**
- Allow TCP port 9999 inbound
- Or temporarily disable firewall for testing

### No Packets Captured

**Find correct network interface:**
```bash
ip addr show
```
Look for active interface (ens33, ens32, eth0, etc.)

**Check game is running:**
- Must be in-game (not character select)
- Game traffic should be visible

**Verify sender is capturing:**
```bash
# Test with tcpdump
sudo tcpdump -i ens33 udp port 5050
```

### Radar Shows Nothing

**Check configuration:**
1. `network_config.json` exists
2. Mode is set to "remote"
3. game_port is 5050

**Check assets:**
1. ITEMS directory exists with images
2. ao-bin-dumps directory exists with XML files
3. Font is installed

**Check console output:**
- Look for error messages
- Verify "Processed X packets" messages

---

## 📋 Common Network Interface Names

| Environment | Common Interface Names |
|-------------|----------------------|
| VMware Ubuntu | ens33, ens32 |
| VirtualBox Ubuntu | enp0s3, enp0s8 |
| Physical Ubuntu | eth0, eno1, enp3s0 |
| WSL2 | eth0 |

---

## ⚙️ Advanced Configuration

### Change Receiver Port

Edit `network_config.json`:
```json
{
  "mode": "remote",
  "remote_port": 8888,  // Change this
  "game_port": 5050
}
```

Then update sender command:
```bash
sudo python3 packet_sender.py 192.168.1.100 8888 ens33
```

### Switch to Local Mode (npcap)

Edit `network_config.json`:
```json
{
  "mode": "local",
  "remote_port": 9999,
  "game_port": 5050
}
```

Radar will use npcap to capture locally (single PC setup).

---

## ✅ Checklist

**Before running:**
- [ ] Windows PC and Ubuntu VM on same network
- [ ] VMware network adapter set to Bridge mode
- [ ] Npcap installed on Windows
- [ ] .NET Framework 4.8 installed
- [ ] DEATHEYE font installed
- [ ] ITEMS directory in radar folder
- [ ] ao-bin-dumps directory in radar folder
- [ ] network_config.json configured with mode="remote"
- [ ] Python and scapy installed on Ubuntu

**When running:**
- [ ] Start Windows radar first
- [ ] Wait for "Waiting for sender connection..."
- [ ] Start Ubuntu sender (with sudo)
- [ ] See "Connected to receiver" message
- [ ] Start Albion Online
- [ ] See radar overlay appear

---

## 🎯 Expected Behavior

### Successful Setup

**Windows radar console:**
```
[NetworkConfig] Mode: remote, Remote Port: 9999, Game Port: 5050
[Init] Using REMOTE mode (TCP receiver on port 9999)
[Init] No need to modify hosts file or run Cryptonite!
[NetworkPacketReceiver] Started on port 9999
[NetworkPacketReceiver] Waiting for sender connection...
[NetworkPacketReceiver] Sender connected from 192.168.1.200:xxxxx
[NetworkPacketReceiver] Processed 100 packets
```

**Ubuntu sender console:**
```
Connected to receiver at 192.168.1.100:9999
Starting packet capture on interface ens33
Filter: UDP port 5050 (Albion Online)
Sent packet: 156 bytes
Sent packet: 234 bytes
...
```

### If Nothing Happens

1. Check both consoles for error messages
2. Verify network connectivity (ping test)
3. Try different network interface name
4. Check firewall settings
5. Review checklist above

---

## 💡 Tips

- **First time:** Use manual IP/port specification rather than auto-detect
- **Testing:** Keep both console windows visible to see packet flow
- **Performance:** Bridge mode has best performance for packet capture
- **Security:** This setup is more secure than modifying hosts file
- **Debugging:** Enable verbose logging in sender with `-v` flag (if implemented)

