using MySqlConnector;

public sealed class ExamStore
{
    private const string DemoPassword = "student123";
    private readonly string connectionString;
    private readonly SemaphoreSlim schemaLock = new(1, 1);
    private bool schemaReady;

    public ExamStore(string connectionString) => this.connectionString = connectionString;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (schemaReady) return;
        await schemaLock.WaitAsync(cancellationToken);
        try
        {
            if (schemaReady) return;
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS app_users (
                    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    username VARCHAR(64) NOT NULL UNIQUE,
                    password_hash VARCHAR(256) NOT NULL,
                    role VARCHAR(32) NOT NULL,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                )
                """, cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS questions (
                    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    owner_id BIGINT NULL,
                    stem TEXT NOT NULL,
                    option_a VARCHAR(500) NOT NULL,
                    option_b VARCHAR(500) NOT NULL,
                    option_c VARCHAR(500) NOT NULL,
                    option_d VARCHAR(500) NOT NULL,
                    correct_option TINYINT NOT NULL,
                    is_published BOOLEAN NOT NULL DEFAULT TRUE,
                    visibility VARCHAR(32) NOT NULL DEFAULT 'PublicPractice'
                )
                """, cancellationToken);
            await EnsureColumnAsync(connection, "questions", "points", "INT NOT NULL DEFAULT 1", cancellationToken);
            await EnsureColumnAsync(connection, "questions", "owner_id", "BIGINT NULL", cancellationToken);
            await EnsureColumnAsync(connection, "questions", "visibility", "VARCHAR(32) NOT NULL DEFAULT 'PublicPractice'", cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS question_answers (
                    user_id BIGINT NOT NULL,
                    question_id BIGINT NOT NULL,
                    selected_option TINYINT NOT NULL,
                    is_correct BOOLEAN NOT NULL,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (user_id, question_id),
                    CONSTRAINT fk_answer_user FOREIGN KEY (user_id) REFERENCES app_users(id),
                    CONSTRAINT fk_answer_question FOREIGN KEY (question_id) REFERENCES questions(id)
                )
                """, cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS exams (
                    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    title VARCHAR(200) NOT NULL,
                    created_by BIGINT NOT NULL,
                    is_published BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT fk_exam_creator FOREIGN KEY (created_by) REFERENCES app_users(id)
                )
                """, cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS exam_questions (
                    exam_id BIGINT NOT NULL,
                    question_id BIGINT NOT NULL,
                    sort_order INT NOT NULL,
                    points INT NOT NULL DEFAULT 1,
                    stem TEXT NULL,
                    option_a VARCHAR(500) NULL,
                    option_b VARCHAR(500) NULL,
                    option_c VARCHAR(500) NULL,
                    option_d VARCHAR(500) NULL,
                    correct_option TINYINT NULL,
                    PRIMARY KEY (exam_id, question_id),
                    CONSTRAINT fk_exam_question_exam FOREIGN KEY (exam_id) REFERENCES exams(id),
                    CONSTRAINT fk_exam_question_question FOREIGN KEY (question_id) REFERENCES questions(id)
                )
                """, cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS exam_attempts (
                    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    exam_id BIGINT NOT NULL,
                    user_id BIGINT NOT NULL,
                    status VARCHAR(32) NOT NULL DEFAULT 'InProgress',
                    started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    submitted_at TIMESTAMP NULL,
                    total_score INT NULL,
                    UNIQUE KEY uq_exam_attempt_user (exam_id, user_id),
                    CONSTRAINT fk_attempt_exam FOREIGN KEY (exam_id) REFERENCES exams(id),
                    CONSTRAINT fk_attempt_user FOREIGN KEY (user_id) REFERENCES app_users(id)
                )
                """, cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS exam_answers (
                    attempt_id BIGINT NOT NULL,
                    question_id BIGINT NOT NULL,
                    selected_option TINYINT NOT NULL,
                    is_correct BOOLEAN NOT NULL,
                    points INT NOT NULL DEFAULT 0,
                    PRIMARY KEY (attempt_id, question_id),
                    CONSTRAINT fk_exam_answer_attempt FOREIGN KEY (attempt_id) REFERENCES exam_attempts(id),
                    CONSTRAINT fk_exam_answer_question FOREIGN KEY (question_id) REFERENCES questions(id)
                )
                """, cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS exam_assignments (
                    exam_id BIGINT NOT NULL,
                    user_id BIGINT NOT NULL,
                    PRIMARY KEY (exam_id, user_id),
                    CONSTRAINT fk_exam_assignment_exam FOREIGN KEY (exam_id) REFERENCES exams(id),
                    CONSTRAINT fk_exam_assignment_user FOREIGN KEY (user_id) REFERENCES app_users(id)
                )
                """, cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS classes (
                    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(120) NOT NULL,
                    join_code VARCHAR(32) NOT NULL UNIQUE,
                    teacher_id BIGINT NOT NULL,
                    is_open BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT fk_class_teacher FOREIGN KEY (teacher_id) REFERENCES app_users(id)
                )
                """, cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS class_members (
                    class_id BIGINT NOT NULL,
                    user_id BIGINT NOT NULL,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (class_id, user_id),
                    CONSTRAINT fk_class_member_class FOREIGN KEY (class_id) REFERENCES classes(id),
                    CONSTRAINT fk_class_member_user FOREIGN KEY (user_id) REFERENCES app_users(id)
                )
                """, cancellationToken);
            await EnsureColumnAsync(connection, "exam_questions", "stem", "TEXT NULL", cancellationToken);
            await EnsureColumnAsync(connection, "exam_questions", "option_a", "VARCHAR(500) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "exam_questions", "option_b", "VARCHAR(500) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "exam_questions", "option_c", "VARCHAR(500) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "exam_questions", "option_d", "VARCHAR(500) NULL", cancellationToken);
            await EnsureColumnAsync(connection, "exam_questions", "correct_option", "TINYINT NULL", cancellationToken);
            // Legacy exams can only freeze the question content available at upgrade.
            await ExecuteAsync(connection, """
                UPDATE exam_questions eq JOIN questions q ON q.id=eq.question_id
                SET eq.stem=q.stem, eq.option_a=q.option_a, eq.option_b=q.option_b,
                    eq.option_c=q.option_c, eq.option_d=q.option_d, eq.correct_option=q.correct_option
                WHERE eq.correct_option IS NULL
                """, cancellationToken);
            await EnsureColumnAsync(connection, "exam_answers", "revision", "BIGINT NOT NULL DEFAULT 0", cancellationToken);
            await EnsureColumnAsync(connection, "exams", "start_at", "DATETIME NULL", cancellationToken);
            await EnsureColumnAsync(connection, "exams", "end_at", "DATETIME NULL", cancellationToken);
            await EnsureColumnAsync(connection, "exams", "grades_published", "BOOLEAN NOT NULL DEFAULT FALSE", cancellationToken);
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS exam_grade_overrides (
                    attempt_id BIGINT NOT NULL,
                    question_id BIGINT NOT NULL,
                    points INT NOT NULL,
                    PRIMARY KEY (attempt_id, question_id),
                    CONSTRAINT fk_grade_override_attempt FOREIGN KEY (attempt_id) REFERENCES exam_attempts(id),
                    CONSTRAINT fk_grade_override_question FOREIGN KEY (question_id) REFERENCES questions(id)
                )
                """, cancellationToken);
            await using (var command = new MySqlCommand("INSERT IGNORE INTO app_users(username, password_hash, role) VALUES ('student', @hash, 'Student')", connection))
            {
                command.Parameters.AddWithValue("@hash", Passwords.Hash(DemoPassword));
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await using (var command = new MySqlCommand("INSERT IGNORE INTO app_users(username, password_hash, role) VALUES ('teacher', @hash, 'Teacher')", connection))
            {
                command.Parameters.AddWithValue("@hash", Passwords.Hash("teacher123"));
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await ExecuteAsync(connection, """
                INSERT IGNORE INTO questions(id, owner_id, stem, option_a, option_b, option_c, option_d, correct_option, is_published, visibility) VALUES
                (1, NULL, '下列哪个是合法的 C 语言 main 函数声明？', 'void main()', 'int main(void)', 'main()', 'function main()', 2, TRUE, 'PublicPractice'),
                (2, NULL, 'C 语言中用于输出格式化文本的函数是？', 'scanf', 'printf', 'malloc', 'strlen', 2, TRUE, 'PublicPractice'),
                (3, NULL, '表达式 2 + 3 * 4 的结果是？', '20', '14', '24', '9', 2, TRUE, 'PublicPractice')
                """, cancellationToken);
            schemaReady = true;
        }
        finally { schemaLock.Release(); }
    }

    public async Task<long?> RegisterAsync(string username, string password, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("INSERT INTO app_users(username, password_hash, role) VALUES (@username, @hash, 'Student'); SELECT LAST_INSERT_ID();", connection);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@hash", Passwords.Hash(password));
        try { return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)); }
        catch (MySqlException exception) when (exception.Number == 1062) { return null; }
    }

    public async Task<UserRecord?> FindUserAsync(string username, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT id, username, password_hash, role FROM app_users WHERE username = @username LIMIT 1", connection);
        command.Parameters.AddWithValue("@username", username);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new UserRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
    }

    public async Task<IReadOnlyList<QuestionRecord>> GetQuestionsAsync(CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<QuestionRecord>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT id, stem, option_a, option_b, option_c, option_d FROM questions WHERE is_published = TRUE AND visibility = 'PublicPractice' ORDER BY id", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new QuestionRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        return result;
    }

    public async Task<bool?> SaveAnswerAsync(long userId, long questionId, int selectedOption, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT correct_option FROM questions WHERE id = @id AND is_published = TRUE AND visibility = 'PublicPractice'", connection);
        command.Parameters.AddWithValue("@id", questionId);
        var correct = await command.ExecuteScalarAsync(cancellationToken);
        if (correct is null) return null;
        var isCorrect = Convert.ToInt32(correct) == selectedOption;
        await using var save = new MySqlCommand("INSERT INTO question_answers(user_id, question_id, selected_option, is_correct) VALUES (@user, @question, @selected, @correct) ON DUPLICATE KEY UPDATE selected_option = VALUES(selected_option), is_correct = VALUES(is_correct), created_at = CURRENT_TIMESTAMP", connection);
        save.Parameters.AddWithValue("@user", userId);
        save.Parameters.AddWithValue("@question", questionId);
        save.Parameters.AddWithValue("@selected", selectedOption);
        save.Parameters.AddWithValue("@correct", isCorrect);
        await save.ExecuteNonQueryAsync(cancellationToken);
        return isCorrect;
    }

    public async Task<IReadOnlyList<TeacherQuestionRecord>> GetTeacherQuestionsAsync(long ownerId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<TeacherQuestionRecord>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT id, stem, option_a, option_b, option_c, option_d, correct_option, is_published, visibility, points FROM questions WHERE owner_id=@owner OR owner_id IS NULL ORDER BY id", connection);
        command.Parameters.AddWithValue("@owner", ownerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new TeacherQuestionRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetInt32(6), reader.GetBoolean(7), reader.GetString(8), reader.GetInt32(9)));
        return result;
    }

    public async Task<long> CreateQuestionAsync(long ownerId, QuestionInput input, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("INSERT INTO questions(owner_id, stem, option_a, option_b, option_c, option_d, correct_option, is_published, visibility, points) VALUES (@owner, @stem, @a, @b, @c, @d, @correct, @published, @visibility, COALESCE(@points,1)); SELECT LAST_INSERT_ID();", connection);
        command.Parameters.AddWithValue("@owner", ownerId);
        AddQuestionParameters(command, input);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<int> CreateQuestionsAsync(long ownerId, IReadOnlyList<QuestionInput> inputs, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var input in inputs)
            {
                await using var command = new MySqlCommand("INSERT INTO questions(owner_id, stem, option_a, option_b, option_c, option_d, correct_option, is_published, visibility, points) VALUES (@owner, @stem, @a, @b, @c, @d, @correct, @published, @visibility, COALESCE(@points,1))", connection, transaction);
                command.Parameters.AddWithValue("@owner", ownerId); AddQuestionParameters(command, input);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken); return inputs.Count;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<bool> UpdateQuestionAsync(long ownerId, long id, QuestionInput input, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("UPDATE questions SET stem=@stem, option_a=@a, option_b=@b, option_c=@c, option_d=@d, correct_option=@correct, is_published=@published, visibility=@visibility, points=COALESCE(@points,points) WHERE id=@id AND owner_id=@owner", connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@owner", ownerId);
        AddQuestionParameters(command, input);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<long> CreateExamAsync(long teacherId, ExamCreateRequest request, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        if (request.QuestionIds is null || request.QuestionIds.Count == 0) throw new ArgumentException("考试至少需要一道题目。");
        if (request.QuestionIds.Distinct().Count() != request.QuestionIds.Count) throw new ArgumentException("考试题目不能重复。");
        if (request.StartAt.HasValue && request.EndAt.HasValue && request.EndAt <= request.StartAt)
            throw new ArgumentException("考试结束时间必须晚于开始时间。");
        if (request.StartDelayMinutes is < 0 || request.DurationMinutes is < 0 ||
            ((request.StartDelayMinutes.HasValue || request.DurationMinutes.HasValue) && (request.StartAt.HasValue || request.EndAt.HasValue)))
            throw new ArgumentException("请使用有效的延迟和时长，或直接指定考试起止时间。");
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var startAt = request.StartAt?.UtcDateTime;
        var endAt = request.EndAt?.UtcDateTime;
        if (request.StartDelayMinutes.HasValue || request.DurationMinutes.HasValue)
        {
            await using var clock = new MySqlCommand("SELECT UTC_TIMESTAMP()", connection);
            var now = (DateTime)(await clock.ExecuteScalarAsync(cancellationToken))!;
            startAt = now.AddMinutes(request.StartDelayMinutes ?? 0);
            if (request.DurationMinutes is > 0) endAt = startAt.Value.AddMinutes(request.DurationMinutes.Value);
        }
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var create = new MySqlCommand("INSERT INTO exams(title, created_by, is_published, start_at, end_at) VALUES (@title, @teacher, TRUE, @start, @end); SELECT LAST_INSERT_ID();", connection, transaction);
            create.Parameters.AddWithValue("@title", request.Title.Trim());
            create.Parameters.AddWithValue("@teacher", teacherId);
            create.Parameters.AddWithValue("@start", (object?)startAt ?? DBNull.Value);
            create.Parameters.AddWithValue("@end", (object?)endAt ?? DBNull.Value);
            var examId = Convert.ToInt64(await create.ExecuteScalarAsync(cancellationToken));
            for (var index = 0; index < request.QuestionIds.Count; index++)
            {
                await using var add = new MySqlCommand("INSERT INTO exam_questions(exam_id, question_id, sort_order, points, stem, option_a, option_b, option_c, option_d, correct_option) SELECT @exam, id, @order, points, stem, option_a, option_b, option_c, option_d, correct_option FROM questions WHERE id=@question AND is_published=TRUE AND (owner_id=@teacher OR owner_id IS NULL)", connection, transaction);
                add.Parameters.AddWithValue("@exam", examId);
                add.Parameters.AddWithValue("@teacher", teacherId);
                add.Parameters.AddWithValue("@question", request.QuestionIds[index]);
                add.Parameters.AddWithValue("@order", index + 1);
                if (await add.ExecuteNonQueryAsync(cancellationToken) != 1) throw new ArgumentException("考试包含不存在或未发布的题目。");
            }
            await using var maxScore = new MySqlCommand("SELECT SUM(points) FROM exam_questions WHERE exam_id=@exam", connection, transaction);
            maxScore.Parameters.AddWithValue("@exam", examId);
            if (Convert.ToInt64(await maxScore.ExecuteScalarAsync(cancellationToken)) > int.MaxValue)
                throw new ArgumentException("整份试卷的总分过大。");
            var studentIds = new HashSet<long>(request.StudentIds ?? []);
            if (request.ClassIds is not null)
            {
                foreach (var classId in request.ClassIds.Distinct())
                {
                    await using var members = new MySqlCommand("SELECT cm.user_id FROM class_members cm JOIN classes c ON c.id=cm.class_id WHERE c.id=@class AND c.teacher_id=@teacher AND c.is_open=TRUE", connection, transaction);
                    members.Parameters.AddWithValue("@class", classId); members.Parameters.AddWithValue("@teacher", teacherId);
                    await using var memberReader = await members.ExecuteReaderAsync(cancellationToken);
                    while (await memberReader.ReadAsync(cancellationToken)) studentIds.Add(memberReader.GetInt64(0));
                    await memberReader.DisposeAsync();
                }
            }
            if (studentIds.Count == 0) throw new ArgumentException("考试必须指定至少一个学生或班级，避免题目意外公开。");
            foreach (var studentId in studentIds)
            {
                await using var assign = new MySqlCommand("INSERT INTO exam_assignments(exam_id, user_id) SELECT @exam, id FROM app_users WHERE id=@student AND role='Student'", connection, transaction);
                assign.Parameters.AddWithValue("@exam", examId); assign.Parameters.AddWithValue("@student", studentId);
                if (await assign.ExecuteNonQueryAsync(cancellationToken) != 1) throw new ArgumentException("考试包含不存在的学生账号。");
            }
            await transaction.CommitAsync(cancellationToken);
            return examId;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<IReadOnlyList<ExamSummary>> GetExamsAsync(long studentId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<ExamSummary>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT e.id, e.title, COUNT(eq.question_id), e.start_at, e.end_at, MAX(a.status), UTC_TIMESTAMP(6), MAX(a.id) FROM exams e JOIN exam_assignments ea ON ea.exam_id=e.id LEFT JOIN exam_questions eq ON eq.exam_id=e.id LEFT JOIN exam_attempts a ON a.exam_id=e.id AND a.user_id=ea.user_id WHERE e.is_published=TRUE AND ea.user_id=@student GROUP BY e.id, e.title, e.start_at, e.end_at ORDER BY e.id DESC", connection);
        command.Parameters.AddWithValue("@student", studentId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(new ExamSummary(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetDateTime(3), reader.IsDBNull(4) ? null : reader.GetDateTime(4), reader.IsDBNull(5) ? null : reader.GetString(5), AsUtc(reader.GetDateTime(6)), reader.IsDBNull(7) ? null : reader.GetInt64(7)));
        return result;
    }

    public async Task<IReadOnlyList<TeacherExamRecord>> GetTeacherExamsAsync(long teacherId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<TeacherExamRecord>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT e.id, e.title, COUNT(DISTINCT eq.question_id), COUNT(DISTINCT ea.user_id), e.start_at, e.end_at, e.grades_published, COALESCE(e.end_at <= UTC_TIMESTAMP(), FALSE) FROM exams e LEFT JOIN exam_questions eq ON eq.exam_id=e.id LEFT JOIN exam_assignments ea ON ea.exam_id=e.id WHERE e.created_by=@teacher GROUP BY e.id, e.title, e.start_at, e.end_at, e.grades_published ORDER BY e.id DESC", connection);
        command.Parameters.AddWithValue("@teacher", teacherId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(new TeacherExamRecord(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3), reader.IsDBNull(4) ? null : reader.GetDateTime(4), reader.IsDBNull(5) ? null : reader.GetDateTime(5), reader.GetBoolean(6), reader.GetBoolean(7)));
        return result;
    }

    public async Task<GradeAnswerResult?> GradeTeacherAnswerAsync(long teacherId, long attemptId, long questionId, int points, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var lookup = new MySqlCommand("SELECT eq.points, a.status, e.grades_published FROM exam_attempts a JOIN exams e ON e.id=a.exam_id JOIN exam_questions eq ON eq.exam_id=a.exam_id AND eq.question_id=@question WHERE a.id=@attempt AND e.created_by=@teacher FOR UPDATE", connection, transaction);
        lookup.Parameters.AddWithValue("@question", questionId);
        lookup.Parameters.AddWithValue("@attempt", attemptId);
        lookup.Parameters.AddWithValue("@teacher", teacherId);
        await using var reader = await lookup.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var maxPoints = reader.GetInt32(0);
        var status = reader.GetString(1);
        var gradesPublished = reader.GetBoolean(2);
        await reader.DisposeAsync();
        if (!string.Equals(status, "Submitted", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("只能批改已交卷答卷。");
        if (gradesPublished) throw new ArgumentException("成绩发布后不能继续批改。");
        if (points < 0 || points > maxPoints) throw new ArgumentException($"分值必须在 0 到 {maxPoints} 之间。");
        if (points != 0 && points != maxPoints) throw new ArgumentException("当前单选题只允许 0 分或满分。");
        await using var updateAnswer = new MySqlCommand("INSERT INTO exam_grade_overrides(attempt_id, question_id, points) VALUES (@attempt,@question,@points) ON DUPLICATE KEY UPDATE points=@points", connection, transaction);
        updateAnswer.Parameters.AddWithValue("@points", points);
        updateAnswer.Parameters.AddWithValue("@attempt", attemptId);
        updateAnswer.Parameters.AddWithValue("@question", questionId);
        await updateAnswer.ExecuteNonQueryAsync(cancellationToken);
        await using var updateAttempt = new MySqlCommand("""
            UPDATE exam_attempts t SET total_score=(
                SELECT COALESCE(SUM(COALESCE(g.points, a.points, 0)),0)
                FROM exam_questions eq
                LEFT JOIN exam_answers a ON a.attempt_id=@attempt AND a.question_id=eq.question_id
                LEFT JOIN exam_grade_overrides g ON g.attempt_id=@attempt AND g.question_id=eq.question_id
                WHERE eq.exam_id=t.exam_id
            ) WHERE t.id=@attempt
            """, connection, transaction);
        updateAttempt.Parameters.AddWithValue("@attempt", attemptId);
        await updateAttempt.ExecuteNonQueryAsync(cancellationToken);
        await using var readScore = new MySqlCommand("SELECT COALESCE(total_score,0) FROM exam_attempts WHERE id=@attempt", connection, transaction);
        readScore.Parameters.AddWithValue("@attempt", attemptId);
        var totalScore = Convert.ToInt32(await readScore.ExecuteScalarAsync(cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return new GradeAnswerResult(totalScore);
    }

    public async Task<bool?> PublishTeacherGradesAsync(long teacherId, long examId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var update = new MySqlCommand("""
            UPDATE exams SET grades_published=TRUE
            WHERE id=@exam AND created_by=@teacher AND end_at <= UTC_TIMESTAMP(6)
              AND NOT EXISTS (SELECT 1 FROM exam_attempts a WHERE a.exam_id=@exam AND a.status='InProgress')
            """, connection);
        update.Parameters.AddWithValue("@exam", examId);
        update.Parameters.AddWithValue("@teacher", teacherId);
        if (await update.ExecuteNonQueryAsync(cancellationToken) > 0) return true;
        await using var check = new MySqlCommand("SELECT grades_published FROM exams WHERE id=@exam AND created_by=@teacher", connection);
        check.Parameters.AddWithValue("@exam", examId);
        check.Parameters.AddWithValue("@teacher", teacherId);
        return (await check.ExecuteScalarAsync(cancellationToken)) is { } published ? Convert.ToBoolean(published) : null;
    }

    public async Task<bool> CloseUntimedExamAsync(long teacherId, long examId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            UPDATE exams SET end_at=UTC_TIMESTAMP()
            WHERE id=@exam AND created_by=@teacher AND end_at IS NULL
              AND (start_at IS NULL OR start_at <= UTC_TIMESTAMP())
            """, connection);
        command.Parameters.AddWithValue("@exam", examId);
        command.Parameters.AddWithValue("@teacher", teacherId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) return false;
        await FinalizeAttemptsAsync(connection, dueOnly: true, cancellationToken, examId: examId);
        return true;
    }

    public async Task<IReadOnlyList<TeacherAttemptRecord>> GetTeacherAttemptsAsync(long teacherId, long examId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<TeacherAttemptRecord>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT a.id, a.exam_id, u.username, a.status, a.total_score, a.submitted_at, COUNT(DISTINCT ans.question_id), COUNT(DISTINCT eq.question_id) FROM exam_attempts a JOIN exams e ON e.id=a.exam_id JOIN app_users u ON u.id=a.user_id JOIN exam_questions eq ON eq.exam_id=a.exam_id LEFT JOIN exam_answers ans ON ans.attempt_id=a.id AND ans.question_id=eq.question_id WHERE e.id=@exam AND e.created_by=@teacher GROUP BY a.id, a.exam_id, u.username, a.status, a.total_score, a.submitted_at ORDER BY a.submitted_at IS NULL, a.submitted_at DESC, a.id DESC", connection);
        command.Parameters.AddWithValue("@exam", examId);
        command.Parameters.AddWithValue("@teacher", teacherId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TeacherAttemptRecord(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetDateTime(5), reader.GetInt32(6), reader.GetInt32(7)));
        }
        return result;
    }

    public async Task<IReadOnlyList<ReviewAnswerRecord>?> GetReviewAnswersAsync(long userId, long attemptId, bool teacher, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<ReviewAnswerRecord>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            SELECT eq.question_id, eq.sort_order, eq.stem, eq.option_a, eq.option_b, eq.option_c, eq.option_d,
                   COALESCE(a.selected_option,0), eq.correct_option, COALESCE(g.points,a.points,0),
                   eq.points, g.attempt_id IS NOT NULL
            FROM exam_attempts t
            JOIN exams e ON e.id=t.exam_id
            JOIN exam_questions eq ON eq.exam_id=t.exam_id
            LEFT JOIN exam_answers a ON a.attempt_id=t.id AND a.question_id=eq.question_id
            LEFT JOIN exam_grade_overrides g ON g.attempt_id=t.id AND g.question_id=eq.question_id
            WHERE t.id=@attempt AND t.status='Submitted'
              AND ((@teacher AND e.created_by=@user)
                   OR (NOT @teacher AND t.user_id=@user AND e.grades_published AND e.end_at <= UTC_TIMESTAMP()))
            ORDER BY eq.sort_order
            """, connection);
        command.Parameters.AddWithValue("@attempt", attemptId);
        command.Parameters.AddWithValue("@user", userId);
        command.Parameters.AddWithValue("@teacher", teacher);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ReviewAnswerRecord(reader.GetInt64(0), reader.GetInt32(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10), reader.GetBoolean(11)));
        }
        return result.Count == 0 ? null : result;
    }

    public async Task<AttemptRecord?> StartAttemptAsync(long userId, long examId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await FinalizeAttemptsAsync(connection, dueOnly: true, cancellationToken, userId: userId, examId: examId);
        await using var create = new MySqlCommand("""
            INSERT IGNORE INTO exam_attempts(exam_id, user_id)
            SELECT e.id, ea.user_id FROM exams e JOIN exam_assignments ea ON ea.exam_id=e.id AND ea.user_id=@user
            WHERE e.id=@exam AND e.is_published=TRUE
              AND (e.start_at IS NULL OR UTC_TIMESTAMP() >= e.start_at) AND (e.end_at IS NULL OR UTC_TIMESTAMP() < e.end_at)
            """, connection);
        create.Parameters.AddWithValue("@exam", examId);
        create.Parameters.AddWithValue("@user", userId);
        await create.ExecuteNonQueryAsync(cancellationToken);
        return await ReadAttemptAsync(connection, userId, cancellationToken, examId: examId);
    }

    public async Task<AttemptRecord?> GetAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await FinalizeAttemptsAsync(connection, dueOnly: true, cancellationToken, userId, attemptId);
        return await ReadAttemptAsync(connection, userId, cancellationToken, attemptId: attemptId);
    }

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static async Task<AttemptRecord?> ReadAttemptAsync(MySqlConnection connection, long userId,
        CancellationToken cancellationToken, long? attemptId = null, long? examId = null)
    {
        await using var read = new MySqlCommand("""
            SELECT a.id, a.exam_id, a.status, IF(e.grades_published, a.total_score, NULL), e.end_at, UTC_TIMESTAMP(6)
            FROM exam_attempts a JOIN exams e ON e.id=a.exam_id
            WHERE a.user_id=@user AND e.is_published=TRUE
              AND (@attempt IS NULL OR a.id=@attempt) AND (@exam IS NULL OR a.exam_id=@exam)
            """, connection);
        read.Parameters.AddWithValue("@attempt", (object?)attemptId ?? DBNull.Value);
        read.Parameters.AddWithValue("@exam", (object?)examId ?? DBNull.Value);
        read.Parameters.AddWithValue("@user", userId);
        await using var reader = await read.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new AttemptRecord(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetInt32(3),
            reader.IsDBNull(4) ? null : AsUtc(reader.GetDateTime(4)),
            AsUtc(reader.GetDateTime(5)));
    }

    public async Task<IReadOnlyList<AttemptQuestionRecord>?> GetAttemptQuestionsAsync(long userId, long attemptId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<AttemptQuestionRecord>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT eq.question_id, eq.stem, eq.option_a, eq.option_b, eq.option_c, eq.option_d, a.selected_option, COALESCE(a.revision,0) FROM exam_attempts t JOIN exams e ON e.id=t.exam_id JOIN exam_questions eq ON eq.exam_id=t.exam_id LEFT JOIN exam_answers a ON a.attempt_id=t.id AND a.question_id=eq.question_id WHERE t.id=@attempt AND t.user_id=@user AND t.status='InProgress' AND (e.end_at IS NULL OR UTC_TIMESTAMP() < e.end_at) ORDER BY eq.sort_order", connection);
        command.Parameters.AddWithValue("@attempt", attemptId);
        command.Parameters.AddWithValue("@user", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(new AttemptQuestionRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetInt32(6), reader.GetInt64(7)));
        return result.Count == 0 ? null : result;
    }

    public async Task<ExamAnswerSave> SaveExamAnswerAsync(long userId, long attemptId, ExamAnswerRequest request, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var lookup = new MySqlCommand("""
            SELECT eq.correct_option, eq.points, e.start_at, e.end_at, a.selected_option, COALESCE(a.revision,0)
            FROM exam_attempts t
            JOIN exams e ON e.id=t.exam_id
            JOIN exam_questions eq ON eq.exam_id=t.exam_id AND eq.question_id=@question
            LEFT JOIN exam_answers a ON a.attempt_id=t.id AND a.question_id=eq.question_id
            WHERE t.id=@attempt AND t.user_id=@user AND t.status='InProgress'
            FOR UPDATE
            """, connection, transaction);
        lookup.Parameters.AddWithValue("@question", request.QuestionId);
        lookup.Parameters.AddWithValue("@attempt", attemptId);
        lookup.Parameters.AddWithValue("@user", userId);
        await using var reader = await lookup.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return ExamAnswerSave.Unavailable;
        var correct = reader.GetInt32(0) == request.SelectedOption;
        var points = correct ? reader.GetInt32(1) : 0;
        DateTime? start = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
        DateTime? end = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
        var previousOption = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
        var previousRevision = reader.GetInt64(5);
        await reader.DisposeAsync();

        // A request may have waited for the row lock across the deadline.
        await using var clock = new MySqlCommand("SELECT UTC_TIMESTAMP(6)", connection, transaction);
        var now = (DateTime)(await clock.ExecuteScalarAsync(cancellationToken))!;
        if (now < start || now >= end) return ExamAnswerSave.Unavailable;
        var revision = request.Revision ?? previousRevision + 1;
        if (request.Revision is long requestedRevision)
        {
            if (requestedRevision == previousRevision && request.SelectedOption == previousOption) return ExamAnswerSave.Saved;
            if (requestedRevision <= previousRevision) return ExamAnswerSave.Stale;
        }

        await using var save = new MySqlCommand("""
            INSERT INTO exam_answers(attempt_id, question_id, selected_option, is_correct, points, revision)
            VALUES (@attempt,@question,@selected,@correct,@points,@revision)
            ON DUPLICATE KEY UPDATE selected_option=@selected, is_correct=@correct, points=@points, revision=@revision
            """, connection, transaction);
        save.Parameters.AddWithValue("@attempt", attemptId);
        save.Parameters.AddWithValue("@question", request.QuestionId);
        save.Parameters.AddWithValue("@selected", request.SelectedOption);
        save.Parameters.AddWithValue("@correct", correct);
        save.Parameters.AddWithValue("@points", points);
        save.Parameters.AddWithValue("@revision", revision);
        await save.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ExamAnswerSave.Saved;
    }

    public async Task<AttemptResult?> SubmitAttemptAsync(long userId, long attemptId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await FinalizeAttemptsAsync(connection, dueOnly: false, cancellationToken, userId, attemptId);
        var attempt = await ReadAttemptAsync(connection, userId, cancellationToken, attemptId: attemptId);
        return attempt?.Status == "Submitted" ? new AttemptResult(attempt.Id, attempt.ExamId) : null;
    }

    public async Task FinalizeExpiredAttemptsAsync(CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await FinalizeAttemptsAsync(connection, dueOnly: true, cancellationToken);
    }

    private static async Task FinalizeAttemptsAsync(MySqlConnection connection, bool dueOnly,
        CancellationToken cancellationToken, long? userId = null, long? attemptId = null, long? examId = null)
    {
        await using var update = new MySqlCommand("""
            UPDATE exam_attempts a
            JOIN exams e ON e.id=a.exam_id
            SET a.status='Submitted', a.submitted_at=CASE WHEN e.end_at <= UTC_TIMESTAMP(6) THEN e.end_at ELSE CURRENT_TIMESTAMP END,
                a.total_score=(SELECT COALESCE(SUM(points),0) FROM exam_answers WHERE attempt_id=a.id)
            WHERE a.status='InProgress'
              AND (@attempt IS NULL OR a.id=@attempt) AND (@user IS NULL OR a.user_id=@user)
              AND (@exam IS NULL OR e.id=@exam)
              AND ((e.end_at <= UTC_TIMESTAMP(6)) OR (NOT @dueOnly AND (e.start_at IS NULL OR e.start_at <= UTC_TIMESTAMP(6))))
            """, connection);
        update.Parameters.AddWithValue("@attempt", (object?)attemptId ?? DBNull.Value);
        update.Parameters.AddWithValue("@user", (object?)userId ?? DBNull.Value);
        update.Parameters.AddWithValue("@exam", (object?)examId ?? DBNull.Value);
        update.Parameters.AddWithValue("@dueOnly", dueOnly);
        await update.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long> CreateClassAsync(long teacherId, string name, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("INSERT INTO classes(name, join_code, teacher_id) VALUES (@name, UPPER(SUBSTRING(REPLACE(UUID(),'-',''),1,8)), @teacher); SELECT LAST_INSERT_ID();", connection);
        command.Parameters.AddWithValue("@name", name.Trim()); command.Parameters.AddWithValue("@teacher", teacherId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<ClassRecord>> GetTeacherClassesAsync(long teacherId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<ClassRecord>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT c.id, c.name, c.join_code, c.is_open, COUNT(cm.user_id) FROM classes c LEFT JOIN class_members cm ON cm.class_id=c.id WHERE c.teacher_id=@teacher GROUP BY c.id,c.name,c.join_code,c.is_open ORDER BY c.id DESC", connection);
        command.Parameters.AddWithValue("@teacher", teacherId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(new ClassRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3), reader.GetInt32(4)));
        return result;
    }

    public async Task<int> AddClassMembersAsync(long teacherId, long classId, IReadOnlyList<string> usernames, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var owner = new MySqlCommand("SELECT COUNT(*) FROM classes WHERE id=@class AND teacher_id=@teacher AND is_open=TRUE", connection, transaction);
            owner.Parameters.AddWithValue("@class", classId); owner.Parameters.AddWithValue("@teacher", teacherId);
            if (Convert.ToInt32(await owner.ExecuteScalarAsync(cancellationToken)) != 1) throw new ArgumentException("班级不存在、已关闭或不属于当前教师。");
            var added = 0;
            foreach (var username in usernames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await using var add = new MySqlCommand("INSERT IGNORE INTO class_members(class_id,user_id) SELECT @class,id FROM app_users WHERE username=@username AND role='Student'", connection, transaction);
                add.Parameters.AddWithValue("@class", classId); add.Parameters.AddWithValue("@username", username);
                added += await add.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken); return added;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<IReadOnlyList<GradeRecord>> GetGradesAsync(long studentId, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new List<GradeRecord>();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("SELECT a.id,e.title,a.total_score,a.submitted_at FROM exam_attempts a JOIN exams e ON e.id=a.exam_id WHERE a.user_id=@student AND a.status='Submitted' AND e.grades_published=TRUE AND e.end_at <= UTC_TIMESTAMP() ORDER BY a.submitted_at DESC", connection);
        command.Parameters.AddWithValue("@student", studentId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(new GradeRecord(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2), reader.GetDateTime(3)));
        return result;
    }

    private static void AddQuestionParameters(MySqlCommand command, QuestionInput input)
    {
        command.Parameters.AddWithValue("@stem", input.Stem.Trim()); command.Parameters.AddWithValue("@a", input.OptionA.Trim()); command.Parameters.AddWithValue("@b", input.OptionB.Trim()); command.Parameters.AddWithValue("@c", input.OptionC.Trim()); command.Parameters.AddWithValue("@d", input.OptionD.Trim()); command.Parameters.AddWithValue("@correct", input.CorrectOption); command.Parameters.AddWithValue("@published", input.IsPublished); command.Parameters.AddWithValue("@visibility", input.Visibility);
        command.Parameters.AddWithValue("@points", (object?)input.Points ?? DBNull.Value);
    }

    private static async Task EnsureColumnAsync(MySqlConnection connection, string table, string column, string definition, CancellationToken cancellationToken)
    {
        await using var check = new MySqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@table AND COLUMN_NAME=@column", connection);
        check.Parameters.AddWithValue("@table", table); check.Parameters.AddWithValue("@column", column);
        if (Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken)) == 0)
            await ExecuteAsync(connection, $"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition}", cancellationToken);
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
