using System;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
///     In-memory <see cref="IFreeRespecWindow"/>. Thread-safe toggle; state is intentionally
///     not persisted — a free-respec window is a transient admin action, not world state, so it
///     resets when the server restarts (epic #425, slice 4 — #462).
/// </summary>
public sealed class FreeRespecWindow : IFreeRespecWindow
{
    private readonly object _lock = new();
    private bool _active;

    public bool IsActive
    {
        get
        {
            lock (_lock) return _active;
        }
    }

    public event Action? Changed;

    public void SetActive(bool active)
    {
        bool changed;
        lock (_lock)
        {
            changed = _active != active;
            _active = active;
        }

        if (changed)
            Changed?.Invoke();
    }

    public bool Toggle()
    {
        bool next;
        lock (_lock)
        {
            _active = !_active;
            next = _active;
        }

        Changed?.Invoke();
        return next;
    }
}
