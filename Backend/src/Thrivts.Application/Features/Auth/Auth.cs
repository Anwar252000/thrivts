using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Security;
using Thrivts.Domain.Identity;

namespace Thrivts.Application.Features.Auth;

public sealed record LoginUserCard(Guid Id, string DisplayName, string Title, string Color, int PinLength);

public sealed record SessionUser(Guid Id, string DisplayName, string Title, string Color, UserRole Role, AppKind App, IReadOnlyCollection<string> Permissions);

public sealed record SessionResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, SessionUser User);

internal static class SessionFactory
{
    public static SessionUser ToSessionUser(AppUser user) =>
        new(user.Id, user.DisplayName, user.Title, user.Color, user.Role, user.App, Permissions.For(user.Role));

    public static SessionResponse Issue(AppUser user, Guid familyId, IAppDbContext db, ITokenService tokens, IClock clock, ICurrentUser caller, string? userAgent, out RefreshToken stored)
    {
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();
        stored = new RefreshToken(user.Id, refresh.Hash, familyId, clock.UtcNow, refresh.ExpiresAt, caller.IpAddress, userAgent);
        db.RefreshTokens.Add(stored);
        return new SessionResponse(access.Token, access.ExpiresAt, refresh.Token, refresh.ExpiresAt, ToSessionUser(user));
    }
}

// ---------- The "who are you?" picker ----------

/// <summary>Names, titles and colours for the login picker of one app. No PINs, no roles beyond
/// the display title, no ids of the other app's users.</summary>
public sealed record GetLoginUsersQuery(AppKind App) : IQuery<IReadOnlyList<LoginUserCard>>;

public sealed class GetLoginUsersHandler(IAppDbContext db) : IQueryHandler<GetLoginUsersQuery, IReadOnlyList<LoginUserCard>>
{
    public async ValueTask<IReadOnlyList<LoginUserCard>> Handle(GetLoginUsersQuery query, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking()
            .Where(u => u.App == query.App && u.IsActive)
            .OrderBy(u => u.SortOrder).ThenBy(u => u.DisplayName)
            .Select(u => new { u.Id, u.DisplayName, u.Title, u.Color, u.Role })
            .ToListAsync(ct);

        return users.Select(u => new LoginUserCard(u.Id, u.DisplayName, u.Title, u.Color, UserRoles.PinLengthFor(u.Role))).ToList();
    }
}

// ---------- PIN login ----------

public sealed record LoginCommand(Guid UserId, string Pin, AppKind App, string? UserAgent) : ICommand<ErrorOr<SessionResponse>>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Pin).NotEmpty().Length(4, 6).Matches("^[0-9]+$").WithMessage("PIN must be digits only.");
    }
}

public sealed class LoginHandler(IAppDbContext db, IPinHasher hasher, ITokenService tokens, IClock clock, ICurrentUser caller, ILogger<LoginHandler> logger)
    : ICommandHandler<LoginCommand, ErrorOr<SessionResponse>>
{
    private static readonly Error WrongPin = Error.Unauthorized("Auth.WrongPin", "Wrong PIN — try again.");

    public async ValueTask<ErrorOr<SessionResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        // The same message for "no such user", "wrong app" and "deactivated" — never confirm which.
        if (user is null || !user.IsActive || user.App != command.App)
        {
            hasher.Verify(DummyHash, command.Pin);
            return WrongPin;
        }

        if (user.IsLockedOut(now))
        {
            var seconds = (int)Math.Ceiling((user.LockoutUntil!.Value - now).TotalSeconds);
            return Error.Forbidden("Auth.LockedOut", $"Too many wrong attempts — try again in {seconds} seconds.");
        }

        if (!hasher.Verify(user.PinHash, command.Pin))
        {
            user.RecordFailedPin(now);
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Failed PIN attempt for user {UserId} ({Attempts} in a row)", user.Id, user.FailedPinAttempts);

            return user.IsLockedOut(now)
                ? Error.Forbidden("Auth.LockedOut", $"Too many wrong attempts — locked for {(int)(user.LockoutUntil!.Value - now).TotalSeconds} seconds.")
                : Error.Unauthorized("Auth.WrongPin", $"Wrong PIN — {5 - user.FailedPinAttempts % 5} tries left before a pause.");
        }

        user.RecordSuccessfulLogin(now);
        var session = SessionFactory.Issue(user, Guid.CreateVersion7(), db, tokens, clock, caller, command.UserAgent, out _);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} signed in to the {App} app", user.Id, user.App);
        return session;
    }

    // Verifying against a fixed hash when the user does not exist keeps response timing the same,
    // so the endpoint cannot be used to probe which user ids are real.
    private const string DummyHash = "v1$600000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
}

// ---------- Refresh (rotating, with reuse detection) ----------

public sealed record RefreshSessionCommand(string RefreshToken, string? UserAgent) : ICommand<ErrorOr<SessionResponse>>;

public sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
}

public sealed class RefreshSessionHandler(IAppDbContext db, ITokenService tokens, IClock clock, ICurrentUser caller, ILogger<RefreshSessionHandler> logger)
    : ICommandHandler<RefreshSessionCommand, ErrorOr<SessionResponse>>
{
    private static readonly Error Expired = Error.Unauthorized("Auth.SessionExpired", "Your session has ended — please sign in again.");

    public async ValueTask<ErrorOr<SessionResponse>> Handle(RefreshSessionCommand command, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var hash = tokens.HashRefreshToken(command.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null)
            return Expired;

        if (token.RevokedAt is not null)
        {
            // A token that was already rotated is being replayed: someone else has a copy.
            // Kill the whole family so both the thief and the victim must sign in again.
            await db.RefreshTokens
                .Where(t => t.FamilyId == token.FamilyId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.RevokedReason, "reuse-detected"), ct);
            logger.LogWarning("Refresh token reuse detected for user {UserId}; session family revoked", token.UserId);
            return Expired;
        }

        if (!token.IsActive(now))
            return Expired;

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null || !user.IsActive)
        {
            token.Revoke(now, "user-inactive");
            await db.SaveChangesAsync(ct);
            return Expired;
        }

        var session = SessionFactory.Issue(user, token.FamilyId, db, tokens, clock, caller, command.UserAgent, out var next);
        token.Revoke(now, "rotated", next.Id);
        await db.SaveChangesAsync(ct);
        return session;
    }
}

// ---------- Logout ----------

public sealed record LogoutCommand(string RefreshToken) : ICommand<Success>;

public sealed class LogoutHandler(IAppDbContext db, ITokenService tokens, IClock clock) : ICommandHandler<LogoutCommand, Success>
{
    public async ValueTask<Success> Handle(LogoutCommand command, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(command.RefreshToken);
        var familyId = await db.RefreshTokens.Where(t => t.TokenHash == hash).Select(t => (Guid?)t.FamilyId).FirstOrDefaultAsync(ct);
        if (familyId is not null)
        {
            var now = clock.UtcNow;
            await db.RefreshTokens
                .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.RevokedReason, "logout"), ct);
        }

        return Result.Success;
    }
}

// ---------- Me / change PIN ----------

public sealed record GetMeQuery : IQuery<ErrorOr<SessionUser>>;

public sealed class GetMeHandler(IAppDbContext db, ICurrentUser caller) : IQueryHandler<GetMeQuery, ErrorOr<SessionUser>>
{
    public async ValueTask<ErrorOr<SessionUser>> Handle(GetMeQuery query, CancellationToken ct)
    {
        var id = caller.RequireUserId();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);
        return user is null ? Error.Unauthorized(description: "Please sign in again.") : SessionFactory.ToSessionUser(user);
    }
}

public sealed record ChangeMyPinCommand(string CurrentPin, string NewPin) : ICommand<ErrorOr<Success>>;

public sealed class ChangeMyPinValidator : AbstractValidator<ChangeMyPinCommand>
{
    public ChangeMyPinValidator()
    {
        RuleFor(x => x.CurrentPin).NotEmpty().Matches("^[0-9]{4,6}$");
        RuleFor(x => x.NewPin).NotEmpty().Matches("^[0-9]{4,6}$").WithMessage("PIN must be digits only.")
            .Must(p => p.Distinct().Count() > 1).WithMessage("Choose a PIN that isn't one digit repeated.")
            .NotEqual(x => x.CurrentPin).WithMessage("The new PIN must be different.");
    }
}

public sealed class ChangeMyPinHandler(IAppDbContext db, ICurrentUser caller, IPinHasher hasher, IClock clock)
    : ICommandHandler<ChangeMyPinCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(ChangeMyPinCommand command, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(u => u.Id == caller.RequireUserId(), ct);
        if (command.NewPin.Length != UserRoles.PinLengthFor(user.Role))
            return Error.Validation(description: $"Your PIN must be {UserRoles.PinLengthFor(user.Role)} digits.");

        if (!hasher.Verify(user.PinHash, command.CurrentPin))
        {
            user.RecordFailedPin(clock.UtcNow);
            await db.SaveChangesAsync(ct);
            return Error.Unauthorized(description: "Current PIN is wrong.");
        }

        user.ChangePin(hasher.Hash(command.NewPin));
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}
