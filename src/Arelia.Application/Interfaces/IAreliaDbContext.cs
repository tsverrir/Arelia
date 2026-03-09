using Arelia.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Interfaces;

public interface IAreliaDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationUser> OrganizationUsers { get; }
    DbSet<Person> Persons { get; }
    DbSet<VoiceGroup> VoiceGroups { get; }
    DbSet<Role> Roles { get; }
    DbSet<RoleAssignment> RoleAssignments { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Activity> Activities { get; }
    DbSet<ActivityParticipant> ActivityParticipants { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<RehearsalRecurrenceTemplate> RehearsalRecurrenceTemplates { get; }
    DbSet<Charge> Charges { get; }
    DbSet<ChargeLine> ChargeLines { get; }
    DbSet<Payment> Payments { get; }
    DbSet<CreditBalance> CreditBalances { get; }
    DbSet<CreditTransaction> CreditTransactions { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<ExpenseAttachment> ExpenseAttachments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLogEntry> AuditLogEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
