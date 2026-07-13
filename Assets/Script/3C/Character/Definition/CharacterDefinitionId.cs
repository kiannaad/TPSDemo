using System;

namespace CGame
{
    public readonly struct CharacterDefinitionId : IEquatable<CharacterDefinitionId>
    {
        public CharacterDefinitionId(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CharacterDefinitionId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterDefinitionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CharacterDefinitionId left, CharacterDefinitionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterDefinitionId left, CharacterDefinitionId right)
        {
            return !left.Equals(right);
        }
    }
}
