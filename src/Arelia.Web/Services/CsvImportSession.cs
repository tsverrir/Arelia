using Arelia.Application.People;

namespace Arelia.Web.Services;

/// <summary>
/// Scoped (per-circuit) staging area for a pending CSV people import.
/// Holds parsed rows between the file-upload step on the People list and
/// the full-page review/edit step before committing.
/// </summary>
public sealed class CsvImportSession
{
    public List<PersonImportRow>? Rows { get; private set; }

    public void Begin(List<PersonImportRow> rows) => Rows = rows;

    public void Clear() => Rows = null;

    public bool HasData => Rows is not null;
}
