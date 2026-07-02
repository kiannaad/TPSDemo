#pragma once

#include <mutex>
#include <string>
#include <unordered_map>

#include "types.h"

class RoomService
{
public:
    Room CreateRoom(const std::string& owner_player_id);
    Room* GetRoom(const std::string& room_id);
    bool JoinRoom(const std::string& room_id, const std::string& player_id);

private:
    std::mutex mutex_;
    std::unordered_map<std::string, Room> rooms_;
    unsigned int next_room_id_ = 1;
};
