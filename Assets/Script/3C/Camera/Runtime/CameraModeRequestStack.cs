using System;
using System.Collections.Generic;

namespace CGame
{
    public sealed class CameraModeRequestStack : IDisposable
    {
        private readonly List<RequestHandle> handles = new List<RequestHandle>();
        private long nextSequence;
        private bool isDisposed;

        public CameraModeRequest ActiveRequest
        {
            get
            {
                RemovingInvalidRequests();
                RequestHandle active = null;
                for (int index = 0; index < handles.Count; index++)
                {
                    RequestHandle candidate = handles[index];
                    if (active == null || candidate.Request.Priority > active.Request.Priority ||
                        candidate.Request.Priority == active.Request.Priority && candidate.Sequence > active.Sequence)
                    {
                        active = candidate;
                    }
                }

                return active?.Request;
            }
        }

        public CameraMode ActiveMode => ActiveRequest?.Mode ?? CameraMode.GameplayFirstPerson;
        public long Revision { get; private set; }

        public ICameraModeRequestHandle Request(CameraModeRequest request)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(CameraModeRequestStack));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            RequestHandle handle = new RequestHandle(this, request, ++nextSequence);
            if (!request.Target.IsValid)
            {
                handle.ReleasingFromStack();
                return handle;
            }

            handles.Add(handle);
            Revision++;
            return handle;
        }

        public void Clear()
        {
            if (handles.Count == 0)
            {
                return;
            }

            for (int index = handles.Count - 1; index >= 0; index--)
            {
                handles[index].ReleasingFromStack();
            }

            handles.Clear();
            Revision++;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            Clear();
            isDisposed = true;
        }

        private void RemovingInvalidRequests()
        {
            for (int index = handles.Count - 1; index >= 0; index--)
            {
                RequestHandle handle = handles[index];
                if (handle.Request.Target.IsValid)
                {
                    continue;
                }

                handles.RemoveAt(index);
                handle.ReleasingFromStack();
                Revision++;
            }
        }

        private void Releasing(RequestHandle handle)
        {
            if (handle.IsReleased)
            {
                return;
            }

            bool removed = handles.Remove(handle);
            handle.ReleasingFromStack();
            if (removed)
            {
                Revision++;
            }
        }

        private sealed class RequestHandle : ICameraModeRequestHandle
        {
            private CameraModeRequestStack owner;

            public RequestHandle(CameraModeRequestStack owner, CameraModeRequest request, long sequence)
            {
                this.owner = owner;
                Request = request;
                Sequence = sequence;
            }

            public CameraModeRequest Request { get; }
            public long Sequence { get; }
            public bool IsReleased { get; private set; }

            public void Dispose()
            {
                owner?.Releasing(this);
            }

            public void ReleasingFromStack()
            {
                IsReleased = true;
                owner = null;
            }
        }
    }
}
