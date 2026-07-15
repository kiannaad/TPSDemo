namespace CGame.Animation
{
    public readonly struct WeaponPresentationLoadResult
    {
        public WeaponPresentationLoadResult(IWeaponPresentationResourceLease lease, string missingField = null)
        {
            Lease = lease;
            MissingField = missingField;
        }

        public IWeaponPresentationResourceLease Lease { get; }
        public string MissingField { get; }
        public bool IsSuccess => Lease != null && Lease.Definition != null && string.IsNullOrEmpty(MissingField);
        public string DefinitionId => Lease?.DefinitionId ?? "<unresolved>";
    }
}
