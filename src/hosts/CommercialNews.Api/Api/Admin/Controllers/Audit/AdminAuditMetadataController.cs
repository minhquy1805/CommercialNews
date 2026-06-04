using Audit.Application.Models.Queries.Metadata;
using Audit.Application.Models.Results.Metadata;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Metadata.Mapping;
using CommercialNews.Api.Api.Admin.Contracts.Audit.Metadata.Responses;
using CommercialNews.Api.Api.Common.ErrorHandling;
using CommercialNews.Api.Api.ErrorHandling;
using CommercialNews.Api.Authorization;
using CommercialNews.BuildingBlocks.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommercialNews.Api.Api.Admin.Controllers.Audit;

[ApiController]
[Route("api/v1/admin/audit")]
public sealed class AdminAuditMetadataController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAuditMetadataController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("modules")]
    [Authorize(Policy = AuthorizationPolicies.AuditModulesRead)]
    [ProducesResponseType(typeof(GetAuditModulesHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetModules(
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<AuditModuleResult>> result =
            await _mediator.Send(
                new GetAuditModulesQuery(),
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetAuditModulesHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetAuditModulesHttpResponse>.Success(
                AuditMetadataHttpMapper.ToModules(result.Value)));
    }

    [HttpGet("modules/{sourceModule}/actions")]
    [Authorize(Policy = AuthorizationPolicies.AuditModulesRead)]
    [ProducesResponseType(typeof(GetAuditModuleActionsHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetModuleActions(
        [FromRoute] string sourceModule,
        CancellationToken cancellationToken)
    {
        Result<AuditModuleActionsResult> result =
            await _mediator.Send(
                new GetAuditModuleActionsQuery(sourceModule),
                cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult(
                Result<GetAuditModuleActionsHttpResponse>.Failure(result.Error!));
        }

        return this.ToActionResult(
            Result<GetAuditModuleActionsHttpResponse>.Success(
                AuditMetadataHttpMapper.ToActions(result.Value)));
    }
}
