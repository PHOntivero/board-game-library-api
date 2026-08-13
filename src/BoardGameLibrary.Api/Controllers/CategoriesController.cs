using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Api.Contracts.Categories;
using BoardGameLibrary.Application.Categories;
using BoardGameLibrary.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLibrary.Api.Controllers;

[Route("api/categories")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CategoriesController(
    ICategoryService service,
    ILogger<CategoriesController> logger) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<CategoryListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<CategoryListItemResponse>>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        Result<PageRequest> pageRequest = pagination.ToPageRequest(
            CategorySortFields.Allowed,
            CategorySortFields.Default,
            CategorySortFields.DefaultDirection);

        if (pageRequest.IsFailure)
        {
            return ToErrorResponse(pageRequest);
        }

        Result<PagedResult<CategoryListItem>> result = await service.ListAsync(
            new ListCategoriesQuery(search, isActive, pageRequest.Value),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpGet("{id}", Name = nameof(GetCategoryByIdAsync))]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<CategoryDetails> result = await service.GetAsync(
            new GetCategoryQuery(id),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value.ToResponse()) : ToErrorResponse(result);
    }

    [HttpPost]
    [ProducesResponseType<CreatedResourceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedResourceResponse>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await service.CreateAsync(
            new CreateCategoryCommand(request.Name!),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "CreateCategory", result);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "CreateCategory", result.Value);
        return CreatedAtRoute(
            nameof(GetCategoryByIdAsync),
            new { id = result.Value },
            new CreatedResourceResponse(result.Value));
    }

    [HttpPut("{id}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        ActionResult? validation = ValidateIdentifier(id, nameof(id));

        if (validation is not null)
        {
            return validation;
        }

        Result<CategoryDetails> result = await service.UpdateAsync(
            new UpdateCategoryCommand(id, request.Name!, request.IsActive!.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "UpdateCategory", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "UpdateCategory", id);
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
            new DeleteCategoryCommand(id),
            cancellationToken);

        if (result.IsFailure)
        {
            LogConflict(logger, "DeleteCategory", result, id);
            return ToErrorResponse(result);
        }

        LogSucceeded(logger, "DeleteCategory", id);
        return NoContent();
    }
}
