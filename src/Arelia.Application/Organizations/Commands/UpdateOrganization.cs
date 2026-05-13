//-------------------------------------------------------------------------------------------------
//
// UpdateOrganization.cs -- The UpdateOrganizationCommand and handler.
//
// Copyright (c) 2026 JBT Marel. All rights reserved.
//
//-------------------------------------------------------------------------------------------------

using Arelia.Domain.Common;

namespace Arelia.Application.Organizations.Commands;

/// <summary>
/// Updates the editable fields of an organisation. System Admin use only — bypasses tenant filter.
/// </summary>
public record UpdateOrganizationCommand(
	Guid OrganizationId,
	string Name,
	string? ContactEmail,
	string? ContactPhone,
	string? DefaultLanguage,
	bool IsActive) : IRequest<Result>;

/// <summary>
/// Handles <see cref="UpdateOrganizationCommand"/>.
/// </summary>
public sealed class UpdateOrganizationHandler(IAreliaDbContext context)
	: IRequestHandler<UpdateOrganizationCommand, Result>
{
	//-----------------------------------------------------------------------------------------
	/// <summary>
	/// Applies the updates to the organisation and persists the changes.
	/// </summary>
	public async Task<Result> Handle(
		UpdateOrganizationCommand request,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Name))
		{
			return Result.Failure("Organisation name is required.");
		}

		var org = await context.Organizations
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

		if (org is null)
		{
			return Result.Failure($"Organisation {request.OrganizationId} not found.");
		}

		org.Name = request.Name.Trim();
		org.ContactEmail = request.ContactEmail;
		org.ContactPhone = request.ContactPhone;
		org.DefaultLanguage = request.DefaultLanguage;
		org.IsActive = request.IsActive;

		await context.SaveChangesAsync(cancellationToken);
		return Result.Success();
	}
}
