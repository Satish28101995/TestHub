using Android.App;
using Android.Runtime;
using TestHub.Services;

namespace TestHub
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();

            // Kick off the FCM token retrieval as soon as the process is
            // up. By the time the user reaches the login screen the token
            // will be cached in DeviceTokenProvider, so the network
            // request completes synchronously from the UI's perspective.
            //
            // We deliberately fire-and-forget here — DeviceTokenProvider
            // already swallows any FCM/Play-Services errors internally
            // and falls back to a stable GUID, so this can never bubble
            // an exception up to the Android runtime.
            _ = Task.Run(async () =>
            {
                try
                {
                    var provider = new DeviceTokenProvider();
                    await provider.GetTokenAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Token prefetch is purely an optimisation. If it
                    // fails the next explicit GetTokenAsync() call from
                    // the auth flow will retry on its own.
                }
            });
        }
    }
}
