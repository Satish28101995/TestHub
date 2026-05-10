using Firebase.CloudMessaging;
using Firebase.Core;
using Foundation;
using TestHub.Services;
using UIKit;
using UserNotifications;

namespace TestHub
{
    /// <summary>
    /// iOS application delegate. Beyond the MAUI plumbing, this class owns
    /// the FCM lifecycle on iOS:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Calls <see cref="App.Configure()"/> on
    ///       <see cref="FinishedLaunching"/> so the Firebase SDK reads
    ///       <c>GoogleService-Info.plist</c> before any other init code
    ///       runs.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Asks the OS for notification authorization and registers for
    ///       remote notifications, which is what triggers APNs to issue
    ///       a device token.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Manually forwards the APNs token to FCM (we set
    ///       <c>FirebaseAppDelegateProxyEnabled = NO</c> in Info.plist so
    ///       the swizzling is off).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Implements <see cref="IMessagingDelegate.DidReceiveRegistrationToken"/>
    ///       and pushes the resulting FCM token into
    ///       <see cref="DeviceTokenProvider.StoreToken(string)"/> — the
    ///       same static entry point the Android
    ///       <c>MyFirebaseMessagingService.OnNewToken</c> override calls.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Implements <see cref="IUNUserNotificationCenterDelegate.WillPresentNotification"/>
    ///       so foreground pushes show as banners — matching the Android
    ///       foreground UX provided by
    ///       <c>MyFirebaseMessagingService.OnMessageReceived</c>.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate, IUNUserNotificationCenterDelegate, IMessagingDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // Configure Firebase BEFORE the MAUI host spins up. App.Configure
            // reads GoogleService-Info.plist from the bundle; if the file is
            // missing it logs a warning and FCM stays uninitialised, but the
            // rest of the app keeps working — DeviceTokenProvider will fall
            // back to the persisted GUID just like on Android without
            // google-services.json.
            try
            {
                App.Configure();
            }
            catch
            {
                // Swallow — a missing/invalid Firebase config must never
                // prevent the app from launching.
            }

            // FCM token + foreground display delegates. We assign them
            // before MAUI loads so the very first DidReceiveRegistrationToken
            // (which can fire during launch) is delivered.
            Messaging.SharedInstance.Delegate = this;
            UNUserNotificationCenter.Current.Delegate = this;

            RequestNotificationAuthorization();

            // Same cold-start prefetch we run from Platforms/Android/MainApplication.OnCreate.
            // By the time the user reaches the login screen the FCM token
            // is already cached in DeviceTokenProvider, so login carries
            // it without an extra round-trip.
            _ = Task.Run(async () =>
            {
                try
                {
                    await new DeviceTokenProvider().GetTokenAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Prefetch is purely an optimisation — the next
                    // explicit GetTokenAsync() call will retry.
                }
            });

            return base.FinishedLaunching(application, launchOptions);
        }

        /// <summary>
        /// Asks the user once for permission to display alerts / play
        /// sounds / set the app icon badge, then kicks off APNs
        /// registration on the main thread. Both calls are required: the
        /// authorization controls the user-visible behaviour, the
        /// registration is what causes APNs to mint the device token
        /// that <see cref="RegisteredForRemoteNotifications"/> receives.
        /// </summary>
        private static void RequestNotificationAuthorization()
        {
            var options = UNAuthorizationOptions.Alert
                | UNAuthorizationOptions.Badge
                | UNAuthorizationOptions.Sound;

            UNUserNotificationCenter.Current.RequestAuthorization(options, (granted, error) =>
            {
                // We deliberately ignore `granted` here — even when the
                // user denies banners, the OS will still issue an APNs
                // token, which means we can keep the FCM-token-as-
                // deviceToken contract on /v1/Account/Login intact.
                _ = granted;
                _ = error;

                // Token registration must happen on the main thread.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.RegisterForRemoteNotifications();
                });
            });
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            // Forward the APNs token to FCM. With the
            // FirebaseAppDelegateProxyEnabled swizzling disabled we MUST
            // do this assignment manually — without it, FCM never derives
            // an FCM registration token from the APNs token and
            // DidReceiveRegistrationToken never fires.
            Messaging.SharedInstance.ApnsToken = deviceToken;
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            // APNs registration can fail when the build's aps-environment
            // entitlement doesn't match the provisioning profile, when
            // running on the iOS simulator without push capabilities, or
            // when Apple's APNs service is unreachable. None of these
            // should bring the auth flow down — DeviceTokenProvider keeps
            // serving the persisted GUID until APNs comes back.
            _ = error;
        }

        // ------------------------------------------------------------------
        // IMessagingDelegate — fires on first launch and whenever the FCM
        // SDK rotates the registration token.
        // ------------------------------------------------------------------
        [Export("messaging:didReceiveRegistrationToken:")]
        public void DidReceiveRegistrationToken(Messaging messaging, string fcmToken)
        {
            if (string.IsNullOrEmpty(fcmToken))
            {
                return;
            }

            // Same static entry point the Android side hits from
            // MyFirebaseMessagingService.OnNewToken. Updates the volatile
            // in-memory cache + persisted Preferences entry so the next
            // login / OTP-verify request carries the new value.
            DeviceTokenProvider.StoreToken(fcmToken);
        }

        // ------------------------------------------------------------------
        // IUNUserNotificationCenterDelegate — controls how a push is
        // surfaced while the app is in the foreground.
        // ------------------------------------------------------------------
        [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
        public void WillPresentNotification(
            UNUserNotificationCenter center,
            UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            // Mirrors the Android OnMessageReceived UX: when a push lands
            // while the app is active we still want the user to see a
            // banner + hear the sound + get the badge bump rather than
            // the notification being silently dropped.
            completionHandler(
                UNNotificationPresentationOptions.Banner
                | UNNotificationPresentationOptions.Sound
                | UNNotificationPresentationOptions.Badge);
        }

        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public void DidReceiveNotificationResponse(
            UNUserNotificationCenter center,
            UNNotificationResponse response,
            Action completionHandler)
        {
            // Hook for "user tapped the push" — wired up so the export
            // signature is satisfied. We currently rely on the standard
            // MAUI launch path to bring the user to the dashboard, so
            // there's nothing additional to do here yet.
            completionHandler();
        }
    }
}
