using System.Collections.Concurrent;
using System.Security.Cryptography;

public static class QuestionVisibilities
{
    public const string TeacherPrivate = "TeacherPrivate";
    public const string ClassOnly = "ClassOnly";
    public const string PublicPractice = "PublicPractice";
}

public sealed record UserRecord(long Id, string Username, string PasswordHash, string Role);
public sealed record QuestionRecord(long Id, string Stem, string OptionA, string OptionB, string OptionC, string OptionD);
public sealed record TeacherQuestionRecord(long Id, string Stem, string OptionA, string OptionB, string OptionC, string OptionD, int CorrectOption, bool IsPublished, string Visibility, int Points);
public sealed record QuestionInput(string Stem, string OptionA, string OptionB, string OptionC, string OptionD, int CorrectOption, bool IsPublished, string Visibility, int? Points = null);
public sealed record RegisterRequest(string Username, string Password);
public sealed record LoginRequest(string Username, string Password);
public sealed record AnswerRequest(int SelectedOption);
public sealed record ExamCreateRequest(string Title, IReadOnlyList<long> QuestionIds, IReadOnlyList<long>? StudentIds, IReadOnlyList<long>? ClassIds, DateTimeOffset? StartAt, DateTimeOffset? EndAt, int? StartDelayMinutes = null, int? DurationMinutes = null);
public sealed record ClassCreateRequest(string Name);
public sealed record ClassMemberRequest(IReadOnlyList<string> Usernames);
public sealed record ClassRecord(long Id, string Name, string JoinCode, bool IsOpen, int MemberCount);
public sealed record GradeRecord(long AttemptId, string ExamTitle, int TotalScore, DateTime SubmittedAt);
public sealed record ExamSummary(long Id, string Title, int QuestionCount, DateTime? StartAt, DateTime? EndAt, string? AttemptStatus, DateTime ServerNow, long? AttemptId);
public sealed record TeacherExamRecord(long Id, string Title, int QuestionCount, int AssignmentCount, DateTime? StartAt, DateTime? EndAt, bool GradesPublished, bool HasEnded);
public sealed record TeacherAttemptRecord(long Id, long ExamId, string StudentUsername, string Status, int? TotalScore, DateTime? SubmittedAt, int AnsweredCount, int QuestionCount);
public sealed record ReviewAnswerRecord(long QuestionId, int QuestionNumber, string Stem, string OptionA, string OptionB, string OptionC, string OptionD, int SelectedOption, int CorrectOption, int Points, int MaxPoints, bool ManuallyGraded);
public sealed record GradeAnswerRequest(int? Points);
public sealed record GradeAnswerResult(int TotalScore);
public sealed record AttemptRecord(long Id, long ExamId, string Status, int? TotalScore, DateTime? EndAt, DateTime ServerNow);
public sealed record AttemptQuestionRecord(long Id, string Stem, string OptionA, string OptionB, string OptionC, string OptionD, int? SelectedOption, long Revision);
public sealed record ExamAnswerRequest(long QuestionId, int SelectedOption, long? Revision = null);
public enum ExamAnswerSave { Saved, Unavailable, Stale }
public sealed record AttemptResult(long AttemptId, long ExamId);
public sealed record SessionUser(long Id, string Username, string Role, DateTimeOffset ExpiresAt);
public sealed record AuthResponse(string Token, string Username, string Role);

public static class Passwords
{
    private const int Iterations = 120_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"v1${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iterations)) return false;
        try
        {
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[2]), iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}

public sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, SessionUser> sessions = new();

    public AuthResponse Create(UserRecord user)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var session = new SessionUser(user.Id, user.Username, user.Role, DateTimeOffset.UtcNow.AddHours(8));
        sessions[token] = session;
        return new AuthResponse(token, user.Username, user.Role);
    }

    public bool TryGet(string? authorization, out SessionUser user)
    {
        user = null!;
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return false;
        if (!sessions.TryGetValue(authorization[7..].Trim(), out var session) || session.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        user = session;
        return true;
    }
}
