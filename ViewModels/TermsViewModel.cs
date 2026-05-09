using System.Windows.Input;
using TestHub.Models.Terms;
using TestHub.Services;

namespace TestHub.ViewModels;

public sealed class TermsViewModel : BaseViewModel
{
    public const int MinTermsLength = 10;
    public const int MaxTermsLength = 4000;

    private readonly IApiClient _api;

    private string _termsAndConditions = string.Empty;
    private string _termsError = string.Empty;
    private DateTimeOffset? _updatedAt;

    public TermsViewModel(IApiClient api)
    {
        _api = api;

        BackCommand   = new AsyncRelayCommand(GoBackAsync);
        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
    }

    public string TermsAndConditions
    {
        get => _termsAndConditions;
        set
        {
            if (SetProperty(ref _termsAndConditions, value))
            {
                if (!string.IsNullOrEmpty(_termsError))
                {
                    TermsError = string.Empty;
                }
                OnPropertyChanged(nameof(CharacterCount));
            }
        }
    }

    public string TermsError
    {
        get => _termsError;
        private set
        {
            if (SetProperty(ref _termsError, value))
            {
                OnPropertyChanged(nameof(HasTermsError));
            }
        }
    }

    public bool HasTermsError => !string.IsNullOrEmpty(TermsError);

    public DateTimeOffset? UpdatedAt
    {
        get => _updatedAt;
        private set => SetProperty(ref _updatedAt, value);
    }

    public int CharacterCount => TermsAndConditions?.Length ?? 0;

    public ICommand BackCommand { get; }
    public ICommand SubmitCommand { get; }

    /// <summary>
    /// Loads any existing terms for the contractor. Failures fall back
    /// silently to an empty editor so the user can author fresh terms.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            IsBusy = true;

            var result = await _api
                .GetAsync<TermsDto>(AppConfig.Endpoints.GetTerms, requireAuth: true, ct)
                .ConfigureAwait(true);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data?.TermsAndConditions))
            {
                TermsAndConditions = result.Data!.TermsAndConditions!;
                UpdatedAt = result.Data.UpdatedAt;
                TermsError = string.Empty;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SubmitAsync()
    {
        if (!Validate())
        {
            return;
        }

        try
        {
            IsBusy = true;

            var payload = new UpdateTermsRequest
            {
                TermsAndConditions = TermsAndConditions.Trim(),
            };

            var result = await _api.PostAsync<TermsDto>(
                AppConfig.Endpoints.UpdateTerms, payload, requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess)
            {
                await DisplayAlertSafeAsync("Could not save terms",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Please try again."
                        : result.Message, "OK").ConfigureAwait(true);
                return;
            }

            if (result.Data is not null)
            {
                if (!string.IsNullOrWhiteSpace(result.Data.TermsAndConditions))
                {
                    TermsAndConditions = result.Data.TermsAndConditions!;
                }
                UpdatedAt = result.Data.UpdatedAt ?? DateTimeOffset.UtcNow;
            }

            await DisplayAlertSafeAsync("Terms saved",
                "Your default contract terms have been updated.", "OK").ConfigureAwait(true);

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//dashboard").ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool Validate()
    {
        var value = TermsAndConditions?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(value))
        {
            TermsError = "Please enter your default contract terms.";
            return false;
        }

        if (value.Length < MinTermsLength)
        {
            TermsError = $"Terms must be at least {MinTermsLength} characters.";
            return false;
        }

        if (value.Length > MaxTermsLength)
        {
            TermsError = $"Terms must be {MaxTermsLength} characters or fewer.";
            return false;
        }

        TermsError = string.Empty;
        return true;
    }

    private static Task GoBackAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        try { return Shell.Current.GoToAsync(".."); }
        catch { return Shell.Current.GoToAsync("//dashboard"); }
    }

    private static Task DisplayAlertSafeAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
