using Microsoft.AspNetCore.Mvc;
using TEO_Airline_Project.Models.CustomerAccounts;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers;

[ApiController]
[Route("api/counter/customers")]
public sealed class CustomerAccountsController : ControllerBase
{
    private readonly ICustomerAccountService _customerService;

    public CustomerAccountsController(ICustomerAccountService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>Lists searchable, paginated customer accounts for counter staff.</summary>
    [HttpGet]
    [EndpointSummary("Danh sách tài khoản khách hàng")]
    [EndpointDescription("Tìm kiếm và lọc tài khoản có role Customer; chỉ dành cho nhân viên quầy đã đăng nhập.")]
    [ProducesResponseType<CustomerAccountPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] CustomerAccountSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsStaffAsync(cancellationToken))
        {
            return UnauthorizedStaff();
        }

        var result = await _customerService.SearchAsync(request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(
                statusCode: result.ErrorStatus,
                title: "Không thể tải danh sách khách hàng",
                detail: result.Error);
    }

    /// <summary>Returns the details of one customer account.</summary>
    [HttpGet("{customerId:int}")]
    [EndpointSummary("Thông tin tài khoản khách hàng")]
    [EndpointDescription("Trả về thông tin hồ sơ và số đặt chỗ, không bao gồm mật khẩu hoặc mã băm.")]
    [ProducesResponseType<CustomerAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int customerId,
        CancellationToken cancellationToken)
    {
        if (!await IsStaffAsync(cancellationToken))
        {
            return UnauthorizedStaff();
        }

        var result = await _customerService.GetByIdAsync(customerId, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(
                statusCode: result.ErrorStatus,
                title: "Không thể tải tài khoản khách hàng",
                detail: result.Error);
    }

    /// <summary>Updates a customer's name, email, and phone number.</summary>
    [HttpPut("{customerId:int}")]
    [ValidateAntiForgeryToken]
    [EndpointSummary("Cập nhật thông tin khách hàng")]
    [EndpointDescription("Cập nhật tên, email và số điện thoại; email và điện thoại phải chưa được tài khoản khác sử dụng.")]
    [ProducesResponseType<CustomerAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int customerId,
        [FromBody] UpdateCustomerAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsStaffAsync(cancellationToken))
        {
            return UnauthorizedStaff();
        }

        var result = await _customerService.UpdateAsync(customerId, request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(
                statusCode: result.ErrorStatus,
                title: "Không thể cập nhật thông tin khách hàng",
                detail: result.Error);
    }

    /// <summary>Replaces a customer's password with a newly hashed password.</summary>
    [HttpPost("{customerId:int}/password")]
    [ValidateAntiForgeryToken]
    [EndpointSummary("Đặt mật khẩu mới cho khách hàng")]
    [EndpointDescription("Không đọc hoặc trả về mật khẩu hiện tại; lưu mật khẩu mới bằng ASP.NET Core PasswordHasher.")]
    [ProducesResponseType<CustomerAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        int customerId,
        [FromBody] ResetCustomerPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsStaffAsync(cancellationToken))
        {
            return UnauthorizedStaff();
        }

        var result = await _customerService.ResetPasswordAsync(customerId, request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(
                statusCode: result.ErrorStatus,
                title: "Không thể đặt mật khẩu mới",
                detail: result.Error);
    }

    /// <summary>Locks a customer account, preventing future sign-ins.</summary>
    [HttpPatch("{customerId:int}/lock")]
    [ValidateAntiForgeryToken]
    [EndpointSummary("Khóa tài khoản khách hàng")]
    [EndpointDescription("Đặt IsActive thành false; lịch sử và dữ liệu đặt chỗ được giữ nguyên.")]
    [ProducesResponseType<CustomerAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lock(
        int customerId,
        CancellationToken cancellationToken)
    {
        if (!await IsStaffAsync(cancellationToken))
        {
            return UnauthorizedStaff();
        }

        var result = await _customerService.LockAsync(customerId, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(
                statusCode: result.ErrorStatus,
                title: "Không thể khóa tài khoản khách hàng",
                detail: result.Error);
    }

    /// <summary>Unlocks a customer account, allowing the customer to sign in again.</summary>
    [HttpPatch("{customerId:int}/unlock")]
    [ValidateAntiForgeryToken]
    [EndpointSummary("Mở khóa tài khoản khách hàng")]
    [EndpointDescription("Đặt IsActive thành true để khách hàng có thể đăng nhập lại.")]
    [ProducesResponseType<CustomerAccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlock(
        int customerId,
        CancellationToken cancellationToken)
    {
        if (!await IsStaffAsync(cancellationToken))
        {
            return UnauthorizedStaff();
        }

        var result = await _customerService.UnlockAsync(customerId, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(
                statusCode: result.ErrorStatus,
                title: "Không thể mở khóa tài khoản khách hàng",
                detail: result.Error);
    }

    private async Task<bool> IsStaffAsync(CancellationToken cancellationToken) =>
        TryGetStaffId(out var staffId) &&
        await _customerService.IsStaffAsync(staffId, cancellationToken);

    private bool TryGetStaffId(out int staffId)
    {
        staffId = 0;
        return string.Equals(
                   HttpContext.Session.GetString("Role")?.Trim(),
                   "Staff",
                   StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(HttpContext.Session.GetString("UserId"), out staffId);
    }

    private ObjectResult UnauthorizedStaff() =>
        Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Phiên nhân viên không hợp lệ",
            detail: "Vui lòng đăng nhập lại bằng tài khoản nhân viên quầy.");
}
