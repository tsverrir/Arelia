# Documents & Activity Attachments — Feature Specification

_Targeting the existing Arelia Blazor Server stack: MudBlazor · Radzen.Blazor · Clean Architecture · .NET 10_

---

## Overview

Two closely related features are added together because they share a common infrastructure dependency — disk-based file storage.

| Feature | Summary |
|---|---|
| **Documents** | Board/Admin can create, edit, and export rich-text meeting minutes and internal documents using an in-app editor |
| **Activity Attachments** | Any user with `ManageActivities` permission can attach external files (PDFs, Word documents, images, etc.) to an activity/event |
| **Shared Infrastructure** | A single `IFileStorageService` abstraction is introduced and used by both Activity Attachments and the existing Expense Attachments |
| **PDF Export** | Documents can be downloaded as a formatted PDF via QuestPDF |

---

## New NuGet Dependencies

| Package | Project | Purpose |
|---|---|---|
| `QuestPDF` | `Arelia.Infrastructure` | PDF generation |
| `HtmlSanitizer` | `Arelia.Infrastructure` | Strip unsafe HTML before persisting editor content |

> `Radzen.Blazor` (already referenced) provides `RadzenHtmlEditor` — no new UI package needed.
> The `DocumentType` enum from earlier drafts is replaced by a dynamic `DocumentCategory` lookup entity — same pattern as `VoiceGroup`.

---

## Feature 1: Documents

### 1.1 Domain — `Arelia.Domain`

**New entity: `DocumentCategory`** (inherits `BaseEntity`)

A per-organisation lookup table. Admin/Board can create, rename, reorder, and delete categories at any time.

| Field | Type | Notes |
|---|---|---|
| `Name` | `string` | Required, max 100 chars, unique per org |
| `SortOrder` | `int` | Controls display order in lists and selects |
| `OrganizationId` | `Guid` | Inherited; tenant-scoped |
| `IsActive` | `bool` | Inherited; soft delete |
| `Documents` | `ICollection<Document>` | Navigation |

**Seeded defaults** (per org, created during `CreateOrganization`):
`Meeting Minutes` (sort 1), `Internal Document` (sort 2), `Policy` (sort 3)

---

**New entity: `Document`** (inherits `BaseEntity`)

| Field | Type | Notes |
|---|---|---|
| `Title` | `string` | Required, max 300 chars |
| `ContentHtml` | `string` | Sanitized HTML from the editor, stored as-is |
| `DocumentCategoryId` | `Guid?` | FK to `DocumentCategory`; nullable — uncategorised is valid |
| `OrganizationId` | `Guid` | Inherited; tenant-scoped |
| `IsActive` | `bool` | Inherited; soft delete |
| `Category` | `DocumentCategory?` | Navigation |

`BaseEntity` audit fields (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`) provide all required traceability.

---

### 1.2 Application Layer — `Arelia.Application`

#### Document Commands

**`CreateDocumentCommand`**  
Fields: `Title`, `ContentHtml`, `DocumentCategoryId` (`Guid?`), `OrganizationId`  
Returns: `Result<Guid>` (the new document's Id)  
Validation: `Title` required; `ContentHtml` required (non-empty after sanitization); if `DocumentCategoryId` is provided, it must belong to the same org.  
Handler sanitizes `ContentHtml` via `IHtmlSanitizerService` before persisting.

**`UpdateDocumentCommand`**  
Fields: `DocumentId`, `Title`, `ContentHtml`, `DocumentCategoryId` (`Guid?`)  
Returns: `Result`  
Handler re-sanitizes `ContentHtml` on every save.

**`DeleteDocumentCommand`**  
Fields: `DocumentId`  
Returns: `Result`  
Performs soft delete (`IsActive = false`).

#### DocumentCategory Commands

**`CreateDocumentCategoryCommand`**  
Fields: `Name`, `SortOrder`, `OrganizationId`  
Returns: `Result<Guid>`  
Validation: `Name` required; duplicate name (case-insensitive) within org is rejected.

**`UpdateDocumentCategoryCommand`**  
Fields: `Id`, `Name`, `SortOrder`  
Returns: `Result`  
Validation: same duplicate check excluding self.

**`DeleteDocumentCategoryCommand`**  
Fields: `CategoryId`, `Mode` (`UnassignDocuments` | `MoveDocuments`), `MoveToCategoryId?`  
Returns: `Result`  
Behaviour mirrors `DeleteVoiceGroupCommand`:
- `UnassignDocuments` — sets `DocumentCategoryId = null` on all affected documents
- `MoveDocuments` — reassigns all affected documents to `MoveToCategoryId`; target must exist and be active

#### Document Queries

**`GetDocumentsQuery`**  
Filters: `OrganizationId`, optional `DocumentCategoryId?`, optional title search string.  
Returns: `IReadOnlyList<DocumentSummaryDto>`  

```
DocumentSummaryDto(Guid Id, string Title, Guid? DocumentCategoryId, string? CategoryName, DateTime CreatedAt, string? CreatedBy, DateTime? UpdatedAt)
```

**`GetDocumentDetailQuery`**  
Filter: `DocumentId`  
Returns: `DocumentDetailDto?`  

```
DocumentDetailDto(Guid Id, string Title, string ContentHtml, Guid? DocumentCategoryId, string? CategoryName, DateTime CreatedAt, string? CreatedBy, DateTime? UpdatedAt, string? UpdatedBy)
```

#### DocumentCategory Query

**`GetDocumentCategoriesQuery`**  
Filter: `OrganizationId`  
Returns: `List<DocumentCategoryDto>`  

```
DocumentCategoryDto(Guid Id, string Name, int SortOrder, int DocumentCount)
```

#### New Application Interface: `IHtmlSanitizerService`

Defined in `Arelia.Application\Interfaces\IHtmlSanitizerService.cs`.  
Single method: `string Sanitize(string html)`.  
Implementation (`HtmlSanitizerService`) lives in `Arelia.Infrastructure` using the `HtmlSanitizer` NuGet package.  
Default allowed tags: `<p>`, `<strong>`, `<em>`, `<u>`, `<h1>`–`<h3>`, `<ul>`, `<ol>`, `<li>`, `<a>` (href only, no `javascript:`), `<br>`, `<table>`, `<thead>`, `<tbody>`, `<tr>`, `<th>`, `<td>`.

#### New Application Interface: `IPdfExportService`

Defined in `Arelia.Application\Interfaces\IPdfExportService.cs`.  
Single method: `Task<byte[]> ExportDocumentAsync(DocumentDetailDto document, string organizationName, CancellationToken ct)`.  
Implementation (`QuestPdfExportService`) lives in `Arelia.Infrastructure` using QuestPDF.

---

### 1.3 Infrastructure — `Arelia.Infrastructure`

#### `HtmlSanitizerService`
Wraps `Ganss.Xss.HtmlSanitizer`. Configured with the allowed-tag list defined above.

#### `QuestPdfExportService`
Generates a structured A4 PDF using QuestPDF's fluent API:
- **Header:** Organization name (left), document type label (right)
- **Title block:** Document title in `h1`-equivalent style
- **Metadata line:** "Created by {CreatedBy} on {CreatedAt:dd MMM yyyy}" (if available)
- **Divider**
- **Content:** Rendered as structured QuestPDF elements (paragraphs, bold, lists). The HTML is parsed with a lightweight traverser — not a full browser renderer. Supported tags: same as the sanitizer allow-list.
- **Footer:** Page `N / M` centered

#### EF Core & Migration
- Add `DocumentCategories` and `Documents` `DbSet`s to `AreliaDbContext`
- `DocumentCategory` configuration: `Name` required max 100, unique index on `(OrganizationId, Name)`, global query filter `IsActive == true && OrganizationId == tenantId`
- `Document` configuration: `Title` required max 300, `ContentHtml` required, FK `DocumentCategoryId → DocumentCategory.Id` with `DeleteBehavior.SetNull`, global query filter matching existing pattern
- New EF Core migration: `AddDocuments`

#### `IAreliaDbContext` update
Add:
```
DbSet<DocumentCategory> DocumentCategories { get; }
DbSet<Document> Documents { get; }
```

---

### 1.4 Web UI — `Arelia.Web`

#### New page: `Documents.razor` — `/documents`

- **Route:** `/documents`
- **Permission required:** Any authenticated member of the current org (read), `ManageDocuments` permission (create/edit/delete)
- **Layout:**
  - Page title + "New Document" button (shown only with `ManageDocuments` permission) at the top right
  - Category filter: `MudSelect` (or chip group) listing all active `DocumentCategory` names + an "All" option; filters the table client-side after initial load
  - Title search field
  - `MudTable` listing documents: Title, Category (name or "—" if uncategorised), Created by, Last updated, action buttons (View, Edit, Delete)

#### New page: `DocumentEditor.razor` — `/documents/new` and `/documents/{DocumentId:guid}/edit`

- **Permission required:** `ManageDocuments`
- **Layout:**
  - `MudTextField` for Title
  - `MudSelect<Guid?>` for Category, populated from `GetDocumentCategoriesQuery`; includes an explicit "No category" option (value `null`)
  - `RadzenHtmlEditor` for content (height: ~500px)
    - Toolbar includes: bold, italic, underline, headings, bullet list, numbered list, link, undo/redo
  - Save and Cancel `MudButton`s

#### New page: `DocumentDetail.razor` — `/documents/{DocumentId:guid}`

- **Permission required:** Authenticated member
- **Layout:**
  - Back button → `/documents`
  - Title + category chip (omitted when uncategorised)
  - Metadata line: created by / updated by + dates
  - Edit button (shown with `ManageDocuments` permission)
  - **"Download PDF"** button — triggers `IPdfExportService`, pushes a browser download via `IJSRuntime`
  - Content area: renders `ContentHtml` using `@((MarkupString)document.ContentHtml)`
  - `MudCard` wrapper with `pa-4` padding for readability

#### New page: `DocumentCategories.razor` — `/document-categories`

- **Permission required:** `ManageDocuments`
- **Layout and behaviour:** Mirrors `VoiceGroups.razor` exactly:
  - `MudTable` showing Name, Sort Order, Document Count, Edit/Delete actions
  - Inline create/edit dialog with Name + Sort Order fields
  - Delete dialog with two-mode choice (`UnassignDocuments` / `MoveDocuments`) when documents are assigned, simple confirm when none are

---

### 1.5 Navigation & Access

**NavMenu entries** added under the `Management` nav group:

```
Documents           (icon: Description)  →  /documents
Document Categories (icon: LocalOffer)   →  /document-categories
```

Both entries shown only to authenticated users. `Document Categories` can be placed alongside the existing `Expense Categories` entry.

---

## Feature 2: Activity Attachments

### 2.1 Domain — `Arelia.Domain`

**New entity: `ActivityAttachment`** (inherits `BaseEntity`)

| Field | Type | Notes |
|---|---|---|
| `ActivityId` | `Guid` | FK to `Activity` |
| `FileName` | `string` | Original filename shown in UI |
| `ContentType` | `string` | MIME type |
| `FilePath` | `string` | Relative path on disk (managed by `IFileStorageService`) |
| `UploadedAt` | `DateTime` | Set on creation |
| `Activity` | `Activity` (nav) | Navigation property |

The `Activity` entity gains a navigation collection: `ICollection<ActivityAttachment> Attachments`.

**Accepted MIME types** (enforced in application layer):
`application/pdf`, `application/msword`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `application/vnd.ms-excel`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `image/jpeg`, `image/png`, `image/gif`.

**Max file size:** 20 MB (configurable via `appsettings.json` key `FileStorage:MaxFileSizeBytes`).

---

### 2.2 Application Layer — `Arelia.Application`

#### Commands

**`AttachFileToActivityCommand`**  
Fields: `ActivityId`, `FileName`, `ContentType`, `Stream FileContent`, `OrganizationId`  
Returns: `Result<Guid>` (new `ActivityAttachment` Id)  
Handler validates MIME type and file size, then calls `IFileStorageService.SaveAsync(...)` and persists the `ActivityAttachment` entity.

**`DeleteActivityAttachmentCommand`**  
Fields: `AttachmentId`  
Returns: `Result`  
Handler calls `IFileStorageService.DeleteAsync(...)` then soft-deletes the entity.

#### Queries

**`GetActivityAttachmentsQuery`**  
Filter: `ActivityId`  
Returns: `IReadOnlyList<ActivityAttachmentDto>`  

```
ActivityAttachmentDto(Guid Id, string FileName, string ContentType, long FileSizeBytes, DateTime UploadedAt)
```

> `FileSizeBytes` is read from disk via `IFileStorageService` at query time, **not** stored in the DB, keeping the entity lean.

#### IAreliaDbContext update
Add: `DbSet<ActivityAttachment> ActivityAttachments { get; }`

---

### 2.3 Infrastructure — `Arelia.Infrastructure`

#### EF Core & Migration
- Add `ActivityAttachments` `DbSet<ActivityAttachment>` to `AreliaDbContext`
- Entity configuration: required `FileName` (max 500), required `ContentType` (max 100), required `FilePath` (max 1000)
- FK: `ActivityId` → `Activity.Id`, `DeleteBehavior.Cascade` (deleting an activity deletes its attachments from DB — a background job or domain event handles disk cleanup)
- New EF Core migration: `AddActivityAttachments`

---

### 2.4 Web UI — `Arelia.Web`

#### `ActivityDetail.razor` — new Attachments panel

Added as a new `MudCard` section below the existing attendance grid on `ActivityDetail.razor`.

**Shown to:** All authenticated members (read/download). Upload and delete shown only to users with `ManageActivities` permission.

**Layout:**
- Card header: "Attachments" + count chip + Upload button (permission-gated)
- List of existing attachments as `MudChip` rows or a compact `MudSimpleTable`:
  - File icon (based on content type), filename, upload date
  - Download button (navigates to `/api/attachments/{id}` — see API endpoint below)
  - Delete button (permission-gated, with `ConfirmDialog`)
- Upload uses Blazor's built-in `InputFile` component wrapped in a `MudButton` trigger; max 20 MB enforced client-side before calling the command

#### New minimal API endpoint: `GET /api/attachments/{attachmentId:guid}`

Defined in `Program.cs` as a minimal API route (not a full controller).  
Streams the file from disk via `IFileStorageService.ReadAsync(...)`.  
Returns the file with the correct `Content-Disposition: attachment` header.  
Authorization: requires authenticated user belonging to the same org as the attachment.

---

## Feature 3: Shared File Storage Infrastructure

### 3.1 Interface — `Arelia.Application\Interfaces\IFileStorageService.cs`

```csharp
public interface IFileStorageService
{
    /// Saves content to storage and returns the relative file path.
    Task<string> SaveAsync(Stream content, string fileName, string contentType,
                           string subDirectory, CancellationToken ct = default);

    /// Opens a read stream for the given relative file path.
    Task<Stream> ReadAsync(string relativePath, CancellationToken ct = default);

    /// Deletes the file at the given relative file path. No-op if not found.
    Task DeleteAsync(string relativePath, CancellationToken ct = default);

    /// Returns the size in bytes for the given relative file path, or null if not found.
    Task<long?> GetFileSizeAsync(string relativePath, CancellationToken ct = default);
}
```

### 3.2 Implementation — `DiskFileStorageService`

Located in `Arelia.Infrastructure\Services\DiskFileStorageService.cs`.

- Base path read from `IConfiguration["FileStorage:BasePath"]`, defaulting to `./uploads` (relative to app root)
- `subDirectory` parameter used to separate storage buckets: `"activity-attachments"`, `"expense-attachments"`
- Each saved file is stored as `{basePath}/{subDirectory}/{orgId}/{Guid.NewGuid()}{ext}` — original filename is **never** used as the on-disk path (prevents path traversal)
- `ReadAsync` opens a `FileStream` with `FileShare.Read`
- `DeleteAsync` calls `File.Delete`, swallows `FileNotFoundException`
- Registered as **scoped** in `DependencyInjection.cs`

### 3.3 Expense Attachment Retrofit

The existing `ExpenseAttachment` entity already exists. Once `IFileStorageService` and `DiskFileStorageService` are in place, any future expense-attachment upload/download UI will use the same service — no changes to existing entities or migrations required.

---

## Feature 4: Permission

### New permission value: `ManageDocuments`

Add `ManageDocuments` to the `Permission` enum in `Arelia.Domain\Enums\Permission.cs`.

**Default role assignments** (applied in the org-creation seeder in `CreateOrganization`):

| Role | `ManageDocuments` |
|---|---|
| Admin | ✅ |
| Board | ✅ |
| Conductor | ❌ |
| Treasurer | ❌ |

This permission gates: creating/editing/deleting documents **and** managing document categories.

---

## Cross-cutting Concerns

### Security

- All `ContentHtml` is sanitized via `IHtmlSanitizerService` **before** persisting — not only on display.
- File download endpoint validates that the requesting user belongs to the same `OrganizationId` as the attachment — no cross-tenant file access.
- Uploaded filenames are **never** used as disk paths. On-disk names are GUIDs.
- The file download endpoint uses `Content-Disposition: attachment` — never inline — to prevent browser execution of uploaded content.
- MIME type is validated server-side against the accept list regardless of the browser-supplied value.

### Localization

New string keys (English + Danish) required for all new UI text: page titles, button labels, empty states, error messages, column headers. Keys follow the existing `Feature.Key` pattern (e.g. `Documents.Title`, `Documents.NewDocument`, `Documents.NoCategory`, `DocumentCategories.Title`, `DocumentCategories.Create`, `ActivityDetail.Attachments`).

### Configuration (`appsettings.json`)

```json
"FileStorage": {
  "BasePath": "./uploads",
  "MaxFileSizeBytes": 20971520
}
```

Both keys must be documented in the Docker Compose environment variable block and the `README`.

### EF Core Migrations

Two new migrations in order:
1. `AddDocuments` — `Document` table
2. `AddActivityAttachments` — `ActivityAttachment` table + `Activity.Attachments` navigation

---

## Implementation Order

The natural build order respects layer dependencies:

1. **`IFileStorageService` + `DiskFileStorageService`** — shared foundation
2. **`IHtmlSanitizerService` + `HtmlSanitizerService`** — needed by document commands
3. **`ManageDocuments` permission** — enum update + `CreateOrganization` seeder update (role assignment + default category seeding)
4. **`DocumentCategory` + `Document` entities + EF config + migration `AddDocuments`**
5. **DocumentCategory application commands + query** (`CreateDocumentCategory`, `UpdateDocumentCategory`, `DeleteDocumentCategory`, `GetDocumentCategories`)
6. **Document application commands + queries** (`CreateDocument`, `UpdateDocument`, `DeleteDocument`, `GetDocuments`, `GetDocumentDetail`)
7. **`IPdfExportService` + `QuestPdfExportService`**
8. **Documents UI** (`DocumentCategories.razor`, `Documents.razor`, `DocumentEditor.razor`, `DocumentDetail.razor`) + NavMenu entries
9. **`ActivityAttachment` entity + `Activity` nav property + EF config + migration `AddActivityAttachments`**
10. **Activity attachment application commands + query** (`AttachFileToActivity`, `DeleteActivityAttachment`, `GetActivityAttachments`)
11. **Activity attachments UI panel in `ActivityDetail.razor`** + minimal API download endpoint

---

## Out of Scope (for this phase)

- Document version history
- Document sharing / public links
- In-app image upload within the editor (the editor can paste images as base64; that is acceptable for now)
- Expense attachment upload UI (the infrastructure is ready; the UI is deferred to the Finance phase)
- Activity detail PDF export (only the Document entity is exported; activity PDFs are out of scope)
- Full-text search across document content
- Reordering document categories via drag-and-drop (sort order is set manually via the numeric field, same as voice groups)
