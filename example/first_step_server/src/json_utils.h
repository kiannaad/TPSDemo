#pragma once

#include <string>
#include <vector>

#include "types.h"

namespace JsonUtils
{
    std::string GetString(const std::string& json, const std::string& key);
    std::string BuildError(bool ok, const std::string& message);
    std::string BuildLoginResponse(const Session& session);
    std::string BuildRoomResponse(const Room& room, bool ok);
    std::string BuildHeartbeatResponse(bool ok);
    std::string BuildMembersArray(const std::vector<std::string>& members);
}
