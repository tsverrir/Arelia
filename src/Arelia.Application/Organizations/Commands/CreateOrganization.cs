using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Organizations.Commands;

public record CreateOrganizationCommand(
    string Name,
    string? ContactEmail,
    string? ContactPhone) : IRequest<Guid>;

public class CreateOrganizationHandler(IAreliaDbContext context) : IRequestHandler<CreateOrganizationCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = new Organization
        {
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
        };

        context.Organizations.Add(org);

        // Seed System Roles (Admin, Board, Member)
        var systemRoles = new (string Name, RoleType Type, Permission[] Permissions)[]
        {
            ("Member", RoleType.Member, []),
            ("Board",  RoleType.Board,  [Permission.ManagePeople, Permission.ManageActivities, Permission.ManageAttendance, Permission.RsvpOnBehalf, Permission.ViewAttendanceReports, Permission.ViewMembershipReports, Permission.ManageDocuments]),
            ("Admin",  RoleType.Admin,  Enum.GetValues<Permission>()),
        };

        foreach (var (roleName, roleType, permissions) in systemRoles)
        {
            var role = new Role { Name = roleName, RoleType = roleType, OrganizationId = org.Id };
            context.Roles.Add(role);

            foreach (var permission in permissions)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    Permission = permission,
                    OrganizationId = org.Id,
                });
            }
        }

        // Seed Custom roles as org defaults
        var customRoles = new (string Name, Permission[] Permissions)[]
        {
            ("Treasurer", [Permission.ManageCharges, Permission.ManageExpenses, Permission.ViewFinanceReports, Permission.ViewMembershipReports]),
            ("Conductor", [Permission.ManageAttendance, Permission.ViewAttendanceReports]),
        };

        foreach (var (roleName, permissions) in customRoles)
        {
            var role = new Role { Name = roleName, RoleType = RoleType.Custom, OrganizationId = org.Id };
            context.Roles.Add(role);

            foreach (var permission in permissions)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    Permission = permission,
                    OrganizationId = org.Id,
                });
            }
        }

        // Seed default expense categories
        var defaultCategories = new[]
        {
            "SHEET MUSIC", "VENUE RENTAL", "INSTRUMENT MAINTENANCE", "TRAVEL",
            "REFRESHMENTS", "MARKETING", "INSURANCE", "OTHER"
        };

        foreach (var category in defaultCategories)
        {
            context.ExpenseCategories.Add(new ExpenseCategory
            {
                Name = category,
                OrganizationId = org.Id,
            });
        }

        // Seed default voice groups
        var defaultVoiceGroups = new (string Name, int SortOrder)[]
        {
            ("Soprano", 1), ("Mezzo-Soprano", 2), ("Alto", 3),
            ("Tenor", 4), ("Baritone", 5), ("Bass", 6),
            ("Other", 7),
        };

        foreach (var (name, sortOrder) in defaultVoiceGroups)
        {
            context.VoiceGroups.Add(new VoiceGroup
            {
                Name = name,
                SortOrder = sortOrder,
                OrganizationId = org.Id,
            });
        }

        // Seed default document categories
        var defaultDocumentCategories = new (string Name, int SortOrder)[]
        {
            ("Meeting Minutes", 1), ("Internal Document", 2), ("Policy", 3),
        };

        foreach (var (name, sortOrder) in defaultDocumentCategories)
        {
            context.DocumentCategories.Add(new DocumentCategory
            {
                Name = name,
                SortOrder = sortOrder,
                OrganizationId = org.Id,
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return org.Id;
    }
}
