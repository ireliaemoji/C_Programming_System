using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq; 
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class ExamClientDemo : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:5000";
    private UiRoot uiRoot;
    private string token;
    private string role;
    private string signedInName;
    private readonly List<ExamDto> exams = new();
    private readonly List<ClassDto> classes = new();
    private QuestionDto[] teacherQuestions = Array.Empty<QuestionDto>();
    private TeacherExamDto[] teacherExams = Array.Empty<TeacherExamDto>();
    private AttemptQuestionDto[] attemptQuestions = Array.Empty<AttemptQuestionDto>();
    private AttemptDto attempt;
    private ExamDto activeExam;
    private DateTimeOffset? activeEnd;
    private int activeQuestion;
    private long selectedClassId;
    private bool submitting;
    private bool startingExam;
    private bool reviewMutationPending;
    private Coroutine autoSave;
    private ExamDraftFile examDraft;
    private long nextAnswerRevision;
    private DateTimeOffset? serverNow;
    private TimeSpan serverClockOffset;
    private bool refreshingExpiredAttempt;
    private readonly Dictionary<long, PendingExamAnswer> pendingAnswers = new();

    private void Awake() { BuildCanvas(); ShowLogin(); }

    private void Update()
    {
        if (activeExam == null || !activeEnd.HasValue || submitting) return;
        var remaining = activeEnd.Value - CurrentServerTime;
        if (remaining <= TimeSpan.Zero) { if (!refreshingExpiredAttempt) { refreshingExpiredAttempt = true; SetStatus("考试时间已结束，正在同步最后状态…"); StartCoroutine(RefreshExpiredAttempt()); } return; }
        foreach (var v in uiRoot.Views) v.timer.text = $"剩余 {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }

    private void BuildCanvas()
    {
        uiRoot = UiRoot.Create();
        foreach (var view in uiRoot.Views) Bind(view);
        uiRoot.LayoutChanged += CopyFormValues;
    }

    private void Bind(UiLayoutView v)
    {
        v.login.onClick.AddListener(() => StartCoroutine(Login()));
        v.register.onClick.AddListener(() => StartCoroutine(Register()));
        v.logout.onClick.AddListener(() => QueueUi(ShowLogin));
        v.heroAction.onClick.AddListener(() => QueueUi(ShowExamList));
        v.refreshExams.onClick.AddListener(() => { if (role == "Teacher") StartCoroutine(LoadTeacherData()); else StartCoroutine(LoadExams()); });
        for (var i = 0; i < v.navigationButtons.Length; i++)
        {
            var index = i;
            v.navigationButtons[i].onClick.AddListener(() => Navigate(index));
        }
        v.shortcuts[0].onClick.AddListener(() => QueueUi(ShowExamList));
        v.shortcuts[1].interactable = v.shortcuts[2].interactable = false;
        v.shortcuts[3].onClick.AddListener(() => StartCoroutine(LoadGrades()));
        var teacherPages = new[] { UiPage.Classes, UiPage.Questions, UiPage.Publish, UiPage.Exams };
        for (var i = 0; i < v.teacherShortcuts.Length; i++)
        {
            var page = teacherPages[i];
            v.teacherShortcuts[i].onClick.AddListener(() => QueueUi(() => ShowPage(page)));
        }
        v.refreshTeacher.onClick.AddListener(() => StartCoroutine(LoadTeacherData()));
        v.createClass.onClick.AddListener(() => StartCoroutine(CreateClass(v.className.text)));
        v.addMembers.onClick.AddListener(() => StartCoroutine(AddMembers(v.members.text)));
        v.importQuestions.onClick.AddListener(() => StartCoroutine(ImportQuestions(v.importPath.text)));
        v.publishExam.onClick.AddListener(() => StartCoroutine(CreateExam(v.examName.text, v.questionIds.text, v.studentIds.text, v.classIds.text, v.delayMinutes.text, v.durationMinutes.text)));
        for (var i = 0; i < v.options.Length; i++)
        {
            var option = i + 1;
            v.options[i].onClick.AddListener(() => SelectOption(option));
        }
        v.previousQuestion.onClick.AddListener(() => ChangeQuestion(-1));
        v.nextQuestion.onClick.AddListener(() => ChangeQuestion(1));
        v.submitExam.onClick.AddListener(() =>
        {
            v.submitSummary.text = $"已答 {attemptQuestions.Count(q => q.selectedOption > 0)} / {attemptQuestions.Length} 题。交卷后本次作答将结束。";
            v.submitDialog.SetActive(true);
        });
        v.cancelSubmit.onClick.AddListener(() => v.submitDialog.SetActive(false));
        v.confirmSubmit.onClick.AddListener(() => { v.submitDialog.SetActive(false); StartCoroutine(SubmitExam()); });
        v.leaveExam.onClick.AddListener(() => QueueUi(() => { activeExam = null; ShowStudentHome(); }));
        v.openQuestionNumbers.onClick.AddListener(() => v.questionDrawer.SetActive(true));
        v.closeQuestionNumbers.onClick.AddListener(() => v.questionDrawer.SetActive(false));
        v.environment.text = "请使用教师分配的账号参加考试。";
    }

    private static void CopyFormValues(UiLayoutView from, UiLayoutView to)
    {
        var source = new[] { from.username, from.password, from.className, from.members, from.importPath, from.examName, from.questionIds, from.studentIds, from.classIds, from.delayMinutes, from.durationMinutes };
        var target = new[] { to.username, to.password, to.className, to.members, to.importPath, to.examName, to.questionIds, to.studentIds, to.classIds, to.delayMinutes, to.durationMinutes };
        for (var i = 0; i < source.Length; i++) target[i].SetTextWithoutNotify(source[i].text);
    }

    private void ShowPage(UiPage page)
    {
        uiRoot.Show(page, role == "Teacher", signedInName);
        foreach (var v in uiRoot.Views)
        {
            v.examList.gameObject.SetActive(role != "Teacher");
            v.teacherExamList.gameObject.SetActive(role == "Teacher");
        }
    }

    private void ShowLogin()
    {
        StopAutoSave();
        activeExam = null; attempt = null; activeEnd = null; startingExam = false; examDraft = null; serverNow = null; serverClockOffset = TimeSpan.Zero; refreshingExpiredAttempt = false;
        token = role = signedInName = null;
        ShowPage(UiPage.Login);
        SetStatus("请输入账号和密码。");
    }

    private void ShowTeacherHome()
    {
        StopAutoSave(); ShowPage(UiPage.TeacherHome); StartCoroutine(LoadTeacherData());
    }

    private void ShowStudentHome()
    {
        StopAutoSave(); ShowPage(UiPage.StudentHome); StartCoroutine(LoadExams());
    }

    private void ShowExamList() { ShowPage(UiPage.Exams); }

    private string WindowText(ExamDto exam) => string.IsNullOrEmpty(exam.startAt) && string.IsNullOrEmpty(exam.endAt) ? "开放时间不限" : $"{exam.startAt ?? "立即"} 至 {exam.endAt ?? "不限"}";

    private string ExamActionLabel(ExamDto exam)
    {
        if (string.Equals(exam.attemptStatus, "Submitted", StringComparison.OrdinalIgnoreCase)) return "已完成";
        if (ParseDate(exam.endAt).HasValue && ParseDate(exam.endAt).Value <= CurrentServerTime) return "已结束";
        if (ParseDate(exam.startAt).HasValue && ParseDate(exam.startAt).Value > CurrentServerTime) return "未开始";
        return "开始考试";
    }

    private bool CanStartExam(ExamDto exam) => ExamActionLabel(exam) == "开始考试";

    private void ShowExam()
    {
        if (activeExam == null) return;
        ShowPage(UiPage.Exam);
        foreach (var v in uiRoot.Views)
        {
            v.examTitle.text = activeExam.title;
            v.timer.text = activeEnd.HasValue ? "计算剩余时间…" : "不限时";
            v.questionNumbers.Clear();
            v.previousQuestion.interactable = !submitting && activeQuestion > 0;
            v.nextQuestion.interactable = !submitting && activeQuestion + 1 < attemptQuestions.Length;
            v.submitExam.interactable = !submitting;
            v.questionProgress.text = $"第 {(attemptQuestions.Length == 0 ? 0 : activeQuestion + 1)} / {attemptQuestions.Length} 题";
            if (attemptQuestions.Length == 0) { v.questionStem.text = "暂无题目或考试已结束。"; foreach (var b in v.options) b.gameObject.SetActive(false); continue; }
            var q = attemptQuestions[activeQuestion];
            v.questionStem.text = q.stem;
            var labels = new[] { q.optionA, q.optionB, q.optionC, q.optionD };
            for (var i = 0; i < 4; i++)
            {
                v.options[i].gameObject.SetActive(true);
                v.options[i].interactable = !submitting;
                v.optionLabels[i].text = $"{(char)('A' + i)}. {labels[i]}" + (q.selectedOption == i + 1 ? "  ✓" : "");
                v.options[i].GetComponent<Image>().color = q.selectedOption == i + 1 ? new Color(.78f, .9f, 1f) : Color.white;
            }
            for (var i = 0; i < attemptQuestions.Length; i++)
            {
                var index = i;
                var mark = attemptQuestions[i].selectedOption > 0 ? "已答" : "未答";
                v.questionNumbers.Add($"{i + 1:00}  {mark}", "", i == activeQuestion ? "当前" : "选择", () => { activeQuestion = index; QueueUi(ShowExam); });
            }
        }
    }

    private void ChangeQuestion(int offset)
    {
        if (submitting) return;
        activeQuestion = Mathf.Clamp(activeQuestion + offset, 0, Mathf.Max(0, attemptQuestions.Length - 1));
        QueueUi(ShowExam);
    }

    private void SelectOption(int option) { if (submitting) return; attemptQuestions[activeQuestion].selectedOption = option; StartCoroutine(SaveCurrentAnswer()); QueueUi(ShowExam); }
    private IEnumerator AutoSaveLoop()
    {
        var retryDelay = 1f;
        while (activeExam != null && !submitting)
        {
            yield return new WaitForSecondsRealtime(retryDelay);
            if (activeExam == null || submitting) yield break;
            var synced = false;
            yield return SyncPendingAnswers(result => synced = result);
            retryDelay = synced || pendingAnswers.Count == 0 ? 1f : retryDelay >= 60f ? 60f : retryDelay >= 10f ? 30f : retryDelay >= 5f ? 10f : retryDelay >= 2f ? 5f : 2f;
        }
    }
    private IEnumerator SaveCurrentAnswer()
    {
        if (attempt == null || activeQuestion >= attemptQuestions.Length) yield break; var q = attemptQuestions[activeQuestion]; if (q.selectedOption < 1 || q.selectedOption > 4) yield break;
        var revision = ++nextAnswerRevision;
        q.revision = revision;
        pendingAnswers[q.id] = new PendingExamAnswer { questionId = q.id, selectedOption = q.selectedOption, revision = revision };
        WriteDraft();
        var synced = false;
        yield return UploadPendingAnswer(pendingAnswers[q.id], result => synced = result);
        if (synced && pendingAnswers.TryGetValue(q.id, out var current) && current.revision == revision)
        {
            pendingAnswers.Remove(q.id);
            WriteDraft();
            SetStatus("答案已自动保存。");
        }
    }
    private IEnumerator SubmitExam()
    {
        if (submitting || attempt == null) yield break; submitting = true; StopAutoSave();
        yield return Send($"/api/exam-attempts/{attempt.id}/submit", null, "POST", token, _ => { try { examDraft?.Write(Array.Empty<PendingExamAnswer>()); } catch { } examDraft = null; SetStatus("交卷成功，成绩将在教师开放后显示。"); QueueUi(() => { activeExam = null; ShowStudentHome(); }); }); submitting = false;
    }
    private void StopAutoSave() { if (autoSave != null) StopCoroutine(autoSave); autoSave = null; }

    private IEnumerator Login()
    {
        yield return Send("/api/auth/login", JsonUtility.ToJson(new LoginRequest { username = uiRoot.View.username.text, password = uiRoot.View.password.text }), "POST", null, response => { var auth = JsonUtility.FromJson<AuthDto>(response); token = auth.token; role = auth.role; signedInName = auth.username; SetStatus($"已登录：{auth.username}（{role}）"); QueueUi(() => { if (role == "Teacher") ShowTeacherHome(); else ShowStudentHome(); }); });
    }
    private IEnumerator Register() { yield return Send("/api/auth/register", JsonUtility.ToJson(new LoginRequest { username = uiRoot.View.username.text, password = uiRoot.View.password.text }), "POST", null, _ => SetStatus("注册成功，请登录。")); }

    private IEnumerator LoadTeacherData()
    {
        yield return Send("/api/teacher/classes", null, "GET", token, response => { classes.Clear(); classes.AddRange(JsonUtility.FromJson<ClassEnvelope>($"{{\"items\":{response}}}").items ?? Array.Empty<ClassDto>()); });
        yield return Send("/api/teacher/questions", null, "GET", token, response => teacherQuestions = JsonUtility.FromJson<QuestionEnvelope>($"{{\"items\":{response}}}").items ?? Array.Empty<QuestionDto>());
        yield return Send("/api/teacher/exams", null, "GET", token, response => teacherExams = JsonUtility.FromJson<TeacherExamEnvelope>($"{{\"items\":{response}}}").items ?? Array.Empty<TeacherExamDto>());
        QueueUi(ShowTeacherHomeWithoutReload);
    }
    private void ShowTeacherHomeWithoutReload()
    {
        foreach (var v in uiRoot.Views)
        {
            v.classCount.text = classes.Count.ToString();
            v.questionCount.text = teacherQuestions.Length.ToString();
            v.teacherExamCount.text = teacherExams.Length.ToString();
            v.classList.Clear(); v.questionList.Clear(); v.teacherExamList.Clear();
            var selected = classes.FirstOrDefault(c => c.id == selectedClassId);
            v.selectedClass.text = selected == null ? "请选择下方班级" : "当前班级：" + selected.name;
            foreach (var item in classes)
            {
                var id = item.id;
                v.classList.Add(item.name, $"班级 ID：{id}　成员：{item.memberCount}", id == selectedClassId ? "已选择" : "选择班级", () =>
                {
                    selectedClassId = id;
                    foreach (var view in uiRoot.Views) view.classIds.text = id.ToString();
                    QueueUi(ShowTeacherHomeWithoutReload);
                });
            }
            foreach (var q in teacherQuestions) v.questionList.Add($"#{q.id}　{q.stem}", "单选题");
            foreach (var exam in teacherExams)
            {
                var selectedExam = exam;
                v.teacherExamList.Add(exam.title, $"#{exam.id}　{exam.questionCount} 题　分配 {exam.assignmentCount} 人", "查看答卷", () => StartCoroutine(LoadTeacherAttempts(selectedExam)));
            }
            if (string.IsNullOrWhiteSpace(v.questionIds.text)) v.questionIds.text = string.Join(",", teacherQuestions.Take(3).Select(q => q.id));
        }
    }

    private IEnumerator LoadTeacherAttempts(TeacherExamDto exam)
    {
        var session = token;
        var refreshed = false;
        yield return Send("/api/teacher/exams", null, "GET", session, response =>
        {
            if (session != token) return;
            var current = JsonUtility.FromJson<TeacherExamEnvelope>($"{{\"items\":{response}}}").items;
            var updated = current?.FirstOrDefault(item => item.id == exam.id);
            if (updated == null) return;
            exam = updated;
            teacherExams = current;
            refreshed = true;
        });
        if (!refreshed || session != token) yield break;
        yield return Send($"/api/teacher/exams/{exam.id}/attempts", null, "GET", session, response =>
        {
            var attempts = JsonUtility.FromJson<TeacherAttemptEnvelope>($"{{\"items\":{response}}}").items ?? Array.Empty<TeacherAttemptDto>();
            QueueUi(() => { if (session == token) ShowTeacherAttempts(exam, attempts); });
        });
    }

    private void ShowTeacherAttempts(TeacherExamDto exam, TeacherAttemptDto[] attempts)
    {
        foreach (var v in uiRoot.Views)
        {
            v.teacherExamList.Clear();
            v.teacherExamList.Add(exam.title, "学生答卷", "返回", () => QueueUi(ShowTeacherHomeWithoutReload));
            if (exam.gradesPublished)
                v.teacherExamList.Add("已开放学生查看", "学生可以在成绩页查看自己的答卷。");
            else if (exam.hasEnded)
                v.teacherExamList.Add("批阅完成后开放", "开放后学生可查看答卷，分值将锁定。", "开放查看", () => QueueUi(() => ShowExamActionConfirmation(exam, false, attempts)));
            else if (string.IsNullOrEmpty(exam.endAt))
                v.teacherExamList.Add("考试未设置截止时间", "结束考试后可以确认批阅完成并开放查看。", "结束考试", () => QueueUi(() => ShowExamActionConfirmation(exam, true, attempts)));
            else
                v.teacherExamList.Add("等待考试结束", "考试结束后可开放学生查看。");

            foreach (var attemptItem in attempts)
            {
                var submitted = attemptItem.status == "Submitted";
                var detail = submitted
                    ? $"{attemptItem.totalScore} 分　已答 {attemptItem.answeredCount}/{attemptItem.questionCount} 题　提交于 {attemptItem.submittedAt}"
                    : $"未交卷　已答 {attemptItem.answeredCount}/{attemptItem.questionCount} 题";
                v.teacherExamList.Add(attemptItem.studentUsername, detail, submitted ? "查看答卷" : null,
                    submitted ? () => StartCoroutine(LoadTeacherAnswers(exam, attemptItem)) : (Action)null);
            }
            if (attempts.Length == 0) v.teacherExamList.Add("暂无学生答卷", "学生尚未开始这场考试。");
        }
    }

    private void ShowExamActionConfirmation(TeacherExamDto exam, bool closeExam, TeacherAttemptDto[] attempts)
    {
        foreach (var v in uiRoot.Views)
        {
            v.teacherExamList.Clear();
            v.teacherExamList.Add(exam.title, closeExam ? "结束后学生将无法继续作答。" : "确认已完成批阅。开放后学生可以查看答卷，分值将锁定。",
                closeExam ? "确认结束" : "确认开放", () => StartCoroutine(ApplyExamAction(exam, closeExam)));
            v.teacherExamList.Add("返回学生列表", "", "取消", () => QueueUi(() => ShowTeacherAttempts(exam, attempts)));
        }
    }

    private IEnumerator ApplyExamAction(TeacherExamDto exam, bool closeExam)
    {
        if (reviewMutationPending) yield break;
        reviewMutationPending = true;
        var session = token;
        var action = closeExam ? "close" : "publish-grades";
        yield return Send($"/api/teacher/exams/{exam.id}/{action}", null, "POST", session, _ =>
        {
            if (session != token) return;
            SetStatus(closeExam ? "考试已结束。" : "学生答卷已开放查看。");
            StartCoroutine(LoadTeacherAttempts(exam));
        });
        reviewMutationPending = false;
    }

    private IEnumerator LoadTeacherAnswers(TeacherExamDto exam, TeacherAttemptDto attemptItem)
    {
        var session = token;
        yield return Send($"/api/teacher/attempts/{attemptItem.id}/answers", null, "GET", session, response =>
        {
            var answers = ParseReviewAnswers(response);
            QueueUi(() => { if (session == token) ShowTeacherAnswers(exam, attemptItem, answers); });
        });
    }

    private void ShowTeacherAnswers(TeacherExamDto exam, TeacherAttemptDto attemptItem, ReviewAnswerDto[] answers)
    {
        foreach (var v in uiRoot.Views)
        {
            v.teacherExamList.Clear();
            v.teacherExamList.Add(attemptItem.studentUsername, $"{exam.title}　总分 {answers.Sum(a => (long)a.points)}/{answers.Sum(a => (long)a.maxPoints)}",
                "返回", () => StartCoroutine(LoadTeacherAttempts(exam)));
            foreach (var answer in answers)
            {
                Action edit = exam.gradesPublished ? null : () => QueueUi(() => ShowScoreChoices(exam, attemptItem, answers, answer));
                AddReviewAnswer(v.teacherExamList, answer, edit);
            }
        }
    }

    private void ShowScoreChoices(TeacherExamDto exam, TeacherAttemptDto attemptItem, ReviewAnswerDto[] answers, ReviewAnswerDto answer)
    {
        foreach (var v in uiRoot.Views)
        {
            v.teacherExamList.Clear();
            AddReviewAnswer(v.teacherExamList, answer);
            v.teacherExamList.Add("给 0 分", "", "确定", () => StartCoroutine(SaveTeacherScore(exam, attemptItem, answer, 0)));
            v.teacherExamList.Add($"给 {answer.maxPoints} 分", "", "确定", () => StartCoroutine(SaveTeacherScore(exam, attemptItem, answer, answer.maxPoints)));
            v.teacherExamList.Add("返回答卷", "", "取消", () => QueueUi(() => ShowTeacherAnswers(exam, attemptItem, answers)));
        }
    }

    private IEnumerator SaveTeacherScore(TeacherExamDto exam, TeacherAttemptDto attemptItem, ReviewAnswerDto answer, int points)
    {
        if (reviewMutationPending) yield break;
        reviewMutationPending = true;
        var session = token;
        var request = JsonUtility.ToJson(new GradeAnswerRequest { points = points });
        yield return Send($"/api/teacher/attempts/{attemptItem.id}/answers/{answer.questionId}/grade", request, "POST", session, _ =>
        {
            if (session != token) return;
            SetStatus("分值已保存。");
            StartCoroutine(LoadTeacherAnswers(exam, attemptItem));
        });
        reviewMutationPending = false;
    }

    private static ReviewAnswerDto[] ParseReviewAnswers(string json) =>
        JsonUtility.FromJson<ReviewAnswerEnvelope>($"{{\"items\":{json}}}").items ?? Array.Empty<ReviewAnswerDto>();

    private static void AddReviewAnswer(UiList list, ReviewAnswerDto answer, Action edit = null)
    {
        var options = new[] { answer.optionA, answer.optionB, answer.optionC, answer.optionD };
        var selected = answer.selectedOption is >= 1 and <= 4 ? ((char)('A' + answer.selectedOption - 1)).ToString() : "未答";
        var key = answer.correctOption is >= 1 and <= 4 ? $"　预设答案 {(char)('A' + answer.correctOption - 1)}" : "";
        var graded = answer.manuallyGraded ? "　人工给分" : "";
        var detail = $"{answer.stem}\n{string.Join("\n", options.Select((option, index) => $"{(char)('A' + index)}. {option}"))}\n作答 {selected}{key}{graded}";
        var score = answer.points < answer.maxPoints ? "失分" : "满分";
        list.AddExpanded($"第 {answer.questionNumber:00} 题　{score} {answer.points}/{answer.maxPoints}", detail, edit == null ? null : "修改分值", edit);
    }

    private IEnumerator ImportQuestions(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) { SetStatus("题库导入是可选的；请输入文件路径后再上传。"); yield break; }
        if (!File.Exists(path)) { SetStatus("找不到题库文件：" + path); yield break; }
        byte[] bytes;
        try { bytes = File.ReadAllBytes(path); } catch (Exception exception) { SetStatus("读取题库文件失败：" + exception.Message); yield break; }
        var form = new List<IMultipartFormSection> { new MultipartFormFileSection("file", bytes, Path.GetFileName(path), "text/csv") };
        using var request = UnityWebRequest.Post(baseUrl.TrimEnd('/') + "/api/teacher/questions/import", form);
        request.SetRequestHeader("Authorization", "Bearer " + token);
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success) { SetStatus($"题库导入失败（{request.responseCode}）：{request.downloadHandler?.text ?? request.error}"); yield break; }
        var result = JsonUtility.FromJson<ImportResult>(request.downloadHandler.text);
        SetStatus($"题库导入成功：{result.imported} 道题。正在刷新题库…");
        yield return LoadTeacherData();
    }

    private IEnumerator CreateClass(string name) { yield return Send("/api/teacher/classes", JsonUtility.ToJson(new ClassCreateRequest { name = name }), "POST", token, _ => { SetStatus("班级创建成功。"); StartCoroutine(LoadTeacherData()); }); }
    private IEnumerator AddMembers(string usernames) { if (selectedClassId <= 0) { SetStatus("请先点击选择班级。"); yield break; } yield return Send($"/api/teacher/classes/{selectedClassId}/members", JsonUtility.ToJson(new ClassMemberRequest { usernames = usernames.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray() }), "POST", token, _ => { SetStatus("班级成员已更新。"); StartCoroutine(LoadTeacherData()); }); }
    private IEnumerator CreateExam(string title, string questionText, string studentText, string classText, string delayText, string durationText)
    {
        var questionIds = ParseIds(questionText);
        var classIds = ParseIdsOrNull(classText);
        if (classIds == null && selectedClassId > 0) classIds = new[] { selectedClassId };
        if (string.IsNullOrWhiteSpace(title)) { SetStatus("请输入考试名称。"); yield break; }
        if (questionIds.Length == 0) { SetStatus("请至少填写一道题目 ID。"); yield break; }
        if (classIds == null && ParseIdsOrNull(studentText) == null) { SetStatus("请先选择班级，或填写学生 ID。"); yield break; }
        var start = DateTimeOffset.UtcNow.AddMinutes(ParseInt(delayText));
        var duration = ParseInt(durationText);
        var request = new ExamCreateRequest { title = title.Trim(), questionIds = questionIds, studentIds = ParseIdsOrNull(studentText), classIds = classIds, startAt = start.ToString("O"), endAt = duration > 0 ? start.AddMinutes(duration).ToString("O") : null };
        SetStatus($"正在发布考试（班级：{(classIds == null ? "无" : string.Join(",", classIds))}，开始：{start:MM-dd HH:mm}）…");
        yield return Send("/api/teacher/exams", JsonUtility.ToJson(request), "POST", token, _ => { SetStatus("考试创建成功。"); StartCoroutine(LoadTeacherData()); });
    }

    private IEnumerator LoadExams() { yield return Send("/api/exams", null, "GET", token, response => { exams.Clear(); exams.AddRange(JsonUtility.FromJson<ExamEnvelope>($"{{\"items\":{response}}}").items ?? Array.Empty<ExamDto>()); serverNow = ParseDate(exams.FirstOrDefault()?.serverNow); UpdateServerClockOffset(); SetStatus($"已获取 {exams.Count} 场考试。"); QueueUi(ShowStudentHomeWithoutReload); }); }
    private void ShowStudentHomeWithoutReload()
    {
        foreach (var v in uiRoot.Views)
        {
            v.heroTitle.text = exams.Count > 0 ? "已分配考试 · " + exams.Count + " 场" : "当前没有待考考试";
            v.heroDescription.text = exams.Count > 0 ? "查看考试安排，准备开始作答" : "教师发布并分配考试后，将在这里显示";
            v.examList.Clear();
            foreach (var exam in exams)
            {
                var selected = exam;
                var actionLabel = ExamActionLabel(exam);
                v.examList.Add(exam.title, $"#{exam.id}　{exam.questionCount} 题　{WindowText(exam)}", actionLabel, CanStartExam(exam) ? () => BeginExam(selected) : null);
            }
        }
    }
    private void BeginExam(ExamDto exam)
    {
        if (startingExam || activeExam != null) return;
        startingExam = true;
        SetStatus($"正在打开“{exam.title}”…");
        StartCoroutine(StartExam(exam));
    }

    private IEnumerator StartExam(ExamDto exam)
    {
        activeExam = exam;
        attempt = null;
        yield return Send($"/api/exams/{exam.id}/start", null, "POST", token, response =>
        {
            startingExam = false;
            attempt = JsonUtility.FromJson<AttemptDto>(response);
            serverNow = ParseDate(attempt.serverNow); UpdateServerClockOffset();
            if (string.Equals(attempt.status, "Submitted", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("本场考试已交卷。教师开放后可在成绩页查看。");
                activeExam = null;
                QueueUi(() => ShowPage(UiPage.Exams));
            }
            else StartCoroutine(LoadAttemptQuestions());
        });
        // Send reports API failures through the status label. Clear the
        // transient state so the user can retry another exam.
        if (attempt == null)
        {
            activeExam = null;
            startingExam = false;
        }
    }
    private IEnumerator LoadAttemptQuestions() { yield return Send($"/api/exam-attempts/{attempt.id}/questions", null, "GET", token, response => { attemptQuestions = JsonUtility.FromJson<AttemptQuestionEnvelope>($"{{\"items\":{response}}}").items ?? Array.Empty<AttemptQuestionDto>(); activeQuestion = 0; activeEnd = ParseDate(attempt.endAt) ?? ParseDate(activeExam.endAt); LoadDraft(); QueueUi(() => { ShowExam(); StopAutoSave(); autoSave = StartCoroutine(AutoSaveLoop()); }); }); }
    private IEnumerator LoadGrades() { yield return Send("/api/grades", null, "GET", token, response => { var grades = JsonUtility.FromJson<GradeEnvelope>($"{{\"items\":{response}}}").items ?? Array.Empty<GradeDto>(); QueueUi(() => ShowGrades(grades)); }); }
    private void ShowGrades(GradeDto[] grades)
    {
        ShowPage(UiPage.Grades);
        foreach (var v in uiRoot.Views)
        {
            v.gradeList.Clear();
            foreach (var grade in grades)
                v.gradeList.Add(grade.examTitle, $"{grade.totalScore} 分", "查看答卷", () => StartCoroutine(LoadStudentReview(grade)));
        }
    }

    private IEnumerator LoadStudentReview(GradeDto grade)
    {
        var session = token;
        yield return Send($"/api/grades/{grade.attemptId}/answers", null, "GET", session, response =>
        {
            var answers = ParseReviewAnswers(response);
            QueueUi(() =>
            {
                if (session != token) return;
                ShowPage(UiPage.Grades);
                foreach (var v in uiRoot.Views)
                {
                    v.gradeList.Clear();
                    v.gradeList.Add(grade.examTitle, $"总分 {grade.totalScore}/{answers.Sum(a => (long)a.maxPoints)}　失分题 {answers.Count(a => a.points < a.maxPoints)} 道",
                        "返回", () => StartCoroutine(LoadGrades()));
                    foreach (var answer in answers) AddReviewAnswer(v.gradeList, answer);
                }
            });
        });
    }

    private IEnumerator Send(string path, string json, string method, string bearer, Action<string> success)
    {
        using var request = new UnityWebRequest(baseUrl.TrimEnd('/') + path, method); if (json != null) { request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)); request.SetRequestHeader("Content-Type", "application/json"); } request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 5; if (!string.IsNullOrEmpty(bearer)) request.SetRequestHeader("Authorization", "Bearer " + bearer); yield return request.SendWebRequest(); if (request.result == UnityWebRequest.Result.Success) success?.Invoke(request.downloadHandler.text); else SetStatus($"请求失败（{request.responseCode}）：{request.downloadHandler?.text ?? request.error}");
    }

    private bool uiTransitionQueued;
    private void QueueUi(Action action)
    {
        if (uiTransitionQueued) return;
        uiTransitionQueued = true;
        StartCoroutine(ApplyUiNextFrame(action));
    }

    private IEnumerator ApplyUiNextFrame(Action action)
    {
        yield return null;
        uiTransitionQueued = false;
        action();
    }

    private void SetStatus(string value) => uiRoot.SetStatus(value);

    private void Navigate(int index)
    {
        if (role == "Teacher")
        {
            var pages = new[] { UiPage.TeacherHome, UiPage.Classes, UiPage.Questions, UiPage.Publish, UiPage.Exams, UiPage.Settings };
            QueueUi(() => ShowPage(pages[index]));
        }
        else if (index == 4) StartCoroutine(LoadGrades());
        else
        {
            var pages = new[] { UiPage.StudentHome, UiPage.StudentHome, UiPage.StudentHome, UiPage.Exams, UiPage.Grades, UiPage.Settings };
            QueueUi(() => ShowPage(pages[index]));
        }
    }

    private static int ParseInt(string value) => int.TryParse(value, out var result) ? Mathf.Max(0, result) : 0;
    private static long[] ParseIds(string value) => value.Split(',').Select(x => long.TryParse(x.Trim(), out var id) ? id : 0).Where(x => x > 0).Distinct().ToArray();
    private static long[] ParseIdsOrNull(string value) { var ids = ParseIds(value); return ids.Length == 0 ? null : ids; }
    private DateTimeOffset CurrentServerTime => DateTimeOffset.UtcNow + serverClockOffset;

    private void UpdateServerClockOffset()
    {
        if (serverNow.HasValue) serverClockOffset = serverNow.Value - DateTimeOffset.UtcNow;
    }

    private void LoadDraft()
    {
        examDraft = new ExamDraftFile(baseUrl, signedInName, activeExam.id, attempt.id);
        nextAnswerRevision = 0;
        pendingAnswers.Clear();
        try
        {
            foreach (var pending in examDraft.Read())
            {
                var question = attemptQuestions.FirstOrDefault(item => item.id == pending.questionId);
                if (question == null || pending.selectedOption is < 1 or > 4) continue;
                nextAnswerRevision = Math.Max(nextAnswerRevision, Math.Max(pending.revision, question.revision));
                if (pending.revision > question.revision)
                {
                    question.selectedOption = pending.selectedOption;
                    question.revision = pending.revision;
                    pendingAnswers[pending.questionId] = pending;
                }
            }
            WriteDraft();
        }
        catch (Exception exception) { SetStatus("本地答案草稿损坏，已忽略：" + exception.Message); }
    }

    private void WriteDraft()
    {
        if (examDraft == null) return;
        var pending = attemptQuestions.Where(item => item.selectedOption is >= 1 and <= 4)
            .Select(item => new PendingExamAnswer { questionId = item.id, selectedOption = item.selectedOption, revision = item.revision })
            .ToArray();
        try { examDraft.Write(pending); } catch (Exception exception) { SetStatus("本地答案保存失败：" + exception.Message); }
    }

    private IEnumerator SyncPendingAnswers(Action<bool> completed)
    {
        if (pendingAnswers.Count == 0) { completed?.Invoke(true); yield break; }
        foreach (var pending in pendingAnswers.Values.ToArray())
        {
            var synced = false;
            yield return UploadPendingAnswer(pending, result => synced = result);
            if (!synced) { completed?.Invoke(false); yield break; }
            if (pendingAnswers.TryGetValue(pending.questionId, out var current) && current.revision == pending.revision)
                pendingAnswers.Remove(pending.questionId);
        }
        WriteDraft();
        completed?.Invoke(true);
    }

    private IEnumerator UploadPendingAnswer(PendingExamAnswer pending, Action<bool> completed)
    {
        using var request = new UnityWebRequest(baseUrl.TrimEnd('/') + $"/api/exam-attempts/{attempt.id}/answers", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new AnswerRequest { questionId = pending.questionId, selectedOption = pending.selectedOption, revision = pending.revision })));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 5;
        request.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            completed?.Invoke(true);
            yield break;
        }
        if (request.responseCode == 409)
        {
            SetStatus("本地答案版本较旧，已保留服务器较新答案。");
            completed?.Invoke(true);
            yield break;
        }
        completed?.Invoke(false);
    }

    private IEnumerator RefreshExpiredAttempt()
    {
        if (attempt == null || submitting) yield break;
        yield return Send($"/api/exam-attempts/{attempt.id}", null, "GET", token, response =>
        {
            var latest = JsonUtility.FromJson<AttemptDto>(response);
            serverNow = ParseDate(latest.serverNow); UpdateServerClockOffset();
            if (latest.status == "Submitted")
            {
                pendingAnswers.Clear();
                WriteDraft();
                QueueUi(() => { activeExam = null; SetStatus("考试已结束，本地未同步修改未计入答卷。"); ShowStudentHome(); });
            }
            else SetStatus("考试已到截止时间，服务器正在完成交卷。");
        });
        refreshingExpiredAttempt = false;
    }

    private static DateTimeOffset? ParseDate(string value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) ? date : null;

    [Serializable] private sealed class LoginRequest { public string username; public string password; }
    [Serializable] private sealed class AuthDto { public string token; public string username; public string role; }
    [Serializable] private sealed class ClassCreateRequest { public string name; }
    [Serializable] private sealed class ClassMemberRequest { public string[] usernames; }
    [Serializable] private sealed class ExamCreateRequest { public string title; public long[] questionIds; public long[] studentIds; public long[] classIds; public string startAt; public string endAt; }
    [Serializable] private sealed class AnswerRequest { public long questionId; public int selectedOption; public long revision; }
    [Serializable] private sealed class AttemptDto { public long id; public long examId; public string status; public int totalScore; public string endAt; public string serverNow; }
    [Serializable] private sealed class QuestionDto { public long id; public string stem; }
    [Serializable] private sealed class AttemptQuestionDto { public long id; public string stem; public string optionA; public string optionB; public string optionC; public string optionD; public int selectedOption; public long revision; }
    [Serializable] private sealed class ExamDto { public long id; public string title; public int questionCount; public string startAt; public string endAt; public string attemptStatus; public string serverNow; public long attemptId; }
    [Serializable] private sealed class ClassDto { public long id; public string name; public int memberCount; }
    [Serializable] private sealed class TeacherExamDto { public long id; public string title; public int questionCount; public int assignmentCount; public string endAt; public bool gradesPublished; public bool hasEnded; }
    [Serializable] private sealed class TeacherAttemptDto { public long id; public long examId; public string studentUsername; public string status; public int totalScore; public string submittedAt; public int answeredCount; public int questionCount; }
    [Serializable] private sealed class GradeAnswerRequest { public int points; }
    [Serializable] private sealed class ReviewAnswerDto
    {
        public long questionId;
        public int questionNumber, selectedOption, correctOption, points, maxPoints;
        public string stem, optionA, optionB, optionC, optionD;
        public bool manuallyGraded;
    }
    [Serializable] private sealed class GradeDto { public long attemptId; public string examTitle; public int totalScore; public string submittedAt; }
    [Serializable] private sealed class ImportResult { public int imported; }
    [Serializable] private sealed class QuestionEnvelope { public QuestionDto[] items; }
    [Serializable] private sealed class AttemptQuestionEnvelope { public AttemptQuestionDto[] items; }
    [Serializable] private sealed class ExamEnvelope { public ExamDto[] items; }
    [Serializable] private sealed class ClassEnvelope { public ClassDto[] items; }
    [Serializable] private sealed class TeacherExamEnvelope { public TeacherExamDto[] items; }
    [Serializable] private sealed class TeacherAttemptEnvelope { public TeacherAttemptDto[] items; }
    [Serializable] private sealed class ReviewAnswerEnvelope { public ReviewAnswerDto[] items; }
    [Serializable] private sealed class GradeEnvelope { public GradeDto[] items; }
}


