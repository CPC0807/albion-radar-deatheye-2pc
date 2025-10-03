# 檢查 Albion Online 使用的端口

## 方法 1：使用命令提示字元（CMD）

### 步驟：

1. **確保 Albion Online 正在運行**

2. **打開命令提示字元（系統管理員）**
   - 按 `Win + X`
   - 選擇「命令提示字元（系統管理員）」或「Windows PowerShell（系統管理員）」

3. **執行以下命令查看端口 5056**:
   ```cmd
   netstat -ano | findstr "5056"
   ```

4. **或者查看所有 UDP 連線**:
   ```cmd
   netstat -ano -p UDP
   ```

5. **找 Albion Online 的進程 ID (PID)**:
   ```cmd
   tasklist | findstr -i "albion"
   ```

6. **然後用 PID 找對應的端口**:
   ```cmd
   netstat -ano | findstr "[找到的PID]"
   ```

### 預期輸出：

如果端口是 5056，應該看到類似：
```
UDP    0.0.0.0:5056           *:*                    [PID]
UDP    [你的IP]:5056          *:*                    [PID]
```

---

## 方法 2：使用 Resource Monitor（資源監視器）

1. 按 `Win + R`，輸入 `resmon`，按 Enter
2. 切換到「網路」標籤
3. 在「網路活動」區段找到 `Albion-Online.exe`
4. 查看「遠端位址」和「遠端連接埠」欄位

---

## 方法 3：使用 TCPView（第三方工具）

1. 下載 [TCPView](https://learn.microsoft.com/en-us/sysinternals/downloads/tcpview)
2. 執行 TCPView.exe（需系統管理員權限）
3. 在列表中找到 `Albion-Online.exe`
4. 查看 `Remote Port` 欄位

---

## 當前 Radar 配置

查看 `bin\Debug\network_config.json`：
```json
{
  "game_port": 5056
}
```

**Radar 目前監聽端口：5056**

---

## 如果端口不是 5056

1. 修改 `bin\Debug\network_config.json`：
   ```json
   {
     "game_port": [你找到的端口號]
   }
   ```

2. 重新啟動 Radar

---

## 注意事項

- Albion Online 使用 **UDP** 協議（不是 TCP）
- 標準端口通常是 **5056** 或 **5055**
- 如果使用 Bridge Mode，確保 VM 和主機在同一網段
- 檢查防火牆是否允許 npcap 捕獲封包
