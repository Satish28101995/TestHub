using System.Globalization;
using Microsoft.Maui.Graphics;

namespace TestHub.Drawables;

/// <summary>
/// IDrawable that paints the "Revenue Trend" bar chart used on the Reports
/// page. Designed to mirror the marketing screenshot — indigo-gradient bars
/// with rounded tops, a single highlighted bar with a floating tooltip,
/// integer Y axis grid (0, 10k, 20k…), and three-letter month labels.
/// Colors map to BrandAccent / BrandAccentLight / BrandAccentDark in
/// Resources/Styles/Colors.xaml so the chart stays in lockstep with the
/// rest of the design system.
/// </summary>
public sealed class RevenueBarChartDrawable : IDrawable
{
    private static readonly Color GridColor        = Color.FromArgb("#F1F5F9");
    private static readonly Color AxisLabelColor   = Color.FromArgb("#94A3B8");
    private static readonly Color BarTopColor      = Color.FromArgb("#4338CA"); // BrandAccentDark
    private static readonly Color BarBottomColor   = Color.FromArgb("#A5B4FC"); // BrandAccentLight
    private static readonly Color TooltipBgColor   = Color.FromArgb("#6366F1"); // BrandAccent
    private static readonly Color TooltipTextColor = Colors.White;

    /// <summary>
    /// 12 entries, one per month. Missing months should be supplied as 0 so
    /// the X axis still renders the full Jan–Dec range.
    /// </summary>
    public IReadOnlyList<(string Label, decimal Amount)> Bars { get; set; }
        = Array.Empty<(string, decimal)>();

    /// <summary>
    /// Index (0..11) of the bar that should display the floating tooltip.
    /// Use -1 to hide the tooltip entirely (e.g. while data is loading).
    /// </summary>
    public int HighlightedIndex { get; set; } = -1;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Bars.Count == 0)
        {
            return;
        }

        const float leftAxisWidth   = 36f;
        const float bottomAxisHeight = 24f;
        const float topPadding       = 28f;
        const float rightPadding     = 8f;
        const float barWidth         = 12f;
        const float barCornerRadius  = 6f;

        var plotLeft   = dirtyRect.Left + leftAxisWidth;
        var plotRight  = dirtyRect.Right - rightPadding;
        var plotTop    = dirtyRect.Top + topPadding;
        var plotBottom = dirtyRect.Bottom - bottomAxisHeight;
        var plotHeight = Math.Max(1f, plotBottom - plotTop);
        var plotWidth  = Math.Max(1f, plotRight - plotLeft);

        // ---------- Y axis scale (rounded up to a "nice" step) ----------
        var maxAmount = (float)Math.Max(1d, (double)Bars.Max(b => b.Amount));
        var (axisMax, axisStep) = NiceScale(maxAmount, targetSteps: 5);

        canvas.FontSize = 10f;
        canvas.FontColor = AxisLabelColor;

        // Horizontal grid lines + Y labels
        canvas.StrokeColor = GridColor;
        canvas.StrokeSize = 1f;

        for (var i = 0; i <= 5; i++)
        {
            var fraction = i / 5f;
            var y = plotBottom - (plotHeight * fraction);

            if (i > 0)
            {
                canvas.DrawLine(plotLeft, y, plotRight, y);
            }

            var label = FormatYLabel(axisStep * i);
            canvas.DrawString(
                label,
                dirtyRect.Left,
                y - 6f,
                leftAxisWidth - 6f,
                12f,
                HorizontalAlignment.Right,
                VerticalAlignment.Center);
        }

        // ---------- Bars ----------
        var slot = plotWidth / Bars.Count;

        for (var i = 0; i < Bars.Count; i++)
        {
            var (label, amount) = Bars[i];
            var center = plotLeft + (slot * i) + (slot / 2f);

            var amountFloat = (float)amount;
            var ratio = axisMax <= 0f ? 0f : amountFloat / axisMax;
            ratio = Math.Clamp(ratio, 0f, 1f);
            var barHeight = plotHeight * ratio;

            if (barHeight > 0f)
            {
                var x = center - (barWidth / 2f);
                var y = plotBottom - barHeight;

                // Vertical gold gradient: dark on top, light at the base.
                var gradient = new LinearGradientPaint
                {
                    StartColor = BarTopColor,
                    EndColor   = BarBottomColor,
                    StartPoint = new PointF(0.5f, 0f),
                    EndPoint   = new PointF(0.5f, 1f),
                };

                canvas.SetFillPaint(gradient, new RectF(x, y, barWidth, barHeight));
                var radius = Math.Min(barCornerRadius, barHeight / 2f);
                canvas.FillRoundedRectangle(x, y, barWidth, barHeight, radius, radius, 0f, 0f);
            }

            // Month label
            canvas.FontColor = AxisLabelColor;
            canvas.FontSize = 10f;
            canvas.DrawString(
                label,
                center - (slot / 2f),
                plotBottom + 6f,
                slot,
                bottomAxisHeight - 6f,
                HorizontalAlignment.Center,
                VerticalAlignment.Top);
        }

        // ---------- Tooltip (highlighted bar) ----------
        if (HighlightedIndex >= 0 && HighlightedIndex < Bars.Count)
        {
            var (_, amount) = Bars[HighlightedIndex];
            if (amount > 0m)
            {
                DrawTooltip(canvas, HighlightedIndex, amount,
                    plotLeft, plotBottom, plotHeight, slot, axisMax);
            }
        }
    }

    private static void DrawTooltip(ICanvas canvas, int index, decimal amount,
        float plotLeft, float plotBottom, float plotHeight, float slot, float axisMax)
    {
        const float padX = 8f;
        const float padY = 4f;
        const float arrowH = 5f;

        var center = plotLeft + (slot * index) + (slot / 2f);

        var ratio = axisMax <= 0f ? 0f : (float)amount / axisMax;
        ratio = Math.Clamp(ratio, 0f, 1f);
        var barHeight = plotHeight * ratio;
        var barTop = plotBottom - barHeight;

        var text = string.Format(CultureInfo.InvariantCulture, "$ {0:0}", amount);

        canvas.FontSize = 11f;
        var measure = canvas.GetStringSize(text, Microsoft.Maui.Graphics.Font.Default, 11f);
        var pillW = measure.Width + (padX * 2f);
        var pillH = measure.Height + (padY * 2f);

        var x = center - (pillW / 2f);
        var y = barTop - pillH - arrowH - 2f;

        // Pill body
        canvas.FillColor = TooltipBgColor;
        canvas.FillRoundedRectangle(x, y, pillW, pillH, 6f);

        // Down-pointing arrow
        var path = new PathF();
        path.MoveTo(center - 5f, y + pillH);
        path.LineTo(center + 5f, y + pillH);
        path.LineTo(center, y + pillH + arrowH);
        path.Close();
        canvas.FillPath(path);

        // Tooltip text
        canvas.FontColor = TooltipTextColor;
        canvas.DrawString(
            text,
            x,
            y,
            pillW,
            pillH,
            HorizontalAlignment.Center,
            VerticalAlignment.Center);
    }

    /// <summary>
    /// Returns a "nice" axis maximum and step that comfortably contains
    /// <paramref name="rawMax"/>. The result rounds the step up to the
    /// next 1/2/5 × 10^n so the grid lines land on round numbers.
    /// </summary>
    private static (float Max, float Step) NiceScale(float rawMax, int targetSteps)
    {
        if (rawMax <= 0f)
        {
            return (50_000f, 10_000f);
        }

        var roughStep = rawMax / targetSteps;
        var magnitude = (float)Math.Pow(10d, Math.Floor(Math.Log10(roughStep)));
        var residual  = roughStep / magnitude;

        float niceResidual;
        if      (residual <= 1f) niceResidual = 1f;
        else if (residual <= 2f) niceResidual = 2f;
        else if (residual <= 5f) niceResidual = 5f;
        else                     niceResidual = 10f;

        var step = niceResidual * magnitude;
        var max  = (float)(Math.Ceiling(rawMax / step) * step);
        if (max <= 0f) max = step * targetSteps;
        return (max, step);
    }

    private static string FormatYLabel(float value)
    {
        if (value <= 0f) return "0";
        if (value >= 1_000_000f)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.#}M", value / 1_000_000f);
        }
        if (value >= 1_000f)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0}k", value / 1_000f);
        }
        return value.ToString("0", CultureInfo.InvariantCulture);
    }
}
