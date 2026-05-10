namespace TestHub.Models.Auth;

/// <summary>
/// Application user type. Values mirror the server contract.
/// </summary>
public enum UserType
{
    Unknown = 0,
    Contractor = 2,
    Customer = 3,
    Admin = 1,
}
