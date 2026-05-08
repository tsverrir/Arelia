using Arelia.Application.Common.Validation;
using Arelia.Domain.Entities;

namespace Arelia.Application.People.Commands;

public record BulkCreatePeopleCommand(
    List<PersonImportRow> Rows,
    Guid OrganizationId) : IRequest<int>;

public class BulkCreatePeopleHandler(IAreliaDbContext context)
    : IRequestHandler<BulkCreatePeopleCommand, int>
{
    /// <summary>Creates all valid rows as persons, auto-assigns the Member role, and returns the count created.</summary>
    public async Task<int> Handle(BulkCreatePeopleCommand request, CancellationToken cancellationToken)
    {
        var validRows = request.Rows.Where(r => !r.HasError).ToList();
        if (validRows.Count == 0)
            return 0;

        var memberRole = await context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.OrganizationId == request.OrganizationId &&
                                      r.Name == "Member" && r.IsActive, cancellationToken);

        // Build a name→id lookup so CSV rows with VoiceGroupName can be resolved
        var voiceGroupLookup = await context.VoiceGroups
            .IgnoreQueryFilters()
            .Where(v => v.OrganizationId == request.OrganizationId && v.IsActive)
            .ToDictionaryAsync(v => v.Name.ToLower(), v => v.Id, cancellationToken);

        foreach (var row in validRows)
        {
            var voiceGroupId = row.VoiceGroupId;
            if (voiceGroupId is null && row.VoiceGroupName is not null &&
                voiceGroupLookup.TryGetValue(row.VoiceGroupName.ToLower(), out var resolvedId))
                voiceGroupId = resolvedId;

            var person = new Person
            {
                FirstName      = row.FirstName.Trim(),
                LastName       = row.LastName.Trim(),
                Email          = InputValidation.NormalizeOptional(row.Email),
                Phone          = InputValidation.NormalizeOptional(row.Phone),
                VoiceGroupId   = voiceGroupId,
                Notes          = InputValidation.NormalizeOptional(row.Notes),
                OrganizationId = request.OrganizationId,
            };
            context.Persons.Add(person);

            if (memberRole is not null)
            {
                context.RoleAssignments.Add(new RoleAssignment
                {
                    PersonId = person.Id,
                    RoleId = memberRole.Id,
                    FromDate = DateTime.UtcNow,
                    OrganizationId = request.OrganizationId,
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return validRows.Count;
    }
}
