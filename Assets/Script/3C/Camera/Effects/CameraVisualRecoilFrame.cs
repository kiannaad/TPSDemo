namespace CGame
{
    public readonly struct CameraVisualRecoilFrame
    {
        public CameraVisualRecoilFrame(CameraPoseDelta cameraDelta, CameraPoseDelta viewModelDelta)
        {
            CameraDelta = cameraDelta;
            ViewModelDelta = viewModelDelta;
        }

        public CameraPoseDelta CameraDelta { get; }
        public CameraPoseDelta ViewModelDelta { get; }

        public static CameraVisualRecoilFrame None =>
            new CameraVisualRecoilFrame(CameraPoseDelta.None, CameraPoseDelta.None);
    }
}
