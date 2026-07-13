#pragma once

#include <ctime>
#include <string>
#include <vector>

struct Session
{
    std::string player_id;
    std::string session_id;
    std::string steam_id;
    std::string display_name;
    std::string current_room_id;
    std::time_t last_heartbeat;
};

struct Room
{
    std::string room_id;
    std::string owner_player_id;
    std::vector<std::string> members;
};

struct HttpRequest
{
    std::string method;
    std::string path;
    std::string body;
};

struct HttpResponse
{
    int status_code;
    std::string status_text;
    std::string content_type;
    std::string body;
};
