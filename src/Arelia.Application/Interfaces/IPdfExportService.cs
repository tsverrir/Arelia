using Arelia.Application.Documents.Queries;

namespace Arelia.Application.Interfaces;

public interface IPdfExportService
{
    /// <summary>Generates a PDF for the given document and returns the raw bytes.</summary>
    Task<byte[]> ExportDocumentAsync(DocumentDetailDto document, CancellationToken ct = default);
}
