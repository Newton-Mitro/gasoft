namespace Gasoft.Application.Abstractions;

public sealed record CompanyDataExport(byte[] Content, string FileName);

public interface ICompanyDataExportService
{
    Task<CompanyDataExport?> ExportAsync(long companyId, CancellationToken cancellationToken);
}
