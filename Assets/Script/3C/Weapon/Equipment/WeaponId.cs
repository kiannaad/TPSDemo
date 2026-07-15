using System;

namespace CGame
{
    public readonly struct WeaponId : IEquatable<WeaponId>
    {
        public WeaponId(string value)
        {
            Value = value;
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(WeaponId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is WeaponId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(WeaponId left, WeaponId right) => left.Equals(right);
        public static bool operator !=(WeaponId left, WeaponId right) => !left.Equals(right);
    }
}
