using Gasoft.Application.Billing;

namespace Gasoft.Application.Abstractions;

public interface IBillingService
{
    Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(long companyId, CancellationToken cancellationToken);
    Task<InvoiceDto> CreateInvoiceAsync(long companyId, CreateInvoiceRequest request, CancellationToken cancellationToken);
    Task<InvoiceDto?> UpdateInvoiceAsync(long companyId, long invoiceId, UpdateInvoiceRequest request, CancellationToken cancellationToken);
    Task<InvoiceDto?> GetInvoiceAsync(long companyId, long invoiceId, CancellationToken cancellationToken);
    Task<InvoiceDto?> PostInvoiceAsync(long companyId, long invoiceId, PostSalesInvoiceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SalesCreditNoteDto>> GetSalesCreditNotesAsync(long companyId, CancellationToken cancellationToken);
    Task<SalesCreditNoteDto> CreateSalesCreditNoteAsync(long companyId, CreateSalesCreditNoteRequest request, CancellationToken cancellationToken);
    Task<SalesCreditNoteDto?> PostSalesCreditNoteAsync(long companyId, long creditNoteId, PostSalesCreditNoteRequest request, CancellationToken cancellationToken);
    Task<CustomerRefundDto?> RefundSalesCreditNoteAsync(long companyId, long creditNoteId, CreateCustomerRefundRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<BillDto>> GetBillsAsync(long companyId, CancellationToken cancellationToken);
    Task<BillDto> CreateBillAsync(long companyId, CreateBillRequest request, CancellationToken cancellationToken);
    Task<BillDto?> UpdateBillAsync(long companyId, long billId, UpdateBillRequest request, CancellationToken cancellationToken);
    Task<BillDto?> GetBillAsync(long companyId, long billId, CancellationToken cancellationToken);
    Task<BillDto?> PostBillAsync(long companyId, long billId, PostPurchaseBillRequest request, CancellationToken cancellationToken);
    Task<PaymentDto> CreatePaymentAsync(long companyId, CreatePaymentRequest request, CancellationToken cancellationToken);
    Task<PaymentDto?> GetPaymentAsync(long companyId, long paymentId, CancellationToken cancellationToken);
    Task<PaymentDto?> UpdatePaymentAsync(long companyId, long paymentId, UpdatePaymentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(long companyId, CancellationToken cancellationToken);
    Task<PaymentDto?> AllocatePaymentAsync(long companyId, long paymentId, AllocatePaymentRequest request, CancellationToken cancellationToken);
    Task<PaymentDto?> PostPaymentAsync(long companyId, long paymentId, PostPaymentRequest request, CancellationToken cancellationToken);
}
