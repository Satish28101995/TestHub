namespace TestHub.Models.Auth;

/// <summary>
/// Numeric device type expected by the auth API. The default
/// (<see cref="Unknown"/>) keeps backwards compatibility with
/// servers that haven't enumerated all platforms.
/// </summary>
public enum DeviceType
{
    Unknown = 0,
    Android = 1,
    iOS = 2,
    Windows = 3,
    MacCatalyst = 4,
    Web = 5,
}
