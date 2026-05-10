namespace TestHub.Services;

/// <summary>
/// Persists the "Remember me" choice from the login page. Backed by
/// <see cref="Preferences"/> so it's plain shared-prefs / NSUserDefaults
/// — never SecureStorage, since only the email is stored (never the
/// password) and we want it readable across cold starts.
/// </summary>
public interface IRememberedAccountStore
{
    /// <summary>True when the user previously ticked "Remember me".</summary>
    bool ShouldRemember { get; }

    /// <summary>
    /// The previously remembered email address, or <c>null</c> when no email
    /// is stored or the user opted out.
    /// </summary>
    string? SavedEmail { get; }

    /// <summary>Stores the email and flips the remember flag on.</summary>
    void Save(string email);

    /// <summary>Clears any saved email and flips the remember flag off.</summary>
    void Clear();
}
