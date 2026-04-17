# Protocol 18 修复 - 测试指南

## 快速测试步骤

### 1. 构建项目
```bash
cmd /c rebuild-quick.bat
```

### 2. 启动测试
1. 启动 Albion Online 游戏
2. 运行 `bin\Debug\VRise.exe`
3. 选择正确的网络适配器（Wi-Fi 或 Ethernet）

### 3. 检查控制台输出

**✅ 成功标志 (Success Indicators):**

```
[PacketDeviceSelector] Using Protocol 18 PhotonParser
[DebugHandler] OnHandleAsync called - packet type: EventPacket
[DebugHandler] Event: 29   ← NewCharacter event
[DebugHandler] Event: 46   ← Leave event
[DebugHandler] Event: 593  ← KeySync event
```

**❌ 错误标志 (Error - 如果还有这些说明修复未生效):**

```
Type code: 217 not implemented
Type code: 74 not implemented
ArgumentException in Protocol18
at Protocol16.Protocol16Deserializer.Deserialize
```

### 4. 功能测试

测试以下功能是否正常工作：

#### A. 地图切换
- [ ] 在游戏中移动到新地图区域
- [ ] 雷达应显示新地图名称
- [ ] 雷达应重置并清除旧实体

#### B. 资源显示
- [ ] 雷达显示资源节点（树木、矿石、纤维等）
- [ ] 资源显示正确的等级 (T1-T8)
- [ ] 资源显示附魔等级 (.0, .1, .2, .3)

#### C. 怪物显示
- [ ] 雷达显示普通怪物
- [ ] 雷达显示精英怪物
- [ ] 雷达显示迷雾怪物（如果在迷雾中）

#### D. 玩家追踪
- [ ] 雷达显示其他玩家
- [ ] 玩家位置实时更新
- [ ] 玩家装备信息显示（如果启用）

## 预期结果

### Protocol 18 修复生效
- **不应再看到** "Type code: X not implemented" 错误
- **应该看到** 正常的事件处理日志
- **雷达应该能够**显示游戏实体

### 如果问题仍然存在

#### 问题 1: 仍然看到 "Type code not implemented" 错误
**原因**: 可能使用了旧版本的 exe

**解决方案**:
```bash
# 检查 exe 的修改时间
ls -lh bin/Debug/VRise.exe

# 应该显示今天的日期和时间
# 如果不是，重新构建：
cmd /c rebuild-quick.bat
```

#### 问题 2: 雷达不显示任何实体
**原因**: 可能 KeySync 未触发或 packet offsets 过期

**解决方案**:
1. 在游戏中切换地图来触发 KeySync
2. 查找控制台消息: `[KeySync] XorCode received!`
3. 如果没有看到，查看 `POSITION_DECRYPTION_FIX.md`

#### 问题 3: 控制台没有任何输出
**原因**: 可能选择了错误的网络适配器

**解决方案**:
1. 重启 VRise.exe
2. 尝试选择不同的网络适配器
3. 确保选择游戏实际使用的适配器（通常是 Wi-Fi 或 Ethernet）

#### 问题 4: "Could not find network adapter" 错误
**原因**: npcap 未正确安装

**解决方案**:
1. 下载 npcap: https://npcap.com/
2. 安装时保持所有默认选项
3. 重启电脑
4. 重新运行 VRise.exe

## 详细文档

如需更多信息，请参考：

- **Protocol 18 修复详解**: `PROTOCOL18_PARSER_FIX.md`
- **鲁棒性修复**: `PROTOCOL18_ROBUSTNESS_FIXES.md`
- **所有修复汇总**: `QUICK_FIX_SUMMARY.md`
- **位置解密问题**: `POSITION_DECRYPTION_FIX.md`

## 修复历史

### 2026-04-16: Protocol 18 Parser Fix
- **问题**: Albion.Network 使用 Protocol 16 deserializer，游戏使用 Protocol 18
- **修复**: 使用自定义 PhotonParser 替代 Albion.Network 的内部解析器
- **文件**: PacketDeviceSelector.cs
- **测试状态**: 等待用户验证

### 之前的修复
1. **opJoin 容错处理** - JoinResponseOperation.cs
2. **字节序规范化** - PhotonProtocolHelper.cs
3. **类型转换安全性** - PhotonProtocolHelper.cs
4. **EventPacket 构造错误** - PacketDeviceSelector.cs (已移除反射代码)
5. **C# 8.0 兼容性** - PhotonProtocolHelper.cs (移除 pattern matching)

---

**修复日期**: 2026-04-16
**当前版本**: Protocol 18 Full Support
**状态**: ✅ 已构建，等待测试
