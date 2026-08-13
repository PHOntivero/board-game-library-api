using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Api.Contracts.Loans;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Loans;
using BoardGameLibrary.Domain.Loans;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLibrary.Api.Controllers;

[Route("api/loans")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class LoansController(
    ILoanService service,
    ILogger<LoansController> logger) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<LoanListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<LoanListItemResponse>>> ListAsync(
        [FromQuery] Guid? memberId,
        [FromQuery] Guid? gameCopyId,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? loanedFrom,
        [FromQuery] DateTimeOffset? loanedTo,
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        Result<OptionalEnumValue<LoanStatus>> parsedStatus =
            QueryValueParser.ParseOptionalEnum<LoanStatus>(status, nameof(status));

        if (parsedStatus.IsFailure)
        {
            return ToErrorResponse(parsedStatus);
        }

        Result<PageRequest> pageRequest = pagination.ToPageRequest(
            LoanSortFields.Allowed,
            LoanSortFields.Default,
            LoanSortFields.DefaultDirection);

        if (pageRequest.IsFailure)
        {
            return ToErrorResponse(pageRequest);
        }

        Result<PagedResult<LoanListItem>> result = await service.ListAsync(
            new ListLoansQuery(
                memberId,
                gameCopyId,
                parsedStatus.Value.Value,
                loanedFrom,
                loanedTo,
                pageRequest.Value),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpGet("{id}", Name = nameof(GetLoanByIdAsync))]
    [ProducesResponseType<LoanResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanResponse>> GetLoanByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<LoanDetails> result = await service.GetAsync(
            new GetLoanQuery(id),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<CreatedResourceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedResourceResponse>> CreateAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await service.CreateAsync(
            new CreateLoanCommand(request.MemberId!.Value, request.GameCopyId!.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "CreateLoan", result);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "CreateLoan", result.Value);
        return CreatedAtRoute(
            nameof(GetLoanByIdAsync),
            new { id = result.Value },
            new CreatedResourceResponse(result.Value));
    }

    [HttpPost("{id}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReturnAsync(Guid id, CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result result = await service.ReturnAsync(
            new ReturnLoanCommand(id),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "ReturnLoan", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "ReturnLoan", id);
        return NoContent();
    }
}
