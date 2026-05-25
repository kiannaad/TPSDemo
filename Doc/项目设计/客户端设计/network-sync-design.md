# 项目网络同步设计

## 1. 结论

这个项目的网络同步，本质上是一套“服务端权威 + 客户端镜像还原”的设计。

- 客户端负责输入、表现、局部插值和请求发起。
- 服务端负责保存权威状态、执行业务规则、决定世界内有哪些实体可见，并把变化同步给客户端。
- 同步不是客户端之间互发，而是所有客户端都连到服务端，由服务端统一做状态更新和广播。

从客户端代码能确认，这套设计主要由三部分构成：

- `RPC / Notify`：客户端主动请求服务端做事
- `SyncObject / SyncDictionary`：客户端承接服务端下发的同步数据
- `EntityCreate / EntityLeave / FieldMsg`：服务端把世界中的实体和字段变化推给客户端

## 2. 主链路概览

项目里的网络同步主链路可以概括成下面这条：

```text
客户端登录
  -> 连接 NetworkManager
  -> 调用 login RPC
服务端返回
  -> player_data + world_data + player_id + world_id
客户端创建本地主玩家和世界实体
  -> MainPlayerEntity
  -> WorldEntity
运行时交互
  -> 客户端通过 Notify / RPC 请求服务端执行业务
服务端更新权威状态
  -> 创建/删除实体
  -> 修改实体同步字段
服务端回推
  -> EntityCreate / EntityLeave / FieldMsg
客户端落地
  -> EntityManager 找到实体
  -> SyncObject 反序列化字段
  -> UI / 表现监听字段变化刷新
```

最关键的一点是：客户端本地的 `PlayerEntity`、`WorldEntity`、`Farm`、`Fruit` 等对象，不是数据真源，而是服务端权威数据在客户端的一份运行时镜像。

## 3. 关键对象和模块

### 3.1 网络入口

网络入口在 [NetworkManager.cs](/D:/Trunk/Script/Gameplay/Network/NetworkManager.cs)。

它负责：

- 建立连接
- 发送 RPC 请求
- 处理服务端回包
- 把消息分发给 `NetworkRecvHandler`

其中最关键的方法有：

- `CallServer(...)`
- `CallServerWithCallback(...)`
- `CallServerAsync(...)`
- `OnMessage(...)`

这说明业务层并不直接碰底层连接，而是统一经由 `NetworkManager` 发请求。

### 3.2 服务端消息落地入口

服务端主动推给客户端的消息，主要在 [NetworkRecvHandler.cs](/D:/Trunk/Script/Gameplay/Network/NetworkRecvHandler.cs) 里处理。

关键入口有：

- `EntityCreate(...)`
- `EntityLeave(...)`
- `FieldMsg(...)`
- `SwitchWorld(...)`
- `CallEntityMsg(...)`

这几个入口基本定义了“世界里发生变化时，客户端怎么更新”。

### 3.3 同步对象系统

同步对象系统在：

- [SyncObject.cs](/D:/Trunk/Script/Gameplay/Network/SyncField/SyncObject.cs)
- [SyncDictionary.cs](/D:/Trunk/Script/Gameplay/Network/SyncField/SyncDictionary.cs)

这里的设计核心是：

- 普通同步字段用 `[SyncVar]`
- 复杂嵌套对象继承 `SyncObject`
- 集合类同步用 `SyncDictionary<TKey, TValue>`

它们负责把服务端下发的字典结构反序列化到本地对象，并触发字段变化回调。

### 3.4 世界和玩家实体

世界和玩家的本地承载对象分别是：

- [WorldEntity.cs](/D:/Trunk/Script/Gameplay/World/WorldEntity.cs)
- [PlayerEntity.cs](/D:/Trunk/Script/Gameplay/Player/PlayerEntity.cs)

其中：

- `WorldEntity` 持有当前世界里的实体集合和玩家集合
- `PlayerEntity / MainPlayerEntity` 承接玩家的同步数据、表现对象和业务能力

这意味着“联机世界”在客户端并不是一个单独的网络层概念，而是直接还原成了本地实体世界。

### 3.5 登录和重连入口

登录和重连在 [LoginMgr.cs](/D:/Trunk/Script/Gameplay/Login/LoginMgr.cs)。

这里做了几件关键事：

- 连接服务器
- 登录成功后创建 `MainPlayerEntity` 和 `WorldEntity`
- 断线后重连并重新拉取完整快照
- 切世界时销毁旧实体，重建新世界

它是“从无状态进入联机世界”的总入口。

## 4. 时序和数据流

### 4.1 首次登录时的数据同步

首次登录不是一点点补数据，而是服务端先返回完整快照。

从 [LoginMgr.cs](/D:/Trunk/Script/Gameplay/Login/LoginMgr.cs) 可以看到，`login` 成功后客户端会从返回值里取出：

- `world_id`
- `world_data`
- `player_data`
- `player_id`

然后本地做两件事：

1. 创建 `MainPlayerEntity`
2. 创建 `WorldEntity`

再把主玩家放进当前世界。

这说明首登同步采用的是：

- **先快照建世界**
- **再进入增量同步阶段**

这是一种很常见也很稳的设计，因为客户端只要拿到完整快照，就能立刻进入一个自洽状态。

### 4.2 运行时的增量同步

运行中主要靠三类消息增量更新：

#### `EntityCreate`

服务端通知客户端：世界里出现了新实体。  
客户端会：

- 根据实体类型创建本地实体对象
- 让实体进入 `WorldEntity`

目前从客户端代码能明确看到 `PlayerEntity` 的创建分支，见 [NetworkRecvHandler.cs](/D:/Trunk/Script/Gameplay/Network/NetworkRecvHandler.cs)。

#### `EntityLeave`

服务端通知客户端：某个实体离开世界。  
客户端会：

- 从 `WorldEntity` 中移除它
- 再从 `EntityManager` 销毁它

#### `FieldMsg`

服务端通知客户端：某个实体的同步字段有变化。  
客户端会：

- 用 `entityId` 找到本地实体
- 调 `DeserializeSyncObject(data)` 把增量写入本地对象

这是最核心的一步，因为大多数业务状态更新最终都会走到这里。

### 4.3 玩家主动操作如何上行

客户端不是直接改本地状态来“假装成功”，而是先把动作请求发给服务端。

这个入口主要通过 [Entity.Notify.cs](/D:/Trunk/Script/Gameplay/Entity/Entity.Notify.cs) 提供：

- `Notify(...)`
- `NotifyWithCallback(...)`
- `NotifyAsync(...)`

业务层大量这样调用：

- 主玩家移动时 `Notify("cli_sync_pos", ...)`
- 农场操作时调用各种 `NotifyAsync(...)`
- 背包、任务、邮件、商店、交易也都是类似模式

也就是说，客户端的动作语义是“请求服务端处理”，不是“直接提交本地最终结果”。

### 4.4 玩家位置同步

位置同步是理解多人联机最直观的例子。

在 [PlayerEntity.Move.cs](/D:/Trunk/Script/Gameplay/Player/PlayerEntity.Move.cs) 里可以看到：

- 主玩家本地移动后，会调用 `Notify("cli_sync_pos", curPos.x, curPos.y, curPos.z, yaw)`
- 服务端后续再通过服务端消息，把位置变化回给各客户端
- 客户端其他玩家对象通过 `[ServerRpc] SvrSyncPos(...)` 更新位置，并交给 `ActorCtr` 做插值/平滑

这说明：

- 上行是“主玩家把操作结果提交给服务端”
- 下行是“服务端把权威位置广播给相关客户端”
- 表现层通过插值隐藏网络离散感

## 5. 服务端职责 / 客户端职责

### 5.1 服务端职责

从客户端协议和目录结构可以确认，服务端至少承担这些职责：

#### 1. 权威状态持有者

服务端持有：

- 玩家权威数据
- 世界权威数据
- 世界中有哪些实体
- 实体当前同步字段值

证据是登录、重连、切世界时，客户端都要向服务端重新拿 `player_data/world_data`。

#### 2. 业务规则执行者

客户端很多操作只是 `Notify(...)` 发请求，说明真正的业务判断在服务端。

例如：

- 能不能购买
- 能不能收获
- 是否满足任务完成条件
- 当前交易状态如何变化

这些如果都放客户端，会非常容易被篡改，也无法统一多人状态。

#### 3. 世界和房间管理者

服务端决定：

- 当前客户端属于哪个 `world_id`
- 世界里有哪些玩家和实体
- 切世界时应该拉什么数据

#### 4. 同步分发者

服务端负责把变化分发给相关客户端，包括：

- 新实体进入
- 实体离开
- 某实体字段变化

#### 5. 登录与重连恢复者

服务端负责：

- 登录时返回完整状态
- 重连时恢复玩家状态
- 切世界时返回目标世界快照

### 5.2 客户端职责

客户端主要做四类事：

#### 1. 输入与操作发起

客户端负责采集玩家输入，把它翻译成动作请求，再发给服务端。

#### 2. 本地镜像状态承接

客户端用 `SyncObject` 承接服务端下发的权威状态，并把它们还原成本地对象图。

#### 3. 表现与交互

客户端负责：

- 模型和动画
- UI 展示
- 相机
- 音频
- 特效
- 插值和平滑

#### 4. 局部容错体验

例如玩家位置更新后的插值、断线重连后的世界重建，这些都属于客户端体验层责任。

## 6. 如何做到多人联机

这个项目的多人联机不是客户端互连，而是标准的服务端中转和仲裁。

成立条件有四个：

### 1. 所有玩家都连接到同一个服务端世界

登录返回值里有 `world_id`，说明服务端明确维护了“玩家所在世界”。

### 2. 世界内玩家在客户端被还原成 `PlayerEntity`

客户端收到 `EntityCreate` 后，会在本地创建其他玩家实体并挂进 `WorldEntity`。

### 3. 玩家动作先到服务端，再由服务端决定如何广播

例如移动同步走的是：

- 本地玩家上报位置
- 服务端更新权威状态
- 服务端同步给世界内其他客户端

### 4. 每个客户端都维护同一世界的局部镜像

每个客户端并不需要知道所有服务器内部状态，只需要维护“当前世界中和自己相关的镜像状态”。

所以多人联机的本质不是“大家互相发消息”，而是：

**服务端维护一个共享世界，客户端各自持有这个共享世界的局部镜像。**

## 7. 为什么会这样设计

这套设计的核心目标是四个：

### 1. 防止客户端成为权威

如果客户端自己决定最终状态，就会带来：

- 易作弊
- 多人状态不一致
- 重连恢复困难

服务端权威可以从根上解决这些问题。

### 2. 把多人状态统一到一个地方

多人联机最怕的是每个客户端都“以为自己是对的”。  
服务端权威意味着：

- 位置
- 农场对象
- 任务状态
- 交易结果

都只有一个真源。

### 3. 用快照 + 增量降低复杂度

登录/重连/切世界时先给完整快照，运行中再给增量，这样有两个好处：

- 客户端容易进入一个稳定起点
- 运行时同步包更小，不需要一直全量同步

### 4. 让业务层写法统一

在这个项目里，业务层普遍遵循一个统一模式：

- 客户端用 `Notify / NotifyAsync` 发请求
- 服务端改权威状态
- 客户端通过 `FieldMsg / EntityCreate / EntityLeave` 被动收结果

这样业务代码的心智模型比较统一，长期维护成本更低。

## 8. 服务端为什么用 Python

从服务端配置可以确认，服务进程是通过 Python 模块启动的：

- [type.gate.json](/D:/Trunk/server/etc/gops/type.gate.json)
- [type.lobby.json](/D:/Trunk/server/var.chenyujia/template/type.lobby.json)

配置中可以看到：

- `python -m server.gate`
- `python -m server.lobby`

这说明上层服务逻辑至少是 Python 驱动的。

结合目录中的二进制库，可以推断这是“Python 写上层业务，底层能力由原生库承载”的混合架构。这里是推断，不是完整源码级确认，因为当前仓库没有带出完整服务端业务源码。

这种设计常见原因通常有：

- 业务迭代快
- 活动和配置逻辑更适合脚本化
- 服务端上层业务开发效率更高
- 底层性能敏感部分可以交给 C/C++ 动态库

## 9. 如果用 C++ 重写服务端是否可行

可行，但难点不在“能不能写 socket”，而在“要不要兼容现有同步协议和世界模型”。

如果目标是锻炼自己，最合理的做法不是全量重写，而是做一个最小可运行版本：

1. 支持登录并返回 `player_data/world_data`
2. 支持玩家进入世界
3. 支持位置上报与广播
4. 支持 `EntityCreate / EntityLeave / FieldMsg`

只要这条链打通，你就已经把这个项目网络同步的核心骨架练到了。

## 10. 风险 / 未确认点

当前仓库里，服务端部署配置和运行环境是可见的，但完整服务端业务源码没有完整带出，所以有几件事不能在这份文档里做“源码级百分百确认”：

- `gate` 和 `lobby` 的内部职责边界
- 服务端广播范围的具体实现
- 世界分片、房间迁移的具体服务端代码细节
- Python 与原生动态库之间的调用边界

但基于客户端协议、登录流程、同步对象设计和服务端启动配置，下面这些结论是稳的：

- 这是服务端权威模型
- 这是快照 + 增量同步模型
- 多人联机依赖服务端统一管理世界内实体与字段变化
- 客户端负责镜像还原和表现，不负责最终裁决
