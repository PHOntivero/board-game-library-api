using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.Infrastructure.Persistence.Seeding;

internal static class DemoSeedCatalog
{
    internal const int CategoryCount = 17;
    internal const int BoardGameCount = 120;
    internal const int GameCopyCount = 180;
    internal const int MemberCount = 30;
    internal const int LoanCount = 50;

    private static readonly string[] CategoryNames =
    [
        "Strategy", "Family", "Party", "Cooperative", "Abstract", "Thematic",
        "Horror", "Adventure", "Fantasy", "Science Fiction", "Mystery", "Economic",
        "Wargame", "Trivia", "Dexterity", "Children's", "Other",
    ];

    private static readonly CanonicalGame[] CanonicalGames =
    [
        new("Catan", "Catan Studio", 1995, 3, 4, 90, ["Strategy", "Family"], "CATAN"),
        new("Pandemic", "Z-Man Games", 2008, 2, 4, 45, ["Cooperative", "Thematic"], "PANDEMIC"),
        new("Ticket to Ride", "Days of Wonder", 2004, 2, 5, 60, ["Family", "Strategy"], "TICKET"),
        new("Carcassonne", "Hans im Glueck", 2000, 2, 5, 45, ["Family", "Strategy"], "CARCASSONNE"),
        new("Azul", "Next Move Games", 2017, 2, 4, 45, ["Abstract", "Family"], "AZUL"),
        new("Wingspan", "Stonemaier Games", 2019, 1, 5, 70, ["Strategy", "Thematic"], "WINGSPAN"),
        new("Brass: Birmingham", "Roxley", 2018, 2, 4, 120, ["Economic", "Strategy"], "BRASS"),
        new("7 Wonders", "Repos Production", 2010, 3, 7, 30, ["Strategy", "Family"], "7WONDERS"),
        new("Codenames", "Czech Games Edition", 2015, 2, 8, 20, ["Party", "Mystery"], "CODENAMES"),
        new("Gloomhaven", "Cephalofair Games", 2017, 1, 4, 120, ["Adventure", "Fantasy"], "GLOOMHAVEN"),
    ];

    private static readonly string[] GeneratedPublishers =
    [
        "North Star Publishing", "Meeple House", "Open Table Games", "Blue Oak Studio",
        "Lantern Works", "Cardboard Harbor", "Red Fox Games", "Clockwork Tabletop",
    ];

    private static readonly string[] GeneratedAdjectives =
    ["Amber", "Ancient", "Brilliant", "Crimson", "Hidden", "Iron", "Lost", "Midnight", "Silver", "Verdant"];

    private static readonly string[] GeneratedNouns =
    ["Archipelago", "Citadel", "Expedition", "Frontier", "Guild", "Kingdom", "Labyrinth", "Observatory", "Outpost", "Voyage", "Workshop"];

    private static readonly string[] MemberNames =
    [
        "Alex Morgan", "Jordan Lee", "Taylor Brooks", "Casey Rivera", "Morgan Bailey",
        "Avery Carter", "Riley Cooper", "Jamie Foster", "Cameron Gray", "Drew Hughes",
        "Emerson James", "Finley Kelly", "Harper Lane", "Kai Mitchell", "Logan Parker",
        "Micah Reed", "Noel Sanders", "Peyton Turner", "Quinn Walker", "Reese Young",
        "Robin Bennett", "Sage Collins", "Shawn Diaz", "Skyler Evans", "Terry Flores",
        "Val Garcia", "Winter Hayes", "Charlie Irving", "Dakota Jensen", "Frankie Kim",
    ];

    internal static DemoSeedPlan Create(DateTimeOffset utcNow)
    {
        IReadOnlyList<CategorySeed> categories = CreateCategories();
        IReadOnlyList<BoardGameSeed> boardGames = CreateBoardGames();
        IReadOnlyList<GameCopySeed> gameCopies = CreateGameCopies(boardGames);
        IReadOnlyList<MemberSeed> members = CreateMembers();
        IReadOnlyList<LoanSeed> loans = CreateLoans(gameCopies, members, utcNow.ToUniversalTime());

        return new DemoSeedPlan(categories, boardGames, gameCopies, members, loans);
    }

    internal static Guid CreateDeterministicId(int idNamespace, int sequence)
    {
        if (idNamespace is < 1 or > 0xffff)
        {
            throw new ArgumentOutOfRangeException(nameof(idNamespace));
        }

        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        return Guid.ParseExact($"0198a000-{idNamespace:x4}-7000-8000-{sequence:x12}", "D");
    }

    private static IReadOnlyList<CategorySeed> CreateCategories() =>
        CategoryNames.Select((name, index) => new CategorySeed(CreateDeterministicId(1, index + 1), name)).ToArray();

    private static IReadOnlyList<BoardGameSeed> CreateBoardGames()
    {
        var boardGames = new List<BoardGameSeed>(BoardGameCount);

        for (int index = 0; index < BoardGameCount; index++)
        {
            int number = index + 1;

            if (index < CanonicalGames.Length)
            {
                CanonicalGame canonical = CanonicalGames[index];
                boardGames.Add(new BoardGameSeed(
                    CreateDeterministicId(2, number), canonical.Title, canonical.Publisher,
                    $"A reviewer-friendly seeded edition of {canonical.Title}.",
                    canonical.PublicationYear, canonical.MinPlayers, canonical.MaxPlayers,
                    canonical.PlayingTimeMinutes, canonical.CategoryNames, canonical.InventoryCodePrefix));
                continue;
            }

            int generatedIndex = index - CanonicalGames.Length;
            string title = $"Library Game {number:000}: " +
                $"{GeneratedAdjectives[generatedIndex % GeneratedAdjectives.Length]} " +
                GeneratedNouns[generatedIndex % GeneratedNouns.Length];
            string primaryCategory = CategoryNames[index % CategoryNames.Length];
            string[] categories = index % 3 == 0
                ? [primaryCategory, CategoryNames[(index + 5) % CategoryNames.Length]]
                : [primaryCategory];
            int minPlayers = 1 + (index % 4);

            boardGames.Add(new BoardGameSeed(
                CreateDeterministicId(2, number), title,
                GeneratedPublishers[index % GeneratedPublishers.Length],
                $"Deterministic demonstration game number {number:000} for API search and filtering.",
                1990 + (index % 35), minPlayers, minPlayers + 1 + (index % 4),
                30 + (15 * (index % 8)), categories, $"BGL{number:000}"));
        }

        return boardGames;
    }

    private static IReadOnlyList<GameCopySeed> CreateGameCopies(IReadOnlyList<BoardGameSeed> boardGames)
    {
        var copies = new List<GameCopySeed>(GameCopyCount);

        for (int gameIndex = 0; gameIndex < boardGames.Count; gameIndex++)
        {
            BoardGameSeed boardGame = boardGames[gameIndex];
            int copiesForGame = gameIndex < 60 ? 2 : 1;

            for (int copyNumber = 1; copyNumber <= copiesForGame; copyNumber++)
            {
                int copyIndex = copies.Count;
                GameCopyCondition condition = (copyIndex + 1) % 13 == 0
                    ? GameCopyCondition.Damaged
                    : (GameCopyCondition)((copyIndex % 3) + 1);

                if (copyIndex is >= 50 and < 70)
                {
                    condition = GameCopyCondition.Good;
                }

                copies.Add(new GameCopySeed(
                    CreateDeterministicId(3, copyIndex + 1), boardGame.Id,
                    $"{boardGame.InventoryCodePrefix}-{copyNumber:000}", condition,
                    new DateOnly(2020 + (copyIndex % 6), 1 + (copyIndex % 12), 1 + (copyIndex % 27))));
            }
        }

        return copies;
    }

    private static IReadOnlyList<MemberSeed> CreateMembers()
    {
        var members = new List<MemberSeed>(MemberCount);

        for (int index = 0; index < MemberCount; index++)
        {
            int number = index + 1;
            members.Add(new MemberSeed(
                CreateDeterministicId(4, number), $"MEM-{number:000}", MemberNames[index],
                $"member{number:000}@example.test", index % 3 == 0 ? null : $"+1 555 01{number:00}",
                new DateOnly(2021 + (index % 5), 1 + (index % 12), 1 + (index % 27))));
        }

        return members;
    }

    private static IReadOnlyList<LoanSeed> CreateLoans(
        IReadOnlyList<GameCopySeed> copies,
        IReadOnlyList<MemberSeed> members,
        DateTimeOffset utcNow)
    {
        var loans = new List<LoanSeed>(LoanCount);

        for (int index = 0; index < LoanCount; index++)
        {
            DateTimeOffset loanedAtUtc;
            DateTimeOffset? returnedAtUtc;
            int memberIndex;

            if (index < 30)
            {
                loanedAtUtc = utcNow.AddDays(-(70 + index));
                returnedAtUtc = loanedAtUtc.AddDays(index % 4 == 0 ? 18 : 7);
                memberIndex = index % members.Count;
            }
            else if (index < 40)
            {
                loanedAtUtc = utcNow.AddDays(-(25 + (index % 5)));
                returnedAtUtc = null;
                memberIndex = index - 30;
            }
            else
            {
                loanedAtUtc = utcNow.AddDays(-(1 + (index % 7)));
                returnedAtUtc = null;
                memberIndex = 10 + (index - 40);
            }

            loans.Add(new LoanSeed(
                CreateDeterministicId(5, index + 1), copies[index + 20].Id, members[memberIndex].Id,
                loanedAtUtc, returnedAtUtc));
        }

        return loans;
    }

    private sealed record CanonicalGame(
        string Title,
        string Publisher,
        int PublicationYear,
        int MinPlayers,
        int MaxPlayers,
        int PlayingTimeMinutes,
        IReadOnlyList<string> CategoryNames,
        string InventoryCodePrefix);
}

internal static class DemoSeedWalkthrough
{
    internal static readonly Guid StrategyCategoryId = DemoSeedCatalog.CreateDeterministicId(1, 1);
    internal static readonly Guid CatanBoardGameId = DemoSeedCatalog.CreateDeterministicId(2, 1);
    internal static readonly Guid CatanFirstCopyId = DemoSeedCatalog.CreateDeterministicId(3, 1);
    internal static readonly Guid FirstMemberId = DemoSeedCatalog.CreateDeterministicId(4, 1);
    internal static readonly Guid FirstActiveLoanId = DemoSeedCatalog.CreateDeterministicId(5, 41);

    internal const string StrategyCategoryName = "Strategy";
    internal const string CatanTitle = "Catan";
    internal const string CatanFirstInventoryCode = "CATAN-001";
    internal const string FirstMemberNumber = "MEM-001";
}

internal sealed record DemoSeedPlan(
    IReadOnlyList<CategorySeed> Categories,
    IReadOnlyList<BoardGameSeed> BoardGames,
    IReadOnlyList<GameCopySeed> GameCopies,
    IReadOnlyList<MemberSeed> Members,
    IReadOnlyList<LoanSeed> Loans);

internal sealed record CategorySeed(Guid Id, string Name);

internal sealed record BoardGameSeed(
    Guid Id, string Title, string Publisher, string? Description, int PublicationYear,
    int MinPlayers, int MaxPlayers, int PlayingTimeMinutes,
    IReadOnlyList<string> CategoryNames, string InventoryCodePrefix);

internal sealed record GameCopySeed(
    Guid Id, Guid BoardGameId, string InventoryCode,
    GameCopyCondition Condition, DateOnly? AcquiredOn);

internal sealed record MemberSeed(
    Guid Id, string MemberNumber, string FullName, string Email,
    string? PhoneNumber, DateOnly JoinedOn);

internal sealed record LoanSeed(
    Guid Id, Guid GameCopyId, Guid MemberId,
    DateTimeOffset LoanedAtUtc, DateTimeOffset? ReturnedAtUtc);
