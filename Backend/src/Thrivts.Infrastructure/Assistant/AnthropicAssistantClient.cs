using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Thrivts.Application.Features.Owner;

namespace Thrivts.Infrastructure.Assistant;

public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    /// <summary>Anthropic API key — user-secrets / environment only, never committed.</summary>
    public string? ApiKey { get; init; }

    public string Model { get; init; } = "claude-opus-5-5";

    public int MaxTokens { get; init; } = 16000;
}

/// <summary>
/// Calls Claude for the VH Assistant. The system prompt is split in two: fixed instructions first
/// (marked cacheable) and the per-request live data after them, so the stable part can be reused
/// from the prompt cache while the numbers stay fresh. A refusal is retried on a fallback model by
/// the API itself. Failures are mapped to user-facing errors and logged with the request id —
/// never surfaced raw.
/// </summary>
public sealed class AnthropicAssistantClient(AnthropicClient client, IOptions<AssistantOptions> options, ILogger<AnthropicAssistantClient> logger) : IAssistantClient
{
    private readonly AssistantOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<ErrorOr<AssistantReply>> AskAsync(string instructions, string liveContext, IReadOnlyList<AssistantTurn> history, CancellationToken cancellationToken)
    {
        var request = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Betas = ["server-side-fallback-2026-06-01"],
            Fallbacks = new List<BetaFallbackParam> { new() { Model = "claude-opus-4-8" } },
            OutputConfig = new BetaOutputConfig { Effort = Effort.Medium },
            System = new List<BetaTextBlockParam>
            {
                new() { Text = instructions, CacheControl = new BetaCacheControlEphemeral() },
                new() { Text = liveContext },
            },
            Messages = history
                .Select(t => t.Role == AssistantRole.User
                    ? (BetaMessageParam)new BetaUserMessageParam { Content = t.Content }
                    : new BetaAssistantMessageParam { Content = t.Content })
                .ToList(),
        };

        try
        {
            var response = await client.Beta.Messages.Create(request, cancellationToken);

            if (response.StopReason == "refusal")
            {
                logger.LogWarning("Assistant request was declined ({Category})", response.StopDetails?.Category);
                return Error.Validation("Assistant.Declined", "The assistant can't help with that question — try rephrasing it.");
            }

            var text = string.Join("\n", response.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(b => b.Text)).Trim();
            if (text.Length == 0)
                return Error.Failure("Assistant.Empty", "The assistant didn't return an answer — please try again.");

            logger.LogInformation("Assistant answered: {InputTokens} in / {OutputTokens} out, {CachedTokens} cached",
                response.Usage.InputTokens, response.Usage.OutputTokens, response.Usage.CacheReadInputTokens);
            return new AssistantReply(text);
        }
        catch (AnthropicRateLimitException ex)
        {
            logger.LogWarning(ex, "Assistant rate-limited");
            return Error.Failure("Assistant.Busy", "The assistant is busy right now — please try again in a minute.");
        }
        catch (AnthropicApiException ex)
        {
            logger.LogError(ex, "Assistant API call failed");
            return Error.Failure("Assistant.Unavailable", "The assistant is unavailable right now — please try again shortly.");
        }
    }
}
