using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Orders;

public enum ClientOrderStatus
{
    InProcess,
    BeingGraded,
    WaitingForPayment,
    ReadyToShip,
    Completed,
    Shipped,
    OnHold,
}

public enum OrderLineStatus
{
    Pending,
    BeingGraded,
    PartialReady,
    Ready,
    OnHold,
    Shipped,
}

/// <summary>
/// A buyer's order (e.g. "Brandor-11"). One model serves both apps: the staff Requirements board
/// sees lines with required/ready counts; the owner's VEW Clients view additionally sees the order
/// value, the client portal login code and the client-facing "ready" figure. In the legacy apps
/// these were two separate copies that only got connected "at cutover".
/// </summary>
public class ClientOrder : AuditableEntity, ISoftDeletable, IVersioned
{
    public const int MaxLines = 1000;

    private readonly List<OrderLine> _lines = [];
    private readonly List<ClientOrderPayment> _payments = [];

    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;
    public string? ClientName { get; private set; }
    public ClientOrderStatus Status { get; private set; } = ClientOrderStatus.InProcess;
    public DateOnly? Deadline { get; private set; }
    public bool IsCompleted { get; private set; }
    public string? LoginCode { get; private set; }
    public decimal TotalValue { get; private set; }
    public string? DestinationCountry { get; private set; }
    public DateOnly? OrderReceivedDate { get; private set; }
    public DateOnly? DispatchDate { get; private set; }
    public int? Boxes { get; private set; }
    public decimal? WeightKg { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }
    public uint Version { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;
    public IReadOnlyList<ClientOrderPayment> Payments => _payments;

    private ClientOrder()
    {
    }

    public ClientOrder(string name, string? clientName)
    {
        Rename(name);
        ClientName = clientName;
    }

    public void Rename(string name)
    {
        var clean = Text.Clean(name);
        if (clean.Length is 0 or > 80)
            throw new DomainException("Order name must be 1–80 characters.");

        Name = clean;
        NormalizedName = Text.Normalize(clean);
    }

    public void UpdateDetails(string? clientName, DateOnly? deadline, string? destinationCountry, DateOnly? orderReceivedDate, DateOnly? dispatchDate, int? boxes, decimal? weightKg)
    {
        if (boxes is < 0 || weightKg is < 0)
            throw new DomainException("Boxes and weight cannot be negative.");

        ClientName = clientName;
        Deadline = deadline;
        DestinationCountry = destinationCountry;
        OrderReceivedDate = orderReceivedDate;
        DispatchDate = dispatchDate;
        Boxes = boxes;
        WeightKg = weightKg;
    }

    public void SetStatus(ClientOrderStatus status) => Status = status;

    public void SetCompleted(bool completed) => IsCompleted = completed;

    public void SetOwnerFields(decimal totalValue, string? loginCode)
    {
        if (totalValue < 0)
            throw new DomainException("Order value cannot be negative.");

        TotalValue = totalValue;
        LoginCode = string.IsNullOrWhiteSpace(loginCode) ? null : loginCode.Trim();
    }

    /// <summary>Adds a line, or — if the same item already exists on the order — merges into it the
    /// way the legacy importer deduplicated (keep the larger required/ready, fill blanks).</summary>
    public OrderLine UpsertLine(string itemName, string? grade, string? category, int required, int ready, OrderLineStatus? status, string? note)
    {
        var key = Text.Normalize(itemName);
        var existing = _lines.FirstOrDefault(l => l.NormalizedItemName == key);
        if (existing is not null)
        {
            existing.MergeFrom(grade, category, required, ready, note);
            return existing;
        }

        if (_lines.Count >= MaxLines)
            throw new DomainException($"An order can hold at most {MaxLines} lines.");

        var line = new OrderLine(Id, itemName, grade, category, required, ready, status ?? OrderLineStatus.Pending, note, _lines.Count == 0 ? 1 : _lines.Max(l => l.Seq) + 1);
        _lines.Add(line);
        return line;
    }

    public OrderLine Line(Guid lineId) =>
        _lines.FirstOrDefault(l => l.Id == lineId) ?? throw new DomainException("That line is not on this order.");

    public void RemoveLine(Guid lineId) => _lines.Remove(Line(lineId));

    public void MarkAllReady()
    {
        foreach (var line in _lines)
            line.MarkReady();
    }

    public ClientOrderPayment AddPayment(string? label, decimal amount, bool isPaid, DateOnly? paidOn)
    {
        var payment = new ClientOrderPayment(Id, label, amount, isPaid, paidOn);
        _payments.Add(payment);
        return payment;
    }

    public void RemovePayment(Guid paymentId) =>
        _payments.Remove(_payments.FirstOrDefault(p => p.Id == paymentId) ?? throw new DomainException("Payment not found."));

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }
}

public class OrderLine : Entity
{
    public Guid ClientOrderId { get; private set; }
    public string ItemName { get; private set; } = default!;
    public string NormalizedItemName { get; private set; } = default!;
    public string? Grade { get; private set; }
    public string? Category { get; private set; }
    public int Required { get; private set; }
    public int Ready { get; private set; }
    public int ClientReady { get; private set; }
    public OrderLineStatus Status { get; private set; }
    public string? Note { get; private set; }
    public long Seq { get; private set; }

    private OrderLine()
    {
    }

    internal OrderLine(Guid orderId, string itemName, string? grade, string? category, int required, int ready, OrderLineStatus status, string? note, long seq)
    {
        ClientOrderId = orderId;
        SetItem(itemName);
        Grade = grade;
        Category = category;
        SetQuantities(required, ready);
        Status = status;
        Note = note;
        Seq = seq;
    }

    public int Remaining => Required - Ready;

    public void SetItem(string itemName)
    {
        var clean = Text.Clean(itemName);
        if (clean.Length is 0 or > 120)
            throw new DomainException("Item name must be 1–120 characters.");

        ItemName = clean;
        NormalizedItemName = Text.Normalize(clean);
    }

    public void SetQuantities(int required, int ready)
    {
        if (required < 0 || ready < 0)
            throw new DomainException("Quantities cannot be negative.");

        Required = required;
        Ready = ready;
    }

    public void Update(string? grade, string? category, OrderLineStatus status, string? note)
    {
        Grade = grade;
        Category = category;
        Status = status;
        Note = note;
    }

    public void SetClientReady(int clientReady)
    {
        if (clientReady < 0)
            throw new DomainException("Client-ready cannot be negative.");

        ClientReady = clientReady;
    }

    /// <summary>A sack assigned to this line counts towards Ready immediately.</summary>
    public void Commit(int pieces)
    {
        Ready += pieces;
        if (Ready >= Required && Required > 0)
            Status = OrderLineStatus.Ready;
        else if (Ready > 0 && Status == OrderLineStatus.Pending)
            Status = OrderLineStatus.PartialReady;
    }

    public void Uncommit(int pieces)
    {
        Ready = Math.Max(0, Ready - pieces);
        if (Ready < Required && Status == OrderLineStatus.Ready)
            Status = Ready > 0 ? OrderLineStatus.PartialReady : OrderLineStatus.Pending;
    }

    public void MarkReady()
    {
        Ready = Math.Max(Ready, Required);
        Status = OrderLineStatus.Ready;
    }

    internal void MergeFrom(string? grade, string? category, int required, int ready, string? note)
    {
        Required = Math.Max(Required, required);
        Ready = Math.Max(Ready, ready);
        Grade ??= grade;
        Category ??= category;
        Note ??= note;
    }
}

/// <summary>A payment milestone on a client order. Money records are never physically removed:
/// removing one from the order soft-deletes it (the persistence layer turns the delete into an
/// update) and the audit log keeps who removed it and what it was.</summary>
public class ClientOrderPayment : AuditableEntity, ISoftDeletable
{
    public Guid ClientOrderId { get; private set; }
    public string? Label { get; private set; }
    public decimal Amount { get; private set; }
    public bool IsPaid { get; private set; }
    public DateOnly? PaidOn { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedById { get; private set; }

    private ClientOrderPayment()
    {
    }

    public void MarkDeleted(DateTimeOffset at, Guid? by)
    {
        IsDeleted = true;
        DeletedAt = at;
        DeletedById = by;
    }

    internal ClientOrderPayment(Guid orderId, string? label, decimal amount, bool isPaid, DateOnly? paidOn)
    {
        ClientOrderId = orderId;
        Update(label, amount, isPaid, paidOn);
    }

    public void Update(string? label, decimal amount, bool isPaid, DateOnly? paidOn)
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative.");

        Label = label;
        Amount = amount;
        IsPaid = isPaid;
        PaidOn = isPaid ? paidOn : null;
    }
}

public enum ClientMessageType
{
    Request,
    Call,
    Video,
}

/// <summary>A request a buyer sends from their order portal (legacy client_messages): a note, or
/// asking for a call or a video of the goods. The owner works through them and ticks them done.</summary>
public class ClientMessage : Entity
{
    public const int MaxMessageLength = 1000;

    public Guid ClientOrderId { get; private set; }
    public string OrderName { get; private set; } = default!;
    public string? ClientName { get; private set; }
    public ClientMessageType Type { get; private set; }
    public string Message { get; private set; } = default!;
    public string? RequestedWhen { get; private set; }
    public bool IsDone { get; private set; }
    public DateTimeOffset? DoneAt { get; private set; }
    public Guid? DoneById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ClientMessage()
    {
    }

    public ClientMessage(ClientOrder order, ClientMessageType type, string message, string? requestedWhen, DateTimeOffset now)
    {
        var text = message.Trim();
        if (text.Length is 0 or > MaxMessageLength)
            throw new DomainException($"Message must be 1–{MaxMessageLength} characters.");

        ClientOrderId = order.Id;
        OrderName = order.Name;
        ClientName = order.ClientName;
        Type = type;
        Message = text;
        RequestedWhen = string.IsNullOrWhiteSpace(requestedWhen) ? null : requestedWhen.Trim();
        CreatedAt = now;
    }

    public void SetDone(bool done, Guid userId, DateTimeOffset now)
    {
        IsDone = done;
        DoneAt = done ? now : null;
        DoneById = done ? userId : null;
    }
}
