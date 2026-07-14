namespace CGame
{
    public enum CameraMode
    {
        GameplayFirstPerson = 0,
        Respawn = 100,
        Death = 200,
        Spectator = 300,
        Cinematic = 400
    }

    public enum CameraModeTransition
    {
        Cut,
        EaseInOut
    }
}
