using System;
using System.Collections.Generic;

namespace CGame
{
    public sealed class AIRuntimeRegistry
    {
        private readonly Dictionary<CharacterRuntimeId, AIRuntimeRegistration> registrations =
            new Dictionary<CharacterRuntimeId, AIRuntimeRegistration>();

        public AIRuntimeRegistry()
        {
            SquadContext = new AISquadContext();
        }

        public int Count => registrations.Count;
        public AISquadContext SquadContext { get; }

        public bool TryGet(CharacterRuntimeId runtimeId, out AIRuntimeRegistration registration)
        {
            return registrations.TryGetValue(runtimeId, out registration);
        }

        internal void Add(AIRuntimeRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            if (!registrations.TryAdd(registration.RuntimeId, registration))
            {
                throw new InvalidOperationException("An AI runtime is already registered for this character runtime ID.");
            }
        }

        internal void Remove(CharacterRuntimeId runtimeId, AIRuntimeRegistration registration)
        {
            if (!registrations.TryGetValue(runtimeId, out AIRuntimeRegistration current)
                || current != registration)
            {
                return;
            }

            registrations.Remove(runtimeId);
            current.Deactivate();
        }

        internal void Shutdown()
        {
            foreach (AIRuntimeRegistration registration in registrations.Values)
            {
                registration.Deactivate();
            }

            registrations.Clear();
            SquadContext.Shutdown();
        }
    }
}
