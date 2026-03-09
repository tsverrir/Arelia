using Arelia.Domain.Entities;
using Arelia.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arelia.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.PreferredLanguage).HasMaxLength(10);
    }
}

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.DefaultCurrencyCode).HasMaxLength(3);
        builder.Property(o => o.Timezone).HasMaxLength(100);
        builder.Property(o => o.DefaultRehearsalLocation).HasMaxLength(500);
        builder.Property(o => o.DefaultLanguage).HasMaxLength(10);
        builder.Property(o => o.ContactEmail).HasMaxLength(200);
        builder.Property(o => o.ContactPhone).HasMaxLength(50);

        builder.HasQueryFilter(o => o.IsActive);
    }
}

public class OrganizationUserConfiguration : IEntityTypeConfiguration<OrganizationUser>
{
    public void Configure(EntityTypeBuilder<OrganizationUser> builder)
    {
        builder.HasKey(ou => ou.Id);
        builder.Property(ou => ou.UserId).IsRequired().HasMaxLength(450);

        builder.HasOne(ou => ou.Organization)
            .WithMany(o => o.OrganizationUsers)
            .HasForeignKey(ou => ou.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ou => ou.Person)
            .WithMany()
            .HasForeignKey(ou => ou.PersonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(ou => new { ou.UserId, ou.OrganizationId }).IsUnique();
    }
}

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Email).HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(2000);
    }
}

public class RoleConfiguration
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
    }
}

public class RoleAssignmentConfiguration
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.HasOne(ra => ra.Person)
            .WithMany(p => p.RoleAssignments)
            .HasForeignKey(ra => ra.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ra => ra.Role)
            .WithMany(r => r.RoleAssignments)
            .HasForeignKey(ra => ra.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RolePermissionConfiguration
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rp => new { rp.RoleId, rp.Permission }).IsUnique();
    }
}

public class ActivityConfiguration
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Description).HasMaxLength(2000);
        builder.Property(a => a.Location).HasMaxLength(500);

        builder.HasOne(a => a.ParentActivity)
            .WithMany(a => a.ChildActivities)
            .HasForeignKey(a => a.ParentActivityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ActivityParticipantConfiguration : IEntityTypeConfiguration<ActivityParticipant>
{
    public void Configure(EntityTypeBuilder<ActivityParticipant> builder)
    {
        builder.HasOne(ap => ap.Activity)
            .WithMany(a => a.Participants)
            .HasForeignKey(ap => ap.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ap => ap.Person)
            .WithMany()
            .HasForeignKey(ap => ap.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ap => new { ap.ActivityId, ap.PersonId }).IsUnique();
    }
}

public class AttendanceRecordConfiguration
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasOne(ar => ar.Activity)
            .WithMany(a => a.AttendanceRecords)
            .HasForeignKey(ar => ar.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.Person)
            .WithMany()
            .HasForeignKey(ar => ar.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ar => new { ar.ActivityId, ar.PersonId }).IsUnique();
        builder.Property(ar => ar.RecordedByUserId).HasMaxLength(450);
        builder.Property(ar => ar.Comment).HasMaxLength(255);
    }
}

public class RehearsalRecurrenceTemplateConfiguration
{
    public void Configure(EntityTypeBuilder<RehearsalRecurrenceTemplate> builder)
    {
        builder.Property(t => t.Location).HasMaxLength(500);

        builder.HasOne(t => t.Semester)
            .WithMany()
            .HasForeignKey(t => t.SemesterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> builder)
    {
        builder.Property(c => c.Description).IsRequired().HasMaxLength(500);
        builder.Property(c => c.CurrencyCode).HasMaxLength(3);

        builder.HasOne(c => c.Person)
            .WithMany()
            .HasForeignKey(c => c.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Semester)
            .WithMany()
            .HasForeignKey(c => c.SemesterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ChargeLineConfiguration : IEntityTypeConfiguration<ChargeLine>
{
    public void Configure(EntityTypeBuilder<ChargeLine> builder)
    {
        builder.Property(cl => cl.Description).IsRequired().HasMaxLength(200);
        builder.Property(cl => cl.Amount).HasPrecision(18, 2);

        builder.HasOne(cl => cl.Charge)
            .WithMany(c => c.ChargeLines)
            .HasForeignKey(cl => cl.ChargeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.PayerDescription).HasMaxLength(500);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.PaymentMethod).HasMaxLength(100);
        builder.Property(p => p.Reference).HasMaxLength(200);
        builder.Property(p => p.CurrencyCode).HasMaxLength(3);
        builder.Property(p => p.OriginalAmount).HasPrecision(18, 2);
        builder.Property(p => p.OriginalCurrencyCode).HasMaxLength(3);

        builder.HasOne(p => p.Charge)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.ChargeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.PayerPerson)
            .WithMany()
            .HasForeignKey(p => p.PayerPersonId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CreditBalanceConfiguration : IEntityTypeConfiguration<CreditBalance>
{
    public void Configure(EntityTypeBuilder<CreditBalance> builder)
    {
        builder.Property(cb => cb.BalanceAmount).HasPrecision(18, 2);
        builder.Property(cb => cb.CurrencyCode).HasMaxLength(3);

        builder.HasOne(cb => cb.Person)
            .WithMany()
            .HasForeignKey(cb => cb.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cb => new { cb.PersonId, cb.OrganizationId }).IsUnique();
    }
}

public class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.Property(ct => ct.Amount).HasPrecision(18, 2);
        builder.Property(ct => ct.Reason).IsRequired().HasMaxLength(500);

        builder.HasOne(ct => ct.Person)
            .WithMany()
            .HasForeignKey(ct => ct.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.CurrencyCode).HasMaxLength(3);
        builder.Property(e => e.ReceivedBy).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.Property(ec => ec.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(ec => new { ec.Name, ec.OrganizationId }).IsUnique();
    }
}

public class ExpenseAttachmentConfiguration : IEntityTypeConfiguration<ExpenseAttachment>
{
    public void Configure(EntityTypeBuilder<ExpenseAttachment> builder)
    {
        builder.Property(ea => ea.FileName).IsRequired().HasMaxLength(500);
        builder.Property(ea => ea.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(ea => ea.FilePath).IsRequired().HasMaxLength(1000);

        builder.HasOne(ea => ea.Expense)
            .WithMany(e => e.Attachments)
            .HasForeignKey(ea => ea.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.RecipientUserId).IsRequired().HasMaxLength(450);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.LinkUrl).HasMaxLength(500);
    }
}

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserId).HasMaxLength(450);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EntityType).HasMaxLength(200);
        builder.Property(a => a.EntityId).HasMaxLength(450);

        // No query filter — audit logs are never soft-deleted
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.OrganizationId);
    }
}
