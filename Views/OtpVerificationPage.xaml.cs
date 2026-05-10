using TestHub.Services;
using TestHub.ViewModels;

namespace TestHub.Views;

public partial class OtpVerificationPage : ContentPage
{
    private readonly OtpVerificationViewModel _vm;

    public OtpVerificationPage()
    {
        InitializeComponent();
        _vm = ServiceHelper.GetRequiredService<OtpVerificationViewModel>();
        BindingContext = _vm;
    }

    // ------------------------------------------------------------------
    // Auto-advance focus between the 4 digit boxes. We branch on whether
    // the user typed a single digit (advance to the next box) or pasted
    // a multi-digit code (distribute the digits across the remaining
    // boxes). The TextChanged events are also the place where we honour
    // backspace — when a box is cleared, focus snaps back to the
    // previous one so the user can keep deleting without tapping.
    // ------------------------------------------------------------------
    private void OnDigit1Changed(object? sender, TextChangedEventArgs e)
        => HandleDigitChanged(e.NewTextValue, e.OldTextValue, OtpEntry1, OtpEntry2, isFirst: true);

    private void OnDigit2Changed(object? sender, TextChangedEventArgs e)
        => HandleDigitChanged(e.NewTextValue, e.OldTextValue, OtpEntry2, OtpEntry3, previous: OtpEntry1);

    private void OnDigit3Changed(object? sender, TextChangedEventArgs e)
        => HandleDigitChanged(e.NewTextValue, e.OldTextValue, OtpEntry3, OtpEntry4, previous: OtpEntry2);

    private void OnDigit4Changed(object? sender, TextChangedEventArgs e)
        => HandleDigitChanged(e.NewTextValue, e.OldTextValue, OtpEntry4, next: null, previous: OtpEntry3);

    /// <summary>
    /// Centralises the focus-shifting rules:
    ///   - single digit typed → focus the <paramref name="next"/> box
    ///   - box cleared (paste/backspace) → focus the <paramref name="previous"/> box
    ///   - multi-character paste → spill the extra digits into the
    ///     trailing boxes so a clipboard like "1234" fills the row
    /// </summary>
    private void HandleDigitChanged(
        string? newText,
        string? oldText,
        Entry current,
        Entry? next,
        Entry? previous = null,
        bool isFirst = false)
    {
        // Multi-digit paste — spread across the remaining boxes.
        if (!string.IsNullOrEmpty(newText) && newText.Length > 1)
        {
            DistributePastedDigits(newText, isFirst ? OtpEntry1 : current);
            return;
        }

        // Single digit typed → advance.
        if (!string.IsNullOrEmpty(newText))
        {
            next?.Focus();
            return;
        }

        // Cleared and the user is still hitting backspace → step back.
        if (string.IsNullOrEmpty(newText) && !string.IsNullOrEmpty(oldText))
        {
            previous?.Focus();
        }
    }

    /// <summary>
    /// Walks the four entries from <paramref name="startEntry"/> onwards
    /// and writes one digit per box, skipping non-numeric characters.
    /// Triggered when the platform delivers a >1-character TextChanged
    /// event (i.e. a clipboard paste).
    /// </summary>
    private void DistributePastedDigits(string text, Entry startEntry)
    {
        var entries = new[] { OtpEntry1, OtpEntry2, OtpEntry3, OtpEntry4 };
        var startIndex = Array.IndexOf(entries, startEntry);
        if (startIndex < 0)
        {
            startIndex = 0;
        }

        var index = startIndex;
        foreach (var ch in text)
        {
            if (!char.IsDigit(ch))
            {
                continue;
            }

            if (index >= entries.Length)
            {
                break;
            }

            entries[index].Text = ch.ToString();
            index++;
        }

        // Park the cursor on the next empty slot (or the last box if
        // the paste was a complete 4-digit code).
        var landing = Math.Min(index, entries.Length - 1);
        entries[landing].Focus();
    }
}
