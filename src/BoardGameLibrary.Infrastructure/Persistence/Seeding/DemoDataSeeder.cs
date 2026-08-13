using System.Reflection;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.GameCopies;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BoardGameLibrary.Infrastructure.Persistence.Seeding;

internal static class DemoDataSeeder
{
    internal static void Seed(DbContext context, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        using IDbContextTransaction? ownedTransaction = BeginTransactionIfNeeded(context);

        try
        {
            DateTimeOffset utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);
            DemoSeedPlan plan = DemoSeedCatalog.Create(utcNow);
            Guid[] categoryIds = plan.Categories.Select(item => item.Id).ToArray();
            string[] normalizedCategoryNames = plan.Categories
                .Select(item => item.Name.ToUpperInvariant())
                .ToArray();

            Category[] categoryCandidates = context.Set<Category>()
                .Where(category =>
                    categoryIds.Contains(category.Id) ||
                    normalizedCategoryNames.Contains(category.NormalizedName))
                .ToArray();
            IReadOnlyDictionary<string, Category> categories = AddMissingCategories(
                context,
                plan.Categories,
                categoryCandidates);
            context.SaveChanges();

            Guid[] boardGameIds = plan.BoardGames.Select(item => item.Id).ToArray();
            HashSet<Guid> existingBoardGameIds = context.Set<BoardGame>()
                .Where(boardGame => boardGameIds.Contains(boardGame.Id))
                .Select(boardGame => boardGame.Id)
                .ToHashSet();
            AddMissingBoardGames(context, plan.BoardGames, categories, existingBoardGameIds, todayUtc);
            context.SaveChanges();

            Guid[] plannedCopyIds = plan.GameCopies.Select(item => item.Id).ToArray();
            string[] inventoryCodes = plan.GameCopies.Select(item => item.InventoryCode).ToArray();
            GameCopy[] copyCandidates = context.Set<GameCopy>()
                .Where(copy =>
                    plannedCopyIds.Contains(copy.Id) ||
                    inventoryCodes.Contains(copy.InventoryCode))
                .ToArray();
            IReadOnlyDictionary<Guid, Guid> copyIds = AddMissingGameCopies(
                context,
                plan.GameCopies,
                copyCandidates,
                todayUtc);
            context.SaveChanges();

            Guid[] plannedMemberIds = plan.Members.Select(item => item.Id).ToArray();
            string[] memberNumbers = plan.Members.Select(item => item.MemberNumber).ToArray();
            string[] normalizedEmails = plan.Members.Select(item => item.Email.ToUpperInvariant()).ToArray();
            Member[] memberCandidates = context.Set<Member>()
                .Where(member =>
                    plannedMemberIds.Contains(member.Id) ||
                    memberNumbers.Contains(member.MemberNumber) ||
                    normalizedEmails.Contains(member.NormalizedEmail))
                .ToArray();
            IReadOnlyDictionary<Guid, Guid> memberIds = AddMissingMembers(
                context,
                plan.Members,
                memberCandidates,
                todayUtc);
            context.SaveChanges();

            Guid[] loanIds = plan.Loans.Select(item => item.Id).ToArray();
            Loan[] loanCandidates = context.Set<Loan>()
                .Where(loan => loanIds.Contains(loan.Id) || loan.ReturnedAtUtc == null)
                .ToArray();
            AddMissingLoans(context, plan.Loans, loanCandidates, copyIds, memberIds, utcNow);
            context.SaveChanges();

            ownedTransaction?.Commit();
        }
        catch
        {
            ownedTransaction?.Rollback();
            throw;
        }
    }

    internal static async Task SeedAsync(
        DbContext context,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        await using IDbContextTransaction? ownedTransaction = await BeginTransactionIfNeededAsync(
            context,
            cancellationToken);

        try
        {
            DateTimeOffset utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);
            DemoSeedPlan plan = DemoSeedCatalog.Create(utcNow);
            Guid[] categoryIds = plan.Categories.Select(item => item.Id).ToArray();
            string[] normalizedCategoryNames = plan.Categories
                .Select(item => item.Name.ToUpperInvariant())
                .ToArray();

            Category[] categoryCandidates = await context.Set<Category>()
                .Where(category =>
                    categoryIds.Contains(category.Id) ||
                    normalizedCategoryNames.Contains(category.NormalizedName))
                .ToArrayAsync(cancellationToken);
            IReadOnlyDictionary<string, Category> categories = AddMissingCategories(
                context,
                plan.Categories,
                categoryCandidates);
            await context.SaveChangesAsync(cancellationToken);

            Guid[] boardGameIds = plan.BoardGames.Select(item => item.Id).ToArray();
            HashSet<Guid> existingBoardGameIds = (await context.Set<BoardGame>()
                    .Where(boardGame => boardGameIds.Contains(boardGame.Id))
                    .Select(boardGame => boardGame.Id)
                    .ToArrayAsync(cancellationToken))
                .ToHashSet();
            AddMissingBoardGames(context, plan.BoardGames, categories, existingBoardGameIds, todayUtc);
            await context.SaveChangesAsync(cancellationToken);

            Guid[] plannedCopyIds = plan.GameCopies.Select(item => item.Id).ToArray();
            string[] inventoryCodes = plan.GameCopies.Select(item => item.InventoryCode).ToArray();
            GameCopy[] copyCandidates = await context.Set<GameCopy>()
                .Where(copy => plannedCopyIds.Contains(copy.Id) || inventoryCodes.Contains(copy.InventoryCode))
                .ToArrayAsync(cancellationToken);
            IReadOnlyDictionary<Guid, Guid> copyIds = AddMissingGameCopies(
                context,
                plan.GameCopies,
                copyCandidates,
                todayUtc);
            await context.SaveChangesAsync(cancellationToken);

            Guid[] plannedMemberIds = plan.Members.Select(item => item.Id).ToArray();
            string[] memberNumbers = plan.Members.Select(item => item.MemberNumber).ToArray();
            string[] normalizedEmails = plan.Members.Select(item => item.Email.ToUpperInvariant()).ToArray();
            Member[] memberCandidates = await context.Set<Member>()
                .Where(member =>
                    plannedMemberIds.Contains(member.Id) ||
                    memberNumbers.Contains(member.MemberNumber) ||
                    normalizedEmails.Contains(member.NormalizedEmail))
                .ToArrayAsync(cancellationToken);
            IReadOnlyDictionary<Guid, Guid> memberIds = AddMissingMembers(
                context,
                plan.Members,
                memberCandidates,
                todayUtc);
            await context.SaveChangesAsync(cancellationToken);

            Guid[] loanIds = plan.Loans.Select(item => item.Id).ToArray();
            Loan[] loanCandidates = await context.Set<Loan>()
                .Where(loan => loanIds.Contains(loan.Id) || loan.ReturnedAtUtc == null)
                .ToArrayAsync(cancellationToken);
            AddMissingLoans(context, plan.Loans, loanCandidates, copyIds, memberIds, utcNow);
            await context.SaveChangesAsync(cancellationToken);

            if (ownedTransaction is not null)
            {
                await ownedTransaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (ownedTransaction is not null)
            {
                await ownedTransaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
    }

    private static IReadOnlyDictionary<string, Category> AddMissingCategories(
        DbContext context,
        IReadOnlyList<CategorySeed> seeds,
        IReadOnlyCollection<Category> candidates)
    {
        var byId = candidates.ToDictionary(category => category.Id);
        var byNormalizedName = candidates.ToDictionary(
            category => category.NormalizedName,
            StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

        foreach (CategorySeed seed in seeds)
        {
            string normalizedName = seed.Name.ToUpperInvariant();

            if (!byId.TryGetValue(seed.Id, out Category? category) &&
                !byNormalizedName.TryGetValue(normalizedName, out category))
            {
                category = SetFixedId(Category.Create(seed.Name), seed.Id);
                context.Set<Category>().Add(category);
                byId.Add(category.Id, category);
                byNormalizedName.Add(category.NormalizedName, category);
            }

            result.Add(seed.Name, category);
        }

        return result;
    }

    private static void AddMissingBoardGames(
        DbContext context,
        IReadOnlyList<BoardGameSeed> seeds,
        IReadOnlyDictionary<string, Category> categories,
        IReadOnlySet<Guid> existingIds,
        DateOnly todayUtc)
    {
        foreach (BoardGameSeed seed in seeds)
        {
            if (existingIds.Contains(seed.Id))
            {
                continue;
            }

            Category[] assignedCategories = seed.CategoryNames
                .Select(categoryName => categories[categoryName])
                .ToArray();
            BoardGame boardGame = BoardGame.Create(
                seed.Title,
                seed.Publisher,
                seed.Description,
                seed.PublicationYear,
                seed.MinPlayers,
                seed.MaxPlayers,
                seed.PlayingTimeMinutes,
                assignedCategories,
                todayUtc);

            context.Set<BoardGame>().Add(SetFixedId(boardGame, seed.Id));
        }
    }

    private static IReadOnlyDictionary<Guid, Guid> AddMissingGameCopies(
        DbContext context,
        IReadOnlyList<GameCopySeed> seeds,
        IReadOnlyCollection<GameCopy> candidates,
        DateOnly todayUtc)
    {
        var byId = candidates.ToDictionary(copy => copy.Id);
        var byInventoryCode = candidates.ToDictionary(
            copy => copy.InventoryCode,
            StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<Guid, Guid>();

        foreach (GameCopySeed seed in seeds)
        {
            if (!byId.TryGetValue(seed.Id, out GameCopy? copy) &&
                !byInventoryCode.TryGetValue(seed.InventoryCode, out copy))
            {
                copy = SetFixedId(
                    GameCopy.Create(
                        seed.BoardGameId,
                        seed.InventoryCode,
                        seed.Condition,
                        seed.AcquiredOn,
                        todayUtc),
                    seed.Id);
                context.Set<GameCopy>().Add(copy);
                byId.Add(copy.Id, copy);
                byInventoryCode.Add(copy.InventoryCode, copy);
            }

            result.Add(seed.Id, copy.Id);
        }

        return result;
    }

    private static IReadOnlyDictionary<Guid, Guid> AddMissingMembers(
        DbContext context,
        IReadOnlyList<MemberSeed> seeds,
        IReadOnlyCollection<Member> candidates,
        DateOnly todayUtc)
    {
        var byId = candidates.ToDictionary(member => member.Id);
        var byMemberNumber = candidates.ToDictionary(
            member => member.MemberNumber,
            StringComparer.OrdinalIgnoreCase);
        var byNormalizedEmail = candidates.ToDictionary(
            member => member.NormalizedEmail,
            StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<Guid, Guid>();

        foreach (MemberSeed seed in seeds)
        {
            string normalizedEmail = seed.Email.ToUpperInvariant();

            if (!byId.TryGetValue(seed.Id, out Member? member) &&
                !byMemberNumber.TryGetValue(seed.MemberNumber, out member) &&
                !byNormalizedEmail.TryGetValue(normalizedEmail, out member))
            {
                member = SetFixedId(
                    Member.Create(
                        seed.MemberNumber,
                        seed.FullName,
                        seed.Email,
                        seed.PhoneNumber,
                        seed.JoinedOn,
                        todayUtc),
                    seed.Id);
                context.Set<Member>().Add(member);
                byId.Add(member.Id, member);
                byMemberNumber.Add(member.MemberNumber, member);
                byNormalizedEmail.Add(member.NormalizedEmail, member);
            }

            result.Add(seed.Id, member.Id);
        }

        return result;
    }

    private static void AddMissingLoans(
        DbContext context,
        IReadOnlyList<LoanSeed> seeds,
        IReadOnlyCollection<Loan> candidates,
        IReadOnlyDictionary<Guid, Guid> copyIds,
        IReadOnlyDictionary<Guid, Guid> memberIds,
        DateTimeOffset utcNow)
    {
        HashSet<Guid> existingIds = candidates.Select(loan => loan.Id).ToHashSet();
        HashSet<Guid> copiesWithOpenLoans = candidates
            .Where(loan => loan.ReturnedAtUtc is null)
            .Select(loan => loan.GameCopyId)
            .ToHashSet();
        Dictionary<Guid, int> openLoansByMember = candidates
            .Where(loan => loan.ReturnedAtUtc is null)
            .GroupBy(loan => loan.MemberId)
            .ToDictionary(group => group.Key, group => group.Count());
        HashSet<Guid> membersWithOverdueLoans = candidates
            .Where(loan => loan.ReturnedAtUtc is null && utcNow > loan.DueAtUtc)
            .Select(loan => loan.MemberId)
            .ToHashSet();

        foreach (LoanSeed seed in seeds)
        {
            if (existingIds.Contains(seed.Id))
            {
                continue;
            }

            Guid copyId = copyIds[seed.GameCopyId];
            Guid memberId = memberIds[seed.MemberId];
            bool isOpen = seed.ReturnedAtUtc is null;
            int memberOpenLoanCount = openLoansByMember.GetValueOrDefault(memberId);

            if (isOpen &&
                (copiesWithOpenLoans.Contains(copyId) ||
                 memberOpenLoanCount >= 3 ||
                 membersWithOverdueLoans.Contains(memberId)))
            {
                continue;
            }

            Loan loan = SetFixedId(Loan.Create(copyId, memberId, seed.LoanedAtUtc), seed.Id);

            if (seed.ReturnedAtUtc.HasValue)
            {
                loan.Return(seed.ReturnedAtUtc.Value);
            }

            context.Set<Loan>().Add(loan);
            existingIds.Add(loan.Id);

            if (isOpen)
            {
                copiesWithOpenLoans.Add(copyId);
                openLoansByMember[memberId] = memberOpenLoanCount + 1;

                if (utcNow > loan.DueAtUtc)
                {
                    membersWithOverdueLoans.Add(memberId);
                }
            }
        }
    }

    private static IDbContextTransaction? BeginTransactionIfNeeded(DbContext context) =>
        context.Database.CurrentTransaction is null
            ? context.Database.BeginTransaction()
            : null;

    private static async Task<IDbContextTransaction?> BeginTransactionIfNeededAsync(
        DbContext context,
        CancellationToken cancellationToken) =>
        context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;

    private static TEntity SetFixedId<TEntity>(TEntity entity, Guid id)
        where TEntity : class
    {
        PropertyInfo idProperty = typeof(TEntity).GetProperty(
                "Id",
                BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} does not expose an Id property.");

        idProperty.SetValue(entity, id);
        return entity;
    }
}
