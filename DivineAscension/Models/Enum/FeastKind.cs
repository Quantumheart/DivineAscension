namespace DivineAscension.Models.Enum;

/// <summary>
///     Origin of a religion feast day (#375). Persisted as int via ProtoBuf —
///     append new members, never reorder.
/// </summary>
public enum FeastKind
{
    Founding = 0,
    PatronDomain = 1,
    Custom = 2
}
