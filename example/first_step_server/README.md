# First-Step Prototype Server

这是一个完全隔离在 `example/first_step_server` 下的第一阶段原型，用来对应 `server/first-step-guide.md` 里的最小 Gate/Room 服务端闭环。

## 目录结构

```text
example/first_step_server/
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

## 这版实现了什么

- `POST /login`
- `POST /create_room`
- `POST /join_room`
- `POST /heartbeat`

同时保留了 guide 里的第一阶段核心对象：

- `player_id`
- `session_id`
- `room_id`
- 房间成员列表
- 最小请求/响应协议

## 如何构建

```powershell
.\example\first_step_server\build.ps1
```

## 如何运行

```powershell
.\example\first_step_server\bin\first_step_server.exe
```

程序会先做一次内存态 smoke test，然后开始监听 `8080` 端口。

## 如何验证接口

### 1. login

```powershell
curl -X POST http://127.0.0.1:8080/login -H "Content-Type: application/json" -d "{\"steam_id\":\"76561190000000001\",\"display_name\":\"Alice\"}"
```

### 2. create_room

先从 `/login` 返回里拿到 `session_id`，再请求：

```powershell
curl -X POST http://127.0.0.1:8080/create_room -H "Content-Type: application/json" -d "{\"session_id\":\"S_3\"}"
```

### 3. join_room

第二个玩家登录后，拿到它的 `session_id` 再请求：

```powershell
curl -X POST http://127.0.0.1:8080/join_room -H "Content-Type: application/json" -d "{\"session_id\":\"S_4\",\"room_id\":\"ROOM_2\"}"
```

### 4. heartbeat

```powershell
curl -X POST http://127.0.0.1:8080/heartbeat -H "Content-Type: application/json" -d "{\"session_id\":\"S_3\"}"
```

## 这版为什么适合跟着做

- 服务端对象模型和 guide 一致
- HTTP 壳、路由、状态服务、房间服务已经拆开
- JSON 读写保持最小，不额外引入第三方库
- 所有代码都在 `example` 下，不会污染你的原项目
