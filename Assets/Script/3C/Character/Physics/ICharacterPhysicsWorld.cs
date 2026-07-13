using System;

namespace CGame
{
    public interface ICharacterPhysicsWorld
    {
        IPhysicsRegistration Register(CharacterPhysicsMotor motor);
        IPhysicsRegistration Register(CharacterPhysicsMover mover);
    }

    public interface IPhysicsRegistration : IDisposable
    {
        bool IsActive { get; }
    }
}
