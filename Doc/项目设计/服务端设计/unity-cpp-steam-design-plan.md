# Unity + C++ + Steam 最终设计方案

## 1. 结论

项目后续按“两条主线、四个阶段”推进：

- 主线 1：最小在线闭环
  - 先把 `登录 -> 建房 -> 加房 -> 心跳 -> 断线重连` 跑通
- 主线 2：FPS 权威同步闭环
  - 在房间基础上，再做 `输入上传 -> 服务端权威推进 -> 快照广播 -> Hitscan 判定 -> 伤害/死亡同步`

当前固定方向如下：

- 客户端：Unity C#
- 服务端：C++
- 上线目标：Steam
- 同步模型：权威服务端
- 第一版战斗模型：Hitscan
- 非实时链路：HTTP
- 实时链路：长连接
- 部署目标：远程云服，不依赖本地笔记本常驻

当前工程状态：

- Unity 客户端目前还很轻，联网玩法层尚未成型
- `server/` 下已有最小 C++ 服务端原型，可继续演进
- 当前不适合直接做“大而全联机框架”，而应先完成主干闭环

## 2. 整体结构

后续系统分成两层链路、四层能力。

### 2.1 两层链路

#### 非实时链路

负责会话和房间生命周期，先继续用 HTTP：

- `POST /login`
- `POST /create_room`
- `POST /join_room`
- `POST /heartbeat`
- `POST /reconnect`

这层负责：

- 登录
- 建房/加房
- 在线状态维持
- 断线恢复
- 比赛开始前的组织工作

#### 实时链路

负责 FPS 规则同步，单独使用长连接：

- 客户端持续发送输入命令
- 服务端推进权威玩家状态
- 服务端广播世界快照
- 服务端执行 hitscan 判定
- 服务端广播伤害/死亡事件

### 2.2 四层能力

#### 传输层

- 非实时接口继续走 HTTP，便于快速调试
- 实时同步新增长连接通道，第一版优先 WebSocket
- 实时消息统一信封：`msg_type + room_id + player_id + tick + payload`

#### 同步层

- 服务端按固定 tick 推进世界状态
- 客户端上传输入，不上传最终权威位置
- 服务端保存玩家最近状态与输入确认信息
- 服务端按固定频率广播快照
- 客户端本地玩家做预测，远端玩家做插值

#### 规则层

- 第一版只支持 hitscan
- 命中、伤害、死亡全部由服务端决定
- 客户端只负责输入和表现反馈
- 房间比赛状态保持最小化：未开始、进行中、结束

#### 表现层

- 本地枪口火焰、后坐力、击中特效可立即播放
- 最终伤害和命中结果以服务端为准
- 本地玩家被服务端校正时做平滑回正
- UI 只消费服务端确认后的生命值和死亡状态

## 3. 分层定义

### 3.1 客户端层

客户端负责：

- 输入采集
- 镜头和移动表现
- 本地动画、UI、音效
- 非实时接口调用
- 实时输入上传
- 远端玩家表现平滑

客户端不负责：

- 最终命中判定
- 最终伤害计算
- 房间权威成员状态
- 最终在线状态判断

### 3.2 服务端层

服务端负责：

- Session 生命周期
- Room 生命周期
- 玩家权威状态
- 输入消费和世界推进
- Hitscan 命中判定
- 伤害和死亡广播

服务端不负责：

- 画面表现
- 本地输入体验
- 客户端即时视觉反馈的最终呈现

### 3.3 运维层

运维层负责：

- 云服务器部署
- 配置分离
- 进程守护
- 日志采集
- 发版与回滚

运维层不依赖本地笔记本常驻在线。

## 4. 核心模块职责

### 4.1 Session

`Session` 表示一个已登录客户端在服务端上的会话状态。

最小字段建议：

- `player_id`
- `session_id`
- `display_name`
- `current_room_id`
- `last_heartbeat`

职责：

- 登录成功后创建
- 后续请求身份归属
- 心跳保活
- 断线恢复

### 4.2 Room

`Room` 是组织一组玩家关系的业务边界。

最小字段建议：

- `room_id`
- `owner_player_id`
- `members`
- `match_state`

职责：

- 建房
- 加房
- 离房
- 比赛开始前的上下文组织

### 4.3 InputCommand

`InputCommand` 是客户端某个 tick 的输入快照。

最小字段建议：

- `client_tick`
- 移动输入
- 视角输入
- 跳跃意图
- 开火意图

职责：

- 作为实时同步最小输入单元
- 驱动服务端权威推进

### 4.4 PlayerState

`PlayerState` 是服务端上的权威玩家状态。

最小字段建议：

- 位置
- 朝向
- 速度
- 生命值
- 所属房间
- 最后确认 tick

职责：

- 作为快照广播的核心来源
- 作为命中与伤害计算依据

### 4.5 WorldSnapshot

`WorldSnapshot` 是服务端发给客户端的世界状态摘要。

最小字段建议：

- `server_tick`
- 房间玩家列表
- 玩家位置/朝向/生命值

职责：

- 驱动远端玩家表现
- 修正本地预测误差

## 5. 模块关系 / 依赖方向

推荐后续按下面的依赖方向组织，而不是相互交叉调用：

- 客户端 UI / 表现层
  -> 客户端网络封装层
  -> 非实时接口 / 实时长连接
  -> 服务端接入层
  -> Session / Room / Match / Combat 逻辑

关键原则：

- 业务层依赖传输抽象，不直接依赖底层 socket 细节
- Session 和 Room 是基础对象
- Match 和 Combat 建立在 Session / Room 之上
- 表现层只消费结果，不拥有权威状态

## 6. 关键设计原因

### 6.1 为什么先做最小在线闭环

因为后续所有联机能力都依赖：

- 玩家身份
- 会话状态
- 房间边界
- 在线状态维持

如果这一层没稳，后面实时同步会没有落点。

### 6.2 为什么选权威服务端

因为后续目标是 Steam 上线和长期维护。

权威服务端方案更适合：

- 状态一致性
- 命中判定统一
- 日后做基础反作弊
- 远程正式服部署

### 6.3 为什么第一版选 Hitscan

因为 hitscan 最适合先验证：

- 输入同步
- 朝向同步
- 服务端命中判定
- 伤害广播

相比 projectile，第一版复杂度更低，更适合作为联机闭环的第一战斗模型。

### 6.4 为什么分 HTTP 和实时长连接两层

因为非实时接口和实时同步的诉求不同：

- HTTP 更适合登录、建房、加房、调试
- 长连接更适合持续同步输入和快照

两者分离后，调试和扩展都更清晰。

### 6.5 为什么部署不依赖本地笔记本

因为正式上线后，本地笔记本不能承担：

- 24 小时常驻
- 公网稳定性
- 长期运维

本地机器只负责开发、发版、调试和查看日志，线上服务必须跑在远程机器。

### 6.6 为什么现在继续保留 HTTP，而不是立刻全换 WebSocket

因为当前项目实际上在处理两类不同通信问题：

#### 非实时控制链路

这类请求的特点是：

- 一次请求，对应一次明确响应
- 调用频率低
- 便于用 Postman 和 curl 调试
- 更适合先把业务边界做稳

典型接口：

- `POST /login`
- `POST /create_room`
- `POST /join_room`
- `POST /heartbeat`
- `POST /reconnect`

#### 实时同步链路

这类消息的特点是：

- 高频
- 双向持续传输
- 服务端需要主动推送
- 天然依赖连接状态

典型消息：

- `InputCommand`
- `WorldSnapshot`
- `FireRequest`
- `DamageEvent`
- `DeathEvent`

因此当前方案不是“HTTP 和 WebSocket 二选一”，而是：

- HTTP 继续承担非实时控制面
- WebSocket 以后承担 FPS 实时同步数据面

这样做的原因：

1. 当前先用 HTTP 更容易把 `Session`、`Room`、`player_id`、`session_id` 这些基础对象做稳。
2. HTTP 更适合建立清晰主链路，方便你自己手写和调试。
3. 如果现在一上来把所有能力都搬到 WebSocket，会同时引入长连接管理、消息路由、心跳、重连、连接态绑定等复杂度，不利于当前阶段学习。

结论：

- 现在继续用 HTTP 是为了把非实时业务边界先做稳。
- WebSocket 不是不用，而是应该在“FPS 实时同步”阶段加入。

### 6.7 protobuf 在这个项目里什么时候引入

`protobuf` 不是网络连接方案，而是“客户端和服务端消息结构定义方案”。

它最适合在下面这个时机引入：

- 主要消息类型已经基本确定
- 字段语义已经比较稳定
- 客户端和服务端都开始维护同样的数据结构
- 实时同步消息开始变多，JSON 手写维护成本上升

当前项目的建议时机：

#### 现在不急着上 protobuf

原因：

- 当前还在稳定 `Session`、`Room`、`Heartbeat`、实时同步模型
- 如果现在立刻引入 `protobuf`，会额外引入 `protoc`、C++/C# 生成代码、构建链路和调试成本
- 当前主任务仍然是把主链路和概念边界做稳

#### 更适合引入的阶段

当项目进入“FPS 实时同步闭环”阶段后，再把高频实时消息逐步切到 `protobuf`。

优先适合改成 `protobuf` 的消息：

- `InputCommand`
- `WorldSnapshot`
- `FireRequest`
- `FireResult`
- `DamageEvent`
- `DeathEvent`

暂时仍然适合继续保持 `HTTP + JSON` 的接口：

- `/login`
- `/create_room`
- `/join_room`
- `/leave_room`
- `/heartbeat`
- `/reconnect`

推荐最终形态：

- 控制面：`HTTP + JSON`
- 实时面：`WebSocket + protobuf`

## 7. 后续推进顺序

后续按四个阶段推进，不跳步。

### 阶段 1：最小在线闭环

目标：

- 自己写清楚 `SessionService` 和 `RoomService`
- 跑通 `login/create_room/join_room/heartbeat/reconnect`

产出：

- 最小在线服务端
- 接口定义
- 基础日志链路

### 阶段 2：Unity 联调闭环

目标：

- Unity 真正驱动服务端
- 客户端 UI 正常显示登录态和房间状态

产出：

- Unity 联调面板
- 客户端请求封装
- 一条完整主链路日志

### 阶段 3：FPS 实时同步闭环

目标：

- 房间里的玩家能互相看到、移动、转向、开火、死亡

产出：

- 输入同步
- 快照广播
- Hitscan 判定
- 伤害和死亡广播

### 阶段 4：面向 Steam 上线的工程化

目标：

- 把原型升级成可部署、可维护、可上线的系统

产出：

- 远程测试服
- 最小部署文档
- 发版和日志流程

## 8. 调试与验证方法

当前阶段的最核心能力不是“性能”，而是“可验证”。  
所以所有非实时 HTTP 接口都应该能被下面两种方式直接调试：

- Postman
- curl

### 8.1 用 Postman 调试

适合场景：

- 人工一步步走主链路
- 观察 JSON 响应
- 保存和复用测试请求

推荐调试顺序：

1. `POST /login`
2. `POST /create_room`
3. `POST /join_room`
4. `POST /heartbeat`
5. `POST /reconnect`

#### 示例：登录

请求方式：

- `POST`

URL：

```text
http://127.0.0.1:18080/api/login
```

Header：

```text
Content-Type: application/json
```

Body：

```json
{
  "steam_id": "76561198000000001",
  "steam_ticket": "mock-ticket",
  "display_name": "Alice"
}
```

期望结果：

- 返回 `player_id`
- 返回 `session_id`
- 返回 `reconnect_token`

#### 示例：创建房间

```text
POST http://127.0.0.1:18080/api/rooms/create
```

```json
{
  "session_id": "S_xxx"
}
```

期望结果：

- 返回 `room_id`
- 返回 `owner_player_id`
- 返回 `members`

#### 示例：加入房间

```text
POST http://127.0.0.1:18080/api/rooms/join
```

```json
{
  "session_id": "S_xxx",
  "room_id": "ROOM_xxx"
}
```

#### 示例：心跳

```text
POST http://127.0.0.1:18080/api/heartbeat
```

```json
{
  "session_id": "S_xxx"
}
```

### 8.2 用 curl 调试

适合场景：

- 终端快速验证
- 写入脚本
- 自动化回归

Windows PowerShell 下建议直接调用 `curl.exe`，避免和 PowerShell 自带别名混淆。

#### 登录

```powershell
curl.exe -X POST "http://127.0.0.1:18080/api/login" `
  -H "Content-Type: application/json" `
  -d "{\"steam_id\":\"76561198000000001\",\"steam_ticket\":\"mock-ticket\",\"display_name\":\"Alice\"}"
```

#### 创建房间

```powershell
curl.exe -X POST "http://127.0.0.1:18080/api/rooms/create" `
  -H "Content-Type: application/json" `
  -d "{\"session_id\":\"S_xxx\"}"
```

#### 加入房间

```powershell
curl.exe -X POST "http://127.0.0.1:18080/api/rooms/join" `
  -H "Content-Type: application/json" `
  -d "{\"session_id\":\"S_xxx\",\"room_id\":\"ROOM_xxx\"}"
```

#### 心跳

```powershell
curl.exe -X POST "http://127.0.0.1:18080/api/heartbeat" `
  -H "Content-Type: application/json" `
  -d "{\"session_id\":\"S_xxx\"}"
```

### 8.3 调试时要验证什么

不只是“接口返回 200”，还要核对这几件事：

1. 客户端/工具端请求是否成功发出
2. 服务端日志里是否收到了请求
3. 服务端权威状态是否真的发生了变化
4. 返回 JSON 字段是否符合预期
5. 下一步链路是否拿到了上一步的关键字段

例如：

- 登录后必须拿到 `session_id`
- 建房后必须拿到 `room_id`
- 加房后 `members` 必须变化
- 心跳后服务端的会话活跃时间必须刷新

### 8.4 推荐保留的最小日志

当前阶段建议每条主链路至少保留这几类日志：

- 请求进入日志
- 关键状态变更日志
- 响应返回日志

最小示例：

- `login request received`
- `session created`
- `room created`
- `player joined room`
- `heartbeat updated`

## 9. 阅读和实现建议

后续学习和开发建议保持这个原则：

- 主做自己的 demo
- 辅看现有源码做校准

优先自己实现：

- `Session`
- `Room`
- `InputCommand`
- `PlayerState`
- `WorldSnapshot`
- 登录、建房、心跳、重连
- 最小 FPS 权威同步

暂不优先全手写：

- 真 Steam Ticket 校验
- 数据库驱动
- 完整资源热更新平台
- 大规模分布式架构
- Projectile、回滚网络、复杂反作弊

一句话原则：

先把主链路一段段做通，再逐步提升工程化层级。
