using HeroPath.Shared.Application.Contracts;

namespace HeroPath.Shared.Application;

/// <summary>
/// Whether sounds are on. Off by default; only this on/off value is stored, and a failing storage never breaks the switch.
/// Any stored value other than "on" means off.
/// </summary>
public sealed class SoundSettings(IAppStorage storage)
{
    private const string StoredOn = "on";
    private const string StoredOff = "off";

    /// <summary>Raised after <see cref="SetAsync"/> changed the setting.</summary>
    public event Action? Changed;

    /// <summary>True when sounds should be played.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Applies the stored setting without raising <see cref="Changed"/>; call once at startup.</summary>
    public async Task ApplyStoredAsync(CancellationToken cancellationToken)
    {
        string? stored = null;
        try
        {
            stored = await storage.GetAsync(StorageKeys.Sound, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Storage is optional: without it sounds stay off.
        }

        Enabled = stored == StoredOn;
    }

    /// <summary>Switches sounds on or off, remembers it in storage (best effort) and raises <see cref="Changed"/>.</summary>
    public async Task SetAsync(bool enabled, CancellationToken cancellationToken)
    {
        Enabled = enabled;
        try
        {
            _ = await storage.SetAsync(StorageKeys.Sound, enabled ? StoredOn : StoredOff, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The setting stays for this session even if it cannot be remembered.
        }

        Changed?.Invoke();
    }
}
