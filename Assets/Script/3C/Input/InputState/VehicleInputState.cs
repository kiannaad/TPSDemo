using UnityEngine;

namespace CGame
{
    public struct VehicleInputState
    {
        public Vector2 SteerInput;
        public float   ThrottleValue;
        public bool    BrakeHeld;
        public bool    ExitPressed;
    }
}
