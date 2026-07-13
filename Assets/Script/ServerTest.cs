using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerTest : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverBaseUrl = "http://127.0.0.1:8080";

    [Header("Players")]
    [SerializeField] private string playerOneSteamId = "76561190000000001";
    [SerializeField] private string playerOneDisplayName = "Alice";
    [SerializeField] private string playerTwoSteamId = "76561190000000002";
    [SerializeField] private string playerTwoDisplayName = "Bob";

    private void Start()
    {
        StartCoroutine(RunBasicLoop());
    }

    private IEnumerator RunBasicLoop()
    {
        Debug.Log("Starting basic client/server loop test.");

        LoginResponse playerOneLogin = null;
        yield return PostJson(
            "/login",
            new LoginRequest
            {
                steam_id = playerOneSteamId,
                display_name = playerOneDisplayName
            },
            body =>
            {
                playerOneLogin = JsonUtility.FromJson<LoginResponse>(body);
                Debug.Log($"Player one login response: {body}");
            });

        if (!IsSuccessful(playerOneLogin)) {
            yield break;
        }

        RoomResponse createRoomResponse = null;
        yield return PostJson(
            "/create_room",
            new SessionRequest
            {
                session_id = playerOneLogin.session_id
            },
            body =>
            {
                createRoomResponse = JsonUtility.FromJson<RoomResponse>(body);
                Debug.Log($"Create room response: {body}");
            });

        if (!IsSuccessful(createRoomResponse)) {
            yield break;
        }

        LoginResponse playerTwoLogin = null;
        yield return PostJson(
            "/login",
            new LoginRequest
            {
                steam_id = playerTwoSteamId,
                display_name = playerTwoDisplayName
            },
            body =>
            {
                playerTwoLogin = JsonUtility.FromJson<LoginResponse>(body);
                Debug.Log($"Player two login response: {body}");
            });

        if (!IsSuccessful(playerTwoLogin)) {
            yield break;
        }

        RoomResponse joinRoomResponse = null;
        yield return PostJson(
            "/join_room",
            new JoinRoomRequest
            {
                session_id = playerTwoLogin.session_id,
                room_id = createRoomResponse.room_id
            },
            body =>
            {
                joinRoomResponse = JsonUtility.FromJson<RoomResponse>(body);
                Debug.Log($"Join room response: {body}");
            });

        if (!IsSuccessful(joinRoomResponse)) {
            yield break;
        }

        BasicResponse heartbeatResponse = null;
        yield return PostJson(
            "/heartbeat",
            new SessionRequest
            {
                session_id = playerOneLogin.session_id
            },
            body =>
            {
                heartbeatResponse = JsonUtility.FromJson<BasicResponse>(body);
                Debug.Log($"Heartbeat response: {body}");
            });

        if (!IsSuccessful(heartbeatResponse)) {
            yield break;
        }

        int memberCount = joinRoomResponse.members == null ? 0 : joinRoomResponse.members.Length;
        Debug.Log("Basic loop finished successfully.");
        Debug.Log($"Room {joinRoomResponse.room_id} now has {memberCount} members.");
    }

    private IEnumerator PostJson(string route, object payload, Action<string> onSuccess)
    {
        string requestBody = JsonUtility.ToJson(payload);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(serverBaseUrl + route, UnityWebRequest.kHttpVerbPOST)) {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"POST {route} with body: {requestBody}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Request to {route} failed: {request.error}");
                yield break;
            }

            if (request.responseCode < 200 || request.responseCode >= 300) {
                Debug.LogError($"Request to {route} returned HTTP {request.responseCode}: {request.downloadHandler.text}");
                yield break;
            }

            onSuccess?.Invoke(request.downloadHandler.text);
        }
    }

    private bool IsSuccessful(BasicResponse response)
    {
        if (response == null) {
            Debug.LogError("Response was null.");
            return false;
        }

        if (!response.ok) {
            string message = string.IsNullOrEmpty(response.message) ? "Unknown server error." : response.message;
            Debug.LogError($"Server returned failure: {message}");
            return false;
        }

        return true;
    }

    private bool IsSuccessful(LoginResponse response)
    {
        if (response == null) {
            Debug.LogError("Response was null.");
            return false;
        }

        if (!response.ok) {
            string message = string.IsNullOrEmpty(response.message) ? "Unknown server error." : response.message;
            Debug.LogError($"Server returned failure: {message}");
            return false;
        }

        return true;
    }

    private bool IsSuccessful(RoomResponse response)
    {
        if (response == null) {
            Debug.LogError("Response was null.");
            return false;
        }

        if (!response.ok) {
            string message = string.IsNullOrEmpty(response.message) ? "Unknown server error." : response.message;
            Debug.LogError($"Server returned failure: {message}");
            return false;
        }

        return true;
    }

    [Serializable]
    private class LoginRequest
    {
        public string steam_id;
        public string display_name;
    }

    [Serializable]
    private class SessionRequest
    {
        public string session_id;
    }

    [Serializable]
    private class JoinRoomRequest
    {
        public string session_id;
        public string room_id;
    }

    [Serializable]
    private class BasicResponse
    {
        public bool ok;
        public string message;
    }

    [Serializable]
    private class LoginResponse
    {
        public bool ok;
        public string message;
        public string player_id;
        public string session_id;
    }

    [Serializable]
    private class RoomResponse
    {
        public bool ok;
        public string message;
        public string room_id;
        public string owner_player_id;
        public string[] members;
    }
}
