using TEO_Airline_Project.Models.CustomerAccounts;

namespace TEO_Airline_Project.Services;

public interface ICustomerAccountService
{
    Task<bool> IsStaffAsync(int staffId, CancellationToken cancellationToken);

    Task<CustomerAccountResult<CustomerAccountPageResponse>> SearchAsync(
        CustomerAccountSearchRequest request,
        CancellationToken cancellationToken);

    Task<CustomerAccountResult<CustomerAccountResponse>> GetByIdAsync(
        int customerId,
        CancellationToken cancellationToken);

    Task<CustomerAccountResult<CustomerAccountResponse>> UpdateAsync(
        int customerId,
        UpdateCustomerAccountRequest request,
        CancellationToken cancellationToken);

    Task<CustomerAccountResult<CustomerAccountResponse>> ResetPasswordAsync(
        int customerId,
        ResetCustomerPasswordRequest request,
        CancellationToken cancellationToken);

    Task<CustomerAccountResult<CustomerAccountResponse>> LockAsync(
        int customerId,
        CancellationToken cancellationToken);

    Task<CustomerAccountResult<CustomerAccountResponse>> UnlockAsync(
        int customerId,
        CancellationToken cancellationToken);
}
