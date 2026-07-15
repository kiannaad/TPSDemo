namespace CGame
{
    public interface ICameraImpulseReceiver
    {
        void ApplyingCameraImpulse(CameraImpulseRequest request);
        void ClearingCameraImpulse();
    }
}
