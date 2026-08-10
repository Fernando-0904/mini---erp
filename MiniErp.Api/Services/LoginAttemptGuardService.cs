using System.Collections.Concurrent;

namespace MiniErp.Api.Services;

public sealed class AuthSecurityOptions
{
    public int MaxFailedAttemptsPerEmail { get; set; } = 5;
    public int MaxFailedAttemptsPerIp { get; set; } = 20;
    public int SlidingWindowMinutes { get; set; } = 15;
    public int LockoutMinutes { get; set; } = 10;
}

public sealed class LoginAttemptGuardService
{
    private const string AnonymousEmailKey = "email:anonymous";
    private const string UnknownIpKey = "ip:unknown";

    private readonly ConcurrentDictionary<string, AttemptState> emailStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, AttemptState> ipStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly int maxFailedAttemptsPerEmail;
    private readonly int maxFailedAttemptsPerIp;
    private readonly TimeSpan slidingWindow;
    private readonly TimeSpan lockoutDuration;

    public LoginAttemptGuardService(IConfiguration configuration)
    {
        AuthSecurityOptions options = configuration
            .GetSection("AuthSecurity")
            .Get<AuthSecurityOptions>()
            ?? new AuthSecurityOptions();

        maxFailedAttemptsPerEmail = Math.Max(options.MaxFailedAttemptsPerEmail, 3);
        maxFailedAttemptsPerIp = Math.Max(options.MaxFailedAttemptsPerIp, maxFailedAttemptsPerEmail);
        slidingWindow = TimeSpan.FromMinutes(Math.Max(options.SlidingWindowMinutes, 1));
        lockoutDuration = TimeSpan.FromMinutes(Math.Max(options.LockoutMinutes, 1));
    }

    public LoginLockDecision AvaliarBloqueio(string? email, string? ipAddress)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string emailKey = BuildEmailKey(email);
        string ipKey = BuildIpKey(ipAddress);

        int? retryAfterByEmail = GetRetryAfterSeconds(emailStates, emailKey, now);
        int? retryAfterByIp = GetRetryAfterSeconds(ipStates, ipKey, now);

        int retryAfterSeconds = Math.Max(retryAfterByEmail ?? 0, retryAfterByIp ?? 0);
        return retryAfterSeconds > 0
            ? LoginLockDecision.Bloqueado(retryAfterSeconds)
            : LoginLockDecision.Liberado();
    }

    public void RegistrarFalha(string? email, string? ipAddress)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        RegisterFailure(emailStates, BuildEmailKey(email), maxFailedAttemptsPerEmail, now);
        RegisterFailure(ipStates, BuildIpKey(ipAddress), maxFailedAttemptsPerIp, now);
    }

    public void RegistrarSucesso(string? email)
    {
        string emailKey = BuildEmailKey(email);
        emailStates.TryRemove(emailKey, out _);
    }

    private int? GetRetryAfterSeconds(ConcurrentDictionary<string, AttemptState> states, string key, DateTimeOffset now)
    {
        if (!states.TryGetValue(key, out AttemptState? state))
        {
            return null;
        }

        lock (state)
        {
            PruneFailures(state, now);

            if (state.LockedUntilUtc is null || state.LockedUntilUtc <= now)
            {
                state.LockedUntilUtc = null;

                if (state.FailedAttemptsUtc.Count == 0)
                {
                    states.TryRemove(key, out _);
                }

                return null;
            }

            return (int)Math.Ceiling((state.LockedUntilUtc.Value - now).TotalSeconds);
        }
    }

    private void RegisterFailure(ConcurrentDictionary<string, AttemptState> states, string key, int threshold, DateTimeOffset now)
    {
        AttemptState state = states.GetOrAdd(key, _ => new AttemptState());

        lock (state)
        {
            PruneFailures(state, now);

            if (state.LockedUntilUtc is not null && state.LockedUntilUtc > now)
            {
                return;
            }

            state.FailedAttemptsUtc.Enqueue(now);

            if (state.FailedAttemptsUtc.Count >= threshold)
            {
                state.LockedUntilUtc = now.Add(lockoutDuration);
                state.FailedAttemptsUtc.Clear();
            }
        }
    }

    private void PruneFailures(AttemptState state, DateTimeOffset now)
    {
        DateTimeOffset cutOff = now.Subtract(slidingWindow);

        while (state.FailedAttemptsUtc.TryPeek(out DateTimeOffset failedAt) && failedAt < cutOff)
        {
            state.FailedAttemptsUtc.Dequeue();
        }
    }

    private static string BuildEmailKey(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return AnonymousEmailKey;
        }

        return $"email:{email.Trim().ToLowerInvariant()}";
    }

    private static string BuildIpKey(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return UnknownIpKey;
        }

        return $"ip:{ipAddress.Trim()}";
    }

    private sealed class AttemptState
    {
        public Queue<DateTimeOffset> FailedAttemptsUtc { get; } = new();
        public DateTimeOffset? LockedUntilUtc { get; set; }
    }
}

public sealed class LoginLockDecision
{
    private LoginLockDecision(bool isLocked, int retryAfterSeconds)
    {
        IsLocked = isLocked;
        RetryAfterSeconds = retryAfterSeconds;
    }

    public bool IsLocked { get; }
    public int RetryAfterSeconds { get; }

    public static LoginLockDecision Liberado()
    {
        return new LoginLockDecision(false, 0);
    }

    public static LoginLockDecision Bloqueado(int retryAfterSeconds)
    {
        return new LoginLockDecision(true, Math.Max(retryAfterSeconds, 1));
    }
}