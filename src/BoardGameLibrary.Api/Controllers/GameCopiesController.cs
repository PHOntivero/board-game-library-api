using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Api.Contracts.GameCopies;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.GameCopies;
using BoardGameLibrary.Domain.GameCopies;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLibrary.Api.Controllers;

[Route("api")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class GameCopiesController(
    IGameCopyService service,
    ILogger<GameCopiesController> logger) : ApiControllerBase
{
    [HttpGet("board-games/{boardGameId}/copies")]
    [ProducesResponseType<PagedResponse<GameCopyListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<GameCopyListItemResponse>>> ListAsync(
        Guid boardGameId,
        [FromQuery] string? condition,
        [FromQuery] bool? isAvailable,
        [FromQuery] bool? isActive,
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(boardGameId, nameof(boardGameId));

        if (validation is not null)
        {
            return validation;
        }

        Result<OptionalEnumValue<GameCopyCondition>> parsedCondition =
            QueryValueParser.ParseOptionalEnum<GameCopyCondition>(condition, nameof(condition));

        if (parsedCondition.IsFailure)
        {
            return ToErrorResponse(parsedCondition);
        }

        Result<PageRequest> pageRequest = pagination.ToPageRequest(
            GameCopySortFields.Allowed,
            GameCopySortFields.Default,
            GameCopySortFields.DefaultDirection);

        if (pageRequest.IsFailure)
        {
            return ToErrorResponse(pageRequest);
        }

        Result<PagedResult<GameCopyListItem>> result = await service.ListAsync(
            new ListGameCopiesQuery(
                boardGameId,
                parsedCondition.Value.Value,
                isAvailable,
                isActive,
                pageRequest.Value),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpGet("game-copies/{id}", Name = nameof(GetGameCopyByIdAsync))]
    [ProducesResponseType<GameCopyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameCopyResponse>> GetGameCopyByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<GameCopyDetails> result = await service.GetAsync(
            new GetGameCopyQuery(id),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpPost("board-games/{boardGameId}/copies")]
    [ProducesResponseType<CreatedResourceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedResourceResponse>> CreateAsync(
        Guid boardGameId,
        CreateGameCopyRequest request,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(boardGameId, nameof(boardGameId));

        if (validation is not null)
        {
            return validation;
        }

        Result<Guid> result = await service.CreateAsync(
            new CreateGameCopyCommand(
                boardGameId,
                request.InventoryCode!,
                request.Condition!.Value,
                request.AcquiredOn),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "CreateGameCopy", result);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "CreateGameCopy", result.Value);
        return CreatedAtRoute(
            nameof(GetGameCopyByIdAsync),
            new { id = result.Value },
            new CreatedResourceResponse(result.Value));
    }

    [HttpPut("game-copies/{id}")]
    [ProducesResponseType<GameCopyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameCopyResponse>> UpdateAsync(
        Guid id,
        UpdateGameCopyRequest request,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<GameCopyDetails> result = await service.UpdateAsync(
            new UpdateGameCopyCommand(
                id,
                request.InventoryCode!,
                request.Condition!.Value,
                request.AcquiredOn,
                request.IsActive!.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "UpdateGameCopy", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "UpdateGameCopy", id);
        return Ok(result.Value.ToResponse());
    }

    [HttpDelete("game-copies/{id}")]
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
            new DeleteGameCopyCommand(id),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "DeleteGameCopy", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "DeleteGameCopy", id);
        return NoContent();
    }
}
