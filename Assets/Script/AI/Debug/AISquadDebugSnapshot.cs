using System;

namespace CGame
{
    public sealed class AISquadDebugSnapshot
    {
        private readonly AISquadReport[] reports;
        private readonly AISquadLeaseDebugRecord[] leases;

        public AISquadDebugSnapshot(
            double capturedAt,
            string memberId,
            AISquadReport[] reportRecords,
            AISquadLeaseDebugRecord[] leaseRecords)
        {
            CapturedAt = capturedAt;
            MemberId = memberId ?? string.Empty;
            reports = reportRecords == null ? Array.Empty<AISquadReport>() : (AISquadReport[])reportRecords.Clone();
            leases = leaseRecords == null
                ? Array.Empty<AISquadLeaseDebugRecord>()
                : (AISquadLeaseDebugRecord[])leaseRecords.Clone();
        }

        public double CapturedAt { get; }
        public string MemberId { get; }
        public AISquadReport[] Reports => (AISquadReport[])reports.Clone();
        public AISquadLeaseDebugRecord[] Leases => (AISquadLeaseDebugRecord[])leases.Clone();
    }
}
