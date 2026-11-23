# Gasoft Operations Manual

This manual explains the operational workflows available in the Gasoft web application. Menu labels may vary slightly by release. Follow your organization's accounting policies and obtain approval before posting or closing financial records.

## 1. Sign in and choose an organization

1. Sign in with your Gasoft account.
2. Use **Current organization** in the left navigation to select the organization you are working in. Company-scoped records and reports use this selection; changing it reloads the application.
3. Use the left navigation to open a module. Related links are grouped by module, with setup and transaction steps ordered in workflow sequence. On a narrow screen, open the navigation menu first.
4. Use the profile link to manage your personal account. Appearance and theme color can be changed in the sidebar. The notification inbox contains personal notifications; items can be marked read or dismissed.

Access to some functions depends on your workspace role. For example, audit history and accounting-period/year close or reopen controls are restricted to Owners and Admins.

## 2. First-time organization setup

Complete setup before entering routine transactions:

1. Open **Administration → Company settings**. Add an organization if needed, then review its legal name, code, functional currency, contact details, address, and tax registration number. A company logo can be uploaded as a JPEG, PNG, or WebP image up to 5 MB.
2. When an organization is created, starter account groups, accounts, and journals are created automatically. Default journals include General, Sales, Purchase, Bank, Cash receipts, and Cash payments. Review **General Ledger → Chart of accounts** and **Journals**; add or edit records to match your approved accounting structure.
3. Create a fiscal year in **General Ledger → Fiscal years**, then create its accounting periods in **Accounting periods**. Fiscal years and periods are not created automatically.
4. Configure **Administration → Tax codes & rates** and **Tax registrations** as applicable.
5. Add customers and suppliers in **Customers & suppliers**. Add their addresses when needed. For emailed quotes, maintain a valid customer email address.
6. If tracking stock, create item categories, inventory items, warehouses, and warehouse locations before recording stock activity.
7. If reconciling bank statements, create an eligible postable asset account in the chart of accounts, then link it to a bank account in **Banking → Bank reconciliation**.

## 3. General ledger

### Chart of accounts

Open **General Ledger → Chart of accounts** to review the account hierarchy. Expand or collapse groups and accounts, search by account code or name, and use **Add account** or **Edit** to maintain accounts. Accounts can be dragged to change their parent in the hierarchy. Confirm the destination and account posting settings before moving or editing an account.

### Journals

Open **General Ledger → Journals** to create and maintain journals. Journal type and active status control how journals are available to transaction workflows. Inactive journals should not be selected for new activity.

### Journal entries

1. Choose **New journal entry**.
2. Select a journal, an open accounting period, entry date, description, and transaction currency; enter the exchange rate.
3. Add at least two lines. Select an account and enter debit or credit amounts as appropriate.
4. Confirm total debits equal total credits, then save the draft.
5. Review the draft before posting it from the journal-entry list. Posting writes the entry to the ledger; posted entries are not available for draft editing.

The current journal-entry form has an entry description, but no dedicated party-reference field. If a source identifier is needed, follow your organization's policy for including it in the description or use the relevant sales, purchasing, or payment workflow that records the party.

### Fiscal years and accounting periods

Use the **History** action to review status changes. Before closing a period, review its close checklist. Draft journal entries and unreconciled imported bank statement transactions are blockers. Resolve blockers, then close the period with a reason. Reopening also requires a reason and is audited. Only authorized Owners and Admins can close or reopen periods.

Fiscal-year close requires periods to exist and be closed, and no draft journal entries to remain. If posted revenue or expense balances remain, use the close checklist to prepare the closing journal, select a retained-earnings account, review the generated draft, and post it through the closing workflow. Lock the year only after the checklist allows it. Reopening a fiscal year does not reopen its accounting periods. For a new year, use **Opening balances** to prepare and review an opening journal from prior-year balance-sheet balances.

## 4. Sales, purchasing, and payments

### Customers and suppliers

Use **Customers & suppliers** to add or edit party details, indicate whether a party is a customer, supplier, or both, and maintain contact and currency details. Use **Addresses** to maintain party addresses. Keeping these details current supports document creation and customer quote email.

### Sales quotes

1. Choose **New quote**, select a customer, enter the quote number, quote date, validity date, currency, and exchange rate.
2. Add one or more lines with a revenue account, description, optional tax code, quantity, and unit price.
3. Save the draft. Review it and use the available actions to edit, email, cancel, accept, or decline according to its status.
4. Email requires a valid email address on the customer. An accepted quote can be converted to an invoice; provide the invoice number and dates and review them before creating it.

### Point of sale

Open **Inventory → Items** and set each item's selling price for the selected company before selling it. Prices use that company's functional currency; an item with no current-company price cannot be added to the register. In **Sales & Receivables → Point of sale**, search by item name or SKU and add priced items. Their configured prices prefill the cart and can be adjusted for the sale. Select the customer, warehouse, open accounting period, journal, and posting accounts before completing checkout. A tax code is optional; when selected, choose its tax liability account. Checkout posts the sales invoice and payment, allocates the payment to the invoice, and issues the sold quantities from the selected warehouse in one transaction. If validation or stock posting fails, the checkout is rolled back. Use **Print receipt** after a successful sale.

### Sales invoices

Create a draft invoice by entering the customer and document details, then add revenue lines, quantities, prices, and any tax codes. Review the calculated totals and tax before saving. To post, select the invoice and complete the posting form. Posting creates receivable and revenue journal entries.

### Purchase orders

Create a draft order by selecting a supplier and warehouse, entering order details and currency, and adding inventory items, quantities, and unit costs. Review the subtotal and save the draft. Use the order detail to record full or partial receipts as goods arrive; receipts update inventory. Follow the order status to track outstanding quantities.

### Purchase bills

Create a supplier bill draft, enter the supplier and bill details, and add expense lines and applicable tax codes. Review and save the draft. Use **Post** after review; posting creates payable and expense journal entries.

### Employee expenses

Open **Expenses → Expense reports** to create a draft report. Add each expense with its date, merchant, description, expense account, amount, and optional JPEG, PNG, WebP, or PDF receipt (up to 5 MB). Save the draft, then submit it for review. Owners and admins can approve or reject submitted reports; a rejected report can be corrected and resubmitted. After approval, an owner or admin posts it with an open accounting period, journal, and employee-payable account. Use **Reimburse** after posting to record the payment against a cash or bank account. Posting and reimbursement create balanced ledger entries.

### Payments and allocations

Create an incoming or outgoing payment with its payment number, date, party, currency, amount, and required account details. Use **Allocate** to apply some or all of an available payment to an invoice or bill. Review the amount and target document before submitting. Use **Post** to record the cash and control-account journal entries. The page shows payment status and allocated amount.

### Credit notes and refunds

Create a credit note from an eligible posted invoice. Select the invoice and enter a credit-note number, date, reason, and the quantities being credited. Review and save the draft. Post the credit note with the journal, accounting period, and required accounts. If money is to be returned, use the refund action and confirm its number, date, amount, journal, period, and cash account before posting.

## 5. Inventory and fixed assets

### Inventory setup and stock movements

Create item categories, inventory items, warehouses, and optional warehouse locations before recording movements. Inventory items include an SKU, category, base unit, and costing method. Configure a selling price for each company in its functional currency from the item form; leaving it blank prevents the item from being sold in that company's POS.

Use **Stock movements → Receive stock** or **Issue stock** to record a movement number, date, item, warehouse, and quantity. Follow the form's prompts for the related journal or period. Posted movements appear in the movement history and affect stock balances.

Use **Inventory counts** to record actual quantities for a warehouse. Review the count against system quantities before posting; posting creates an inventory-adjustment journal entry. Use **Stock balances** to inspect on-hand quantities.

### Fixed assets

Use **Fixed assets → Acquire asset** to enter asset identity, acquisition and in-service dates, cost, residual value, useful life, open accounting period, journal, and the required asset, accumulated-depreciation, expense, and funding accounts. Review before posting the acquisition.

For depreciation, select the due open period through which to depreciate and a journal. The application calculates straight-line depreciation by service days and caps it at residual value. Review the period selection before posting. Before disposal, post depreciation through the disposal period, then select the disposal period, proceeds, journal, and required accounts.

## 6. Bank reconciliation

1. Open **Banking → Bank reconciliation**. If no bank account exists, add one and link it to an active, postable asset account. Only the last four account digits are stored.
2. Select the bank account and import a CSV statement. Required headers are `date`, `description`, and `amount`; `reference` is optional. Dates must use ISO `YYYY-MM-DD`. Positive amounts are deposits and negative amounts are withdrawals.
3. Review imported transactions and their matched, partially matched, or unmatched status.
4. For an unmatched transaction, use **Find match** and review the posted ledger candidates. Allocate amounts in the same direction as the bank transaction; partial allocations can be completed later.
5. Use **Record fee / adjustment** for bank fees or other differences, supplying the requested ledger details. Reconcile all imported transactions before closing the accounting period.

## 7. Reports

Open **Reports** and choose a report:

- **Trial balance** compares posted debit and credit activity for a date range. Review the balanced indicator and investigate any difference.
- **Income statement** summarizes posted revenue and expenses for a date range.
- **Balance sheet** shows posted balances as of a selected date; unclosed earnings are included in equity.
- **Budgets** supports budget entry and budget-versus-actual review.
- **Cash flow**, **Receivables aging**, and **Payables aging** provide their respective reporting views.
- **Consolidated trial balance** supports group reporting for selected organizations.

Set the date filters before interpreting results. Financial statement reports use posted journal entries; draft activity is not included.

## 8. Administration and review

- **Company settings**: review organization details, functional currency, and logo.
- **Tax codes & rates**: maintain company tax codes and effective-dated rates. Confirm tax treatment and rates with an authoritative source.
- **Tax registrations**: review and maintain applicable registrations.
- **Team members**: add a new account or grant a workspace role to an existing Gasoft user. Edit access to change the role or active membership; deactivate or reactivate users from the list as appropriate.
- **Audit history**: Owners and Admins can filter tenant audit records by action, record type, and date. Expand **View changes** to inspect available details. Sensitive credential and bank-account fields are excluded from audit details.

## 9. Common page controls and troubleshooting

- Use **Add/New** to open a create form and **Edit** to update a draft or editable record. Use **Cancel** to leave without saving.
- Required fields and inline validation messages indicate missing or invalid values. Correct the field and retry. If an error alert appears, follow its instructions; an error can also indicate a closed period or missing setup data.
- On list pages, use search or filters before paging. Pagination shows the displayed range and total records; change page size or navigate between pages using its controls.
- If an option list is empty, check that the required related records exist, are active, and belong to the selected organization. For example, posting may need an open period, active journal, and suitable ledger accounts.
- Review document totals, currency, exchange rate, posting accounts, and accounting period before posting. Posting affects the ledger and may restrict later edits.
- If you cannot see a menu item or action, confirm that you selected the correct organization and that your role is permitted to use it.

