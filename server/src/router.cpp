#include "router.h"

#include "json_utils.h"

Router::Router(SessionService& session_service, RoomService& room_service)
    : session_service_(session_service), room_service_(room_service)
{
}

HttpResponse Router::Route(const HttpRequest& request)
{
    if (request.method != "POST") {
        return BuildJsonResponse(405, "Method Not Allowed", JsonUtils::BuildError(false, "Only POST is supported."));
    }

    if (request.path == "/login") {
        return HandleLogin(request);
    }

    if (request.path == "/create_room") {
        return HandleCreateRoom(request);
    }

    if (request.path == "/join_room") {
        return HandleJoinRoom(request);
    }

    if (request.path == "/heartbeat") {
        return HandleHeartbeat(request);
    }

    return BuildJsonResponse(404, "Not Found", JsonUtils::BuildError(false, "Route not found."));
}

HttpResponse Router::HandleLogin(const HttpRequest& request)
{
    const std::string steam_id = JsonUtils::GetString(request.body, "steam_id");
    const std::string display_name = JsonUtils::GetString(request.body, "display_name");

    if (steam_id.empty() || display_name.empty()) {
        return BuildJsonResponse(400, "Bad Request", JsonUtils::BuildError(false, "steam_id or display_name is missing."));
    }

    const Session session = session_service_.CreateSession(steam_id, display_name);
    return BuildJsonResponse(200, "OK", JsonUtils::BuildLoginResponse(session));
}

HttpResponse Router::HandleCreateRoom(const HttpRequest& request)
{
    const std::string session_id = JsonUtils::GetString(request.body, "session_id");
    Session* session = session_service_.GetSession(session_id);
    if (session == nullptr) {
        return BuildJsonResponse(401, "Unauthorized", JsonUtils::BuildError(false, "session_id is invalid."));
    }

    Room room = room_service_.CreateRoom(session->player_id);
    session->current_room_id = room.room_id;
    return BuildJsonResponse(200, "OK", JsonUtils::BuildRoomResponse(room, true));
}

HttpResponse Router::HandleJoinRoom(const HttpRequest& request)
{
    const std::string session_id = JsonUtils::GetString(request.body, "session_id");
    const std::string room_id = JsonUtils::GetString(request.body, "room_id");

    Session* session = session_service_.GetSession(session_id);
    if (session == nullptr) {
        return BuildJsonResponse(401, "Unauthorized", JsonUtils::BuildError(false, "session_id is invalid."));
    }

    if (!room_service_.JoinRoom(room_id, session->player_id)) {
        return BuildJsonResponse(404, "Not Found", JsonUtils::BuildError(false, "room_id is invalid."));
    }

    Room* room = room_service_.GetRoom(room_id);
    if (room == nullptr) {
        return BuildJsonResponse(404, "Not Found", JsonUtils::BuildError(false, "room disappeared unexpectedly."));
    }

    session->current_room_id = room_id;
    return BuildJsonResponse(200, "OK", JsonUtils::BuildRoomResponse(*room, true));
}

HttpResponse Router::HandleHeartbeat(const HttpRequest& request)
{
    const std::string session_id = JsonUtils::GetString(request.body, "session_id");
    const bool ok = session_service_.TouchSession(session_id);

    if (!ok) {
        return BuildJsonResponse(401, "Unauthorized", JsonUtils::BuildError(false, "session_id is invalid."));
    }

    return BuildJsonResponse(200, "OK", JsonUtils::BuildHeartbeatResponse(true));
}

HttpResponse Router::BuildJsonResponse(int status_code, const std::string& status_text, const std::string& body)
{
    HttpResponse response = {};
    response.status_code = status_code;
    response.status_text = status_text;
    response.content_type = "application/json; charset=utf-8";
    response.body = body;
    return response;
}
