using System;
using System.Threading;

namespace CGame.Animation
{
    public sealed class WeaponBindingLifecycle : IDisposable
    {
        private static long nextCharacterLifecycleToken;
        private readonly ulong characterLifecycleToken;
        private ulong nextBindingToken;
        private WeaponPresentationLoadTicket pendingTicket;
        private IDisposable pendingCancellation;
        private Entry current;
        private Entry next;
        private WeaponBindingState settledState = WeaponBindingState.Unbound;

        public WeaponBindingLifecycle()
        {
            characterLifecycleToken = (ulong)Interlocked.Increment(ref nextCharacterLifecycleToken);
        }

        public WeaponBindingState State { get; private set; } = WeaponBindingState.Unbound;
        public ulong CharacterLifecycleToken => characterLifecycleToken;
        public WeaponPresentationBinding CurrentBinding => current?.Binding;
        public WeaponPresentationBinding NextBinding => next?.Binding;
        public bool HasPendingLoad => pendingTicket.IsValid;

        public bool CanAccept(WeaponPresentationLoadTicket ticket)
        {
            return IsCurrent(ticket);
        }

        public WeaponPresentationLoadTicket Begin(WeaponEquipmentSnapshot snapshot)
        {
            ThrowIfDisposed();
            CancelPending();
            ReleaseEntry(ref next);
            var ticket = new WeaponPresentationLoadTicket(characterLifecycleToken, snapshot.Generation, ++nextBindingToken);
            pendingTicket = snapshot.IsEquipped ? ticket : default;
            settledState = snapshot.IsEquipped ? WeaponBindingState.Fallback : WeaponBindingState.Unbound;
            State = snapshot.IsEquipped
                ? WeaponBindingState.PendingPresentation
                : current != null ? WeaponBindingState.Blending : WeaponBindingState.Unbound;
            return ticket;
        }

        public bool SetCancellation(WeaponPresentationLoadTicket ticket, IDisposable cancellation)
        {
            if (!IsCurrent(ticket))
            {
                cancellation?.Dispose();
                return false;
            }

            pendingCancellation?.Dispose();
            pendingCancellation = cancellation;
            return true;
        }

        public bool Accept(
            WeaponPresentationLoadTicket ticket,
            IWeaponPresentationResourceLease lease,
            WeaponPresentationBinding binding,
            Action releasePresentation,
            bool isDegraded)
        {
            if (!IsCurrent(ticket))
            {
                releasePresentation?.Invoke();
                lease?.Dispose();
                return false;
            }

            ClearPending();
            ReleaseEntry(ref next);
            next = new Entry(lease, binding, releasePresentation);
            settledState = isDegraded ? WeaponBindingState.Degraded : WeaponBindingState.Active;
            State = WeaponBindingState.Blending;
            return true;
        }

        public bool Reject(WeaponPresentationLoadTicket ticket, IWeaponPresentationResourceLease lease = null)
        {
            if (!IsCurrent(ticket))
            {
                lease?.Dispose();
                return false;
            }

            lease?.Dispose();
            ClearPending();
            ReleaseEntry(ref next);
            settledState = WeaponBindingState.Fallback;
            State = WeaponBindingState.Fallback;
            return true;
        }

        public void CompleteBlend()
        {
            if (State == WeaponBindingState.Disposed)
            {
                return;
            }

            ReleaseEntry(ref current);
            current = next;
            next = null;
            State = settledState;
        }

        public void Dispose()
        {
            if (State == WeaponBindingState.Disposed)
            {
                return;
            }

            CancelPending();
            ReleaseEntry(ref next);
            ReleaseEntry(ref current);
            State = WeaponBindingState.Disposed;
        }

        private bool IsCurrent(WeaponPresentationLoadTicket ticket)
        {
            return State != WeaponBindingState.Disposed
                && pendingTicket.IsValid
                && ticket.CharacterLifecycleToken == characterLifecycleToken
                && ticket.Generation == pendingTicket.Generation
                && ticket.BindingToken == pendingTicket.BindingToken;
        }

        private void CancelPending()
        {
            pendingCancellation?.Dispose();
            pendingCancellation = null;
            pendingTicket = default;
        }

        private void ClearPending()
        {
            pendingCancellation?.Dispose();
            pendingCancellation = null;
            pendingTicket = default;
        }

        private static void ReleaseEntry(ref Entry entry)
        {
            if (entry == null)
            {
                return;
            }

            entry.Dispose();
            entry = null;
        }

        private void ThrowIfDisposed()
        {
            if (State == WeaponBindingState.Disposed)
            {
                throw new ObjectDisposedException(nameof(WeaponBindingLifecycle));
            }
        }

        private sealed class Entry : IDisposable
        {
            private readonly IWeaponPresentationResourceLease lease;
            private Action releasePresentation;

            public Entry(IWeaponPresentationResourceLease lease, WeaponPresentationBinding binding, Action releasePresentation)
            {
                this.lease = lease;
                Binding = binding;
                this.releasePresentation = releasePresentation;
            }

            public WeaponPresentationBinding Binding { get; }

            public void Dispose()
            {
                Action callback = releasePresentation;
                releasePresentation = null;
                callback?.Invoke();
                lease?.Dispose();
            }
        }
    }
}
