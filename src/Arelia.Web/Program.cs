using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Arelia.Web.Components;
using Arelia.Web.Components.Account;
using Arelia.Web.Localization;
using Arelia.Web.Middleware;
using Arelia.Web.Services;
using Arelia.Application;
using Arelia.Application.Documents.Queries;
using Arelia.Application.Interfaces;
using Arelia.Infrastructure;
using Arelia.Infrastructure.Identity;
using Arelia.Infrastructure.Persistence;
using Arelia.Infrastructure.Services;
using Arelia.Application.Mediator;
using MudBlazor.Services;
using Radzen;

namespace Arelia.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add layer services
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // Blazor
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // MudBlazor
        builder.Services.AddMudServices();

        // Radzen
        builder.Services.AddRadzenComponents();

        // Tenant service
        builder.Services.AddScoped<TenantService>();
        builder.Services.AddScoped<CsvImportSession>();

        // Identity UI support
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityUserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Localization
        builder.Services.AddLocalization();
        builder.Services.AddScoped<CultureService>();
        builder.Services.AddScoped<AdminHighlightService>();
        builder.Services.AddScoped<UiSkinService>();
        builder.Services.AddScoped<NavigationHistoryService>();
        builder.Services.AddScoped<ILocalizer, Localizer>();

        var app = builder.Build();

        // Seed data
        await DataSeeder.SeedAsync(app.Services);

        // When running behind a reverse proxy (e.g. Cloudflare Tunnel → cloudflared container),
        // the real scheme/host/IP are in X-Forwarded-* headers. This must come first so that
        // HTTPS detection, cookie Secure flags, and redirect URIs all see the correct values.
        //
        // KnownNetworks/KnownProxies are cleared because ASP.NET Core only trusts loopback
        // addresses by default. The cloudflared sidecar communicates over the Docker bridge
        // network (172.x.x.x), not loopback, so headers would otherwise be silently ignored.
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };
        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        app.UseMiddleware<MaintenanceMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
            // Only redirect to HTTPS in production; in Docker/dev the container speaks
            // plain HTTP, and redirecting would break the Blazor SignalR WebSocket handshake.
            app.UseHttpsRedirection();
        }

        var supportedCultures = CultureService.SupportedCultures;
        app.UseRequestLocalization(options =>
        {
            options.SetDefaultCulture("en")
                   .AddSupportedCultures(supportedCultures)
                   .AddSupportedUICultures(supportedCultures);

            // Read culture from the cookie written by CultureService
            options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider
            {
                CookieName = ".AspNetCore.Culture"
            });
        });
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();

        // --- Minimal API: document PDF download ---
        app.MapGet("/api/documents/{documentId:guid}/pdf", async (
            Guid documentId,
            IMediator mediator,
            IPdfExportService pdfService,
            ITenantContext tenantContext,
            HttpContext http) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var document = await mediator.Send(new GetDocumentDetailQuery(documentId));
            if (document is null)
                return Results.NotFound();

            // Verify the document belongs to the current tenant
            if (tenantContext.CurrentOrganizationId.HasValue &&
                document.OrganizationId != tenantContext.CurrentOrganizationId.Value)
                return Results.NotFound();

            var bytes = await pdfService.ExportDocumentAsync(document);
            var fileName = $"{document.Title.Replace(" ", "_")}.pdf";
            return Results.File(bytes, "application/pdf", fileName);
        }).RequireAuthorization();

        // --- Minimal API: activity attachment download ---
        app.MapGet("/api/attachments/{attachmentId:guid}", async (
            Guid attachmentId,
            IFileStorageService fileStorage,
            AreliaDbContext db,
            ITenantContext tenantContext,
            HttpContext http) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var attachment = await db.ActivityAttachments.FindAsync(attachmentId);
            if (attachment is null || !attachment.IsActive)
                return Results.NotFound();

            // Verify the attachment belongs to the current tenant
            if (tenantContext.CurrentOrganizationId.HasValue &&
                attachment.OrganizationId != tenantContext.CurrentOrganizationId.Value)
                return Results.NotFound();

            var stream = await fileStorage.ReadAsync(attachment.FilePath);
            return Results.File(stream, attachment.ContentType,
                attachment.FileName, enableRangeProcessing: false);
        }).RequireAuthorization();

        app.Run();
    }
}
