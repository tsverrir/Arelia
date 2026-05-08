using Arelia.Domain.Entities;
using Arelia.Domain.Enums;

namespace Arelia.Application.Organizations.Commands;

public record CreateOrganizationCommand(
    string Name,
    string? ContactEmail,
    string? ContactPhone,
    string CreatingUserId) : IRequest<Guid>;

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

        // Link creating user as member
        var orgUser = new OrganizationUser
        {
            UserId = request.CreatingUserId,
            OrganizationId = org.Id,
            IsActive = true,
        };
        context.OrganizationUsers.Add(orgUser);

        // Seed default roles
        var defaultRoles = new (string Name, Permission[] Permissions)[]
        {
            ("Member",    []),
            ("Board",     [Permission.ManagePeople, Permission.ManageActivities, Permission.ManageAttendance, Permission.RsvpOnBehalf, Permission.ViewAttendanceReports, Permission.ViewMembershipReports, Permission.ManageDocuments]),
            ("Treasurer", [Permission.ManageCharges, Permission.ManageExpenses, Permission.ViewFinanceReports, Permission.ViewMembershipReports]),
            ("Conductor", [Permission.ManageAttendance, Permission.ViewAttendanceReports]),
            ("Admin",     Enum.GetValues<Permission>()),
        };

        Role? adminRole = null;
        foreach (var (roleName, permissions) in defaultRoles)
        {
            var role = new Role
            {
                Name = roleName,
                OrganizationId = org.Id,
            };
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

            if (roleName == "Admin")
                adminRole = role;
        }

        // Create a Person for the creating user and assign Admin role
        var person = new Person
        {
            FirstName = "Admin",
            LastName = "User",
            OrganizationId = org.Id,
        };
        context.Persons.Add(person);

        orgUser.PersonId = person.Id;

        if (adminRole is not null)
        {
            context.RoleAssignments.Add(new RoleAssignment
            {
                PersonId = person.Id,
                RoleId = adminRole.Id,
                FromDate = DateTime.UtcNow,
                OrganizationId = org.Id,
            });
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
