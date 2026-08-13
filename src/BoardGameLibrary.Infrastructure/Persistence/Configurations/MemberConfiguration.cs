using BoardGameLibrary.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameLibrary.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_members_member_number_not_blank",
                "char_length(btrim(member_number)) > 0");
            tableBuilder.HasCheckConstraint(
                "ck_members_member_number_normalized",
                "member_number = upper(btrim(member_number))");
            tableBuilder.HasCheckConstraint(
                "ck_members_full_name_not_blank",
                "char_length(btrim(full_name)) > 0");
            tableBuilder.HasCheckConstraint(
                "ck_members_email_not_blank",
                "char_length(btrim(email)) > 0");
            tableBuilder.HasCheckConstraint(
                "ck_members_normalized_email",
                "normalized_email = upper(btrim(email))");
            tableBuilder.HasCheckConstraint(
                "ck_members_phone_number_not_blank",
                "phone_number IS NULL OR char_length(btrim(phone_number)) > 0");
        });

        builder.HasKey(member => member.Id)
            .HasName("pk_members");

        builder.Property(member => member.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(member => member.MemberNumber)
            .HasColumnName("member_number")
            .HasMaxLength(Member.MemberNumberMaximumLength)
            .IsRequired();

        builder.Property(member => member.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(Member.FullNameMaximumLength)
            .IsRequired();

        builder.Property(member => member.Email)
            .HasColumnName("email")
            .HasMaxLength(Member.EmailMaximumLength)
            .IsRequired();

        builder.Property(member => member.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(Member.EmailMaximumLength)
            .IsRequired();

        builder.Property(member => member.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(Member.PhoneNumberMaximumLength);

        builder.Property(member => member.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(member => member.JoinedOn)
            .HasColumnName("joined_on")
            .HasColumnType("date")
            .IsRequired();

        builder.HasIndex(member => member.MemberNumber)
            .IsUnique()
            .HasDatabaseName("ux_members_member_number");

        builder.HasIndex(member => member.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ux_members_normalized_email");

        builder.HasIndex(member => member.FullName)
            .HasDatabaseName("ix_members_full_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(member => member.Email)
            .HasDatabaseName("ix_members_email_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
