#include <stdio.h>

#include "http_server.h"
#include "room_service.h"
#include "session_service.h"

namespace
{
    void RunStateSmokeTest(SessionService& session_service, RoomService& room_service)
    {
        const Session alice = session_service.CreateSession("76561190000000001", "Alice");
        const Session bob = session_service.CreateSession("76561190000000002", "Bob");
        const Room room = room_service.CreateRoom(alice.player_id);
        const bool join_ok = room_service.JoinRoom(room.room_id, bob.player_id);
        const Room* verified_room = room_service.GetRoom(room.room_id);

        printf("Smoke test:\n");
        printf("  Alice session: %s\n", alice.session_id.c_str());
        printf("  Bob session: %s\n", bob.session_id.c_str());
        printf("  Room created: %s\n", room.room_id.c_str());
        printf("  Bob joined: %s\n", join_ok ? "true" : "false");
        printf("  Members count: %d\n\n", verified_room == nullptr ? 0 : static_cast<int>(verified_room->members.size()));
    }
}

int main()
{
    SessionService session_service = {};
    RoomService room_service = {};
    Router router(session_service, room_service);

    RunStateSmokeTest(session_service, room_service);
    return RunHttpServer(8080, router) ? 0 : 1;
}
