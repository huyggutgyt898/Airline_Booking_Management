using Microsoft.AspNetCore.Mvc;
using TEO_Airline_Project.Models.CounterTickets;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers;

[ApiController]
[Route("api/counter/tickets")]
public sealed class CounterTicketsController : ControllerBase
{
    private readonly ICounterTicketService _ticketService;

    public CounterTicketsController(ICounterTicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>Searches real tickets by booking code and passenger verification data.</summary>
    [HttpGet("search")]
    [EndpointSummary("Tra cứu vé tại quầy")]
    [EndpointDescription("Tìm vé theo mã đặt chỗ, tên hành khách hoặc thông tin liên hệ; chỉ dành cho nhân viên quầy đã đăng nhập.")]
    [ProducesResponseType<IReadOnlyList<CounterTicketResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] CounterTicketSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffId(out var staffId) ||
            !await _ticketService.IsStaffAsync(staffId, cancellationToken))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Phiên nhân viên không hợp lệ",
                detail: "Vui lòng đăng nhập lại bằng tài khoản nhân viên quầy.");
        }

        var result = await _ticketService.SearchAsync(request, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(statusCode: result.ErrorStatus, title: "Không thể tra cứu vé", detail: result.Error);
    }

    /// <summary>Lists same-route flight and seat options for an eligible ticket.</summary>
    [HttpGet("{ticketId:int}/flight-options")]
    [EndpointSummary("Tải chuyến bay đổi vé")]
    [EndpointDescription("Trả về các chuyến cùng hành trình còn ghế và số tiền chênh lệch cùng phụ phí đổi.")]
    [ProducesResponseType<IReadOnlyList<CounterTicketFlightOptionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FlightOptions(int ticketId, CancellationToken cancellationToken)
    {
        if (!TryGetStaffId(out var staffId) ||
            !await _ticketService.IsStaffAsync(staffId, cancellationToken))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Phiên nhân viên không hợp lệ",
                detail: "Vui lòng đăng nhập lại bằng tài khoản nhân viên quầy.");
        }

        var result = await _ticketService.GetFlightOptionsAsync(ticketId, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(statusCode: result.ErrorStatus, title: "Không thể tải chuyến bay", detail: result.Error);
    }

    /// <summary>Exchanges a ticket and records the applicable surcharge and fare difference.</summary>
    [HttpPost("{ticketId:int}/exchange")]
    [EndpointSummary("Đổi vé")]
    [EndpointDescription("Đổi vé đủ điều kiện, cập nhật chuyến và ghế, lưu BookingRequest và ghi nhận khoản thu.")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<CounterTicketOperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Exchange(
        int ticketId,
        [FromBody] ExchangeCounterTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffId(out var staffId) ||
            !await _ticketService.IsStaffAsync(staffId, cancellationToken))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Phiên nhân viên không hợp lệ",
                detail: "Vui lòng đăng nhập lại bằng tài khoản nhân viên quầy.");
        }

        var result = await _ticketService.ExchangeAsync(
            ticketId,
            request.NewFlightId,
            request.Reason,
            request.PaymentConfirmed,
            staffId,
            cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(statusCode: result.ErrorStatus, title: "Không thể đổi vé", detail: result.Error);
    }

    /// <summary>Cancels an eligible ticket and records its refund in the payment ledger.</summary>
    [HttpPost("{ticketId:int}/cancel")]
    [EndpointSummary("Hủy vé")]
    [EndpointDescription("Hủy vé đủ điều kiện, lưu BookingRequest và ghi nhận số tiền hoàn vào sổ thanh toán.")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType<CounterTicketOperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        int ticketId,
        [FromBody] CancelCounterTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetStaffId(out var staffId) ||
            !await _ticketService.IsStaffAsync(staffId, cancellationToken))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Phiên nhân viên không hợp lệ",
                detail: "Vui lòng đăng nhập lại bằng tài khoản nhân viên quầy.");
        }

        var result = await _ticketService.CancelAsync(
            ticketId,
            request.Reason,
            request.RefundConfirmed,
            staffId,
            cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Problem(statusCode: result.ErrorStatus, title: "Không thể hủy vé", detail: result.Error);
    }

    private bool TryGetStaffId(out int staffId)
    {
        staffId = 0;
        return string.Equals(
                   HttpContext.Session.GetString("Role")?.Trim(),
                   "Staff",
                   StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(HttpContext.Session.GetString("UserId"), out staffId);
    }
}
