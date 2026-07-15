using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace CGame
{
    public sealed class AISquadContext
    {
        private const int MaximumReports = 32;
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("CGame.AI.Squad.Update");
        private static readonly ProfilerMarker ReportMarker = new ProfilerMarker("CGame.AI.Squad.Report");
        private static readonly ProfilerMarker LeaseMarker = new ProfilerMarker("CGame.AI.Squad.Lease");

        private readonly List<AISquadReport> reports = new List<AISquadReport>(MaximumReports);
        private readonly Dictionary<string, AISquadLease> leases = new Dictionary<string, AISquadLease>(16);
        private readonly List<string> leaseKeysToRemove = new List<string>(16);
        private bool isShutdown;

        public int ReportCount => reports.Count;
        public int LeaseCount => leases.Count;
        public bool IsShutdown => isShutdown;

        public bool PublishReport(AISquadReport report)
        {
            using (ReportMarker.Auto())
            {
                if (isShutdown)
                {
                    return false;
                }

                if (reports.Count == MaximumReports)
                {
                    reports.RemoveAt(0);
                }

                reports.Add(report);
                return true;
            }
        }

        public bool TryGetLatestSuggestion(
            string receiverId,
            double timestamp,
            out AISquadSuggestion suggestion)
        {
            using (ReportMarker.Auto())
            {
                for (int i = reports.Count - 1; i >= 0; i--)
                {
                    AISquadReport report = reports[i];
                    if (!report.IsAvailable(timestamp)
                        || string.Equals(report.ReporterId, receiverId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    suggestion = new AISquadSuggestion(
                        report.SubjectId,
                        report.EstimatedPosition,
                        report.ObservedAt,
                        report.Confidence,
                        report.UncertaintyRadius);
                    return true;
                }

                suggestion = default;
                return false;
            }
        }

        public bool TryAcquire(
            AISquadResourceKind kind,
            string resourceId,
            string ownerId,
            double timestamp,
            double duration,
            out AISquadLease lease)
        {
            using (LeaseMarker.Auto())
            {
                lease = null;
                if (isShutdown
                    || string.IsNullOrWhiteSpace(resourceId)
                    || string.IsNullOrWhiteSpace(ownerId)
                    || duration <= 0d)
                {
                    return false;
                }

                string key = CreateLeaseKey(kind, resourceId);
                if (leases.TryGetValue(key, out AISquadLease current))
                {
                    if (current.ExpiresAt > timestamp)
                    {
                        return false;
                    }

                    current.Invalidate();
                    leases.Remove(key);
                }

                lease = new AISquadLease(this, kind, resourceId, ownerId, timestamp + duration);
                leases.Add(key, lease);
                return true;
            }
        }

        public int ReleaseOwner(string ownerId)
        {
            using (LeaseMarker.Auto())
            {
                if (string.IsNullOrWhiteSpace(ownerId) || leases.Count == 0)
                {
                    return 0;
                }

                int released = 0;
                leaseKeysToRemove.Clear();
                foreach (KeyValuePair<string, AISquadLease> pair in leases)
                {
                    if (string.Equals(pair.Value.OwnerId, ownerId, StringComparison.Ordinal))
                    {
                        pair.Value.Invalidate();
                        leaseKeysToRemove.Add(pair.Key);
                    }
                }

                for (int i = 0; i < leaseKeysToRemove.Count; i++)
                {
                    leases.Remove(leaseKeysToRemove[i]);
                    released++;
                }

                return released;
            }
        }

        public void Advance(double timestamp)
        {
            using (UpdateMarker.Auto())
            {
                for (int i = reports.Count - 1; i >= 0; i--)
                {
                    if (reports[i].ExpiresAt <= timestamp)
                    {
                        reports.RemoveAt(i);
                    }
                }

                if (leases.Count == 0)
                {
                    return;
                }

                leaseKeysToRemove.Clear();
                foreach (KeyValuePair<string, AISquadLease> pair in leases)
                {
                    if (pair.Value.ExpiresAt <= timestamp)
                    {
                        pair.Value.Invalidate();
                        leaseKeysToRemove.Add(pair.Key);
                    }
                }

                for (int i = 0; i < leaseKeysToRemove.Count; i++)
                {
                    leases.Remove(leaseKeysToRemove[i]);
                }
            }
        }

        public void Shutdown()
        {
            if (isShutdown)
            {
                return;
            }

            isShutdown = true;
            reports.Clear();
            foreach (AISquadLease lease in leases.Values)
            {
                lease.Invalidate();
            }

            leases.Clear();
        }

        public AISquadDebugSnapshot CreateDebugSnapshot(double timestamp, string memberId)
        {
            var reportRecords = reports.ToArray();
            var leaseRecords = new AISquadLeaseDebugRecord[leases.Count];
            int index = 0;
            foreach (AISquadLease lease in leases.Values)
            {
                leaseRecords[index++] = new AISquadLeaseDebugRecord(
                    lease.Kind,
                    lease.ResourceId,
                    lease.OwnerId,
                    lease.ExpiresAt);
            }

            return new AISquadDebugSnapshot(timestamp, memberId, reportRecords, leaseRecords);
        }

        internal bool IsLeaseActive(AISquadLease lease)
        {
            return lease != null
                && leases.TryGetValue(CreateLeaseKey(lease.Kind, lease.ResourceId), out AISquadLease current)
                && ReferenceEquals(current, lease);
        }

        internal bool Release(AISquadLease lease)
        {
            if (!IsLeaseActive(lease))
            {
                return false;
            }

            leases.Remove(CreateLeaseKey(lease.Kind, lease.ResourceId));
            lease.Invalidate();
            return true;
        }

        private static string CreateLeaseKey(AISquadResourceKind kind, string resourceId)
        {
            return $"{kind}:{resourceId}";
        }
    }
}
