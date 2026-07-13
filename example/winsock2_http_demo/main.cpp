#include <winsock2.h>
#include <ws2tcpip.h>

#include <stdio.h>
#include <string.h>

namespace
{
    void LogLastError(const char* step)
    {
        printf("%s failed, WSA error = %d\n", step, WSAGetLastError());
    }

    bool SendAll(SOCKET socketHandle, const char* buffer, int totalBytes)
    {
        int sentBytes = 0;

        while (sentBytes < totalBytes) {
            // send 可能不会一次把全部数据发完，所以要循环发送直到完成。
            const int currentSent = send(socketHandle, buffer + sentBytes, totalBytes - sentBytes, 0);
            if (currentSent == SOCKET_ERROR) {
                LogLastError("send");
                return false;
            }

            sentBytes += currentSent;
        }

        return true;
    }
}

int main()
{
    const unsigned short port = 8080;

    WSADATA wsaData = {};
    // Windows 下使用 socket API 前，必须先初始化 WinSock 运行环境。
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        LogLastError("WSAStartup");
        return 1;
    }

    // 创建一个 IPv4 + TCP 的监听 socket。
    SOCKET listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (listenSocket == INVALID_SOCKET) {
        LogLastError("socket");
        WSACleanup();
        return 1;
    }

    sockaddr_in serverAddress = {};
    serverAddress.sin_family = AF_INET;
    // 端口要转成网络字节序。
    serverAddress.sin_port = htons(port);
    // INADDR_ANY 表示监听本机所有网卡地址。
    serverAddress.sin_addr.s_addr = htonl(INADDR_ANY);

    // bind 负责把 socket 和具体的 IP + 端口绑定起来。
    if (bind(listenSocket, reinterpret_cast<sockaddr*>(&serverAddress), sizeof(serverAddress)) == SOCKET_ERROR) {
        LogLastError("bind");
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    // listen 让这个 socket 进入监听状态，开始等待客户端连接。
    if (listen(listenSocket, SOMAXCONN) == SOCKET_ERROR) {
        LogLastError("listen");
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    printf("WinSock2 demo server is listening on http://127.0.0.1:%u\n", port);
    printf("Open the address in your browser or run curl http://127.0.0.1:%u/\n", port);
    printf("This demo handles one request, then exits.\n");

    sockaddr_in clientAddress = {};
    int clientAddressLength = sizeof(clientAddress);

    // accept 会返回一个新的客户端 socket。
    // listenSocket 继续代表“服务器门口”，clientSocket 才是和具体客户端通信的通道。
    SOCKET clientSocket = accept(listenSocket, reinterpret_cast<sockaddr*>(&clientAddress), &clientAddressLength);
    if (clientSocket == INVALID_SOCKET) {
        LogLastError("accept");
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    char requestBuffer[2048] = {};
    // HTTP 请求本质上是 TCP 上的一段文本，这里先原样收下来并打印。
    const int receivedBytes = recv(clientSocket, requestBuffer, sizeof(requestBuffer) - 1, 0);
    if (receivedBytes == SOCKET_ERROR) {
        LogLastError("recv");
        closesocket(clientSocket);
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    requestBuffer[receivedBytes] = '\0';
    printf("\nReceived request:\n%s\n", requestBuffer);

    const char* responseBody =
        "Hello from the isolated WinSock2 example!\n"
        "Read the comments in example/winsock2_http_demo/main.cpp.\n";

    char responseBuffer[512] = {};
    const int bodyLength = static_cast<int>(strlen(responseBody));

    // 一个最小 HTTP 响应至少要有：
    // 1. 状态行
    // 2. 若干 Header
    // 3. 一行空行
    // 4. Body
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

    if (responseLength < 0 || responseLength >= static_cast<int>(sizeof(responseBuffer))) {
        printf("responseBuffer is too small.\n");
        closesocket(clientSocket);
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    if (!SendAll(clientSocket, responseBuffer, responseLength)) {
        closesocket(clientSocket);
        closesocket(listenSocket);
        WSACleanup();
        return 1;
    }

    // WinSock 的 socket 要用 closesocket 关闭，不能用普通文件 API 的 close。
    closesocket(clientSocket);
    closesocket(listenSocket);

    // 整个 WinSock 使用结束后，再做清理。
    WSACleanup();

    printf("Response sent successfully. Server shutdown complete.\n");
    return 0;
}
