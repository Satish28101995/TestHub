using System.Collections;
using TestHub.Drawables;
using TestHub.Models.Report;

namespace TestHub.Controls;

/// <summary>
/// GraphicsView-backed bar chart used on the Reports page. Exposes
/// <see cref="ItemsSource"/> and <see cref="HighlightedIndex"/> bindable
/// properties so the surrounding XAML stays declarative.
/// </summary>
public sealed class RevenueChartView : GraphicsView
{
    private readonly RevenueBarChartDrawable _drawable = new();

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(RevenueChartView),
            propertyChanged: OnDataChanged);

    public static readonly BindableProperty HighlightedIndexProperty =
        BindableProperty.Create(
            nameof(HighlightedIndex),
            typeof(int),
            typeof(RevenueChartView),
            -1,
            propertyChanged: OnDataChanged);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int HighlightedIndex
    {
        get => (int)GetValue(HighlightedIndexProperty);
        set => SetValue(HighlightedIndexProperty, value);
    }

    public RevenueChartView()
    {
        BackgroundColor = Colors.Transparent;
        Drawable = _drawable;
        ApplyData();
    }

    private static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is RevenueChartView view)
        {
            view.ApplyData();
        }
    }

    private void ApplyData()
    {
        var bars = new List<(string Label, decimal Amount)>(12);
        var monthLabels = new[]
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "July", "Aug", "Sep", "Oct", "Nov", "Dec",
        };

        // Pre-seed 12 months so the chart still renders the full Jan–Dec
        // axis even when the API returns a partial list.
        var byMonth = new (string Label, decimal Amount)[12];
        for (var i = 0; i < 12; i++)
        {
            byMonth[i] = (monthLabels[i], 0m);
        }

        if (ItemsSource is IEnumerable<MonthlyRevenue> monthly)
        {
            foreach (var m in monthly)
            {
                if (m is null) continue;
                var index = m.Month - 1;
                if (index is < 0 or > 11) continue;

                var label = string.IsNullOrWhiteSpace(m.MonthLabel) ? monthLabels[index] : m.MonthLabel!;
                byMonth[index] = (label, m.Amount);
            }
        }

        bars.AddRange(byMonth);
        _drawable.Bars = bars;
        _drawable.HighlightedIndex = HighlightedIndex;
        Invalidate();
    }
}
