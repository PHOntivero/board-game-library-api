namespace BoardGameLibrary.Infrastructure.Persistence.Models;

internal sealed class BoardGameCategory
{
    private BoardGameCategory()
    {
    }

    public Guid BoardGameId { get; private set; }

    public Guid CategoryId { get; private set; }
}
