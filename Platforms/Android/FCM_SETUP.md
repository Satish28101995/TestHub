# Firebase Cloud Messaging — Android setup

The TestHub Android project ships ready to receive FCM push notifications and
to forward the FCM **registration token** as the `deviceToken` field on
`/v1/Account/Login` and `/v1/Account/Email/VerifyOtp`. The only piece that
needs to come from outside source control is the Firebase project
configuration file — `google-services.json`.

Without that file the build still succeeds, but `IDeviceTokenProvider` will
fall back to a stable per-install GUID at runtime (instead of a real FCM
token) and pushes won't be delivered.

## One-time configuration

1. Open the [Firebase console](https://console.firebase.google.com/) and
   create a project (or select an existing one).
2. In the project, click **Add app → Android**.
   - **Package name:** `com.odin.au` (must match `<ApplicationId>` in
     `TestHub.csproj` — the project ships with `com.odin.au` to align
     with the Firebase project `odin-33602` whose configs live in
     `Platforms/Android/google-services.json`).
   - **App nickname:** anything you like, e.g. `TestHub Android`.
   - **Debug signing certificate SHA-1:** optional for FCM, required only if
     you also want Google Sign-In / Dynamic Links.
3. Download the generated `google-services.json` and copy it to
   `Platforms/Android/google-services.json` (right next to this file).
4. The file is auto-discovered by `TestHub.csproj`:

   ```xml
   <ItemGroup Condition="$(TargetFramework.Contains('-android')) AND Exists('Platforms\Android\google-services.json')">
       <GoogleServicesJson Include="Platforms\Android\google-services.json" />
   </ItemGroup>
   ```

   No further IDE steps are required — `dotnet build` will pick it up on the
   next compile.

## How the token flows into the API

```text
                      ┌──────────────────────────┐
   Firebase SDK  ──→  │ MyFirebaseMessagingService│  OnNewToken(token)
                      └──────────┬───────────────┘
                                 │
                                 ▼
                  DeviceTokenProvider.StoreToken(token)   (Preferences + in-memory cache)
                                 │
                                 ▼
              IDeviceTokenProvider.GetTokenAsync()
                                 │
                                 ▼
                AuthService.LoginAsync / VerifyEmailOtpAsync
                                 │
                                 ▼
                /v1/Account/Login   /v1/Account/Email/VerifyOtp
                            { "deviceToken": "<FCM token>", ... }
```

- On a **cold start**, `MainApplication.OnCreate()` fires
  `DeviceTokenProvider.GetTokenAsync()` on a background thread so the token
  is warming up while the user is still on the splash / login screen.
- When FCM rotates the token (every ~3-6 months, or after app reinstall),
  `OnNewToken` writes the new value to the same cache. The very next
  login / OTP-verify request automatically carries it.
- If FCM ever fails (no `google-services.json`, Play Services unavailable,
  no network on first run), the provider returns the persisted GUID and the
  auth flow continues. The next successful FCM call will overwrite the
  GUID on disk.

## Manifest pieces already wired up

`Platforms/Android/AndroidManifest.xml`:

- `<uses-permission android:name="com.google.android.c2dm.permission.RECEIVE" />` — required for FCM message delivery.
- `<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />` — Android 13+ runtime permission. Requested in `MainActivity.OnCreate`.
- `default_notification_icon` / `default_notification_color` / `default_notification_channel_id` meta-data — used by FCM when the OS auto-renders a push that arrived while the app was backgrounded.

`MyFirebaseMessagingService` is auto-registered via its `[Service]` +
`[IntentFilter]` attributes — no manual `<service>` entry needed.

## Sending a test push

After installing a build that includes `google-services.json`:

1. Launch the app once and log in (so the FCM token is sent to the server).
2. Grab the token from your backend logs or by adding a temporary
   `Logger.Log` line inside `MyFirebaseMessagingService.OnNewToken`.
3. In the Firebase console, go to **Cloud Messaging → Send your first
   message**.
4. Pick **Send test message**, paste the FCM token, and hit **Test**.

The notification should arrive in the system tray immediately. If the app
is in the foreground when the push lands, `MyFirebaseMessagingService.OnMessageReceived`
will surface it via `NotificationManagerCompat`.
