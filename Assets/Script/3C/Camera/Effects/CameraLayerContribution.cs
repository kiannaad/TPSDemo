namespace CGame
{
    public readonly struct CameraLayerContribution
    {
        public CameraLayerContribution(
            CameraEffectLayer layer,
            CameraPoseDelta poseDelta,
            CameraLensDelta lensDelta,
            bool isEnabled)
        {
            Layer = layer;
            PoseDelta = poseDelta;
            LensDelta = lensDelta;
            IsEnabled = isEnabled;
        }

        public CameraEffectLayer Layer { get; }
        public CameraPoseDelta PoseDelta { get; }
        public CameraLensDelta LensDelta { get; }
        public bool IsEnabled { get; }

        public static CameraLayerContribution Disabled(CameraEffectLayer layer)
        {
            return new CameraLayerContribution(layer, CameraPoseDelta.None, CameraLensDelta.None, false);
        }
    }
}
