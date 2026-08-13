using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Api.Contracts.Members;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Members;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLibrary.Api.Controllers;

[Route("api/members")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class MembersController(
    IMemberService service,
    ILogger<MembersController> logger) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<MemberListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<MemberListItemResponse>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        Result<PageRequest> pageRequest = pagination.ToPageRequest(
            MemberSortFields.Allowed,
            MemberSortFields.Default,
            MemberSortFields.DefaultDirection);

        if (pageRequest.IsFailure)
        {
            return ToErrorResponse(pageRequest);
        }

        Result<PagedResult<MemberListItem>> result = await service.ListAsync(
            new ListMembersQuery(search, isActive, pageRequest.Value),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpGet("{id}", Name = nameof(GetMemberByIdAsync))]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponse>> GetMemberByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<MemberDetails> result = await service.GetAsync(
            new GetMemberQuery(id),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<CreatedResourceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedResourceResponse>> CreateAsync(
        CreateMemberRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await service.CreateAsync(
            new CreateMemberCommand(
                request.MemberNumber!,
                request.FullName!,
                request.Email!,
                request.PhoneNumber,
                request.JoinedOn!.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "CreateMember", result);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "CreateMember", result.Value);
        return CreatedAtRoute(
            nameof(GetMemberByIdAsync),
            new { id = result.Value },
            new CreatedResourceResponse(result.Value));
    }

    [HttpPut("{id}")]
    [ProducesResponseType<MemberResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberResponse>> UpdateAsync(
        Guid id,
        UpdateMemberRequest request,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<MemberDetails> result = await service.UpdateAsync(
            new UpdateMemberCommand(
                id,
                request.MemberNumber!,
                request.FullName!,
                request.Email!,
                request.PhoneNumber,
                request.JoinedOn!.Value,
                request.IsActive!.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "UpdateMember", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "UpdateMember", id);
        return Ok(result.Value.ToResponse());
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result result = await service.DeleteAsync(
            new DeleteMemberCommand(id),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "DeleteMember", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "DeleteMember", id);
        return NoContent();
    }
}
