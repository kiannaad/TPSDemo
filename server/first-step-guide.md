# 第一阶段服务端起步指南

## 这一步的目标

这一阶段的核心目标不是直接做完整 Steam 联机服务端，而是先亲手完成一个最小、可验证、可扩展的 C++ 服务端闭环。

当前阶段的重点有两个：

1. 自己把 C++ 服务端从 0 到 1 写出来。
2. 顺便熟悉编译、依赖、调试、协议、Unity 对接这些基础环境。

## 为什么先做最小闭环

第一阶段先不追求大而全，而是先稳定下面这些核心对象：

- `player_id`
- `session_id`
- `room_id`
- 房间成员列表
- 请求/响应协议

这些对象一旦稳定，后面无论是把 HTTP 换成 WebSocket，还是把 mock Steam 换成真实 Steam Ticket 校验，都只是替换实现，不需要推倒重来。

## 第一阶段做什么

先写一个最小 `Gate/Room` 原型服务端，先用 HTTP 跑通以下能力：

- `login`
- `create_room`
- `join_room`
- `heartbeat`

## 第一阶段先不做什么

这一阶段先不要做下面这些内容：

- 真 Steam Ticket 校验
- WebSocket / KCP
- 数据库
- 帧同步
- 复杂多线程

## 这一步的可验证结果

写完这一阶段后，应该能验证下面 4 个结果：

1. 客户端或 Postman 能调用 `login`，拿到 `player_id/session_id`
2. 同一个玩家能 `create_room`
3. 第二个玩家能 `join_room`
4. `heartbeat` 能刷新会话，服务端内存里能看到状态变化

## 第一阶段建议使用的库

如果目标是“自己写代码，同时熟悉环境”，建议第一版依赖尽量少：

- `g++`
  - 作用：先把编译环境跑通
- `winsock2`
  - 作用：Windows 下最底层 TCP/HTTP 通信
- `std::thread / std::mutex / std::unordered_map`
  - 作用：先把并发和内存态房间管理写起来

## 第一阶段暂时不要上的库

这一步先不要急着引入这些库：

- `boost::asio`
- `protobuf`
- `nlohmann/json`
- `Steamworks SDK`
- `Redis/Mongo` 驱动

原因不是这些库不好，而是这一步的主任务不是搭豪华技术栈，而是先把最小服务端亲手打通。

## 建议目录结构

```text
server/
  src/
    main.cpp
    http_server.h
    http_server.cpp
    router.h
    router.cpp
    session_service.h
    session_service.cpp
    room_service.h
    room_service.cpp
    json_utils.h
    json_utils.cpp
    types.h
  bin/
  build.ps1
  README.md
```

## 每个文件负责什么

- `main.cpp`
  - 负责启动服务、监听端口、组装各模块
- `http_server.*`
  - 负责收 HTTP 请求、发 HTTP 响应，不放业务逻辑
- `router.*`
  - 负责把 `/login`、`/create_room` 等路径分发到对应业务函数
- `session_service.*`
  - 负责 `player_id`、`session_id`、心跳时间、重连令牌
- `room_service.*`
  - 负责建房、加房、离房、房间成员列表
- `json_utils.*`
  - 先做最小 JSON 读写，够当前 4 个接口用
- `types.h`
  - 放公共结构体，比如 `Session`、`Room`、`HttpRequest`、`HttpResponse`

## 第一版 4 个接口

### 1. POST /login

```json
req:
{
  "steam_id": "7656119...",
  "display_name": "Alice"
}

resp:
{
  "ok": true,
  "player_id": "steam:7656119...",
  "session_id": "S_xxx"
}
```

### 2. POST /create_room

```json
req:
{
  "session_id": "S_xxx"
}

resp:
{
  "ok": true,
  "room_id": "ROOM_xxx",
  "owner_player_id": "steam:7656119...",
  "members": ["steam:7656119..."]
}
```

### 3. POST /join_room

```json
req:
{
  "session_id": "S_xxx",
  "room_id": "ROOM_xxx"
}

resp:
{
  "ok": true,
  "room_id": "ROOM_xxx",
  "owner_player_id": "steam:7656119...",
  "members": ["steam:a", "steam:b"]
}
```

### 4. POST /heartbeat

```json
req:
{
  "session_id": "S_xxx"
}

resp:
{
  "ok": true
}
```

## 第一轮建议先写什么

第一轮先只写 3 个东西，不要贪多：

1. `types.h`
2. `session_service.h/.cpp`
3. `room_service.h/.cpp`

原因：

- 这一阶段先把“数据和状态”写稳
- HTTP 和路由只是壳，后面很好补
- 真正难的是先把对象关系想清楚

## 建议先定义的结构体

```cpp
struct Session {
    std::string player_id;
    std::string session_id;
    std::string steam_id;
    std::string display_name;
    std::string current_room_id;
    std::time_t last_heartbeat;
};

struct Room {
    std::string room_id;
    std::string owner_player_id;
    std::vector<std::string> members;
};
```

## 建议先实现的函数

### SessionService

```cpp
Session CreateSession(const std::string& steamId, const std::string& displayName);
Session* GetSession(const std::string& sessionId);
bool TouchSession(const std::string& sessionId);
```

### RoomService

```cpp
Room* CreateRoom(const std::string& ownerPlayerId);
Room* GetRoom(const std::string& roomId);
bool JoinRoom(const std::string& roomId, const std::string& playerId);
```

## 这一步的验收标准

哪怕这一轮还没有 HTTP，也应该能在 `main.cpp` 里手动调用这些函数，打印出：

- 创建 session 成功
- 创建 room 成功
- 第二个玩家 join 成功
- `members` 数组正确

## 当前推荐推进顺序

按下面顺序一步一步来：

1. 先写 `types.h`
2. 再写 `session_service`
3. 再写 `room_service`
4. 然后在 `main.cpp` 里手动调用验证
5. 等状态层稳定，再补 HTTP 壳

## 配合方式

后续协作方式以“带着写”为主：

1. 先讨论当前这一步的目标和可验证结果
2. 再明确需要哪些库以及为什么需要
3. 你自己动手写代码
4. 遇到问题时，再针对当前这一步给出必要提示、排错建议和收口建议

