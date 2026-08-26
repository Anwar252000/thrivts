using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.NameTranslation;
using Thrivts.Domain.Enums;

namespace Thrivts.Infrastructure.Persistence;

/// <summary>
/// Registers every C# enum that maps to a genuine native Postgres enum type with Npgsql's own
/// enum support, rather than converting it to a plain string via HasConversion.
///
/// Why this exists: a string-based HasConversion (see SnakeCaseEnumConverter) reads these columns
/// fine, but any WHERE-clause equality filter against a non-literal value fails at runtime with
/// "42883: operator does not exist: sometype = text" — Npgsql has no way to know the outgoing
/// parameter should be bound as the native enum type instead of text, and HasColumnType alone
/// does not fix it (verified directly against the live database).
///
/// Getting this right needs BOTH halves, and both need to use the SAME translator instances:
///   1. NpgsqlDataSourceBuilder.MapEnum (ConfigureDataSource) — registers the label translation at
///      the wire-protocol level, so Npgsql knows the OID for "commission_status" etc.
///   2. NpgsqlDbContextOptionsBuilder.MapEnum (ConfigureContextOptions) — tells EF Core's own
///      query translator the CLR enum is natively mapped, so WHERE-clause parameters are typed
///      correctly instead of defaulting to int.
/// Passing a FRESH INpgsqlNameTranslator instance to either call on every DbContext construction
/// makes EF Core treat each context's configuration as distinct (reference inequality), which
/// forces a new internal service provider per DbContext and eventually throws
/// ManyServiceProvidersCreatedWarning — hence the translators below are static/shared, and the
/// NpgsqlDataSource itself must be built once and registered as a singleton (see
/// DependencyInjection.AddInfrastructure), never rebuilt from the connection string per request.
///
/// GradeType uses its own hand-written translator (GradeTranslator below), not SnakeCase — the
/// live grade_type Postgres type's canonical label for AB is "A/B" (a slash, not expressible by
/// automatic casing) and it also carries two legacy-casing duplicate labels (A_B, MIXED) that this
/// enum deliberately does not model. A live check (2026) confirmed zero existing rows use the
/// legacy labels — only "A" and "A/B" are in use — so native mapping is safe; MapEnum would throw
/// on deserializing a row it can't recognize, but nothing in this codebase ever writes the legacy
/// spellings, and .NET is the only writer of this table going forward.
/// </summary>
public static class NpgsqlEnumMapping
{
    private static readonly NpgsqlSnakeCaseNameTranslator SnakeCase = new();
    private static readonly NpgsqlNullNameTranslator Identity = new(); // CurrencyType members (USD/GBP/EUR/PKR) already match the DB labels verbatim.
    private static readonly GradeTypeNameTranslator GradeTranslator = new();

    public static void ConfigureDataSource(NpgsqlDataSourceBuilder builder)
    {
        builder.MapEnum<UserRole>("user_role", nameTranslator: SnakeCase);
        builder.MapEnum<ApprovalStatus>("approval_status", nameTranslator: SnakeCase);
        builder.MapEnum<CommissionStatus>("commission_status", nameTranslator: SnakeCase);
        builder.MapEnum<DealStatus>("deal_status", nameTranslator: SnakeCase);
        builder.MapEnum<RequirementStatus>("requirement_status", nameTranslator: SnakeCase);
        builder.MapEnum<BidStatus>("seller_response_status", nameTranslator: SnakeCase);
        builder.MapEnum<SellerTier>("seller_tier", nameTranslator: SnakeCase);
        builder.MapEnum<MessageSenderType>("message_sender_type", nameTranslator: SnakeCase);
        builder.MapEnum<NotificationChannel>("notification_channel", nameTranslator: SnakeCase);
        builder.MapEnum<LanguagePref>("language_pref", nameTranslator: SnakeCase);
        builder.MapEnum<RequirementType>("requirement_type", nameTranslator: SnakeCase);
        builder.MapEnum<CurrencyType>("currency_type", nameTranslator: Identity);
        builder.MapEnum<GradeType>("grade_type", nameTranslator: GradeTranslator);
    }

    public static void ConfigureContextOptions(NpgsqlDbContextOptionsBuilder npgsqlOptions)
    {
        npgsqlOptions.MapEnum<UserRole>("user_role", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<ApprovalStatus>("approval_status", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<CommissionStatus>("commission_status", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<DealStatus>("deal_status", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<RequirementStatus>("requirement_status", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<BidStatus>("seller_response_status", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<SellerTier>("seller_tier", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<MessageSenderType>("message_sender_type", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<NotificationChannel>("notification_channel", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<LanguagePref>("language_pref", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<RequirementType>("requirement_type", nameTranslator: SnakeCase);
        npgsqlOptions.MapEnum<CurrencyType>("currency_type", nameTranslator: Identity);
        npgsqlOptions.MapEnum<GradeType>("grade_type", nameTranslator: GradeTranslator);
    }

    /// <summary>Explicit member->label map (not a casing rule) — GradeType.AB is the only member
    /// whose canonical DB label ("A/B") can't be derived by any automatic name translator.</summary>
    private sealed class GradeTypeNameTranslator : Npgsql.INpgsqlNameTranslator
    {
        public string TranslateMemberName(string clrName) => clrName switch
        {
            nameof(GradeType.AB) => "A/B",
            var name => name,
        };

        public string TranslateTypeName(string clrName) => clrName;
    }
}
