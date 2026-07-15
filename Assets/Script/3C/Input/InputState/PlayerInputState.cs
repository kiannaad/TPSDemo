using UnityEngine;

namespace CGame
{
    public struct PlayerInputState
    {
        public Vector2 MoveInput;
        public Vector2 LookInput;
        public bool    FirePressed;   // 本帧刚按下
        public bool    FireHeld;      // 持续按住
        public bool    JumpPressed;
        public bool    SprintHeld;
        public bool    AimHeld;
        public bool    ReloadPressed;
    }
}
