using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace TestHub
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int NotificationsPermissionRequestCode = 1001;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Android 13+ requires runtime permission to post notifications.
            // We ask once on launch — denial is non-fatal: FCM still
            // delivers the token, just without system-tray alerts.
            EnsureNotificationsPermission();
        }

        private void EnsureNotificationsPermission()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            {
                return;
            }

            const string permission = "android.permission.POST_NOTIFICATIONS";
            if (ContextCompat.CheckSelfPermission(this, permission) == Permission.Granted)
            {
                return;
            }

            ActivityCompat.RequestPermissions(
                this,
                new[] { permission },
                NotificationsPermissionRequestCode);
        }
    }
}
