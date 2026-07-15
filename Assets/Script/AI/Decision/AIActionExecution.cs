using System;
using UnityEngine;

namespace CGame
{
    public sealed class AIActionExecution
    {
        public AIActionExecution(AIActionRequest request)
        {
            Request = request;
            Status = AIActionStatus.Running;
            Result = new AIActionResult(request.Kind, Status, "running", Vector3.zero);
        }

        public AIActionRequest Request { get; }
        public AIActionStatus Status { get; private set; }
        public AIActionResult Result { get; private set; }
        public bool IsRunning => Status == AIActionStatus.Running;

        public AIActionResult Advance(double timestamp)
        {
            if (!IsRunning)
            {
                return Result;
            }

            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp))
            {
                return Fail("invalid-time");
            }

            return timestamp >= Request.ExpiresAt
                ? Complete("maximum-duration")
                : Result;
        }

        public bool Cancel(double timestamp, string reason, bool force)
        {
            if (Status == AIActionStatus.Cancelled)
            {
                return true;
            }

            if (!IsRunning)
            {
                return false;
            }

            if (!force && timestamp < Request.CommitmentUntil)
            {
                return false;
            }

            Finish(AIActionStatus.Cancelled, reason);
            return true;
        }

        public AIActionResult Complete(string reason)
        {
            if (IsRunning)
            {
                Finish(AIActionStatus.Completed, reason);
            }

            return Result;
        }

        public AIActionResult Fail(string reason)
        {
            if (IsRunning)
            {
                Finish(AIActionStatus.Failed, reason);
            }

            return Result;
        }

        private void Finish(AIActionStatus status, string reason)
        {
            Status = status;
            Result = new AIActionResult(Request.Kind, status, reason, Vector3.zero);
        }
    }
}
