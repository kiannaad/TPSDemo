namespace CGame
{
    public readonly struct CameraLensState
    {
        public CameraLensState(float fieldOfView)
        {
            FieldOfView = fieldOfView;
        }

        public float FieldOfView { get; }
    }
}
