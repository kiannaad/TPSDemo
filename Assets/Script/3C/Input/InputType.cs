namespace CGame
{
    /// <summary>
    /// 输入方案类型。
    /// </summary>
    public enum InputType
    {
        Player,
        Vehicle,
        UI,
    }

    /// <summary>
    /// Player 输入状态语义。
    /// </summary>
    public enum PlayerInputStateKey
    {
        MoveInput,
        LookInput,
        FirePressed,
        FireHeld,
        JumpPressed,
        SprintHeld,
        AimHeld,
    }

    /// <summary>
    /// Vehicle 输入状态语义。
    /// </summary>
    public enum VehicleInputStateKey
    {
        SteerInput,
        ThrottleValue,
        BrakeHeld,
        ExitPressed,
    }

    /// <summary>
    /// Player 底层 Action 名称，仅供 Container 内部映射使用。
    /// </summary>
    internal enum PlayerAction
    {
        Move,
        Look,
        Fire,
        Jump,
        Sprint,
        Aim,
    }

    /// <summary>
    /// Vehicle 底层 Action 名称，仅供 Container 内部映射使用。
    /// </summary>
    internal enum VehicleAction
    {
        Steer,
        Throttle,
        Brake,
        Exit,
    }
}
