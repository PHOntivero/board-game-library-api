using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Api.Contracts.BoardGames;
using BoardGameLibrary.Application.BoardGames;
using BoardGameLibrary.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLibrary.Api.Controllers;

[Route("api/board-games")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class BoardGamesController(
    IBoardGameService service,
    ILogger<BoardGamesController> logger) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<BoardGameListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<BoardGameListItemResponse>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] int? players,
        [FromQuery] bool? isAvailable,
        [FromQuery] bool? isActive,
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        Result<PageRequest> pageRequest = pagination.ToPageRequest(
            BoardGameSortFields.Allowed,
            BoardGameSortFields.Default,
            BoardGameSortFields.DefaultDirection);

        if (pageRequest.IsFailure)
        {
            return ToErrorResponse(pageRequest);
        }

        Result<PagedResult<BoardGameListItem>> result = await service.ListAsync(
            new ListBoardGamesQuery(
                search,
                categoryId,
                players,
                isAvailable,
                isActive,
                pageRequest.Value),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpGet("{id}", Name = nameof(GetBoardGameByIdAsync))]
    [ProducesResponseType<BoardGameResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardGameResponse>> GetBoardGameByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<BoardGameDetails> result = await service.GetAsync(
            new GetBoardGameQuery(id),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<CreatedResourceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedResourceResponse>> CreateAsync(
        CreateBoardGameRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await service.CreateAsync(
            new CreateBoardGameCommand(
                request.Title!,
                request.Publisher!,
                request.Description,
                request.PublicationYear!.Value,
                request.MinPlayers!.Value,
                request.MaxPlayers!.Value,
                request.PlayingTimeMinutes!.Value,
                request.CategoryIds!),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "CreateBoardGame", result);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "CreateBoardGame", result.Value);
        return CreatedAtRoute(
            nameof(GetBoardGameByIdAsync),
            new { id = result.Value },
            new CreatedResourceResponse(result.Value));
    }

    [HttpPut("{id}")]
    [ProducesResponseType<BoardGameResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BoardGameResponse>> UpdateAsync(
        Guid id,
        UpdateBoardGameRequest request,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<BoardGameDetails> result = await service.UpdateAsync(
            new UpdateBoardGameCommand(
                id,
                request.Title!,
                request.Publisher!,
                request.Description,
                request.PublicationYear!.Value,
                request.MinPlayers!.Value,
                request.MaxPlayers!.Value,
                request.PlayingTimeMinutes!.Value,
                request.CategoryIds!,
                request.IsActive!.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "UpdateBoardGame", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "UpdateBoardGame", id);
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
            new DeleteBoardGameCommand(id),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "DeleteBoardGame", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "DeleteBoardGame", id);
        return NoContent();
    }
}
