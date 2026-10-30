using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.CustomerAccounts;

namespace TEO_Airline_Project.Services;

public sealed class CustomerAccountService : ICustomerAccountService
{
    private static readonly Regex PhonePattern =
        new(@"^(0\d{9}|\+84\d{9})$", RegexOptions.Compiled);

    private readonly AirlineBookingDBContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<CustomerAccountService> _logger;

    public CustomerAccountService(
        AirlineBookingDBContext context,
        IPasswordHasher<User> passwordHasher,
        ILogger<CustomerAccountService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public Task<bool> IsStaffAsync(int staffId, CancellationToken cancellationToken) =>
        _context.Users.AsNoTracking().AnyAsync(
            user => user.UserId == staffId && user.Role == "Staff",
            cancellationToken);

    public async Task<CustomerAccountResult<CustomerAccountPageResponse>> SearchAsync(
        CustomerAccountSearchRequest request,
        CancellationToken cancellationToken)
    {
        var status = request.Status?.Trim().ToLowerInvariant();
        if (status is not (null or "" or "all" or "active" or "locked"))
        {
            return CustomerAccountResult<CustomerAccountPageResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "Trạng thái lọc không hợp lệ.");
        }

        var query = _context.Users.AsNoTracking()
            .Where(user => user.Role == "Customer");
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(user =>
                user.FullName.Contains(search) ||
                user.Username.Contains(search) ||
                user.Email.Contains(search) ||
                user.Phone.Contains(search));
        }

        var allCount = await query.CountAsync(cancellationToken);
        var activeCount = await query.CountAsync(
            user => user.IsActive != false,
            cancellationToken);
        var lockedCount = await query.CountAsync(
            user => user.IsActive == false,
            cancellationToken);

        if (status == "active")
        {
            query = query.Where(user => user.IsActive != false);
        }
        else if (status == "locked")
        {
            query = query.Where(user => user.IsActive == false);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)request.PageSize));
        var page = Math.Min(request.Page, pageCount);
        var rows = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.UserId)
            .Skip((page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(user => new
            {
                user.UserId,
                user.Username,
                user.FullName,
                user.Email,
                user.Phone,
                user.IsActive,
                user.CreatedAt,
                BookingCount = _context.Bookings.Count(booking => booking.UserId == user.UserId)
            })
            .ToListAsync(cancellationToken);

        var response = new CustomerAccountPageResponse(
            rows.Select(row => ToResponse(
                row.UserId,
                row.Username,
                row.FullName,
                row.Email,
                row.Phone,
                row.IsActive,
                row.CreatedAt,
                row.BookingCount)).ToList(),
            page,
            request.PageSize,
            totalCount,
            allCount,
            activeCount,
            lockedCount);

        return CustomerAccountResult<CustomerAccountPageResponse>.Success(response);
    }

    public async Task<CustomerAccountResult<CustomerAccountResponse>> GetByIdAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking()
            .Where(candidate => candidate.UserId == customerId && candidate.Role == "Customer")
            .Select(candidate => new
            {
                candidate.UserId,
                candidate.Username,
                candidate.FullName,
                candidate.Email,
                candidate.Phone,
                candidate.IsActive,
                candidate.CreatedAt,
                BookingCount = _context.Bookings.Count(booking => booking.UserId == candidate.UserId)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return user is null
            ? CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status404NotFound,
                "Không tìm thấy tài khoản khách hàng.")
            : CustomerAccountResult<CustomerAccountResponse>.Success(ToResponse(
                user.UserId,
                user.Username,
                user.FullName,
                user.Email,
                user.Phone,
                user.IsActive,
                user.CreatedAt,
                user.BookingCount));
    }

    public async Task<CustomerAccountResult<CustomerAccountResponse>> UpdateAsync(
        int customerId,
        UpdateCustomerAccountRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users.SingleOrDefaultAsync(
            candidate => candidate.UserId == customerId && candidate.Role == "Customer",
            cancellationToken);
        if (user is null)
        {
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status404NotFound,
                "Không tìm thấy tài khoản khách hàng.");
        }

        var fullName = request.FullName.Trim();
        var email = request.Email.Trim();
        var phone = NormalizePhone(request.Phone);
        if (!PhonePattern.IsMatch(phone))
        {
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "Số điện thoại không đúng định dạng. Ví dụ: 0912345678.");
        }

        if (await _context.Users.AnyAsync(
                candidate => candidate.UserId != customerId && candidate.Email == email,
                cancellationToken))
        {
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status409Conflict,
                "Email này đã được một tài khoản khác sử dụng.");
        }

        if (await _context.Users.AnyAsync(
                candidate => candidate.UserId != customerId && candidate.Phone == phone,
                cancellationToken))
        {
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status409Conflict,
                "Số điện thoại này đã được một tài khoản khác sử dụng.");
        }

        user.FullName = fullName;
        user.Email = email;
        user.Phone = phone;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _logger.LogInformation(
                exception,
                "Customer {CustomerId} profile update conflicted with a unique account value.",
                customerId);
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status409Conflict,
                "Email hoặc số điện thoại này vừa được một tài khoản khác sử dụng.");
        }

        return await GetByIdAsync(customerId, cancellationToken);
    }

    public async Task<CustomerAccountResult<CustomerAccountResponse>> ResetPasswordAsync(
        int customerId,
        ResetCustomerPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var passwordError = ValidatePassword(request.NewPassword, request.ConfirmPassword);
        if (passwordError is not null)
        {
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status400BadRequest,
                passwordError);
        }

        var user = await _context.Users.SingleOrDefaultAsync(
            candidate => candidate.UserId == customerId && candidate.Role == "Customer",
            cancellationToken);
        if (user is null)
        {
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status404NotFound,
                "Không tìm thấy tài khoản khách hàng.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(customerId, cancellationToken);
    }

    public async Task<CustomerAccountResult<CustomerAccountResponse>> LockAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users.SingleOrDefaultAsync(
            candidate => candidate.UserId == customerId && candidate.Role == "Customer",
            cancellationToken);
        if (user is null)
        {
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status404NotFound,
                "Không tìm thấy tài khoản khách hàng.");
        }

        user.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(customerId, cancellationToken);
    }

    public async Task<CustomerAccountResult<CustomerAccountResponse>> UnlockAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users.SingleOrDefaultAsync(
            candidate => candidate.UserId == customerId && candidate.Role == "Customer",
            cancellationToken);
        if (user is null)
        {
            return CustomerAccountResult<CustomerAccountResponse>.Failure(
                StatusCodes.Status404NotFound,
                "Không tìm thấy tài khoản khách hàng.");
        }

        user.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(customerId, cancellationToken);
    }

    private static CustomerAccountResponse ToResponse(
        int userId,
        string username,
        string fullName,
        string email,
        string phone,
        bool? isActive,
        DateTime? createdAt,
        int bookingCount) =>
        new(
            userId,
            username,
            fullName,
            email,
            phone,
            isActive != false,
            createdAt is null ? null : new DateTimeOffset(createdAt.Value),
            bookingCount);

    private static string NormalizePhone(string phone) =>
        new(phone
            .Trim()
            .Where(character =>
                !char.IsWhiteSpace(character) &&
                character is not '-' and not '.' and not '(' and not ')')
            .ToArray());

    private static string? ValidatePassword(string password, string confirmPassword)
    {
        if (password.Length < 8 ||
            !password.Any(char.IsLetter) ||
            !password.Any(character =>
                !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character)))
        {
            return "Mật khẩu mới phải có ít nhất 8 ký tự, gồm chữ cái và ký tự đặc biệt.";
        }

        return string.Equals(password, confirmPassword, StringComparison.Ordinal)
            ? null
            : "Mật khẩu xác nhận không khớp.";
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
