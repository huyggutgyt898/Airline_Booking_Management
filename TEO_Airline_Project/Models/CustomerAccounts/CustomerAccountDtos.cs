using System.ComponentModel.DataAnnotations;

namespace TEO_Airline_Project.Models.CustomerAccounts;

/// <summary>Filters the customer accounts visible to counter staff.</summary>
public sealed record CustomerAccountSearchRequest
{
    /// <summary>Matches a customer's name, username, email, or phone number.</summary>
    [StringLength(100)]
    public string? Search { get; init; }

    /// <summary>Limits results to all, active, or locked customer accounts.</summary>
    [StringLength(20)]
    public string? Status { get; init; } = "all";

    /// <summary>The one-based page number.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>The number of accounts per page.</summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}

/// <summary>Represents a customer account returned to counter staff.</summary>
public sealed record CustomerAccountResponse(
    int UserId,
    string Username,
    string FullName,
    string Email,
    string Phone,
    bool IsActive,
    DateTimeOffset? CreatedAt,
    int BookingCount);

/// <summary>A page of customer accounts and matching status totals.</summary>
public sealed record CustomerAccountPageResponse(
    IReadOnlyList<CustomerAccountResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int AllCount,
    int ActiveCount,
    int LockedCount);

/// <summary>Updates the editable profile fields of a customer account.</summary>
public sealed record UpdateCustomerAccountRequest
{
    /// <summary>The customer's display name.</summary>
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải có từ 2 đến 100 ký tự.")]
    public required string FullName { get; init; }

    /// <summary>The customer's email address.</summary>
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
    public required string Email { get; init; }

    /// <summary>The customer's Vietnamese phone number.</summary>
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    public required string Phone { get; init; }
}

/// <summary>Sets a new password without exposing the current password.</summary>
public sealed record ResetCustomerPasswordRequest
{
    /// <summary>The new password to hash and store.</summary>
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải có từ 8 đến 128 ký tự.")]
    public required string NewPassword { get; init; }

    /// <summary>Must match the new password.</summary>
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    public required string ConfirmPassword { get; init; }
}

/// <summary>Contains either a successful service value or a user-safe error.</summary>
public sealed record CustomerAccountResult<T>(int? ErrorStatus, string? Error, T? Value)
{
    public bool Succeeded => ErrorStatus is null;

    public static CustomerAccountResult<T> Success(T value) => new(null, null, value);

    public static CustomerAccountResult<T> Failure(int status, string error) =>
        new(status, error, default);
}
