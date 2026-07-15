namespace CGame
{
    public readonly struct WeaponAimState
    {
        public WeaponAimState(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }

        public float Yaw { get; }
        public float Pitch { get; }
    }
}
