namespace WRMS.Domain.Enums;

public enum WeaponStatus
{
    PendingApproval = 0,
    Active = 1,
    Expired = 2,
    Suspended = 3,
    Revoked = 4,
    Rejected = 5
}

public enum LicenseStatus
{
    PendingApproval = 0,
    Active = 1,
    Expired = 2,
    Suspended = 3,
    Revoked = 4
}

public enum LicenseHistoryAction
{
    Issued = 0,
    Renewed = 1,
    Suspended = 2,
    Reinstated = 3,
    Revoked = 4,
    Expired = 5
}

public enum OwnerStatus
{
    Active = 0,
    Suspended = 1,
    Blacklisted = 2
}

public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2
}

public enum ApprovalDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum TransferApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum NotificationType
{
    LicenseExpiry = 0,
    PendingApproval = 1,
    RegistrationStatusUpdate = 2,
    AdminAlert = 3,
    TransferRequest = 4
}

public enum AuditAction
{
    Login = 0,
    Logout = 1,
    LoginFailed = 2,
    Create = 3,
    Update = 4,
    Delete = 5,
    Approve = 6,
    Reject = 7,
    Suspend = 8,
    Revoke = 9,
    Reinstate = 10,
    Renew = 11,
    Transfer = 12,
    Export = 13
}
