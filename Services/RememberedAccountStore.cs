namespace TestHub.Services;

public sealed class RememberedAccountStore : IRememberedAccountStore
{
    private const string FlagKey  = "th.rememberMe";
    private const string EmailKey = "th.rememberedEmail";

    public bool ShouldRemember => Preferences.Default.Get(FlagKey, false);

    public string? SavedEmail
    {
        get
        {
            if (!Preferences.Default.ContainsKey(EmailKey))
            {
                return null;
            }

            var value = Preferences.Default.Get(EmailKey, string.Empty);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    public void Save(string email)
    {
        var trimmed = (email ?? string.Empty).Trim();

        Preferences.Default.Set(FlagKey, true);
        Preferences.Default.Set(EmailKey, trimmed);
    }

    public void Clear()
    {
        Preferences.Default.Set(FlagKey, false);
        Preferences.Default.Remove(EmailKey);
    }
}
