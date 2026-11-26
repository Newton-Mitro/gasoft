namespace Gasoft.Domain.Notifications;

public sealed record NotificationEventDefinition(
    string EventType,
    string Category,
    string Name,
    string Description);

public static class NotificationEvents
{
    public const string RecordPosted = "record-posted";
    public const string SalesInvoicePosted = "sales-invoice-posted";
    public const string SalesCreditNotePosted = "sales-credit-note-posted";
    public const string CustomerRefundPosted = "customer-refund-posted";
    public const string PurchaseBillPosted = "purchase-bill-posted";
    public const string PaymentPosted = "payment-posted";
    public const string JournalEntryPosted = "journal-entry-posted";
    public const string JournalReversalPosted = "journal-reversal-posted";
    public const string InventoryCountPosted = "inventory-count-posted";
    public const string PurchaseOrderOrdered = "purchase-order-ordered";
    public const string PurchaseOrderPartiallyReceived = "purchase-order-partially-received";
    public const string PurchaseOrderReceived = "purchase-order-received";
    public const string PurchaseOrderCancelled = "purchase-order-cancelled";
    public const string ExpenseReportSubmitted = "expense-report-submitted";
    public const string ExpenseReportApproved = "expense-report-approved";
    public const string ExpenseReportRejected = "expense-report-rejected";
    public const string ExpenseReportPosted = "expense-report-posted";
    public const string ExpenseReportReimbursed = "expense-report-reimbursed";
    public const string SalesQuoteSent = "sales-quote-sent";
    public const string SalesQuoteAccepted = "sales-quote-accepted";
    public const string SalesQuoteDeclined = "sales-quote-declined";
    public const string SalesQuoteCancelled = "sales-quote-cancelled";
    public const string SalesQuoteConverted = "sales-quote-converted";
    public const string FiscalYearStatusChanged = "fiscal-year-status-changed";
    public const string AccountingPeriodStatusChanged = "accounting-period-status-changed";
    public const string ConsolidationRunFinalized = "consolidation-run-finalized";
    public const string FixedAssetAcquired = "fixed-asset-acquired";
    public const string FixedAssetDepreciationPosted = "fixed-asset-depreciation-posted";
    public const string FixedAssetDisposed = "fixed-asset-disposed";
    public const string BankStatementImported = "bank-statement-imported";
    public const string BankTransactionReconciled = "bank-transaction-reconciled";
    public const string PasswordChanged = "password-changed";
    public const string SignInAttemptRejected = "sign-in-attempt-rejected";

    private static readonly HashSet<string> JournalEntriesWithSpecificEvents =
    [
        "Sales invoice ",
        "Sales credit note ",
        "Customer refund ",
        "Purchase bill ",
        "Payment ",
        "Expense report ",
        "Reimbursement for ",
        "Inventory count ",
        "Fixed asset acquisition ",
        "Fixed asset disposal ",
        "Depreciation "
    ];

    public static IReadOnlyList<NotificationEventDefinition> All { get; } =
    [
        new(SalesInvoicePosted, "Sales", "Sales invoice posted", "A sales invoice is posted to the ledger."),
        new(SalesCreditNotePosted, "Sales", "Sales credit note posted", "A sales credit note is posted to the ledger."),
        new(CustomerRefundPosted, "Sales", "Customer refund posted", "A refund to a customer is posted."),
        new(SalesQuoteSent, "Sales", "Sales quote sent", "A sales quote is sent to a customer."),
        new(SalesQuoteAccepted, "Sales", "Sales quote accepted", "A customer accepts a sales quote."),
        new(SalesQuoteDeclined, "Sales", "Sales quote declined", "A customer declines a sales quote."),
        new(SalesQuoteCancelled, "Sales", "Sales quote cancelled", "A sales quote is cancelled."),
        new(SalesQuoteConverted, "Sales", "Sales quote converted", "A sales quote is converted to an invoice."),
        new(PurchaseBillPosted, "Purchases", "Purchase bill posted", "A purchase bill is posted to the ledger."),
        new(PaymentPosted, "Payments", "Payment posted", "A customer receipt or supplier payment is posted."),
        new(PurchaseOrderOrdered, "Purchases", "Purchase order sent", "A purchase order is sent to a supplier."),
        new(PurchaseOrderPartiallyReceived, "Purchases", "Purchase order partially received", "Some items on a purchase order are received."),
        new(PurchaseOrderReceived, "Purchases", "Purchase order received", "All items on a purchase order are received."),
        new(PurchaseOrderCancelled, "Purchases", "Purchase order cancelled", "A purchase order is cancelled."),
        new(ExpenseReportSubmitted, "Expenses", "Expense report submitted", "An expense report is submitted for review."),
        new(ExpenseReportApproved, "Expenses", "Expense report approved", "An expense report is approved."),
        new(ExpenseReportRejected, "Expenses", "Expense report rejected", "An expense report is rejected."),
        new(ExpenseReportPosted, "Expenses", "Expense report posted", "An approved expense report is posted to the ledger."),
        new(ExpenseReportReimbursed, "Expenses", "Expense report reimbursed", "An expense report reimbursement is recorded."),
        new(InventoryCountPosted, "Inventory", "Inventory count posted", "An inventory count adjustment is posted."),
        new(JournalEntryPosted, "Accounting", "Journal entry posted", "A journal entry is posted to the ledger."),
        new(JournalReversalPosted, "Accounting", "Journal reversal posted", "A reversal entry is posted for a journal entry."),
        new(AccountingPeriodStatusChanged, "Accounting", "Accounting period status changed", "An accounting period is closed or reopened."),
        new(FiscalYearStatusChanged, "Accounting", "Fiscal year status changed", "A fiscal year is closed or reopened."),
        new(ConsolidationRunFinalized, "Accounting", "Consolidation finalized", "A consolidation run is finalized."),
        new(FixedAssetAcquired, "Fixed assets", "Fixed asset acquired", "A fixed asset is added to the register."),
        new(FixedAssetDepreciationPosted, "Fixed assets", "Depreciation posted", "A fixed asset depreciation entry is posted."),
        new(FixedAssetDisposed, "Fixed assets", "Fixed asset disposed", "A fixed asset is disposed."),
        new(BankStatementImported, "Banking", "Bank statement imported", "Transactions are imported from a bank statement."),
        new(BankTransactionReconciled, "Banking", "Bank transaction reconciled", "A bank statement transaction is fully reconciled."),
        new(RecordPosted, "Other", "Other record posted", "A record without a specific event type is posted."),
        new(PasswordChanged, "Security", "Password changed", "Your account password is changed."),
        new(SignInAttemptRejected, "Security", "Sign-in attempt rejected", "A sign-in attempt for your workspace is rejected.")
    ];

    public static bool Contains(string eventType) =>
        All.Any(item => item.EventType == eventType);

    public static NotificationEventDefinition? Find(string eventType) =>
        All.FirstOrDefault(item => item.EventType == eventType);

    public static string? ResolveWorkflowEventType(
        string entityType,
        string? status,
        string? description,
        long? reversalOfEntryId,
        bool isAdded)
    {
        if (entityType == "FiscalYearStatusChange")
            return FiscalYearStatusChanged;
        if (entityType == "AccountingPeriodStatusChange")
            return AccountingPeriodStatusChanged;
        if (entityType == "FixedAsset" && isAdded)
            return FixedAssetAcquired;
        if (status is null || !status.Equals("Posted", StringComparison.OrdinalIgnoreCase))
        {
            if (entityType == "FixedAsset" && status?.Equals("Disposed", StringComparison.OrdinalIgnoreCase) == true)
                return FixedAssetDisposed;
            if (entityType == "ConsolidationRun" && status?.Equals("Finalized", StringComparison.OrdinalIgnoreCase) == true)
                return ConsolidationRunFinalized;
            return (entityType, status?.ToUpperInvariant()) switch
            {
                ("PurchaseOrder", "ORDERED") => PurchaseOrderOrdered,
                ("PurchaseOrder", "PARTIALLYRECEIVED") => PurchaseOrderPartiallyReceived,
                ("PurchaseOrder", "RECEIVED") => PurchaseOrderReceived,
                ("PurchaseOrder", "CANCELLED") => PurchaseOrderCancelled,
                ("ExpenseReport", "SUBMITTED") => ExpenseReportSubmitted,
                ("ExpenseReport", "APPROVED") => ExpenseReportApproved,
                ("ExpenseReport", "REJECTED") => ExpenseReportRejected,
                ("ExpenseReport", "REIMBURSED") => ExpenseReportReimbursed,
                ("SalesQuote", "SENT") => SalesQuoteSent,
                ("SalesQuote", "ACCEPTED") => SalesQuoteAccepted,
                ("SalesQuote", "DECLINED") => SalesQuoteDeclined,
                ("SalesQuote", "CANCELLED") => SalesQuoteCancelled,
                ("SalesQuote", "CONVERTED") => SalesQuoteConverted,
                _ => null
            };
        }

        if (entityType == "JournalEntry")
        {
            if (reversalOfEntryId.HasValue)
                return JournalReversalPosted;
            if (!string.IsNullOrEmpty(description) &&
                JournalEntriesWithSpecificEvents.Any(prefix =>
                    description.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                if (description.StartsWith("Depreciation ", StringComparison.OrdinalIgnoreCase))
                    return FixedAssetDepreciationPosted;
                return null;
            }

            return JournalEntryPosted;
        }

        return (entityType, status?.ToUpperInvariant()) switch
        {
            ("SalesInvoice", "POSTED") => SalesInvoicePosted,
            ("SalesCreditNote", "POSTED") => SalesCreditNotePosted,
            ("CustomerRefund", "POSTED") => CustomerRefundPosted,
            ("PurchaseBill", "POSTED") => PurchaseBillPosted,
            ("Payment", "POSTED") => PaymentPosted,
            ("ExpenseReport", "POSTED") => ExpenseReportPosted,
            ("InventoryCount", "POSTED") => InventoryCountPosted,
            _ => status?.Equals("Posted", StringComparison.OrdinalIgnoreCase) == true
                ? RecordPosted
                : null
        };
    }
}
