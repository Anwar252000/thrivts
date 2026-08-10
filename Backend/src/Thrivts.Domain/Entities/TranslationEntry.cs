using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>One localized string (the live schema's translations table — French localization,
/// per README_HANDOVER.md's "finish or hide the partial French/Urdu localization" note).</summary>
public class TranslationEntry : IAggregateRoot
{
    public int Id { get; private set; }
    public string Key { get; private set; } = default!;
    public LanguagePref Language { get; private set; }
    public string Value { get; private set; } = default!;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private TranslationEntry()
    {
        // EF Core
    }

    public TranslationEntry(string key, LanguagePref language, string value)
    {
        Key = key;
        Language = language;
        Value = value;
    }

    public void UpdateValue(string value, DateTimeOffset occurredAt)
    {
        Value = value;
        UpdatedAt = occurredAt;
    }
}
