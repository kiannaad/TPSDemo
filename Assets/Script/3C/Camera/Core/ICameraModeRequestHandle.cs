using System;

namespace CGame
{
    public interface ICameraModeRequestHandle : IDisposable
    {
        bool IsReleased { get; }
    }
}
