using System.Globalization;
using System.Windows.Input;

namespace TestHub.ViewModels;

/// <summary>
/// One row in the Payment Schedule editor. The parent <see cref="NewContractViewModel"/>
/// listens to <see cref="PercentageChanged"/> to recompute totals, validation,
/// and the dependent dollar <see cref="Amount"/> based on the contract total.
/// </summary>
public sealed class MilestoneItemViewModel : BaseViewModel
{
    private static readonly CultureInfo s_invariant = CultureInfo.InvariantCulture;
    private static readonly CultureInfo s_money = CultureInfo.GetCultureInfo("en-US");

    private int _index;
    private string _name = string.Empty;
    private string _percentageText = "0";
    private decimal _percentage;
    private decimal _amount;
    private bool _isInvalid;

    public int Index
    {
        get => _index;
        set => SetProperty(ref _index, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>
    /// String-bound for the Entry. Setter parses, clamps to two decimals,
    /// and raises <see cref="PercentageChanged"/> so the parent can react.
    /// </summary>
    public string PercentageText
    {
        get => _percentageText;
        set
        {
            if (_percentageText == value)
            {
                return;
            }

            _percentageText = value ?? string.Empty;

            if (decimal.TryParse(
                    _percentageText,
                    NumberStyles.Float,
                    s_invariant,
                    out var parsed) ||
                decimal.TryParse(
                    _percentageText,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    out parsed))
            {
                _percentage = Math.Round(Math.Max(0, parsed), 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                _percentage = 0m;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(Percentage));
            PercentageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public decimal Percentage
    {
        get => _percentage;
        set
        {
            var rounded = Math.Round(Math.Max(0, value), 2, MidpointRounding.AwayFromZero);
            if (_percentage == rounded)
            {
                return;
            }

            _percentage = rounded;
            _percentageText = rounded.ToString("0.##", s_invariant);
            OnPropertyChanged();
            OnPropertyChanged(nameof(PercentageText));
            PercentageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public decimal Amount
    {
        get => _amount;
        set
        {
            var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
            if (_amount == rounded)
            {
                return;
            }

            _amount = rounded;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AmountDisplay));
            OnPropertyChanged(nameof(AmountValueDisplay));
        }
    }

    /// <summary>Big green pill display, e.g. "$4,650".</summary>
    public string AmountDisplay => _amount == 0
        ? "$0"
        : _amount.ToString("C0", s_money);

    public string AmountValueDisplay => AmountDisplay;

    /// <summary>"30% of total"</summary>
    public string PercentOfTotalLabel =>
        string.Format(s_invariant, "{0:0.##}% of total", _percentage);

    public bool IsInvalid
    {
        get => _isInvalid;
        set => SetProperty(ref _isInvalid, value);
    }

    public ICommand? DeleteCommand { get; set; }

    public event EventHandler? PercentageChanged;
}
