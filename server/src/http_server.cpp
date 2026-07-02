#include "http_server.h"

#include <winsock2.h>
#include <ws2tcpip.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

namespace
{
    void LogLastError(const char* step)
    {
        printf("%s failed, WSA error = %d\n", step, WSAGetLastError());
    }

    bool SendAll(SOCKET socket_handle, const char* buffer, int total_bytes)
    {
        int sent_bytes = 0;

        while (sent_bytes < total_bytes) {
            const int current_sent = send(socket_handle, buffer + sent_bytes, total_bytes - sent_bytes, 0);
            if (current_sent == SOCKET_ERROR) {
                LogLastError("send");
                return false;
            }

            sent_bytes += current_sent;
        }

        return true;
    }

    int GetContentLength(const std::string& raw_request)
    {
        const std::string header_name = "Content-Length:";
        const std::size_t header_pos = raw_request.find(header_name);
        if (header_pos == std::string::npos) {
            return 0;
        }

        const std::size_t value_start = raw_request.find_first_not_of(' ', header_pos + header_name.size());
        if (value_start == std::string::npos) {
            return 0;
        }

        const std::size_t value_end = raw_request.find("\r\n", value_start);
        const std::string value = raw_request.substr(value_start, value_end - value_start);
        return atoi(value.c_str());
    }

    bool ReceiveHttpRequest(SOCKET client_socket, std::string& raw_request)
    {
        char buffer[1024] = {};
        raw_request.clear();

        while (true) {
            const int received_bytes = recv(client_socket, buffer, sizeof(buffer), 0);
            if (received_bytes == SOCKET_ERROR) {
                LogLastError("recv");
                return false;
            }

            if (received_bytes == 0) {
                break;
            }

            raw_request.append(buffer, received_bytes);

            const std::size_t header_end = raw_request.find("\r\n\r\n");
            if (header_end == std::string::npos) {
                continue;
            }

            const int content_length = GetContentLength(raw_request);
            const std::size_t body_start = header_end + 4;
            const std::size_t body_size = raw_request.size() - body_start;

            if (body_size >= static_cast<std::size_t>(content_length)) {
                return true;
            }
        }

        return true;
    }

    HttpRequest ParseRequest(const std::string& raw_request)
    {
        HttpRequest request = {};

        const std::size_t first_line_end = raw_request.find("\r\n");
        if (first_line_end == std::string::npos) {
            return request;
        }

        const std::string first_line = raw_request.substr(0, first_line_end);
        const std::size_t first_space = first_line.find(' ');
        const std::size_t second_space = first_line.find(' ', first_space + 1);

        if (first_space == std::string::npos || second_space == std::string::npos) {
            return request;
        }

        request.method = first_line.substr(0, first_space);
        request.path = first_line.substr(first_space + 1, second_space - first_space - 1);

        const std::size_t body_start = raw_request.find("\r\n\r\n");
        if (body_start != std::string::npos) {
            request.body = raw_request.substr(body_start + 4);
        }

        return request;
    }

    bool BuildHttpResponse(const HttpResponse& response, char* output_buffer, int buffer_size, int& response_length)
    {
        const int body_length = static_cast<int>(response.body.size());
        response_length = snprintf(
            output_buffer,
            buffer_size,
            "HTTP/1.1 %d %s\r\n"
            "Content-Type: %s\r\n"
            "Content-Length: %d\r\n"
            "Connection: close\r\n"
            "\r\n"
            "%s",
            response.status_code,
            response.status_text.c_str(),
            response.content_type.c_str(),
            body_length,
            response.body.c_str());

        return response_length >= 0 && response_length < buffer_size;
    }

    bool HandleClient(SOCKET client_socket, Router& router)
    {
        std::string raw_request;
        if (!ReceiveHttpRequest(client_socket, raw_request)) {
            return false;
        }

        printf("\nReceived request:\n%s\n", raw_request.c_str());

        const HttpRequest request = ParseRequest(raw_request);
        HttpResponse response = {};

        if (request.method.empty() || request.path.empty()) {
            response.status_code = 400;
            response.status_text = "Bad Request";
            response.content_type = "application/json; charset=utf-8";
            response.body = "{\"ok\":false,\"message\":\"Request line is invalid.\"}";
        } else {
            response = router.Route(request);
        }

        char response_buffer[4096] = {};
        int response_length = 0;
        if (!BuildHttpResponse(response, response_buffer, sizeof(response_buffer), response_length)) {
            printf("Failed to build HTTP response.\n");
            return false;
        }

        return SendAll(client_socket, response_buffer, response_length);
    }
}

bool RunHttpServer(unsigned short port, Router& router)
{
    WSADATA wsa_data = {};
    if (WSAStartup(MAKEWORD(2, 2), &wsa_data) != 0) {
        LogLastError("WSAStartup");
        return false;
    }

    SOCKET listen_socket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (listen_socket == INVALID_SOCKET) {
        LogLastError("socket");
        WSACleanup();
        return false;
    }

    sockaddr_in server_address = {};
    server_address.sin_family = AF_INET;
    server_address.sin_port = htons(port);
    server_address.sin_addr.s_addr = htonl(INADDR_ANY);

    if (bind(listen_socket, reinterpret_cast<sockaddr*>(&server_address), sizeof(server_address)) == SOCKET_ERROR) {
        LogLastError("bind");
        closesocket(listen_socket);
        WSACleanup();
        return false;
    }

    if (listen(listen_socket, SOMAXCONN) == SOCKET_ERROR) {
        LogLastError("listen");
        closesocket(listen_socket);
        WSACleanup();
        return false;
    }

    printf("First-step prototype server is listening on http://127.0.0.1:%u\n", port);
    printf("Supported routes: /login, /create_room, /join_room, /heartbeat\n");
    printf("Press Ctrl+C to stop the server.\n");

    while (true) {
        sockaddr_in client_address = {};
        int client_address_length = sizeof(client_address);

        SOCKET client_socket = accept(
            listen_socket,
            reinterpret_cast<sockaddr*>(&client_address),
            &client_address_length);

        if (client_socket == INVALID_SOCKET) {
            LogLastError("accept");
            continue;
        }

        const bool handled = HandleClient(client_socket, router);
        closesocket(client_socket);

        if (!handled) {
            printf("Client request handling failed.\n");
        }
    }
}
