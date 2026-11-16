using Gasoft.Application.BankReconciliation;
using Gasoft.Application.Common;

namespace Gasoft.Application.Abstractions;

public interface IBankReconciliationService
{
    Task<IReadOnlyList<BankAccountDto>?> GetBankAccountsAsync(long companyId, CancellationToken cancellationToken);
    Task<BankAccountDto?> CreateBankAccountAsync(
        long companyId,
        BankAccountRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<BankTransactionDto>?> GetTransactionsAsync(
        long companyId,
        long bankAccountId,
        CancellationToken cancellationToken);
    Task<PagedResultDto<BankTransactionDto>?> GetTransactionsPageAsync(
        long companyId,
        long bankAccountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task<BankStatementImportResultDto> ImportStatementAsync(
        long companyId,
        long bankAccountId,
        string csv,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<BankMatchCandidateDto>> GetMatchCandidatesAsync(
        long companyId,
        long bankAccountId,
        long transactionId,
        CancellationToken cancellationToken);
    Task<BankMatchResultDto> MatchAsync(
        long companyId,
        long bankAccountId,
        long transactionId,
        BankMatchRequest request,
        CancellationToken cancellationToken);
    Task<bool> UnmatchAllocationAsync(
        long companyId,
        long bankAccountId,
        long transactionId,
        long matchId,
        CancellationToken cancellationToken);
    Task<bool> UnmatchTransactionAsync(
        long companyId,
        long bankAccountId,
        long transactionId,
        CancellationToken cancellationToken);
    Task<BankAdjustmentResponse> CreateAdjustmentAsync(
        long companyId,
        long bankAccountId,
        long transactionId,
        BankAdjustmentRequest request,
        CancellationToken cancellationToken);
}
