namespace CGame
{
    public interface ICameraModeTarget
    {
        CameraPose Pose { get; }
        float FieldOfView { get; }
        bool IsValid { get; }
    }
}
