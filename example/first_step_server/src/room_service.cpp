#include "room_service.h"

#include <algorithm>

Room RoomService::CreateRoom(const std::string& owner_player_id)
{
    std::lock_guard<std::mutex> lock(mutex_);

    Room room = {};
    room.room_id = "ROOM_" + std::to_string(next_room_id_++);
    room.owner_player_id = owner_player_id;
    room.members.push_back(owner_player_id);

    rooms_[room.room_id] = room;
    return room;
}

Room* RoomService::GetRoom(const std::string& room_id)
{
    std::lock_guard<std::mutex> lock(mutex_);

    const auto it = rooms_.find(room_id);
    if (it == rooms_.end()) {
        return nullptr;
    }

    return &it->second;
}

bool RoomService::JoinRoom(const std::string& room_id, const std::string& player_id)
{
    std::lock_guard<std::mutex> lock(mutex_);

    const auto it = rooms_.find(room_id);
    if (it == rooms_.end()) {
        return false;
    }

    Room& room = it->second;
    const auto member_it = std::find(room.members.begin(), room.members.end(), player_id);
    if (member_it == room.members.end()) {
        room.members.push_back(player_id);
    }

    return true;
}
