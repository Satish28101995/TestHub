using System.Globalization;
using System.Windows.Input;
using TestHub.Models.Signature;
using TestHub.Services;

namespace TestHub.ViewModels;

public sealed class SignatureViewModel : BaseViewModel
{
    private readonly IApiClient _api;
    private readonly ISessionStore _session;

    private string? _signatureUrl;
    private bool _hasExistingSignature;
    private bool _isDirty;
    private DateTimeOffset? _updatedAt;

    public SignatureViewModel(IApiClient api, ISessionStore session)
    {
        _api = api;
        _session = session;

        BackCommand = new AsyncRelayCommand(GoBackAsync);
    }

    /// <summary>
    /// Server-side URL of the existing signature, if any. Bound to the
    /// preview <c>Image</c> on the page.
    /// </summary>
    public string? SignatureUrl
    {
        get => _signatureUrl;
        private set
        {
            if (SetProperty(ref _signatureUrl, value))
            {
                OnPropertyChanged(nameof(HasExistingSignature));
            }
        }
    }

    public bool HasExistingSignature
    {
        get => _hasExistingSignature && !string.IsNullOrEmpty(SignatureUrl);
        private set => SetProperty(ref _hasExistingSignature, value);
    }

    /// <summary>True after the user has touched the pad in this session.</summary>
    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    public DateTimeOffset? UpdatedAt
    {
        get => _updatedAt;
        private set => SetProperty(ref _updatedAt, value);
    }

    public ICommand BackCommand { get; }

    /// <summary>
    /// Loads the current contractor's signature. Called by the page on
    /// appearing; failures are swallowed so the user can still draw a
    /// fresh signature.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            IsBusy = true;

            var result = await _api
                .GetAsync<SignatureDto>(AppConfig.Endpoints.GetSignature, requireAuth: true, ct)
                .ConfigureAwait(true);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.SignatureUrl))
            {
                SignatureUrl = result.Data!.SignatureUrl;
                HasExistingSignature = true;
                UpdatedAt = result.Data.UpdatedAt;
            }
            else
            {
                HasExistingSignature = false;
                SignatureUrl = null;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Sends the (just-drawn) signature image name to the server.
    /// The page generates the name and the upload flow attaches the
    /// real bytes elsewhere.
    /// </summary>
    public async Task<bool> SubmitAsync(string signatureFileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(signatureFileName))
        {
            await DisplayAlertSafeAsync("Signature required",
                "Please sign in the box before submitting.", "OK").ConfigureAwait(true);
            return false;
        }

        try
        {
            IsBusy = true;

            var payload = new UpdateSignatureRequest { SignatureUrl = signatureFileName };

            var result = await _api.PostAsync<SignatureDto>(
                AppConfig.Endpoints.UpdateSignature, payload, requireAuth: true, ct)
                .ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                await DisplayAlertSafeAsync("Could not save signature",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Please try again."
                        : result.Message, "OK").ConfigureAwait(true);
                return false;
            }

            // Reflect the latest server state back into the VM.
            SignatureUrl = result.Data?.SignatureUrl ?? signatureFileName;
            HasExistingSignature = true;
            UpdatedAt = result.Data?.UpdatedAt ?? DateTimeOffset.UtcNow;
            IsDirty = false;

            await DisplayAlertSafeAsync("Signature saved",
                "Your digital signature has been updated.", "OK").ConfigureAwait(true);
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Generates a stable per-user signature file name. Used as the
    /// payload for <see cref="SubmitAsync"/> when the API only stores
    /// the filename / object key.
    /// </summary>
    public string BuildSignatureFileName()
    {
        var userId = _session.CurrentUser?.UserId ?? 0;
        var stamp  = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        return $"signature_{userId}_{stamp}.png";
    }

    private static Task GoBackAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        // Prefer the navigation stack when available; otherwise fall back
        // to the dashboard.
        try { return Shell.Current.GoToAsync(".."); }
        catch { return Shell.Current.GoToAsync("//dashboard"); }
    }

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
