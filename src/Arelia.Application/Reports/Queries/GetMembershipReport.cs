
namespace Arelia.Application.Reports.Queries;

public record GetMembershipReportQuery(Guid OrganizationId) : IRequest<MembershipReportDto>;

public record MembershipReportDto(
    int TotalActive,
    int TotalInactive,
    List<VoiceGroupCount> ByVoiceGroup,
    List<RoleMemberCount> ByRole);

public record VoiceGroupCount(string VoiceGroup, int Count);
public record RoleMemberCount(string RoleName, int Count);

public class GetMembershipReportHandler(IAreliaDbContext context)
    : IRequestHandler<GetMembershipReportQuery, MembershipReportDto>
{
    public async Task<MembershipReportDto> Handle(
        GetMembershipReportQuery request, CancellationToken cancellationToken)
    {
        var persons = await context.Persons
            .IgnoreQueryFilters()
            .Where(p => p.OrganizationId == request.OrganizationId)
            .Select(p => new { p.IsActive, VoiceGroupName = p.VoiceGroup != null ? p.VoiceGroup.Name : null })
            .ToListAsync(cancellationToken);

        var active = persons.Count(p => p.IsActive);
        var inactive = persons.Count(p => !p.IsActive);

        var byVoiceGroup = persons
            .Where(p => p.IsActive && p.VoiceGroupName is not null)
            .GroupBy(p => p.VoiceGroupName!)
            .Select(g => new VoiceGroupCount(g.Key, g.Count()))
            .OrderBy(v => v.VoiceGroup)
            .ToList();

        var now = DateTime.UtcNow;
        var activeAssignments = await context.RoleAssignments
            .IgnoreQueryFilters()
            .Include(ra => ra.Role)
            .Where(ra =>
                ra.OrganizationId == request.OrganizationId &&
                ra.IsActive &&
                ra.FromDate <= now &&
                (ra.ToDate == null || ra.ToDate >= now))
            .ToListAsync(cancellationToken);

        var byRole = activeAssignments
            .GroupBy(ra => ra.Role.Name)
            .Select(g => new RoleMemberCount(g.Key, g.Select(ra => ra.PersonId).Distinct().Count()))
            .OrderBy(r => r.RoleName)
            .ToList();

        return new MembershipReportDto(active, inactive, byVoiceGroup, byRole);
    }
}
