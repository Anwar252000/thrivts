using System.Globalization;
using System.Text;
using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Features.Owner;

public enum AssistantRole
{
    User,
    Assistant,
}

public sealed record AssistantTurn(AssistantRole Role, string Content);

public sealed record AssistantReply(string Text);

/// <summary>Port to the language model. The API key lives server-side only; the browser never
/// sees it and cannot choose the system prompt.</summary>
public interface IAssistantClient
{
    bool IsConfigured { get; }

    Task<ErrorOr<AssistantReply>> AskAsync(string instructions, string liveContext, IReadOnlyList<AssistantTurn> history, CancellationToken cancellationToken);
}

/// <summary>The owner's VH Assistant. The client sends only the conversation; the business context
/// is assembled here from live data on every question, so answers always reflect the database.</summary>
public sealed record AskAssistantCommand(IReadOnlyList<AssistantTurn> Messages) : ICommand<ErrorOr<AssistantReply>>;

public sealed class AskAssistantValidator : AbstractValidator<AskAssistantCommand>
{
    public const int MaxTurns = 12;

    public AskAssistantValidator()
    {
        RuleFor(x => x.Messages).NotEmpty().Must(m => m.Count <= MaxTurns).WithMessage($"Send at most the last {MaxTurns} messages.")
            .Must(m => m.Count > 0 && m[^1].Role == AssistantRole.User).WithMessage("The last message must be your question.")
            .Must(m => m.Count > 0 && m[0].Role == AssistantRole.User).WithMessage("The conversation must start with a question.");
        RuleForEach(x => x.Messages).ChildRules(t => t.RuleFor(x => x.Content).NotEmpty().MaximumLength(4000));
    }
}

public sealed class AskAssistantHandler(IAppDbContext db, IAssistantClient assistant, IClock clock) : ICommandHandler<AskAssistantCommand, ErrorOr<AssistantReply>>
{
    private const string Instructions =
        "You are the business assistant for Vintage Hub, a wholesale used-clothing warehouse in Karachi. " +
        "The owner is Afnan. Money is in Pakistani rupees (Rs). Answer concisely and practically, in the language the question is asked in. " +
        "Ground every figure in the live data provided; if the data doesn't cover something, say so. " +
        "You may estimate and suggest next steps, but label estimates as estimates.";

    private static readonly CultureInfo Numbers = CultureInfo.GetCultureInfo("en-US");

    public async ValueTask<ErrorOr<AssistantReply>> Handle(AskAssistantCommand command, CancellationToken ct)
    {
        if (!assistant.IsConfigured)
            return Error.Failure("Assistant.NotConfigured", "The assistant isn't set up yet — add the Anthropic API key on the server.");

        // Collapse consecutive same-role turns so the history always alternates.
        var history = new List<AssistantTurn>();
        foreach (var turn in command.Messages)
        {
            if (history.Count > 0 && history[^1].Role == turn.Role)
                history[^1] = history[^1] with { Content = history[^1].Content + "\n\n" + turn.Content };
            else
                history.Add(turn);
        }

        return await assistant.AskAsync(Instructions, await BuildContextAsync(ct), history, ct);
    }

    private async Task<string> BuildContextAsync(CancellationToken ct)
    {
        var floor = await new GetFloorHandler(db).Handle(new GetFloorQuery(20), ct);
        var hisab = await new GetHisabHandler(db).Handle(new GetHisabQuery(null), ct);
        var valuation = await new GetValuationHandler(db).Handle(new GetValuationQuery(null), ct);
        var orders = await new GetOwnerOrdersHandler(db).Handle(new GetOwnerOrdersQuery(false), ct);
        var openEscalations = await db.Escalations.AsNoTracking().Where(e => !e.IsResolved).OrderByDescending(e => e.CreatedAt).Take(10)
            .Select(e => new { e.FromName, e.Message }).ToListAsync(ct);
        var items = await db.Items.AsNoTracking().CountAsync(i => !i.IsArchived, ct);

        string Rs(decimal v) => "Rs " + v.ToString("N0", Numbers);

        var sb = new StringBuilder();
        sb.AppendLine($"LIVE DATA as of {clock.UtcNow:yyyy-MM-dd HH:mm} UTC (business date {clock.Today:yyyy-MM-dd}):");
        sb.AppendLine($"- Floor: {floor.Lots} lots, {floor.PiecesOnFloor:N0} pcs in process. By stage: " +
                      string.Join(", ", floor.Items.GroupBy(l => l.Stage).Select(g => $"{g.Key} {g.Count()}")) + ".");
        var pricing = floor.Items.Where(l => l.Stage == FloorStage.Pricing).Take(20).Select(l => $"{l.Serial} ({l.SupplierName}, {l.Pieces:N0} pcs)").ToList();
        sb.AppendLine($"- Waiting for the owner's price/invoice: {(pricing.Count == 0 ? "none" : string.Join("; ", pricing))}.");
        var dues = hisab.Rows.Where(r => r.Due > 0).Take(15).Select(r => $"{r.Name}: {Rs(r.Due)}").ToList();
        sb.AppendLine($"- Supplier dues (owner owes, total {Rs(hisab.TotalDue)}): {(dues.Count == 0 ? "none outstanding" : string.Join("; ", dues))}.");
        sb.AppendLine($"- Inventory: {valuation.Pieces:N0} pcs on hand, value {Rs(valuation.Value)} at sell prices.");
        var orderLines = orders.Take(20).Select(o => $"{o.Name} ({o.ClientName ?? "?"}, {o.Status}, client-ready {o.ClientReady:N0}/{o.Required:N0}, value {Rs(o.TotalValue)}, paid {Rs(o.Paid)})").ToList();
        sb.AppendLine($"- Active client orders: {(orderLines.Count == 0 ? "none" : string.Join("; ", orderLines))}.");
        sb.AppendLine($"- Open escalations: {(openEscalations.Count == 0 ? "none" : string.Join("; ", openEscalations.Select(e => $"{e.FromName}: {e.Message}")))}.");
        sb.AppendLine($"- Catalogue: {items} active items.");
        return sb.ToString();
    }
}
