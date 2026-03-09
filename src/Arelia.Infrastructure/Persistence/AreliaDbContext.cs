using Arelia.Application.Interfaces;
using Arelia.Domain.Common;
using Arelia.Domain.Entities;
using Arelia.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Infrastructure.Persistence;

public class AreliaDbContext : IdentityDbContext<ApplicationUser>, IAreliaDbContext
{
	private readonly ITenantContext? _tenantContext;

	public AreliaDbContext(DbContextOptions<AreliaDbContext> options, ITenantContext? tenantContext = null)
		: base(options)
	{
		_tenantContext = tenantContext;
	}

	public DbSet<Organization> Organizations => Set<Organization>();
	public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();
	public DbSet<Person> Persons => Set<Person>();
	public DbSet<VoiceGroup> VoiceGroups => Set<VoiceGroup>();
	public DbSet<Role> Roles => Set<Role>();
	public new DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
	public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
	public DbSet<Activity> Activities => Set<Activity>();
	public DbSet<ActivityParticipant> ActivityParticipants => Set<ActivityParticipant>();
	public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
	public DbSet<RehearsalRecurrenceTemplate> RehearsalRecurrenceTemplates => Set<RehearsalRecurrenceTemplate>();
	public DbSet<Charge> Charges => Set<Charge>();
	public DbSet<ChargeLine> ChargeLines => Set<ChargeLine>();
	public DbSet<Payment> Payments => Set<Payment>();
	public DbSet<CreditBalance> CreditBalances => Set<CreditBalance>();
	public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
	public DbSet<Expense> Expenses => Set<Expense>();
	public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
	public DbSet<ExpenseAttachment> ExpenseAttachments => Set<ExpenseAttachment>();
	public DbSet<Notification> Notifications => Set<Notification>();
	public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		// Ignore DomainEvents — not persisted
		builder.Ignore<DomainEvent>();

		builder.ApplyConfigurationsFromAssembly(typeof(AreliaDbContext).Assembly);

		// Global query filters for tenant isolation and soft delete on BaseEntity
		foreach (var entityType in builder.Model.GetEntityTypes())
		{
			if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
			{
				var method = typeof(AreliaDbContext)
					.GetMethod(nameof(ApplyBaseEntityFilters), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
					.MakeGenericMethod(entityType.ClrType);
				method.Invoke(this, [builder]);
			}
		}
	}

	private void ApplyBaseEntityFilters<T>(ModelBuilder builder) where T : BaseEntity
	{
		builder.Entity<T>().HasQueryFilter(e =>
			e.IsActive &&
			(_tenantContext == null || _tenantContext.CurrentOrganizationId == null || e.OrganizationId == _tenantContext.CurrentOrganizationId));
	}

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		ApplyAuditFields();
		return base.SaveChangesAsync(cancellationToken);
	}

	public override int SaveChanges()
	{
		ApplyAuditFields();
		return base.SaveChanges();
	}

	private void ApplyAuditFields()
	{
		var now = DateTime.UtcNow;
		var userId = _tenantContext?.CurrentUserId;

		// Snapshot before adding audit entries to avoid re-entrancy
		var baseEntries = ChangeTracker.Entries<BaseEntity>().ToList();
		var auditEntries = new List<AuditLogEntry>(baseEntries.Count);

		foreach (var entry in baseEntries)
		{
			switch (entry.State)
			{
				case EntityState.Added:
					entry.Entity.CreatedAt = now;
					entry.Entity.CreatedBy = userId;
					if (entry.Entity.OrganizationId == Guid.Empty && _tenantContext?.CurrentOrganizationId is Guid orgId)
						entry.Entity.OrganizationId = orgId;
					auditEntries.Add(new AuditLogEntry
					{
						Action = "Created",
						EntityType = entry.Entity.GetType().Name,
						EntityId = entry.Entity.Id.ToString(),
						UserId = userId,
						Timestamp = now,
						OrganizationId = entry.Entity.OrganizationId != Guid.Empty
							? entry.Entity.OrganizationId
							: _tenantContext?.CurrentOrganizationId,
					});
					break;
				case EntityState.Modified:
					entry.Entity.UpdatedAt = now;
					entry.Entity.UpdatedBy = userId;
					auditEntries.Add(new AuditLogEntry
					{
						Action = "Updated",
						EntityType = entry.Entity.GetType().Name,
						EntityId = entry.Entity.Id.ToString(),
						UserId = userId,
						Timestamp = now,
						OrganizationId = entry.Entity.OrganizationId != Guid.Empty
							? entry.Entity.OrganizationId
							: _tenantContext?.CurrentOrganizationId,
					});
					break;
			}
		}

		AuditLogEntries.AddRange(auditEntries);

		// Also handle Organization and OrganizationUser audit fields
		foreach (var entry in ChangeTracker.Entries<Organization>())
		{
			switch (entry.State)
			{
				case EntityState.Added:
					entry.Entity.CreatedAt = now;
					entry.Entity.CreatedBy = userId;
					break;
				case EntityState.Modified:
					entry.Entity.UpdatedAt = now;
					entry.Entity.UpdatedBy = userId;
					break;
			}
		}

		foreach (var entry in ChangeTracker.Entries<OrganizationUser>())
		{
			switch (entry.State)
			{
				case EntityState.Added:
					entry.Entity.CreatedAt = now;
					entry.Entity.CreatedBy = userId;
					break;
				case EntityState.Modified:
					entry.Entity.UpdatedAt = now;
					entry.Entity.UpdatedBy = userId;
					break;
			}
		}
	}
}
