using BoardGameLibrary.Domain.GameCopies;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameLibrary.Infrastructure.Persistence.Configurations;

internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("loans", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_loans_fixed_lending_term",
                $"EXTRACT(EPOCH FROM (due_at_utc - loaned_at_utc)) = {Loan.LendingTermDays * 24 * 60 * 60}");
            tableBuilder.HasCheckConstraint(
                "ck_loans_returned_after_loaned",
                "returned_at_utc IS NULL OR returned_at_utc >= loaned_at_utc");
        });

        builder.HasKey(loan => loan.Id)
            .HasName("pk_loans");

        builder.Property(loan => loan.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(loan => loan.GameCopyId)
            .HasColumnName("game_copy_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(loan => loan.MemberId)
            .HasColumnName("member_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(loan => loan.LoanedAtUtc)
            .HasColumnName("loaned_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(loan => loan.DueAtUtc)
            .HasColumnName("due_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(loan => loan.ReturnedAtUtc)
            .HasColumnName("returned_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<GameCopy>()
            .WithMany()
            .HasForeignKey(loan => loan.GameCopyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_loans_game_copies_game_copy_id");

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(loan => loan.MemberId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_loans_members_member_id");

        builder.HasIndex(loan => new { loan.GameCopyId, loan.LoanedAtUtc })
            .HasDatabaseName("ix_loans_game_copy_id_loaned_at_utc");

        builder.HasIndex(loan => loan.LoanedAtUtc)
            .HasDatabaseName("ix_loans_loaned_at_utc");

        builder.HasIndex(loan => new { loan.MemberId, loan.ReturnedAtUtc, loan.DueAtUtc })
            .HasDatabaseName("ix_loans_member_id_returned_at_utc_due_at_utc");

        builder.HasIndex(loan => loan.GameCopyId)
            .IsUnique()
            .HasFilter("returned_at_utc IS NULL")
            .HasDatabaseName("ux_loans_game_copy_id_open");
    }
}
