using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using TestHub.Models.Contract;
using TestHub.Models.Customer;
using TestHub.Models.Terms;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// New / Edit contract page. Drives:
///   - customer email lookup → autofill
///   - terms preload from /v1/contractor/terms
///   - payment schedule with milestone editor + 100% validation
///   - POST /v1/contractor/contracts/add-update
///   - "Ready for signature" state once saved
/// </summary>
public sealed class NewContractViewModel : BaseViewModel
{
    private static readonly CultureInfo s_invariant = CultureInfo.InvariantCulture;

    private readonly IApiClient _api;

    // ---------- header ----------
    private string _projectSubtitle = "Kitchen Renovation";

    // ---------- customer ----------
    private string _customerName = string.Empty;
    private string _customerEmail = string.Empty;
    private string _customerPhone = string.Empty;
    private string _customerAddress = string.Empty;
    private int _customerId;
    private bool _isLookingUpCustomer;

    // ---------- project ----------
    private string _projectName = string.Empty;
    private DateTime _startDate = DateTime.Today;
    private DateTime _endDate = DateTime.Today;
    private string _totalAmountText = string.Empty;
    private decimal _totalContractAmount;

    // ---------- schedule ----------
    private bool _isScheduleEditing = true;
    private decimal _percentageSum;
    private string _scheduleError = string.Empty;
    private bool _hasScheduleError;
    private string _totalAmountDisplay = "$0";
    private string _percentageSumDisplay = "0%";

    // ---------- terms ----------
    private bool _isTermsEditing;
    private string _termsText = string.Empty;
    private string _editTermsLabel = "Edit Terms & Conditions";

    // ---------- contract state ----------
    private int _contractId;
    private bool _isContractSaved;

    public NewContractViewModel(IApiClient api)
    {
        _api = api;

        Milestones = new ObservableCollection<MilestoneItemViewModel>();
        Milestones.CollectionChanged += (_, _) => RecomputeTotals();

        SeedDefaultMilestones();

        BackCommand               = new AsyncRelayCommand(GoBackAsync);
        LookupCustomerCommand     = new AsyncRelayCommand(LookupCustomerAsync);
        ToggleScheduleCommand     = new AsyncRelayCommand(ToggleScheduleAsync);
        AddMilestoneCommand       = new AsyncRelayCommand(AddMilestoneIfValidAsync);
        ToggleTermsCommand        = new AsyncRelayCommand(() => { IsTermsEditing = !IsTermsEditing; return Task.CompletedTask; });
        SaveContractCommand       = new AsyncRelayCommand(SaveContractAsync);
        SendForESignatureCommand  = new AsyncRelayCommand(() => Coming("E-Signature"));
        DownloadContractCommand   = new AsyncRelayCommand(() => Coming("Download Contract"));
    }

    // ============ collections ============
    public ObservableCollection<MilestoneItemViewModel> Milestones { get; }

    // ============ header ============
    public string ProjectSubtitle
    {
        get => _projectSubtitle;
        private set => SetProperty(ref _projectSubtitle, value);
    }

    // ============ customer ============
    public string CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    public string CustomerEmail
    {
        get => _customerEmail;
        set => SetProperty(ref _customerEmail, value);
    }

    /// <summary>
    /// Phone is constrained to exactly 10 digits — non-digits are stripped
    /// and pasted longer values are truncated automatically.
    /// </summary>
    public string CustomerPhone
    {
        get => _customerPhone;
        set
        {
            var digits = string.IsNullOrEmpty(value)
                ? string.Empty
                : new string(value.Where(char.IsDigit).Take(10).ToArray());

            SetProperty(ref _customerPhone, digits);
        }
    }

    public string CustomerAddress
    {
        get => _customerAddress;
        set => SetProperty(ref _customerAddress, value);
    }

    public bool IsLookingUpCustomer
    {
        get => _isLookingUpCustomer;
        private set => SetProperty(ref _isLookingUpCustomer, value);
    }

    // ============ project ============
    public string ProjectName
    {
        get => _projectName;
        set
        {
            if (SetProperty(ref _projectName, value))
            {
                ProjectSubtitle = string.IsNullOrWhiteSpace(value) ? "Kitchen Renovation" : value;
            }
        }
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    /// <summary>String-bound to the dollar Entry — parses live so milestone amounts stay in sync.</summary>
    public string TotalAmountText
    {
        get => _totalAmountText;
        set
        {
            if (_totalAmountText == value)
            {
                return;
            }

            _totalAmountText = value ?? string.Empty;
            _totalContractAmount = ParseDecimal(_totalAmountText);
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalContractAmount));
            RecomputeTotals();
        }
    }

    public decimal TotalContractAmount => _totalContractAmount;

    // ============ schedule ============
    public bool IsScheduleEditing
    {
        get => _isScheduleEditing;
        private set
        {
            if (SetProperty(ref _isScheduleEditing, value))
            {
                OnPropertyChanged(nameof(IsScheduleViewing));
                OnPropertyChanged(nameof(ScheduleToggleLabel));
            }
        }
    }

    public bool IsScheduleViewing => !IsScheduleEditing;
    public string ScheduleToggleLabel => IsScheduleEditing ? "Save Changes" : "Edit Schedule";

    public string TotalAmountDisplay
    {
        get => _totalAmountDisplay;
        private set => SetProperty(ref _totalAmountDisplay, value);
    }

    public string PercentageSumDisplay
    {
        get => _percentageSumDisplay;
        private set => SetProperty(ref _percentageSumDisplay, value);
    }

    public string ScheduleError
    {
        get => _scheduleError;
        private set => SetProperty(ref _scheduleError, value);
    }

    public bool HasScheduleError
    {
        get => _hasScheduleError;
        private set => SetProperty(ref _hasScheduleError, value);
    }

    // ============ terms ============
    public bool IsTermsEditing
    {
        get => _isTermsEditing;
        private set
        {
            if (SetProperty(ref _isTermsEditing, value))
            {
                OnPropertyChanged(nameof(IsTermsViewing));
                EditTermsLabel = value ? "Done" : "Edit Terms & Conditions";
            }
        }
    }

    public bool IsTermsViewing => !IsTermsEditing;

    public string TermsText
    {
        get => _termsText;
        set => SetProperty(ref _termsText, value);
    }

    public string EditTermsLabel
    {
        get => _editTermsLabel;
        private set => SetProperty(ref _editTermsLabel, value);
    }

    // ============ saved state ============
    public bool IsContractSaved
    {
        get => _isContractSaved;
        private set
        {
            if (SetProperty(ref _isContractSaved, value))
            {
                OnPropertyChanged(nameof(IsContractDraft));
            }
        }
    }

    public bool IsContractDraft => !IsContractSaved;

    // ============ commands ============
    public ICommand BackCommand { get; }
    public ICommand LookupCustomerCommand { get; }
    public ICommand ToggleScheduleCommand { get; }
    public ICommand AddMilestoneCommand { get; }
    public ICommand ToggleTermsCommand { get; }
    public ICommand SaveContractCommand { get; }
    public ICommand SendForESignatureCommand { get; }
    public ICommand DownloadContractCommand { get; }

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;

            var terms = await _api
                .GetAsync<TermsDto>(AppConfig.Endpoints.GetTerms, requireAuth: true)
                .ConfigureAwait(true);

            if (terms.IsSuccess && terms.Data is not null)
            {
                TermsText = terms.Data.TermsAndConditions ?? string.Empty;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ============ schedule helpers ============
    private void SeedDefaultMilestones()
    {
        // Start with a single empty milestone — the user fills it in,
        // then taps "Add Milestone" to append another (validation enforced).
        AddMilestone(string.Empty, 0m);
    }

    /// <summary>
    /// Bound to "+ Add Milestone". Refuses to append a new row until every
    /// existing one has both a name and a non-zero percentage so the user
    /// always finishes the row in front of them first.
    /// </summary>
    private async Task AddMilestoneIfValidAsync()
    {
        for (var i = 0; i < Milestones.Count; i++)
        {
            var m = Milestones[i];
            if (string.IsNullOrWhiteSpace(m.Name) || m.Percentage <= 0m)
            {
                await AlertAsync(
                    "Complete this milestone first",
                    $"Fill the name and percentage for milestone {m.Index} before adding another.",
                    "OK");
                return;
            }
        }

        if (Math.Round(_percentageSum, 2) >= 100m)
        {
            await AlertAsync(
                "Schedule already at 100%",
                "Milestone percentages already total 100%. Adjust an existing one before adding more.",
                "OK");
            return;
        }

        AddMilestone();
    }

    private void AddMilestone(string name = "", decimal percentage = 0m)
    {
        var item = new MilestoneItemViewModel
        {
            Index = Milestones.Count + 1,
            Name = name,
            Percentage = percentage,
        };

        item.PercentageChanged += OnMilestonePercentageChanged;
        item.DeleteCommand = new AsyncRelayCommand(() =>
        {
            DeleteMilestone(item);
            return Task.CompletedTask;
        });

        Milestones.Add(item);
    }

    private void DeleteMilestone(MilestoneItemViewModel item)
    {
        if (Milestones.Count <= 1)
        {
            return;
        }

        item.PercentageChanged -= OnMilestonePercentageChanged;
        Milestones.Remove(item);
        ReindexMilestones();
        RecomputeTotals();
    }

    private void ReindexMilestones()
    {
        for (var i = 0; i < Milestones.Count; i++)
        {
            Milestones[i].Index = i + 1;
        }
    }

    private void OnMilestonePercentageChanged(object? sender, EventArgs e) => RecomputeTotals();

    private void RecomputeTotals()
    {
        decimal sum = 0m;
        foreach (var milestone in Milestones)
        {
            sum += milestone.Percentage;
            milestone.Amount = Math.Round(_totalContractAmount * (milestone.Percentage / 100m),
                2, MidpointRounding.AwayFromZero);
        }

        _percentageSum = Math.Round(sum, 2, MidpointRounding.AwayFromZero);

        // Inline error: anything > 100 invalidates the schedule.
        if (_percentageSum > 100m)
        {
            HasScheduleError = true;
            ScheduleError = $"Total percentage is {_percentageSum.ToString("0.##", s_invariant)}% — must not exceed 100%.";
            foreach (var m in Milestones)
            {
                m.IsInvalid = true;
            }
        }
        else
        {
            HasScheduleError = false;
            ScheduleError = string.Empty;
            foreach (var m in Milestones)
            {
                m.IsInvalid = false;
            }
        }

        PercentageSumDisplay = _percentageSum.ToString("0.##", s_invariant) + "%";
        TotalAmountDisplay = _totalContractAmount == 0m
            ? "$0"
            : _totalContractAmount.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
    }

    private async Task ToggleScheduleAsync()
    {
        if (IsScheduleEditing)
        {
            // Trying to commit the schedule. Validate first.
            if (HasScheduleError)
            {
                await AlertAsync("Invalid Schedule", ScheduleError, "OK");
                return;
            }

            if (Math.Round(_percentageSum, 2) != 100m)
            {
                await AlertAsync(
                    "Schedule must total 100%",
                    $"Milestone percentages currently total {_percentageSum.ToString("0.##", s_invariant)}%. Please adjust so they add up to exactly 100%.",
                    "OK");
                return;
            }

            if (_totalContractAmount <= 0m)
            {
                await AlertAsync(
                    "Total Contract Amount required",
                    "Enter the total contract amount before saving the schedule.",
                    "OK");
                return;
            }

            IsScheduleEditing = false;
        }
        else
        {
            IsScheduleEditing = true;
        }
    }

    // ============ customer lookup ============
    public async Task LookupCustomerAsync()
    {
        var email = CustomerEmail?.Trim();
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        {
            return;
        }

        try
        {
            IsLookingUpCustomer = true;

            var path = $"{AppConfig.Endpoints.CustomerDetail}?emailAddress={Uri.EscapeDataString(email)}";
            var result = await _api.GetAsync<CustomerDto>(path, requireAuth: true).ConfigureAwait(true);

            if (!result.IsSuccess || result.Data is null)
            {
                return;
            }

            ApplyCustomer(result.Data);
        }
        finally
        {
            IsLookingUpCustomer = false;
        }
    }

    private void ApplyCustomer(CustomerDto c)
    {
        _customerId = c.CustomerId;

        if (!string.IsNullOrWhiteSpace(c.Name))         CustomerName = c.Name!;
        if (!string.IsNullOrWhiteSpace(c.PhoneNumber))  CustomerPhone = c.PhoneNumber!;

        var primary = c.Addresses?.FirstOrDefault(a => a.IsPrimary)
                      ?? c.Addresses?.FirstOrDefault();
        if (primary is not null)
        {
            var display = primary.ToDisplay();
            if (!string.IsNullOrWhiteSpace(display))
            {
                CustomerAddress = display;
            }
        }
    }

    // ============ save contract ============
    private async Task SaveContractAsync()
    {
        if (!ValidateForSave(out var error))
        {
            await AlertAsync("Cannot save", error, "OK");
            return;
        }

        try
        {
            IsBusy = true;

            var request = new ContractRequest
            {
                ContractId          = _contractId,
                QuoteId             = 0,
                ProjectName         = ProjectName,
                StartDate           = StartDate,
                EndDate             = EndDate,
                TotalContractAmount = _totalContractAmount,
                TermsAndConditions  = TermsText,
                Customer            = new ContractCustomerDto
                {
                    CustomerId   = _customerId,
                    Name         = CustomerName,
                    EmailAddress = CustomerEmail,
                    PhoneNumber  = CustomerPhone,
                    Address      = CustomerAddress,
                },
                ContractMilestones = Milestones
                    .Select(m => new ContractMilestoneRequest
                    {
                        ContractMilestoneId = 0,
                        MilestoneName       = m.Name,
                        Percentage          = m.Percentage,
                        Amount              = m.Amount,
                    })
                    .ToList(),
            };

            var result = await _api
                .PostAsync<ContractResponse>(AppConfig.Endpoints.AddUpdateContract, request, requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess || result.Data is null)
            {
                await AlertAsync(
                    "Couldn't create contract",
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Something went wrong while saving. Please try again."
                        : result.Message!,
                    "OK");
                return;
            }

            // Persist server identity for subsequent updates and lock the schedule
            // into view mode so the page mirrors the "Ready for Signature" state.
            _contractId = result.Data.ContractId;
            IsScheduleEditing = false;
            IsTermsEditing = false;
            IsContractSaved = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool ValidateForSave(out string error)
    {
        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            error = "Customer name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(CustomerEmail) || !CustomerEmail.Contains('@'))
        {
            error = "A valid customer email is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(CustomerPhone) || CustomerPhone.Length != 10)
        {
            error = "Phone number must be exactly 10 digits.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            error = "Project name is required.";
            return false;
        }

        if (_totalContractAmount <= 0m)
        {
            error = "Total contract amount must be greater than zero.";
            return false;
        }

        if (EndDate < StartDate)
        {
            error = "End date cannot be earlier than the start date.";
            return false;
        }

        if (Milestones.Count == 0)
        {
            error = "Add at least one payment milestone.";
            return false;
        }

        if (Math.Round(_percentageSum, 2) != 100m)
        {
            error = $"Milestone percentages total {_percentageSum.ToString("0.##", s_invariant)}% — they must add up to exactly 100%.";
            return false;
        }

        if (Milestones.Any(m => string.IsNullOrWhiteSpace(m.Name)))
        {
            error = "Every milestone needs a name.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(TermsText))
        {
            error = "Terms & Conditions cannot be empty.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    // ============ helpers ============
    private static decimal ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0m;
        }

        var cleaned = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
            .Replace(",", string.Empty);

        return decimal.TryParse(cleaned, NumberStyles.Float, s_invariant, out var v) && v >= 0m
            ? Math.Round(v, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//contracts");

    private static Task Coming(string area) =>
        AlertAsync(area, $"{area} is not implemented yet.", "OK");

    private static Task AlertAsync(string title, string message, string accept)
    {
        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, accept);
    }
}
