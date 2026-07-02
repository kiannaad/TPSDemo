# WinSock2 HTTP Demo

这是一个和现有 `server/` 目录完全隔离的 WinSock2 学习示例。

## 文件说明

- `main.cpp`: 单文件最小 HTTP 服务端示例，带详细注释
- `build.ps1`: 独立构建脚本，只编译当前示例

## 如何构建

在项目根目录执行：

```powershell
.\example\winsock2_http_demo\build.ps1
```

或者先进入目录再执行：

```powershell
cd .\example\winsock2_http_demo
.\build.ps1
```

## 如何运行

构建完成后运行：

```powershell
.\example\winsock2_http_demo\winsock2_http_demo.exe
```

如果你已经在示例目录里：

```powershell
.\winsock2_http_demo.exe
```

然后另开一个终端请求：

```powershell
curl http://127.0.0.1:8080/
```

## 这份示例适合怎么学

建议按下面顺序看：

1. `WSAStartup` / `WSACleanup`
2. `socket` / `bind` / `listen`
3. `accept`
4. `recv`
5. HTTP 响应格式
6. `send`
7. `closesocket`

目标不是直接背 API，而是先把“监听一个端口并回一个 HTTP 响应”的主链路走通。
