#include "session_service.h"

#include <ctime>

Session SessionService::CreateSession(const std::string& steam_id, const std::string& display_name)
{
    std::lock_guard<std::mutex> lock(mutex_);

    Session session = {};
    session.steam_id = steam_id;
    session.display_name = display_name;
    session.player_id = "steam:" + steam_id;
    session.session_id = "S_" + std::to_string(next_session_id_++);
    session.current_room_id = "";
    session.last_heartbeat = std::time(nullptr);

    sessions_[session.session_id] = session;
    return session;
}

Session* SessionService::GetSession(const std::string& session_id)
{
    std::lock_guard<std::mutex> lock(mutex_);

    const auto it = sessions_.find(session_id);
    if (it == sessions_.end()) {
        return nullptr;
    }

    return &it->second;
}

bool SessionService::TouchSession(const std::string& session_id)
{
    std::lock_guard<std::mutex> lock(mutex_);

    const auto it = sessions_.find(session_id);
    if (it == sessions_.end()) {
        return false;
    }

    it->second.last_heartbeat = std::time(nullptr);
    return true;
}
