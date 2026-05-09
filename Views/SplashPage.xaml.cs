namespace TestHub.Views;

public partial class SplashPage : ContentPage
{
    private static readonly TimeSpan EntranceAnimationDuration = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan SplashHoldDuration = TimeSpan.FromSeconds(2);

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Run entrance animation and the minimum hold time concurrently so the
        // total perceived splash never exceeds SplashHoldDuration.
        var animation = AnimateLogoEntranceAsync();
        var hold = Task.Delay(SplashHoldDuration);

        try
        {
            await Task.WhenAll(animation, hold).ConfigureAwait(true);

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//login").ConfigureAwait(true);
            }
        }
        catch (TaskCanceledException)
        {
            // Page disappeared mid-animation — safe to ignore.
        }
    }

    private async Task AnimateLogoEntranceAsync()
    {
        var durationMs = (uint)EntranceAnimationDuration.TotalMilliseconds;
        var fadeIn = LogoImage.FadeToAsync(1, durationMs, Easing.CubicOut);
        var scaleUp = LogoImage.ScaleToAsync(1.0, durationMs, Easing.CubicOut);
        await Task.WhenAll(fadeIn, scaleUp).ConfigureAwait(true);
    }
}
