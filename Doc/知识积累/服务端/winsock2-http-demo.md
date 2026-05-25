# WinSock2 Socket 与 HTTP 最小链路说明

## 1. 结论

这份文档整理了本轮围绕 WinSock2、socket、HTTP 请求处理链路的讨论，目标不是把所有网络编程细节一次讲完，而是先建立一条稳定的主模型：

- `WSAStartup` 负责初始化 WinSock 运行环境
- `socket` 负责创建网络通信端点
- `bind` 负责把 socket 绑定到具体 IP 和端口
- `listen` 负责把它切换成监听状态
- `accept` 负责从监听 socket 上接入一个真实客户端连接
- `recv` 负责从这条连接里读取原始字节流
- HTTP 请求和响应本质上都是 TCP 字节流上的应用层协议文本
- `send` 负责发回响应，但不保证一次发完整，所以常见做法是封装一个 `SendAll`

如果从 C# / Unity 视角过来，这份文档最重要的不是背 API 名字，而是把“网络端点、监听、连接、字节流、协议格式”这几个概念串成闭环。

## 2. 主链路概览

当前最小示例的主链路是：

```text
main
-> WSAStartup
-> socket
-> bind
-> listen
-> accept
-> recv
-> 组装 HTTP 响应
-> send / SendAll
-> closesocket
-> WSACleanup
```

示例代码位于：

- [`main.cpp`](E:/UnityProgram/FPSResearch/example/winsock2_http_demo/main.cpp)
- [`build.ps1`](E:/UnityProgram/FPSResearch/example/winsock2_http_demo/build.ps1)

这个示例只处理一个请求后退出，目的是把行为收敛到最小闭环，便于观察每一步的职责。

## 3. 关键对象与 API

### 3.1 `WSAStartup` / `WSACleanup`

`WSAStartup` 的作用是初始化 WinSock 运行环境。在 Windows 下，不是拿到 `<winsock2.h>` 就能直接开始 `socket()`，而是要先显式告诉系统“我要使用这套网络 API 了”。

典型写法：

```cpp
WSADATA wsaData = {};
if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
    // 初始化失败
}
```

关键理解：

- `MAKEWORD(2, 2)` 表示请求 WinSock 2.2
- `&wsaData` 是给 API 写回初始化信息的输出参数
- 返回 `0` 表示成功，非 `0` 表示失败

`WSACleanup` 是对应的清理动作，和 `WSAStartup` 成对出现。

### 3.2 `SOCKET`

`SOCKET` 是 WinSock2 定义的 socket 句柄类型。它本质上是“网络通信端点”的句柄，而不是一个高层协议对象。

在当前示例中有两种角色：

- 监听 socket：负责在端口上等待新连接
- 客户端 socket：负责和某个具体客户端收发数据

### 3.3 `socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)`

这句表示创建一个基于 IPv4 的 TCP 流式 socket。

三个参数的含义：

- `AF_INET`：地址族，表示 IPv4
- `SOCK_STREAM`：流式 socket，通常对应 TCP
- `IPPROTO_TCP`：使用 TCP 协议

整体理解就是：

```cpp
SOCKET listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
```

= 创建一个 IPv4 TCP socket。

如果返回 `INVALID_SOCKET`，表示创建失败。

### 3.4 `sockaddr_in`

`sockaddr_in` 是 IPv4 地址结构体，用来描述：

- 地址族
- 端口
- IP 地址

典型写法：

```cpp
sockaddr_in serverAddress = {};
serverAddress.sin_family = AF_INET;
serverAddress.sin_port = htons(port);
serverAddress.sin_addr.s_addr = htonl(INADDR_ANY);
```

关键点：

- `htons(port)`：把端口转成网络字节序
- `htonl(INADDR_ANY)`：表示监听本机所有网卡地址

### 3.5 `bind`

`bind` 的作用不是开始监听，而是先把 socket 和具体地址绑定起来。

它的业务含义是：

- 这台服务器要占用哪个 IP
- 要占用哪个端口

可以理解成“给这个 socket 办号码”。

### 3.6 `listen`

`listen` 把已经绑定地址的 socket 切换成监听状态。到这一步，服务端才真正进入“等待客户端连接”的状态。

可以理解成：

- `socket`：造了一部电话
- `bind`：办了号码
- `listen`：电话开始开机等来电

### 3.7 `accept`

`accept` 不是读取 HTTP 请求，而是从监听 socket 上接入一个真实客户端连接，并返回一个新的客户端 socket。

这一步后要明确区分两个角色：

- `listenSocket`：继续代表服务器门口
- `clientSocket`：代表和某个具体客户端的通信通道

这是网络服务端里非常重要的概念分离。

### 3.8 `recv`

`recv` 的作用是从 `clientSocket` 这条连接里读取一段原始字节流到本地缓冲区。

HTTP 请求之所以能被打印成文本，不是因为 `recv` 懂 HTTP，而是因为：

- HTTP 本来就是运行在 TCP 字节流上的文本协议

因此你看到的：

```http
GET / HTTP/1.1
Host: 127.0.0.1:8080
...
```

本质上只是“按文本格式组织的一串字节”。

### 3.9 `send` / `SendAll`

`send` 的作用是把字节发到 TCP 连接里。

但有一个关键现实：

- `send` 不保证一次把全部字节发完

因此示例里用一个 `SendAll` 循环调用 `send`，直到整段响应发送完成。

这也是为什么在 [`main.cpp`](E:/UnityProgram/FPSResearch/example/winsock2_http_demo/main.cpp) 里会先定义 `SendAll`，再在主链路中调用它。

## 4. HTTP 响应是怎么拼出来的

HTTP 客户端要的不是“随便一段字符串”，而是一条符合协议格式的响应报文。最小可用响应至少要有：

1. 状态行
2. 若干 Header
3. 一行空行
4. Body

示例里用 `snprintf` 拼成：

```http
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8
Content-Length: ...
Connection: close

Hello from the isolated WinSock2 example!
```

关键理解：

- `Content-Type` 告诉客户端正文是什么类型
- `Content-Length` 告诉客户端正文长度
- 空行是 Header 和 Body 的分隔符
- `Connection: close` 表示当前示例发完后就关闭连接

### 4.1 `snprintf` 这段代码到底在做什么

示例里关键代码是：

```cpp
const int responseLength = snprintf(
    responseBuffer,
    sizeof(responseBuffer),
    "HTTP/1.1 200 OK\r\n"
    "Content-Type: text/plain; charset=utf-8\r\n"
    "Content-Length: %d\r\n"
    "Connection: close\r\n"
    "\r\n"
    "%s",
    bodyLength,
    responseBody);
```

它的作用是：

- 把一整条 HTTP 响应按文本格式写进 `responseBuffer`
- 用 `bodyLength` 填充 `Content-Length`
- 用 `responseBody` 填充真正的响应正文
- 最后返回写入后的总长度 `responseLength`

这意味着后面的 `send` 发出去的不是单独的 `responseBody`，而是“协议头 + 空行 + 正文”拼好的完整响应。

### 4.2 每个字段分别是什么意思

```http
HTTP/1.1 200 OK
```

这是状态行，告诉客户端：

- 协议版本是 HTTP/1.1
- 状态码是 `200`
- 这次请求处理成功

```http
Content-Type: text/plain; charset=utf-8
```

这是内容类型字段，告诉客户端：

- 正文是纯文本 `text/plain`
- 文本按 `utf-8` 编码解释

如果以后返回 JSON，这里通常会变成：

```http
Content-Type: application/json
```

```http
Content-Length: %d
```

这是正文长度字段，告诉客户端：

- Header 后面的正文一共有多少字节

这里之所以要先算：

```cpp
const int bodyLength = static_cast<int>(strlen(responseBody));
```

就是为了把真实正文长度填进这个字段。客户端会用它来判断 body 读到哪里结束。

```http
Connection: close
```

这是连接控制字段，告诉客户端：

- 当前响应发完后，服务端就会关闭这条连接

这和当前示例“一次请求、一次响应、然后退出”的行为是匹配的。

```http
\r\n
```

这行空行非常关键，它不是装饰，而是协议分隔符，表示：

- 上面的 Header 到这里结束
- 下面开始才是 Body

没有这行空行，客户端通常无法正确区分响应头和响应正文。

```http
%s
```

这里最终会被 `responseBody` 替换，表示真正返回给客户端的正文内容。

### 4.3 为什么要用 `\r\n`

HTTP 协议历史上要求每一行以 CRLF 结尾，也就是：

```text
\r\n
```

所以：

- 状态行后面要 `\r\n`
- 每个 Header 行后面也要 `\r\n`
- Header 结束时还要再来一个额外的 `\r\n` 作为空行

这就是为什么这段响应模板看起来会有很多 `\r\n`。

### 4.4 `responseLength` 为什么重要

`snprintf` 返回的是最终拼出来的响应总长度，所以后面发送时要用：

```cpp
SendAll(clientSocket, responseBuffer, responseLength);
```

而不是把整个 `responseBuffer` 数组大小都发出去。这样可以保证：

- 只发送真正有效的响应内容
- 不把缓冲区里无关的剩余空间也发给客户端

## 5. 示例中的易错点

### 5.1 `SendAll` 为什么会“未定义”

这是本轮对话里出现过的一个典型问题。

原因通常是：

- 你在某个 `.cpp` 里调用了 `SendAll`
- 但这个翻译单元里并没有它的声明或定义

在当前工程里，`SendAll` 定义在：

- [`main.cpp`](E:/UnityProgram/FPSResearch/example/winsock2_http_demo/main.cpp)

而且它被放在匿名命名空间中，只对当前文件内部可见。  
所以如果你在 [`http_server.cpp`](E:/UnityProgram/FPSResearch/server/src/http_server.cpp) 里直接调用它，编译器会报未定义。

这类问题的本质不是网络问题，而是 C++ 可见性和翻译单元边界问题。

### 5.2 `recv` 一次不一定收完整请求

当前示例只调用了一次 `recv`，这是教学简化版。真实场景下：

- TCP 是字节流
- 一次 `recv` 不保证刚好收完整个 HTTP 请求

所以生产级 HTTP 服务器通常会循环读，直到把完整请求拼出来。

### 5.3 `send` 一次不一定发完整响应

这也是为什么示例专门写了 `SendAll`。

### 5.4 `listenSocket` 和 `clientSocket` 不要混用

监听 socket 只负责等连接。真正和客户端通信的是 `accept` 返回的 `clientSocket`。

## 6. 如何运行这份学习示例

构建：

```powershell
.\example\winsock2_http_demo\build.ps1
```

运行：

```powershell
.\example\winsock2_http_demo\winsock2_http_demo.exe
```

然后另开终端请求：

```powershell
curl http://127.0.0.1:8080/
```

当前示例处理一个请求后退出，这是刻意保留的最小行为模型。

## 7. 通用网络连接概念模型

这是这份文档最后最值得记住的部分。先不要把它局限在 WinSock2 上，而是抽象成一个更通用的网络连接模型：

### 7.1 第一层：端点

网络程序首先要有通信端点。

- 客户端：通常是“我要连出去”
- 服务端：通常是“我要监听别人连进来”

在 WinSock2 里，这个端点就是 `socket`。

### 7.2 第二层：地址

连接一定发生在地址之上，地址通常由两部分构成：

- IP
- 端口

所以本质上“网络连接”不是抽象地连某个程序，而是连：

```text
某台机器的某个端口
```

### 7.3 第三层：角色分工

服务端里至少要分清两个角色：

- 监听端点：负责接新连接
- 会话端点：负责和某个具体客户端收发数据

这和现实世界里的“前台”和“会话窗口”很像。

### 7.4 第四层：字节流

像 TCP 这种传输层协议，核心职责是：

- 可靠传输
- 顺序传输
- 面向字节流

它不懂 HTTP、JSON、登录、房间、业务对象。它只负责把字节送过去。

### 7.5 第五层：应用层协议

应用层协议是在字节流之上约定：

- 这串字节代表什么
- 从哪里开始，到哪里结束
- 哪部分是头，哪部分是正文
- 状态码和字段怎么表达

HTTP 就是一个典型例子。

### 7.6 第六层：生命周期

一个最小网络请求通常会经过：

1. 初始化运行环境
2. 创建端点
3. 绑定或连接地址
4. 建立连接
5. 收发字节
6. 按协议解释字节
7. 关闭连接
8. 清理资源

这条生命周期不只适用于 WinSock2，很多网络库和平台只是 API 名字不同，但底层概念非常接近。

## 8. 学习建议

如果你已经看懂这份文档，下一步不要急着跳到高并发和复杂协议，建议按这个顺序继续：

1. 把“处理一个请求后退出”改成循环处理多个请求
2. 解析 HTTP 请求行，区分不同路径
3. 把固定响应改成按路径返回不同内容
4. 再逐步引入 JSON、路由和更完整的会话逻辑

这条路线最容易把 socket 基础变成真正可用的服务端能力。

## 9. HTTP ��һ�㵽���ڱ���ʲô

��һ�������׻����ĵ��ǣ�`socket` �����շ��ֽڣ�`HTTP` ����涨����Щ�ֽڸ���ô���͡���

�Ե�ǰ��Ŀ��˵�������Ȱ�һ�� HTTP �������Ŀ飺

- �����У����Ҫ��ʲô
- Header�������������Щ����˵��
- ���У�Header �� Body �ķָ���
- Body�������������Я����ҵ������

���磺

```http
POST /join_room HTTP/1.1
Content-Type: application/json
Content-Length: 39

{"session_id":"S_2","room_id":"ROOM_1"}
```

���������

- `POST` ��ʾ�������ķ���
- `/join_room` ��ʾ���Ҫ���ʵ�ҵ��·��
- `Content-Type` ��ʾ������ JSON
- `Content-Length` ��ʾ���ĳ���
- ���һ�� JSON ���� Body

�ؼ����⣺

- `socket` ������¼���ӷ�������
- `HTTP` Ҳ������Ϸҵ��
- ������ҵ�����壬�ǡ�HTTP ��� + ���Լ������ Body �ֶΡ���ͬ���������

## 10. URL��·����Body��·�ɷֱ���ʲô

��һ��������һ�����⡣

### 10.1 URL ������

URL ��������Ψһ��λһ�������Ŀ�ꡣ  
���磺

```text
http://127.0.0.1:8080/login
```

��ͬʱ�����ˣ�

- `http`���� HTTP Э��ͨ��
- `127.0.0.1`��Ŀ�������Ǳ���
- `8080`��Ŀ��˿��� 8080
- `/login`�����ʷ������ĵ�¼���

���� URL ͬʱ�е���

- ���綨λ���ҵ���̨�������ĸ��˿�
- ҵ��λ�����������ӿ�

### 10.2 ����·�� `/login`��`/join_room` ��ʲô

���������·�������Ͼ���ҵ���������

���磺

- `/login`
- `/create_room`
- `/join_room`
- `/heartbeat`

���ǲ����ļ��������ǵ�ǰ����˶�������Ľӿ�·����  
�ͻ���ͨ��·�������Ҫ���ļ��¡��������ͨ��·��ʶ����θ����Ķ��߼�����

### 10.3 Body ��ʲô

Body �� HTTP �������ģ����������������������������˵�ҵ�������

���磺

```json
{"session_id":"S_2","room_id":"ROOM_1"}
```

�ⲻ��Ϊ�ˡ��Եø�ʽ��ȷ���Ŵ��ڣ�������Ϊ������������Ҫ�ύ��

- ˭�����뷿�䣺`session_id`
- Ҫ�����ĸ����䣺`room_id`

���Կ����������⣺

- ·���ش���Ҫ��ʲô��
- Body �ش��Ҵ�����Щ������

### 10.4 ·��Ϊʲô��Ҫ����

ͬһ�� HTTP ��������ͬʱ�յ��ܶ಻ͬ·�����������磺

- `/login`
- `/create_room`
- `/join_room`
- `/heartbeat`

���Ƕ�Ӧ��ҵ���߼���ȫ��ͬ�����Է���˱�����һ���ַ���

- �ȿ��������� `path`
- ��ת����Ӧ��������

��ǰ first-step prototype ��� [router.cpp](E:/UnityProgram/FPSResearch/example/first_step_server/src/router.cpp) ���ľ�������¡�

## 11. �ͻ��˲���ֻ�ܡ�����Դ����Ҳ���������ύ����

��������Դ���� HTTP ������Ĵ�ͳ˵��������Ҫ��������ɡ��ͻ���ֻ��ȡ��������

�ڵ�ǰ��Ŀ��ͻ�����ȷ���������ύ���ݣ�

### `/login`

�ͻ��˷��ͣ�

```json
{"steam_id":"76561190000000001","display_name":"Alice"}
```

����˸�����Щ���ݴ����Ự��

### `/create_room`

�ͻ��˷��ͣ�

```json
{"session_id":"S_1"}
```

����˸��ݻỰ�������䡣

### `/join_room`

�ͻ��˷��ͣ�

```json
{"session_id":"S_2","room_id":"ROOM_1"}
```

����˸�����Щ��������Ҽ��뷿�䡣

�����ִ� HTTP �ӿ���ͻ��˼Ȼᡰ����ĳ����Դ����Ҳ�ᡰ�ύ���ҵ�������������ݡ���

## 12. Session��Room�������ֱ���ʲô����

�������������ö�Ӧ������������������಻ͬ�����⡣

### 12.1 Session��������ҵ�����״̬

`Session` �������������˭�����ڻ������𡢵�ǰ���ĸ����䡱��

��ǰ [types.h](E:/UnityProgram/FPSResearch/example/first_step_server/src/types.h) ��� `Session` ��¼�ˣ�

- `player_id`
- `session_id`
- `steam_id`
- `display_name`
- `current_room_id`
- `last_heartbeat`

���԰�������ɷ���˸�ÿ���ѵ�¼��ҽ�����һ������״̬����

### 12.2 Room��һ����ҵĹ�ϵ״̬

`Room` �������Щ�����ͬһ�������������˭����

��ǰ `Room` ��¼�ˣ�

- `room_id`
- `owner_player_id`
- `members`

���԰�������ɷ����ά����һ�ŷ����Ա����

### 12.3 �������쳣���ߵĶ��׻���

��������Ϊ�˴�ҵ�����ݣ�����Ϊ���ÿͻ��˶��ڸ��߷���ˣ�

```text
�һ����ţ������ҵ����ߡ�
```

Ϊʲô����ֻ�����˳�����ʱ������һ����Ϣ����

- ��ҿ���ֱ�ӱ���
- ���ܶ���
- ����ǿ��
- ���������������˳�����

���ԣ�

- �����˳���Ϣ�����������뿪
- ���� + ��ʱ�жϣ������쳣�뿪

�������ǻ�����ϵ�����Ƕ�ѡһ��

### 12.4 �ڵ�ǰʵ���������������

��ǰ�ͻ���ʾ��ֻ����ʾ�ˡ���һ�� `/heartbeat`����  
��������������ͨ��Ҫ�ڿͻ��˵�¼�ɹ�����һ��ѭ����

- ÿ�����뷢��һ�� `/heartbeat`
- �����ˢ�� `session.last_heartbeat`
- ����ܾ�û�յ���������ٰ�����Ự�ж�Ϊ��ʱ

Ҳ����˵����ǰ first-step prototype �Ѿ��У�

- �����ӿ�
- `last_heartbeat` �ֶ�

����û����������

- ��ʱ��ʱ���
- ��ʱ����������߳�����

## 13. ������ڲ�����Ϳͻ�����Ӧ�ṹ����һ����

�ⲿ�ֺ�������ᣬ�������ڿ����ͻ��� `ok` �ֶ�ʱ��

### 13.1 Ϊʲô�ͻ�����Ӧ���� `ok`

�ͻ����յ����ǽӿ���Ӧ�����磺

```json
{"ok":true,"room_id":"ROOM_1","owner_player_id":"steam:...","members":["..."]}
```

����� `ok` ��ʾ��

- ��νӿڵ��óɹ���û��

�����ڡ���Ӧ��ʽ������ֶΡ�

### 13.2 Ϊʲô `Room` / `Session` �ṹ����û�� `ok`

��Ϊ `Room` �� `Session` �Ƿ�����ڲ�ҵ����������������ǣ�

- ���䱾����ʲô��
- �Ự������ʲô��

���ǲ���������� HTTP ����ɹ����𡱡�  
���� `ok` ��Ӧ������ `Room` �� `Session` ������������

### 13.3 ��ǰ��Ŀ������ô�����ת����

�� [json_utils.cpp](E:/UnityProgram/FPSResearch/example/first_step_server/src/json_utils.cpp) ���

- `BuildLoginResponse`
- `BuildRoomResponse`
- `BuildHeartbeatResponse`
- `BuildError`

��Щ��������������ǣ�

- �÷�����ڲ�����
- �ٶ��ⲹ�Ͻӿڲ���Ҫ���ֶΣ����� `ok`��`message`
- ���ƴ�ɿͻ��������յ��� JSON

����������ʵ��������ṹ��

- ����ģ�ͣ�`Session`��`Room`
- ����ģ�ͣ��ͻ����յ�����Ӧ JSON

�����㲻��Ҫ��ȫһ����

## 14. Unity �ͻ���һ�����������ʱ����·

��һ��ר�Űѵ�ǰ�ͻ��˽ű��� first-step server ���������������һ������� Unity ������������˴������ٻص� Unity���м䵽�׷�����ʲô��

### 14.1 ��������

��ǰ��Ŀ��һ���������������·�����ȼǳɣ�

```text
Unity �ű�׼�� URL �� JSON
-> UnityWebRequest ���� HTTP ����
-> ����� socket �յ�ԭʼ�ֽ�
-> http_server ��ȡ���� HTTP ����
-> ParseRequest ��� method/path/body
-> Router �� path �ַ�
-> SessionService / RoomService ����ҵ��״̬
-> JsonUtils ƴ����Ӧ JSON
-> http_server �� JSON ���� HTTP ��Ӧ
-> UnityWebRequest �յ���Ӧ
-> JsonUtility �� JSON ת�ɿͻ��˶���
-> �ͻ��˼��������߼�
```

��������Ҫ���ǰ��������ֿ���

- ����㣺�ֽ�����͹�ȥ
- Э��㣺HTTP �������Ӧ��ô��֯
- ҵ��㣺��¼���������ӷ���������ô����

### 14.2 �ͻ�����һ������ʲô

��ǰ�ͻ��˽ű��ڣ�

- [ServerTest.cs](E:/UnityProgram/FPSResearch/Assets/Script/ServerTest.cs)

���統������һ�ε�¼����ʱ���������⼸���£�

1. ׼�� URL

```text
http://127.0.0.1:8080/login
```

��һ��������ǣ�

- ��Ҫ������̨����
- �����ĸ��˿�
- ���ĸ��ӿ�

2. ׼�����������

���磺

```csharp
new LoginRequest
{
    steam_id = playerOneSteamId,
    display_name = playerOneDisplayName
}
```

3. �� `JsonUtility.ToJson(...)` �Ѷ���ת�� JSON �ַ���

���磺

```json
{"steam_id":"76561190000000001","display_name":"Alice"}
```

4. �� `UnityWebRequest` ���� `POST`

�ͻ�����ʱ�������͵��ǣ�

- ������
- Header
- Body

Ҳ����˵��Unity �ͻ��˲���ֻ˵һ�䡰��Ҫ��¼�������ǰ����� HTTP ����Э����֯�ã��ٽ����ײ�����㷢��ȥ��

### 14.3 ������յ��Ĳ��Ƕ��󣬶���ԭʼ HTTP �ı�

����� socket �����յ�����һ��ԭʼ�ֽ�����  
������ı�����������������

```http
POST /login HTTP/1.1
Content-Type: application/json
Content-Length: ...

{"steam_id":"76561190000000001","display_name":"Alice"}
```

����Ҫ�ر��ס��

- ����˲���ֱ���յ� `LoginRequest` ����
- ������յ����� HTTP �ı���Ӧ���ֽ�
- �������н������������ǰѡ�ԭʼ�ı���һ������ԭ�ɽṹ������

### 14.4 `ReceiveHttpRequest` �����������������

�ڣ�

- [http_server.cpp](E:/UnityProgram/FPSResearch/example/first_step_server/src/http_server.cpp)

�`ReceiveHttpRequest(...)` ���������ǣ�

- ѭ�� `recv`
- �ȵȵ� `\r\n\r\n` ���֣�˵�� Header ��������
- �ٶ�ȡ `Content-Length`
- �ж� Body �Ƿ�Ҳ�չ���

����ְ���ǡ���ҵ�񡱣����ǣ�

**�Ȱ�һ������ HTTP ����� TCP �ֽ�����ƴ������**

### 14.5 `ParseRequest` ������ method/path/body

ͬ���ڣ�

- [http_server.cpp](E:/UnityProgram/FPSResearch/example/first_step_server/src/http_server.cpp)

�`ParseRequest(...)` �������ԭʼ�����ɣ�

- `request.method`
- `request.path`
- `request.body`

���磺

- `method = "POST"`
- `path = "/login"`
- `body = "{\"steam_id\":\"76561190000000001\",\"display_name\":\"Alice\"}"`

����һ����ʼ����������ڲ���ֻ�����һ����ԭʼ�ַ����������õ���һ���ṹ���� `HttpRequest`��

### 14.6 `Router` �����жϡ���θ�������ҵ��

�ڣ�

- [router.cpp](E:/UnityProgram/FPSResearch/example/first_step_server/src/router.cpp)

�`Route(...)` ���ȿ���

- `request.method`
- `request.path`

Ȼ�������ƣ�

```cpp
if (request.path == "/login") {
    return HandleLogin(request);
}
```

���� Router ��ְ���ǣ�

**��·����Э�������ַ�����Ӧҵ����ڡ�**

��һ��֮�󣬷���˲ſ�ʼ����������¼���������ӷ�����������Щҵ������

### 14.7 Service ��������޸ķ����״̬

��ǰ first-step prototype ���ҵ��״̬��Ҫ������ service �

- [session_service.cpp](E:/UnityProgram/FPSResearch/example/first_step_server/src/session_service.cpp)
- [room_service.cpp](E:/UnityProgram/FPSResearch/example/first_step_server/src/room_service.cpp)

���Ƿֱ���

- `SessionService`
  - �����Ự
  - ��ѯ�Ự
  - ˢ������

- `RoomService`
  - ��������
  - ��ѯ����
  - ���뷿��

���� `/login` ʱ��

- Router �� `body` ���ó� `steam_id`��`display_name`
- �� `SessionService::CreateSession(...)`
- ������ڴ�����һ�� `Session`

���� `/join_room` ʱ��

- Router �� `body` ���ó� `session_id`��`room_id`
- ��У�� session
- �ٵ� `RoomService::JoinRoom(...)`
- ����˷���״̬�����仯

������������״̬���ĵط����� `http_server`������ service �㡣

### 14.8 `JsonUtils` �����ҵ����ƴ����Ӧ JSON

�ڣ�

- [json_utils.cpp](E:/UnityProgram/FPSResearch/example/first_step_server/src/json_utils.cpp)

�����˻��ҵ������ת�ɿͻ����ܶ����� JSON �ı���

���磺

- `BuildLoginResponse(...)`
- `BuildRoomResponse(...)`
- `BuildHeartbeatResponse(...)`
- `BuildError(...)`

�����������ǣ�

- �� `Session` / `Room` ��Щ�ڲ�ҵ�������ȡ����
- ���Ͻӿڲ��ֶΣ����� `ok`
- ƴ��һ����Ӧ JSON

���磺

```json
{"ok":true,"player_id":"steam:76561190000000001","session_id":"S_1"}
```

### 14.9 `http_server` �ٰ� JSON ���� HTTP ��Ӧ

����˲���ֱ�Ӱ� JSON �㷢��ȥ�����ǻ�Ҫ�ٰ�һ�� HTTP ��Ӧ��ʽ��

���磺

```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Content-Length: ...
Connection: close

{"ok":true,"player_id":"steam:...","session_id":"S_1"}
```

Ҳ����˵������˷��ظ��ͻ��˵���ʵ�����ǣ�

- ��㣺HTTP ��Ӧ
- �ڲ㣺JSON ҵ������

### 14.10 Unity �ͻ����յ���Ӧ����ʲô

�ͻ��� `UnityWebRequest` �յ���Ӧ�󣬻��Ȱѣ�

- HTTP ״̬��
- ��Ӧ Body �ı�

�����ű���

Ȼ��ű����ã�

```csharp
JsonUtility.FromJson<T>(body)
```

����Ӧ JSON ת�ɿͻ����Լ�����Ӧ�������磺

- `LoginResponse`
- `RoomResponse`
- `BasicResponse`

��һ��֮�󣬿ͻ��˲����ø���Ȼ�ķ�ʽд�߼������磺

- `playerOneLogin.session_id`
- `createRoomResponse.room_id`
- `joinRoomResponse.members.Length`

### 14.11 �� `/join_room` ���������ٴ�һ��

���԰�һ�� `/join_room` �򻯳�������������

```text
Unity ��װ URL = http://127.0.0.1:8080/join_room
-> Unity ��װ body = {"session_id":"S_2","room_id":"ROOM_1"}
-> UnityWebRequest �� POST
-> ����� recv �յ� HTTP ԭʼ�ֽ�
-> ReceiveHttpRequest ƴ����������
-> ParseRequest �õ� path=/join_room, body=...
-> Router ת���� HandleJoinRoom
-> HandleJoinRoom У�� session_id
-> RoomService::JoinRoom �޸� room.members
-> JsonUtils::BuildRoomResponse ������Ӧ JSON
-> http_server ƴ HTTP ��Ӧ
-> UnityWebRequest �յ���Ӧ body
-> JsonUtility.FromJson<RoomResponse>(...)
-> �ͻ����õ����� members �б�
```

��������ÿһ�㶼ֻ���Լ���һ����£����������������

### 14.12 ��һ������·���ץס�ļ�����

- Unity �ͻ��˷��͵Ĳ��Ƕ����������ǡ��������л���� JSON + HTTP ��ǡ�
- ������յ��Ĳ���ҵ����󣬶���ԭʼ HTTP �ֽ���
- `ReceiveHttpRequest` ����������
- `ParseRequest` ������ֶ�
- `Router` ����·���ַ�
- `Service` ����ά��������ҵ��״̬
- `JsonUtils` �����ҵ����ƴ����Ӧ JSON
- �ͻ�������ٰ� JSON �����л����Լ�����Ӧ����

## 15. һ��ʵ�õķֲ���䷨

���������ֿ����ˣ����Է������������׼��䷨���Լ���������

### �ͻ��˲�

��Ҫ�����ĸ� URL����Ҫ����Щ���������õ���Ӧ��Ҫ��ô������

### HTTP ��

�������������С�Header��Body ��ô��֯����Ӧ��״̬�С�Header��Body ��ô��֯��

### Socket/TCP ��

��Щ�ֽ���ô�ɿ��͵��Է�������ô�ӶԷ��ͻ�����

### ҵ���

��¼���������ӷ��������ֱ���ô�ķ����״̬��

ֻҪ�����Ĳ�ֿ������濴�ͻ��˺ͷ���˵���ϾͲ����ٻ��һ�š�
