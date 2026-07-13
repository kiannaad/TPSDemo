#pragma once

#include <mutex>
#include <string>
#include <unordered_map>

#include "types.h"

class SessionService
{
public:
    Session CreateSession(const std::string& steam_id, const std::string& display_name);
    Session* GetSession(const std::string& session_id);
    bool TouchSession(const std::string& session_id);

private:
    std::mutex mutex_;
    std::unordered_map<std::string, Session> sessions_;
    unsigned int next_session_id_ = 1;
};
