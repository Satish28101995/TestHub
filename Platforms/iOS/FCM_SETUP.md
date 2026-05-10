# Firebase Cloud Messaging — iOS setup

The TestHub iOS project ships ready to receive FCM push notifications and
to forward the FCM **registration token** as the `deviceToken` field on
`/v1/Account/Login` and `/v1/Account/Email/VerifyOtp`. The pieces that
need to come from outside source control are the Apple Developer push
certificate / key and the Firebase project configuration file —
`GoogleService-Info.plist`.

Without those the build still succeeds, but `IDeviceTokenProvider` will
fall back to a stable per-install GUID at runtime (instead of a real FCM
token) and pushes won't be delivered.

## Binding library used

We use the community-maintained
[`AdamE.Firebase.iOS.CloudMessaging`](https://www.nuget.org/packages/AdamE.Firebase.iOS.CloudMessaging)
12.5.0.4 native bindings — the original `Xamarin.Firebase.iOS.*`
packages were archived in March 2024. AdamE's fork tracks the modern
Firebase iOS SDK (12.x) and exposes the same `Firebase.Core` /
`Firebase.CloudMessaging` namespaces Apple's docs reference, so the
TypeScript-style swizzling-disabled flow (manual `ApnsToken` assignment +
explicit `IMessagingDelegate.DidReceiveRegistrationToken`) compiles
verbatim.

If you'd rather use [`Plugin.Firebase.CloudMessaging`](https://www.nuget.org/packages/Plugin.Firebase.CloudMessaging)
(higher-level cross-platform abstraction, also `net10.0-ios`-compatible
as of 4.0.x), swap the `<PackageReference>` in `TestHub.csproj` and
replace the AppDelegate's native calls with
`CrossFirebaseCloudMessaging.Current.*`. The `IDeviceTokenProvider`
contract stays unchanged.

## One-time configuration

### 1. Apple Developer portal

1. Sign in to <https://developer.apple.com/account> and go to
   **Certificates, Identifiers & Profiles → Identifiers**.
2. Find / create the App ID for `com.odin.au` (it must match
   `<ApplicationId>` in `TestHub.csproj`, which is wired to the
   Firebase project `odin-33602`).
3. Tick **Push Notifications** under **Capabilities**, save.
4. Under **Keys**, click **+** and create a new key with the **Apple
   Push Notifications service (APNs)** capability ticked. Download the
   resulting `.p8` file — Apple only lets you download it once. Note
   the **Key ID** and your **Team ID** (top-right of the page).
5. Re-generate / update your provisioning profile so it picks up the
   Push Notifications entitlement, then re-import it in your build host.

### 2. Firebase console

1. Open the [Firebase console](https://console.firebase.google.com/) and
   create a project (or reuse the one used for Android).
2. Click **Add app → iOS+**.
   - **Bundle ID:** `com.odin.au` (must match `<ApplicationId>` in
     `TestHub.csproj`; the existing `Platforms/iOS/GoogleService-Info.plist`
     in this repo is for that bundle id under Firebase project
     `odin-33602`).
   - **App nickname:** anything you like, e.g. `TestHub iOS`.
3. Download the generated `GoogleService-Info.plist` and copy it to:

   ```
   Platforms/iOS/GoogleService-Info.plist
   ```

   The file is auto-discovered by `TestHub.csproj`:

   ```xml
   <ItemGroup Condition="($(TargetFramework.Contains('-ios')) OR $(TargetFramework.Contains('-maccatalyst'))) AND Exists('Platforms\iOS\GoogleService-Info.plist')">
       <BundleResource Include="Platforms\iOS\GoogleService-Info.plist" LogicalName="GoogleService-Info.plist" />
   </ItemGroup>
   ```

   No further IDE steps are required — `dotnet build -f net10.0-ios`
   will pick it up on the next compile.
4. In the Firebase project, go to
   **Project Settings → Cloud Messaging → Apple app configuration**
   and upload the `.p8` APNs auth key from step 1.4. Provide the **Key
   ID** and **Team ID** when prompted. This is what lets Firebase trade
   APNs tokens for FCM tokens on Apple's behalf.

### 3. Verify Entitlements.plist

`Platforms/iOS/Entitlements.plist` ships with:

```xml
<key>aps-environment</key>
<string>development</string>
```

Flip to `production` before archiving for App Store / TestFlight
External builds — that string must match the provisioning profile, or
APNs will refuse the build's token registration.

## How the token flows into the API

```text
                        ┌─────────────────────────┐
   APNs ──RegisteredFor──▶ AppDelegate           │
                        │   ApnsToken assignment │
                        └──────────┬──────────────┘
                                   │
                                   ▼
                  Firebase iOS SDK exchanges APNs ↔ FCM
                                   │
                                   ▼
                ┌─────────────────────────────────────┐
                │ AppDelegate.DidReceiveRegistration  │
                │ Token(messaging, fcmToken)          │
                └──────────┬──────────────────────────┘
                           │
                           ▼
            DeviceTokenProvider.StoreToken(fcmToken)   (Preferences + in-memory cache)
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

- On a **cold start**, `AppDelegate.FinishedLaunching()` fires
  `DeviceTokenProvider.GetTokenAsync()` on a background thread so the
  token is warming up while the user is still on the splash / login
  screen.
- When FCM rotates the token (every ~3-6 months, or after app
  reinstall / device restore), `DidReceiveRegistrationToken` writes the
  new value to the same cache. The very next login / OTP-verify request
  automatically carries it.
- If FCM ever fails (no `GoogleService-Info.plist`, APNs unreachable,
  no network on first run), the provider returns the persisted GUID and
  the auth flow continues. The next successful FCM call will overwrite
  the GUID on disk.

## Why we disable Firebase's swizzling

`Platforms/iOS/Info.plist` sets:

```xml
<key>FirebaseAppDelegateProxyEnabled</key>
<false/>
```

By default the FCM SDK swizzles `UIApplicationDelegate` so it can
intercept APNs token registration and notification delivery callbacks
behind your back. We turn that off so the token-flow plumbing is
explicit:

- `AppDelegate.RegisteredForRemoteNotifications` forwards the APNs token
  to `Messaging.SharedInstance.ApnsToken` ourselves.
- `AppDelegate.DidReceiveRegistrationToken` (declared via the
  `IMessagingDelegate` interface) is the single source of truth for the
  FCM token, mirroring how
  `MyFirebaseMessagingService.OnNewToken` works on Android.

## Info.plist pieces already wired up

`Platforms/iOS/Info.plist`:

- `UIBackgroundModes = [ "remote-notification", "fetch" ]` — required
  for FCM to deliver silent / data pushes and to refresh the
  registration token in the background.
- `FirebaseAppDelegateProxyEnabled = NO` — see the section above.

`Platforms/iOS/Entitlements.plist`:

- `aps-environment = development` — APNs sandbox toggle.

## Sending a test push

After installing a build that includes `GoogleService-Info.plist` on a
**physical iOS device** (the Simulator does not deliver remote pushes):

1. Launch the app once, accept the notification permission prompt, and
   log in (so the FCM token is sent to the server).
2. Grab the token from your backend logs or by adding a temporary
   `Console.WriteLine($"FCM token: {fcmToken}")` line inside
   `AppDelegate.DidReceiveRegistrationToken`.
3. In the Firebase console, go to **Cloud Messaging → Send your first
   message**.
4. Pick **Send test message**, paste the FCM token, and hit **Test**.

The notification should arrive in the system tray immediately. If the
app is in the foreground when the push lands,
`AppDelegate.WillPresentNotification` will surface a banner with sound
and a badge bump — the iOS equivalent of Android's
`MyFirebaseMessagingService.OnMessageReceived` UX.

## Building from Windows

`dotnet build -f net10.0-ios` requires either macOS or a configured
remote Mac build host (e.g. Visual Studio Pair to Mac). On Windows the
Android, Windows, and Mac Catalyst (when targeted from a Mac host)
builds work as before. The iOS-specific code in this project is guarded
by `#if IOS` and the iOS-only `<ItemGroup>`s in `TestHub.csproj`, so the
Android build remains unaffected — you can verify with
`dotnet build TestHub.csproj -f net10.0-android`.
