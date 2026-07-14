using System;
using System.Collections.Generic;

namespace CGame
{
    public sealed class AIAlertStateMachine
    {
        private const int HistoryCapacity = 16;
        private readonly Queue<AIAlertTransitionRecord> history = new Queue<AIAlertTransitionRecord>();

        public AIAlertStateMachine(double timestamp)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp));
            }

            State = AIAlertState.Patrol;
            EnteredAt = timestamp;
        }

        public AIAlertState State { get; private set; }
        public double EnteredAt { get; private set; }
        public string LastReason { get; private set; } = "initial";

        public bool TryTransition(AIAlertState next, double timestamp, string reason)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                return false;
            }

            if (next == State)
            {
                return true;
            }

            if (!IsAllowed(State, next))
            {
                return false;
            }

            AIAlertState previous = State;
            State = next;
            EnteredAt = timestamp;
            LastReason = reason ?? string.Empty;
            history.Enqueue(new AIAlertTransitionRecord(previous, next, timestamp, LastReason));
            while (history.Count > HistoryCapacity)
            {
                history.Dequeue();
            }

            return true;
        }

        public AIAlertTransitionRecord[] CopyHistory()
        {
            return history.ToArray();
        }

        private static bool IsAllowed(AIAlertState current, AIAlertState next)
        {
            switch (current)
            {
                case AIAlertState.Patrol:
                    return next == AIAlertState.Investigate || next == AIAlertState.Combat;
                case AIAlertState.Investigate:
                    return next == AIAlertState.Combat
                        || next == AIAlertState.Search
                        || next == AIAlertState.Return;
                case AIAlertState.Combat:
                    return next == AIAlertState.Search || next == AIAlertState.Return;
                case AIAlertState.Search:
                    return next == AIAlertState.Combat || next == AIAlertState.Return;
                case AIAlertState.Return:
                    return next == AIAlertState.Patrol
                        || next == AIAlertState.Investigate
                        || next == AIAlertState.Combat;
                default:
                    return false;
            }
        }
    }
}
