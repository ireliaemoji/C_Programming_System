using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default") ?? string.Empty;
builder.Services.AddSingleton(new HealthOptions(
    connectionString));
builder.Services.AddSingleton(new ExamStore(connectionString));
builder.Services.AddHostedService<ExamDeadlineWorker>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<QuestionImportService>();

var app = builder.Build();

app.MapGet("/health", async (HealthOptions options, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    try
    {
        await using var connection = new MySqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
        return Results.Ok(new { status = "ok", database = "ok" });
    }
    catch (Exception exception) when (exception is MySqlException or InvalidOperationException or ArgumentException)
    {
        logger.LogWarning("Health check database connection failed ({ExceptionType}).", exception.GetType().Name);
        return Results.Json(
            new { status = "degraded", database = "unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPost("/api/auth/register", async (RegisterRequest request, ExamStore store, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 64 || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        return Results.BadRequest(new { message = "用户名不能为空且密码至少需要 8 位。" });
    var userId = await store.RegisterAsync(request.Username.Trim(), request.Password, cancellationToken);
    return userId is null ? Results.Conflict(new { message = "用户名已存在。" }) : Results.Ok(new { id = userId.Value });
});

app.MapPost("/api/auth/login", async (LoginRequest request, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password)) return Results.Unauthorized();
    var user = await store.FindUserAsync(request.Username.Trim(), cancellationToken);
    if (user is null || !Passwords.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();
    return Results.Ok(sessions.Create(user));
});

app.MapGet("/api/teacher/questions", async (HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    return user.Role == "Teacher" ? Results.Ok(await store.GetTeacherQuestionsAsync(user.Id, cancellationToken)) : Results.StatusCode(StatusCodes.Status403Forbidden);
});

app.MapPost("/api/teacher/questions", async (QuestionInput request, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!ValidQuestion(request)) return Results.BadRequest(new { message = "题干、四个选项、可见性和正确选项均不能为空且正确选项必须为1到4。" });
    return Results.Ok(new { id = await store.CreateQuestionAsync(user.Id, request, cancellationToken) });
});

app.MapPut("/api/teacher/questions/{id:long}", async (long id, QuestionInput request, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!ValidQuestion(request)) return Results.BadRequest(new { message = "题干、四个选项、可见性和正确选项均不能为空且正确选项必须为1到4。" });
    return await store.UpdateQuestionAsync(user.Id, id, request, cancellationToken) ? Results.Ok() : Results.NotFound();
});

app.MapPost("/api/teacher/questions/import", async (HttpRequest http, ExamStore store, SessionStore sessions, QuestionImportService importer, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (!http.HasFormContentType) return Results.BadRequest(new { message = "请使用 multipart/form-data 上传 CSV 或 XLSX 题库文件。" });
    var form = await http.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0 || file.Length > 5 * 1024 * 1024) return Results.BadRequest(new { message = "文件不能为空且不能超过5MB。" });
    await using var stream = file.OpenReadStream();
    var parsed = await importer.ParseAsync(stream, file.FileName, cancellationToken);
    if (parsed.Errors.Count > 0) return Results.BadRequest(new { errors = parsed.Errors });
    var count = await store.CreateQuestionsAsync(user.Id, parsed.Rows, cancellationToken);
    return Results.Ok(new { imported = count, visibility = "TeacherPrivate" });
});

app.MapPost("/api/teacher/exams", async (ExamCreateRequest request, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200) return Results.BadRequest(new { message = "考试标题不能为空且不能超过200个字符。" });
    try { return Results.Ok(new { id = await store.CreateExamAsync(user.Id, request, cancellationToken) }); }
    catch (ArgumentException exception) { return Results.BadRequest(new { message = exception.Message }); }
});

app.MapGet("/api/teacher/exams", async (HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    return Results.Ok(await store.GetTeacherExamsAsync(user.Id, cancellationToken));
});

app.MapGet("/api/teacher/exams/{id:long}/attempts", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    return Results.Ok(await store.GetTeacherAttemptsAsync(user.Id, id, cancellationToken));
});

app.MapPost("/api/teacher/exams/{id:long}/publish-grades", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    var published = await store.PublishTeacherGradesAsync(user.Id, id, cancellationToken);
    return published is null ? Results.NotFound() : published.Value ? Results.Ok(new { published = true }) : Results.Conflict(new { message = "考试结束后才能发布成绩。" });
});

app.MapPost("/api/teacher/exams/{id:long}/close", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    return await store.CloseUntimedExamAsync(user.Id, id, cancellationToken)
        ? Results.Ok()
        : Results.Conflict(new { message = "只有本人已开放且未设置截止时间的考试可以手动结束。" });
});

app.MapGet("/api/teacher/attempts/{id:long}/answers", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    var answers = await store.GetReviewAnswersAsync(user.Id, id, teacher: true, cancellationToken);
    return answers is null ? Results.NotFound() : Results.Ok(answers);
});

app.MapPost("/api/teacher/attempts/{attemptId:long}/answers/{questionId:long}/grade", async (long attemptId, long questionId, GradeAnswerRequest request, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (request.Points is not int points) return Results.BadRequest(new { message = "请选择要给的分值。" });
    try
    {
        var result = await store.GradeTeacherAnswerAsync(user.Id, attemptId, questionId, points, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
    catch (ArgumentException exception) { return Results.BadRequest(new { message = exception.Message }); }
});

app.MapPost("/api/teacher/classes", async (ClassCreateRequest request, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 120) return Results.BadRequest(new { message = "班级名称不能为空且不能超过120个字符。" });
    return Results.Ok(new { id = await store.CreateClassAsync(user.Id, request.Name, cancellationToken) });
});

app.MapGet("/api/teacher/classes", async (HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    return Results.Ok(await store.GetTeacherClassesAsync(user.Id, cancellationToken));
});

app.MapPost("/api/teacher/classes/{id:long}/members", async (long id, ClassMemberRequest request, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Teacher") return Results.StatusCode(StatusCodes.Status403Forbidden);
    if (request.Usernames is null || request.Usernames.Count == 0) return Results.BadRequest(new { message = "至少需要一个学生账号。" });
    try { return Results.Ok(new { added = await store.AddClassMembersAsync(user.Id, id, request.Usernames, cancellationToken) }); }
    catch (ArgumentException exception) { return Results.BadRequest(new { message = exception.Message }); }
});

app.MapGet("/api/questions", async (HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out _)) return Results.Unauthorized();
    return Results.Ok(await store.GetQuestionsAsync(cancellationToken));
});

app.MapPost("/api/questions/{id:long}/answer", async (long id, AnswerRequest request, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (request.SelectedOption is < 1 or > 4) return Results.BadRequest(new { message = "选项必须是 1 到 4。" });
    var result = await store.SaveAnswerAsync(user.Id, id, request.SelectedOption, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(new { questionId = id, isCorrect = result.Value });
});

app.MapGet("/api/exams", async (HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    return Results.Ok(await store.GetExamsAsync(user.Id, cancellationToken));
});

app.MapPost("/api/exams/{id:long}/start", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    var attempt = await store.StartAttemptAsync(user.Id, id, cancellationToken);
    return attempt is null ? Results.NotFound() : Results.Ok(attempt);
});

app.MapGet("/api/exam-attempts/{id:long}", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    var attempt = await store.GetAttemptAsync(user.Id, id, cancellationToken);
    return attempt is null ? Results.NotFound() : Results.Ok(attempt);
});

app.MapGet("/api/exam-attempts/{id:long}/questions", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    var questions = await store.GetAttemptQuestionsAsync(user.Id, id, cancellationToken);
    return questions is null ? Results.NotFound() : Results.Ok(questions);
});

app.MapPost("/api/exam-attempts/{id:long}/answers", async (long id, ExamAnswerRequest request, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (request.SelectedOption is < 1 or > 4) return Results.BadRequest(new { message = "选项必须是1到4。" });
    if (request.Revision is <= 0) return Results.BadRequest(new { message = "答案版本必须为正整数。" });
    return await store.SaveExamAnswerAsync(user.Id, id, request, cancellationToken) switch
    {
        ExamAnswerSave.Saved => Results.Ok(),
        ExamAnswerSave.Stale => Results.Conflict(new { message = "服务器已有相同或更新的答案版本。" }),
        _ => Results.NotFound()
    };
});

app.MapPost("/api/exam-attempts/{id:long}/submit", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    var result = await store.SubmitAttemptAsync(user.Id, id, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapGet("/api/grades", async (HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    return Results.Ok(await store.GetGradesAsync(user.Id, cancellationToken));
});

app.MapGet("/api/grades/{id:long}/answers", async (long id, HttpRequest http, ExamStore store, SessionStore sessions, CancellationToken cancellationToken) =>
{
    if (!sessions.TryGet(http.Headers.Authorization, out var user)) return Results.Unauthorized();
    if (user.Role != "Student") return Results.StatusCode(StatusCodes.Status403Forbidden);
    var answers = await store.GetReviewAnswersAsync(user.Id, id, teacher: false, cancellationToken);
    return answers is null ? Results.NotFound() : Results.Ok(answers);
});

static bool ValidQuestion(QuestionInput request) =>
    !string.IsNullOrWhiteSpace(request.Stem) && !string.IsNullOrWhiteSpace(request.OptionA) &&
    !string.IsNullOrWhiteSpace(request.OptionB) && !string.IsNullOrWhiteSpace(request.OptionC) &&
    !string.IsNullOrWhiteSpace(request.OptionD) && request.CorrectOption is >= 1 and <= 4 && request.Points is not <= 0 &&
    request.Visibility is QuestionVisibilities.TeacherPrivate or QuestionVisibilities.ClassOnly or QuestionVisibilities.PublicPractice;

app.Run();

public sealed record HealthOptions(string ConnectionString);
