using Arelia.Application.Interfaces;
using Arelia.Domain.Entities;
using Arelia.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Arelia.Application.Finance.Commands;

public record GenerateMembershipFeesCommand(
    Guid SemesterId,
    decimal BaseFeeAmount,
    decimal? TopUpAmount,
    DateTime DueDate,
    Guid OrganizationId) : IRequest<Domain.Common.Result<int>>;

public class GenerateMembershipFeesHandler(IAreliaDbContext context)
    : IRequestHandler<GenerateMembershipFeesCommand, Domain.Common.Result<int>>
{
    public async Task<Domain.Common.Result<int>> Handle(
        GenerateMembershipFeesCommand request, CancellationToken cancellationToken)
    {
        if (request.BaseFeeAmount <= 0)
            return Domain.Common.Result.Failure<int>("Base fee must be greater than zero.");

        if (request.TopUpAmount is <= 0)
            return Domain.Common.Result.Failure<int>("Optional top-up must be greater than zero when provided.");

        if (request.DueDate.Date < DateTime.UtcNow.Date)
            return Domain.Common.Result.Failure<int>("Due date cannot be in the past.");

        var org = await context.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (org is null)
            return Domain.Common.Result.Failure<int>("Organization not found.");

        var semesterExists = await context.Activities.AnyAsync(a =>
            a.Id == request.SemesterId &&
            a.OrganizationId == request.OrganizationId &&
            a.ActivityType == ActivityType.Semester,
            cancellationToken);

        if (!semesterExists)
            return Domain.Common.Result.Failure<int>("Semester not found.");

        var activeMembers = await context.Persons
            .Where(p => p.OrganizationId == request.OrganizationId && p.IsActive)
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var member in activeMembers)
        {
            var charge = new Charge
            {
                PersonId = member.Id,
                SemesterId = request.SemesterId,
                Description = $"Membership fee",
                DueDate = request.DueDate,
                CurrencyCode = org.DefaultCurrencyCode,
                OrganizationId = request.OrganizationId,
            };

            charge.ChargeLines.Add(new ChargeLine
            {
                Description = "Base fee",
                Amount = request.BaseFeeAmount,
                LineType = ChargeLineType.Base,
                IsSelected = true,
                OrganizationId = request.OrganizationId,
            });

            if (request.TopUpAmount.HasValue && request.TopUpAmount.Value > 0)
            {
                charge.ChargeLines.Add(new ChargeLine
                {
                    Description = "Top-up (optional)",
                    Amount = request.TopUpAmount.Value,
                    LineType = ChargeLineType.TopUp,
                    IsSelected = false,
                    OrganizationId = request.OrganizationId,
                });
            }

            context.Charges.Add(charge);
            count++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Domain.Common.Result.Success(count);
    }
}
