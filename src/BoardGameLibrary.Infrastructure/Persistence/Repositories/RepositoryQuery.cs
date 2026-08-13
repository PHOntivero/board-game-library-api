using BoardGameLibrary.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLibrary.Infrastructure.Persistence.Repositories;

internal static class RepositoryQuery
{
    internal const string LikeEscapeCharacter = "\\";

    internal static string LiteralContainsPattern(string value)
    {
        string escaped = value.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }

    internal static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        IQueryable<T> orderedQuery,
        PageRequest pageRequest,
        int totalCount,
        CancellationToken cancellationToken)
    {
        List<T> items = await orderedQuery
            .Skip(pageRequest.Offset)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, pageRequest, totalCount);
    }

    internal static void RequireExplicitTransaction(BoardGameLibraryDbContext dbContext)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "A PostgreSQL row lock can only be acquired inside an explicit transaction.");
        }
    }
}
