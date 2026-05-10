using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using TestHub.Models.Report;
using TestHub.Services;

namespace TestHub.ViewModels;

/// <summary>
/// Backing VM for the Reports page. Loads
/// <c>/v1/contractor/reports/financial</c> for the selected year and
/// surfaces the data the page needs:
///   - "Total Revenue" / "Outstanding" header tiles
///   - the year filter (current year plus the previous six)
///   - the 12-month <see cref="MonthlyRevenue"/> series for the bar chart
///   - the index of the highest-earning month so the chart can show its
///     floating tooltip on first render
/// </summary>
public sealed class ReportsViewModel : BaseViewModel
{
    private const int YearHistory = 6;

    private readonly IApiClient _api;

    private decimal _totalRevenue;
    private decimal _outstandingAmount;
    private int _year;
    private int _selectedYear;
    private int _highlightedIndex = -1;

    public ReportsViewModel(IApiClient api)
    {
        _api = api;

        Years = BuildYears();
        _selectedYear = Years[0];
        _year = _selectedYear;

        MonthlyRevenue = new ObservableCollection<MonthlyRevenue>();

        BackCommand       = new AsyncRelayCommand(GoBackAsync);
        RefreshCommand    = new AsyncRelayCommand(() => LoadAsync(_selectedYear));
        GoHomeCommand     = new AsyncRelayCommand(() => GoToAsync("//dashboard"));
        GoProjectsCommand = new AsyncRelayCommand(() => GoToAsync("//projects"));
        GoInvoicesCommand = new AsyncRelayCommand(() => GoToAsync("//invoices"));
        GoProfileCommand  = new AsyncRelayCommand(() => GoToAsync("//profile"));
    }

    public ObservableCollection<MonthlyRevenue> MonthlyRevenue { get; }

    /// <summary>
    /// Year drop-down options. Includes the current calendar year and the
    /// previous <see cref="YearHistory"/> years (so 7 entries in total).
    /// </summary>
    public IReadOnlyList<int> Years { get; }

    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value))
            {
                _ = LoadAsync(value);
            }
        }
    }

    public int Year
    {
        get => _year;
        private set => SetProperty(ref _year, value);
    }

    public string ChartTitle => $"Revenue Trend ({Year})";

    public decimal TotalRevenue
    {
        get => _totalRevenue;
        private set
        {
            if (SetProperty(ref _totalRevenue, value))
            {
                OnPropertyChanged(nameof(TotalRevenueDisplay));
            }
        }
    }

    public decimal OutstandingAmount
    {
        get => _outstandingAmount;
        private set
        {
            if (SetProperty(ref _outstandingAmount, value))
            {
                OnPropertyChanged(nameof(OutstandingDisplay));
            }
        }
    }

    public string TotalRevenueDisplay => FormatCompactCurrency(_totalRevenue);
    public string OutstandingDisplay  => FormatCompactCurrency(_outstandingAmount);

    /// <summary>
    /// Index (0..11) of the bar to highlight with the floating tooltip.
    /// We pick the highest-earning month so the chart looks alive on
    /// first render even before the user interacts with it.
    /// </summary>
    public int HighlightedIndex
    {
        get => _highlightedIndex;
        private set => SetProperty(ref _highlightedIndex, value);
    }

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand GoHomeCommand { get; }
    public ICommand GoProjectsCommand { get; }
    public ICommand GoInvoicesCommand { get; }
    public ICommand GoProfileCommand { get; }

    public async Task LoadAsync()
        => await LoadAsync(_selectedYear).ConfigureAwait(true);

    public async Task LoadAsync(int year)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;

            var url = BuildUrl(year);
            var result = await _api
                .GetAsync<FinancialReportDto>(url, requireAuth: true)
                .ConfigureAwait(true);

            if (!result.IsSuccess || result.Data is null)
            {
                ApplyEmpty(year);
                return;
            }

            ApplyData(result.Data, fallbackYear: year);
        }
        catch
        {
            ApplyEmpty(year);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyData(FinancialReportDto data, int fallbackYear)
    {
        TotalRevenue       = data.TotalRevenue;
        OutstandingAmount  = data.OutstandingAmount;
        Year               = data.Year > 0 ? data.Year : fallbackYear;

        MonthlyRevenue.Clear();
        if (data.MonthlyRevenue is { Count: > 0 } months)
        {
            foreach (var m in months)
            {
                MonthlyRevenue.Add(m);
            }
        }

        OnPropertyChanged(nameof(ChartTitle));
        OnPropertyChanged(nameof(MonthlyRevenue));

        HighlightedIndex = ComputeHighlightedIndex();
    }

    private void ApplyEmpty(int year)
    {
        TotalRevenue      = 0m;
        OutstandingAmount = 0m;
        Year              = year;
        MonthlyRevenue.Clear();
        HighlightedIndex  = -1;
        OnPropertyChanged(nameof(ChartTitle));
        OnPropertyChanged(nameof(MonthlyRevenue));
    }

    private int ComputeHighlightedIndex()
    {
        if (MonthlyRevenue.Count == 0)
        {
            return -1;
        }

        var bestIndex = -1;
        var bestAmount = decimal.MinValue;

        foreach (var m in MonthlyRevenue)
        {
            if (m is null) continue;
            if (m.Amount <= bestAmount) continue;

            var idx = m.Month - 1;
            if (idx is < 0 or > 11) continue;

            bestAmount = m.Amount;
            bestIndex = idx;
        }

        return bestAmount > 0m ? bestIndex : -1;
    }

    private static string BuildUrl(int year)
    {
        var sb = new StringBuilder(AppConfig.Endpoints.FinancialReports);
        sb.Append("?Year=").Append(year);
        return sb.ToString();
    }

    /// <summary>
    /// Builds the year filter list — current year first, then the
    /// previous <see cref="YearHistory"/> years.
    /// </summary>
    private static IReadOnlyList<int> BuildYears()
    {
        var years = new List<int>(YearHistory + 1);
        var current = DateTime.Now.Year;
        for (var i = 0; i <= YearHistory; i++)
        {
            years.Add(current - i);
        }
        return years;
    }

    private static string FormatCompactCurrency(decimal value)
    {
        var abs = Math.Abs(value);
        if (abs >= 1_000_000m)
        {
            return string.Format(CultureInfo.InvariantCulture, "${0:0.0}M", value / 1_000_000m);
        }
        if (abs >= 1_000m)
        {
            return string.Format(CultureInfo.InvariantCulture, "${0:0.0}K", value / 1_000m);
        }
        return string.Format(CultureInfo.InvariantCulture, "${0:0.##}", value);
    }

    private static Task GoToAsync(string route)
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync(route);

    private static Task GoBackAsync()
        => Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("//dashboard");
}
