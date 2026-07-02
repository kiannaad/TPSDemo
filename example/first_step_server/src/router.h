#pragma once

#include "room_service.h"
#include "session_service.h"
#include "types.h"

class Router
{
public:
    Router(SessionService& session_service, RoomService& room_service);
    HttpResponse Route(const HttpRequest& request);

private:
    HttpResponse HandleLogin(const HttpRequest& request);
    HttpResponse HandleCreateRoom(const HttpRequest& request);
    HttpResponse HandleJoinRoom(const HttpRequest& request);
    HttpResponse HandleHeartbeat(const HttpRequest& request);
    HttpResponse BuildJsonResponse(int status_code, const std::string& status_text, const std::string& body);

    SessionService& session_service_;
    RoomService& room_service_;
};
