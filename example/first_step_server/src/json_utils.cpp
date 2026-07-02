#include "json_utils.h"

namespace
{
    std::string EscapeJson(const std::string& value)
    {
        std::string escaped;
        escaped.reserve(value.size());

        for (const char ch : value) {
            switch (ch) {
            case '\\':
                escaped += "\\\\";
                break;
            case '"':
                escaped += "\\\"";
                break;
            case '\n':
                escaped += "\\n";
                break;
            default:
                escaped += ch;
                break;
            }
        }

        return escaped;
    }
}

std::string JsonUtils::GetString(const std::string& json, const std::string& key)
{
    const std::string token = "\"" + key + "\"";
    const std::size_t key_pos = json.find(token);
    if (key_pos == std::string::npos) {
        return "";
    }

    const std::size_t colon_pos = json.find(':', key_pos + token.size());
    if (colon_pos == std::string::npos) {
        return "";
    }

    const std::size_t first_quote = json.find('"', colon_pos + 1);
    if (first_quote == std::string::npos) {
        return "";
    }

    const std::size_t second_quote = json.find('"', first_quote + 1);
    if (second_quote == std::string::npos) {
        return "";
    }

    return json.substr(first_quote + 1, second_quote - first_quote - 1);
}

std::string JsonUtils::BuildError(bool ok, const std::string& message)
{
    return std::string("{\"ok\":") + (ok ? "true" : "false") + ",\"message\":\"" + EscapeJson(message) + "\"}";
}

std::string JsonUtils::BuildMembersArray(const std::vector<std::string>& members)
{
    std::string json = "[";

    for (std::size_t index = 0; index < members.size(); ++index) {
        if (index > 0) {
            json += ",";
        }

        json += "\"" + EscapeJson(members[index]) + "\"";
    }

    json += "]";
    return json;
}

std::string JsonUtils::BuildLoginResponse(const Session& session)
{
    return "{\"ok\":true,\"player_id\":\"" + EscapeJson(session.player_id) +
        "\",\"session_id\":\"" + EscapeJson(session.session_id) + "\"}";
}

std::string JsonUtils::BuildRoomResponse(const Room& room, bool ok)
{
    return std::string("{\"ok\":") + (ok ? "true" : "false") +
        ",\"room_id\":\"" + EscapeJson(room.room_id) +
        "\",\"owner_player_id\":\"" + EscapeJson(room.owner_player_id) +
        "\",\"members\":" + BuildMembersArray(room.members) + "}";
}

std::string JsonUtils::BuildHeartbeatResponse(bool ok)
{
    return std::string("{\"ok\":") + (ok ? "true" : "false") + "}";
}
