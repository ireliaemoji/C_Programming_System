using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Editor authoring tool only. The saved prefab contains all fixed controls,
// both reference layouts and list templates; no builder runs in a player.
public static class ExamUiPrefabBuilder
{
    private const string Folder = "Assets/Resources/UI";
    private static Sprite white, blue, secondary, primary, danger, success, warning;
    private static Sprite logo, studentAvatar, teacherAvatar, desktopBackground, mobileBackground, emptyExam, emptyBank;
    private static Sprite[] icons;
    private static Sprite[] teacherIcons;
    private static bool phone;
    private static float pageWidth;
    private static int Body => phone ? 36 : 30;

    [MenuItem("CExam/UI/Rebuild reference prefab")]
    public static void Build()
    {
        white = Art("ui_panel_white", 64); blue = Art("ui_panel_blue", 64);
        success = Art("ui_panel_success", 64); warning = Art("ui_panel_warning", 64);
        secondary = Art("ui_button_secondary", 48); primary = Art("ui_button_primary", 48); danger = Art("ui_button_danger", 48);
        logo = Art("ui_logo_c"); studentAvatar = Art("ui_avatar_student_default"); teacherAvatar = Art("ui_avatar_teacher_default");
        desktopBackground = Art("ui_background_pc_preview"); mobileBackground = Art("ui_background_mobile");
        emptyExam = Art("ui_empty_no_exam"); emptyBank = Art("ui_empty_question_bank");
        icons = SliceIcons("ui_icons_student", "student", 6);
        var teacherSheet = SliceIcons("ui_icons_teacher", "teacher", 8);
        teacherIcons = new[] { teacherSheet[0], teacherSheet[2], teacherSheet[3], teacherSheet[4], teacherSheet[6], icons[5] };
        var root = Node(null, "UiRoot");
        root.gameObject.SetActive(false);
        var ui = root.gameObject.AddComponent<UiRoot>();
        var assets = root.gameObject.AddComponent<UiAssets>();
        var artData = new SerializedObject(assets);
        string[] names = { "desktopBackground", "mobileBackground", "panelWhite", "panelBlue", "panelSuccess", "panelWarning", "buttonPrimary", "buttonSecondary", "buttonDanger", "logo", "studentAvatar", "teacherAvatar" };
        Object[] sprites = { desktopBackground, mobileBackground, white, blue, success, warning, primary, secondary, danger, logo, studentAvatar, teacherAvatar };
        for (var i = 0; i < names.Length; i++) artData.FindProperty(names[i]).objectReferenceValue = sprites[i];
        artData.ApplyModifiedPropertiesWithoutUndo();
        var canvasRect = Node(root, "Canvas");
        var canvas = canvasRect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(2560, 1440); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasRect.gameObject.AddComponent<GraphicRaycaster>();
        var bg = Picture(canvasRect, "Background", desktopBackground, 0, 0, 2560, 1440); Stretch(bg.rectTransform); bg.preserveAspect = false;
        var safe = Node(canvasRect, "SafeArea"); Stretch(safe); var safeArea = safe.gameObject.AddComponent<UiSafeArea>();
        var desktop = BuildView(safe, false);
        var mobile = BuildView(safe, true);
        mobile.gameObject.SetActive(false);
        var events = Node(root, "EventSystem"); events.gameObject.AddComponent<EventSystem>(); events.gameObject.AddComponent<StandaloneInputModule>();
        var data = new SerializedObject(ui);
        string[] fields = { "rootCanvas", "scaler", "assets", "safeArea", "desktop", "mobile", "background" };
        Object[] refs = { canvas, scaler, assets, safeArea, desktop, mobile, bg };
        for (var i = 0; i < fields.Length; i++) data.FindProperty(fields[i]).objectReferenceValue = refs[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "UI");
        // Activate only after serialization has all bindings; Awake never sees a half-built view.
        root.gameObject.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "/UiRoot.prefab");
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        Debug.Log("CExam complete reference prefab saved (PC 2560x1440 / mobile 1080x2160).");
    }

    private static UiLayoutView BuildView(Transform parent, bool isPhone)
    {
        phone = isPhone;
        var w = phone ? 1080 : 2560; var h = phone ? 2160 : 1440;
        var r = Box(parent, phone ? "Mobile" : "Desktop", 0, 0, w, h);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = Vector2.zero;
        r.gameObject.AddComponent<UiDesignFit>().designSize = new Vector2(w, h);
        var v = r.gameObject.AddComponent<UiLayoutView>();
        var head = Panel(r, "Header", null, 0, 0, w, phone ? 160 : 144, UiTheme.Header);
        Picture(head, "Logo", logo, 30, 24, 92, 92);
        v.title = Label(head, "Title", "学生首页", 150, 30, phone ? 500 : 1200, 78, phone ? 44 : 44, Color.white);
        v.avatar = Picture(head, "Avatar", studentAvatar, w - 124, 26, 82, 82);
        v.userName = Label(head, "UserName", "学生 · 学习中心", w - (phone ? 390 : 610), phone ? 110 : 44, phone ? 340 : 440, phone ? 40 : 60, phone ? 28 : 30, phone ? UiTheme.TextMuted : Color.white);
        if (phone) v.userName.gameObject.SetActive(false);
        v.navigation = Panel(r, "Navigation", null, 0, phone ? h - 152 : 144, phone ? w : 340, phone ? 152 : h - 144, UiTheme.Sidebar).gameObject;
        v.navigationButtons = new Button[6];
        v.navigationIcons = new Image[6]; v.studentIcons = icons; v.teacherIcons = teacherIcons;
        string[] labels = { "首页", "题库", "练习", "考试", "成绩", "设置" };
        for (var i = 0; i < 6; i++)
        {
            var b = Button(v.navigation.transform, "Nav" + i, labels[i], phone ? 12 + i * 178 : 24, phone ? 12 : 46 + i * 128, phone ? 166 : 292, phone ? 128 : 96, secondary);
            var text = b.GetComponentInChildren<Text>(); text.fontSize = phone ? 30 : 34;
            if (phone) { Box(text.rectTransform, 4, 78, 158, 42); v.navigationIcons[i] = Picture(b.transform, "Icon", icons[i], 56, 12, 54, 54); }
            else { Box(text.rectTransform, 90, 12, 190, 70); v.navigationIcons[i] = Picture(b.transform, "Icon", icons[i], 18, 22, 54, 54); }
            v.navigationButtons[i] = b;
        }
        var pages = Box(r, "Pages", phone ? 40 : 410, phone ? 210 : 200, phone ? 1000 : 2070, phone ? 1700 : 1140);
        pageWidth = pages.rect.width;
        // rect.width can depend on a parent canvas during editor authoring; use the explicit width.
        pageWidth = phone ? 1000 : 2070;
        v.pages = new GameObject[10];
        for (var i = 0; i < v.pages.Length; i++) { v.pages[i] = Node(pages, ((UiPage)i).ToString()).gameObject; Stretch((RectTransform)v.pages[i].transform); }
        BuildLogin(v);
        BuildStudent(v);
        BuildExams(v);
        BuildExam(v);
        BuildGrades(v);
        BuildTeacher(v);
        BuildClasses(v);
        BuildQuestions(v);
        BuildPublish(v);
        BuildSettings(v);
        BuildDialog(v, r, w, h);
        v.status = Label(r, "Status", "", phone ? 40 : 410, phone ? 1948 : 1356, phone ? 1000 : 2070, 48, phone ? 28 : 26, UiTheme.TextMuted);
        for (var i = 0; i < v.pages.Length; i++) v.pages[i].SetActive(i == (int)UiPage.StudentHome);
        return v;
    }

    private static Transform Page(UiLayoutView v, UiPage page) => v.pages[(int)page].transform;

    private static void BuildLogin(UiLayoutView v)
    {
        var p = Page(v, UiPage.Login);
        var card = Panel(p, "LoginCard", white, phone ? 0 : 310, phone ? 130 : 50, phone ? 1000 : 1120, phone ? 1260 : 1000);
        Picture(card, "Logo", logo, phone ? 400 : 460, 54, 200, 200);
        Label(card, "Welcome", "欢迎回来", 90, 280, phone ? 820 : 940, 90, phone ? 58 : 50);
        Label(card, "Hint", "登录后进入学生学习中心或教师工作台", 90, 380, phone ? 820 : 940, 70, Body, UiTheme.TextMuted);
        v.username = Field(card, "Username", "用户名", "student", 90, 500, phone ? 820 : 940, false);
        v.password = Field(card, "Password", "密码", "student123", 90, 660, phone ? 820 : 940, true);
        v.login = Button(card, "Login", "登录", 90, phone ? 860 : 810, phone ? 820 : 440, 100, primary);
        v.register = Button(card, "Register", "注册学生账号", phone ? 90 : 590, phone ? 1000 : 810, phone ? 820 : 440, 100, secondary);
    }

    private static void BuildStudent(UiLayoutView v)
    {
        var p = Page(v, UiPage.StudentHome);
        Label(p, "Welcome", "开始今天的 C 语言学习", 0, 0, pageWidth, 90, phone ? 48 : 48);
        var hero = Panel(p, "ContinueCard", blue, 0, 120, pageWidth, phone ? 360 : 260);
        v.heroTitle = Label(hero, "Title", "查看已分配考试", 54, 40, pageWidth - 100, 80, phone ? 46 : 46, Color.white);
        v.heroDescription = Label(hero, "Description", "完成考试后，可在成绩中查看结果", 54, 132, phone ? 880 : 1500, 80, Body, new Color(.88f, .94f, 1f));
        v.heroAction = Button(hero, "Continue", "进入考试", phone ? 54 : 1620, phone ? 238 : 84, phone ? 870 : 360, 94, secondary);
        Label(p, "QuickTitle", "快捷入口", 0, phone ? 540 : 440, pageWidth, 80, phone ? 42 : 40);
        v.shortcuts = new Button[4]; string[] names = { "我的考试", "自主练习", "题库中心", "我的成绩" }; int[] ii = { 3, 2, 1, 4 };
        for (var i = 0; i < 4; i++)
        {
            var cw = phone ? 480 : 495; var ch = phone ? 310 : 280;
            var x = phone ? i % 2 * 520 : i * 525; var y = phone ? 650 + i / 2 * 350 : 550;
            var b = Button(p, "Quick" + i, names[i], x, y, cw, ch, white);
            Picture(b.transform, "Icon", icons[ii[i]], (cw - 96) / 2, 42, 96, 96);
            var label = b.GetComponentInChildren<Text>(); Box(label.rectTransform, 10, 166, cw - 20, 88); label.fontSize = phone ? 38 : 36;
            v.shortcuts[i] = b;
        }
        Label(p, "Note", "请提前确认考试安排，作答时留意倒计时。", 0, phone ? 1410 : 940, pageWidth, 90, Body, UiTheme.TextMuted);
    }

    private static void BuildExams(UiLayoutView v)
    {
        var p = Page(v, UiPage.Exams);
        Label(p, "Heading", "我的考试", 0, 0, pageWidth - 440, 80, 48);
        v.refreshExams = Button(p, "Refresh", "刷新考试", pageWidth - 390, 0, 390, 88, secondary);
        v.examList = List(p, "ExamList", 0, 130, pageWidth, phone ? 1500 : 960, "暂无已分配的考试", emptyExam);
        v.teacherExamList = List(p, "PublishedList", 0, 130, pageWidth, phone ? 1500 : 960, "暂无已发布的考试", emptyExam);
        v.teacherExamList.gameObject.SetActive(false);
        v.refreshPublished = v.refreshExams;
    }

    private static void BuildExam(UiLayoutView v)
    {
        var p = Page(v, UiPage.Exam);
        v.examTitle = Label(p, "ExamTitle", "C语言考试", 0, 0, pageWidth - 490, 85, 46);
        v.timer = Label(p, "Timer", "剩余 00:30:00", pageWidth - 480, 0, 480, 85, 38, UiTheme.Warning);
        v.questionProgress = Label(p, "Progress", "第 1 / 4 题", phone ? 0 : 460, phone ? 245 : 120, pageWidth - (phone ? 0 : 460), 70, Body, UiTheme.TextMuted);
        var q = Panel(p, "QuestionCard", white, phone ? 0 : 460, phone ? 350 : 210, phone ? pageWidth : pageWidth - 460, phone ? 1050 : 740);
        v.questionStem = Label(q, "Stem", "下列哪个是正确的 main 函数？", 42, 30, (phone ? pageWidth : pageWidth - 460) - 84, 190, phone ? 42 : 38);
        v.options = new Button[4]; v.optionLabels = new Text[4];
        for (var i = 0; i < 4; i++) { var b = Button(q, "Option" + i, "", 42, 250 + i * (phone ? 178 : 108), (phone ? pageWidth : pageWidth - 460) - 84, phone ? 142 : 92, secondary); v.options[i] = b; v.optionLabels[i] = b.GetComponentInChildren<Text>(); v.optionLabels[i].alignment = TextAnchor.MiddleLeft; v.optionLabels[i].fontSize = Body; }
        var by = phone ? 1460 : 1010;
        v.previousQuestion = Button(p, "Previous", "上一题", 0, by, phone ? 300 : 420, 96, secondary);
        v.nextQuestion = Button(p, "Next", "下一题", phone ? 340 : 450, by, phone ? 300 : 420, 96, primary);
        v.submitExam = Button(p, "Submit", "交卷", phone ? 680 : 900, by, phone ? 320 : 420, 96, primary);
        v.leaveExam = Button(p, "Leave", "返回考试列表", phone ? 0 : 1430, phone ? 1590 : by, phone ? 1000 : 640, 96, secondary);
        v.openQuestionNumbers = Button(p, "OpenNumbers", "题号导航　展开", 0, 120, pageWidth, 100, secondary);
        v.openQuestionNumbers.gameObject.SetActive(phone);
        var drawer = Panel(p, "QuestionDrawer", white, 0, 120, phone ? pageWidth : 400, phone ? 1200 : 800);
        if (phone) Label(drawer, "Heading", "题号导航", 32, 20, 650, 90, 42);
        v.closeQuestionNumbers = Button(drawer, "CloseNumbers", "收起", 690, 20, 270, 90, secondary);
        v.closeQuestionNumbers.gameObject.SetActive(phone);
        v.questionNumbers = List(drawer, "QuestionNumbers", phone ? 24 : 0, phone ? 140 : 0, phone ? pageWidth - 48 : 400, phone ? 1020 : 800, "", null, true);
        v.questionDrawer = drawer.gameObject; drawer.gameObject.SetActive(!phone);
    }

    private static void BuildGrades(UiLayoutView v)
    {
        var p = Page(v, UiPage.Grades); Label(p, "Heading", "已提交成绩", 0, 0, pageWidth, 90, 48);
        v.gradeList = List(p, "GradeList", 0, 130, pageWidth, phone ? 1480 : 960, "还没有已提交成绩", emptyExam);
    }

    private static void BuildTeacher(UiLayoutView v)
    {
        var p = Page(v, UiPage.TeacherHome); Label(p, "Heading", "教学管理总览", 0, 0, pageWidth, 90, 48);
        var stats = new[] { "班级", "题库题目", "已发布考试" }; var values = new Text[3];
        for (var i = 0; i < 3; i++) { var cw = phone ? 310 : 670; var card = Panel(p, "Stat" + i, white, i * (phone ? 345 : 700), 140, cw, phone ? 260 : 220); values[i] = Label(card, "Value", "0", 24, 26, cw - 48, 90, 62, UiTheme.Primary); Label(card, "Label", stats[i], 24, 128, cw - 48, 64, Body); }
        v.classCount = values[0]; v.questionCount = values[1]; v.teacherExamCount = values[2];
        v.teacherShortcuts = new Button[4]; string[] labels = { "班级与成员", "题库管理", "组卷与发布", "已发布考试" };
        for (var i = 0; i < 4; i++) v.teacherShortcuts[i] = Button(p, "Manage" + i, labels[i], phone ? 0 : i % 2 * 1050, (phone ? 470 : 440) + (phone ? i : i / 2) * (phone ? 200 : 230), phone ? 1000 : 1020, 160, secondary);
        v.refreshTeacher = Button(p, "Refresh", "刷新管理数据", 0, phone ? 1340 : 980, pageWidth, 96, primary);
    }

    private static void BuildClasses(UiLayoutView v)
    {
        var p = Page(v, UiPage.Classes);
        v.className = Field(p, "ClassName", "班级名称", "C语言一班", 0, 0, phone ? 1000 : 1450);
        v.createClass = Button(p, "CreateClass", "创建班级", phone ? 0 : 1540, phone ? 160 : 42, phone ? 1000 : 530, 96, primary);
        v.selectedClass = Label(p, "SelectedClass", "请选择下方班级", 0, phone ? 300 : 210, pageWidth, 80, Body);
        v.members = Field(p, "Members", "学生账号（逗号分隔）", "student", 0, phone ? 410 : 330, phone ? 1000 : 1450);
        v.addMembers = Button(p, "AddMembers", "加入选中班级", phone ? 0 : 1540, phone ? 580 : 372, phone ? 1000 : 530, 96, primary);
        v.classList = List(p, "ClassList", 0, phone ? 760 : 570, pageWidth, phone ? 890 : 540, "尚未创建班级", emptyBank);
    }

    private static void BuildQuestions(UiLayoutView v)
    {
        var p = Page(v, UiPage.Questions); Label(p, "Heading", "教师题库", 0, 0, pageWidth, 90, 48);
        v.importPath = Field(p, "ImportPath", "可选：CSV / XLSX 文件路径", "", 0, 140, phone ? 1000 : 1450);
        v.importQuestions = Button(p, "Import", "上传题库", phone ? 0 : 1540, phone ? 310 : 182, phone ? 1000 : 530, 96, primary);
        v.questionList = List(p, "QuestionList", 0, phone ? 500 : 390, pageWidth, phone ? 1150 : 720, "题库暂时为空", emptyBank);
    }

    private static void BuildPublish(UiLayoutView v)
    {
        var p = Page(v, UiPage.Publish);
        // Scrollable form, authored as one column on phones and two columns on PC.
        var form = ScrollContent(p, "Form", 0, 0, pageWidth, phone ? 1540 : 950);
        var row = phone ? 190 : 190; var col = phone ? 1000 : 1005;
        v.examName = Field(form, "ExamName", "考试标题", "C语言阶段考试", 0, 0, col);
        v.questionIds = Field(form, "QuestionIds", "题目 ID（逗号分隔）", "", phone ? 0 : 1065, phone ? row : 0, col);
        v.studentIds = Field(form, "StudentIds", "学生 ID（可空）", "", 0, phone ? row * 2 : row, col);
        v.classIds = Field(form, "ClassIds", "班级 ID（可空）", "", phone ? 0 : 1065, phone ? row * 3 : row, col);
        v.delayMinutes = Field(form, "Delay", "开始延迟分钟", "0", 0, phone ? row * 4 : row * 2, col);
        v.durationMinutes = Field(form, "Duration", "考试时长分钟（0 表示不限）", "30", phone ? 0 : 1065, phone ? row * 5 : row * 2, col);
        form.sizeDelta = new Vector2(0, phone ? 1200 : 620);
        Label(p, "Hint", "发布前请核对题目、时间和学生范围", 0, phone ? 1440 : 800, pageWidth, 80, Body, UiTheme.TextMuted);
        v.publishExam = Button(p, "Publish", "创建并发布考试", 0, phone ? 1570 : 980, pageWidth, 100, primary);
    }

    private static void BuildSettings(UiLayoutView v)
    {
        var p = Page(v, UiPage.Settings); var card = Panel(p, "SettingsCard", white, 0, 0, pageWidth, 650);
        Label(card, "Heading", "账户与设置", 48, 40, pageWidth - 96, 100, 48);
        Label(card, "Info", "退出后可切换学生或教师账号。", 48, 190, pageWidth - 96, 90, Body);
        v.environment = Label(card, "Environment", "本地开发环境", 48, 310, pageWidth - 96, 90, Body, UiTheme.TextMuted);
        v.logout = Button(card, "Logout", "退出登录", 48, 470, pageWidth - 96, 100, danger);
    }

    private static void BuildDialog(UiLayoutView v, Transform parent, float w, float h)
    {
        var overlay = Panel(parent, "SubmitDialog", null, 0, 0, w, h, new Color(0, 0, 0, .45f));
        overlay.GetComponent<Image>().raycastTarget = true;
        var cw = phone ? 920 : 1100; var card = Panel(overlay, "Card", white, (w - cw) / 2, (h - 560) / 2, cw, 560);
        Label(card, "Title", "确认交卷", 54, 44, cw - 108, 96, 48);
        v.submitSummary = Label(card, "Summary", "交卷后本次作答将结束。", 54, 180, cw - 108, 130, Body);
        v.cancelSubmit = Button(card, "Cancel", "继续答题", 54, 380, (cw - 150) / 2, 108, secondary);
        v.confirmSubmit = Button(card, "Confirm", "确认交卷", cw / 2 + 20, 380, (cw - 150) / 2, 108, primary);
        v.submitDialog = overlay.gameObject; v.submitDialog.SetActive(false);
    }

    private static UiList List(Transform parent, string name, float x, float y, float w, float h, string emptyText, Sprite illustration, bool numbers = false)
    {
        var content = ScrollContent(parent, name, x, y, w, h);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 18; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var list = content.gameObject.AddComponent<UiList>();
        var item = Panel(content, "ItemTemplate", white, 0, 0, w, numbers ? 84 : phone ? 260 : 190);
        item.gameObject.AddComponent<LayoutElement>().preferredHeight = numbers ? 84 : phone ? 260 : 190;
        var row = item.gameObject.AddComponent<UiListItem>();
        row.title = Label(item, "Title", "考试标题", 32, 20, w - (numbers ? 110 : 340), numbers ? 50 : 64, numbers ? 30 : Body);
        row.detail = Label(item, "Detail", "考试说明", 32, phone ? 96 : 94, w - 64, phone ? 64 : 70, phone ? 30 : 26, UiTheme.TextMuted);
        if (numbers) row.detail.gameObject.SetActive(false);
        row.action = Button(item, "Action", numbers ? "选择" : "开始考试", w - (numbers ? 104 : 290), numbers ? 10 : phone ? 166 : 40, numbers ? 92 : 250, numbers ? 64 : 82, secondary);
        row.actionLabel = row.action.GetComponentInChildren<Text>(); row.actionLabel.fontSize = numbers ? 24 : Body;
        list.itemTemplate = row; item.gameObject.SetActive(false);
        var empty = Box(content, "EmptyState", 0, 0, w, illustration ? 620 : 130);
        empty.gameObject.AddComponent<LayoutElement>().preferredHeight = illustration ? 620 : 130;
        if (illustration) Picture(empty, "Illustration", illustration, (w - 500) / 2, 40, 500, 340);
        Label(empty, "Message", emptyText, 24, illustration ? 430 : 20, w - 48, 100, Body, UiTheme.TextMuted, TextAnchor.MiddleCenter);
        list.emptyState = empty.gameObject;
        return list;
    }

    private static RectTransform ScrollContent(Transform parent, string name, float x, float y, float w, float h)
    {
        var viewport = Box(parent, name, x, y, w, h); viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        // Published exam rows contain their own buttons. The transparent
        // viewport graphic must not win the raycast before those buttons.
        hit.raycastTarget = name != "PublishedList";
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        var content = Box(viewport, "Content", 0, 0, w, h); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.sizeDelta = new Vector2(0, h);
        scroll.viewport = viewport; scroll.content = content;
        return content;
    }

    private static InputField Field(Transform parent, string name, string caption, string value, float x, float y, float width, bool password = false)
    {
        var host = Box(parent, name + "Field", x, y, width, 150);
        Label(host, "Caption", caption, 0, 0, width, 48, Body, UiTheme.TextMuted);
        var rect = Panel(host, name, secondary, 0, 54, width, 96);
        var input = rect.gameObject.AddComponent<InputField>(); input.targetGraphic = rect.GetComponent<Image>();
        input.textComponent = Label(rect, "Value", "", 24, 10, width - 48, 76, Body);
        input.textComponent.supportRichText = false;
        input.placeholder = Label(rect, "Placeholder", caption, 24, 10, width - 48, 76, Body, UiTheme.TextMuted);
        input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
        input.text = value; return input;
    }

    private static Button Button(Transform parent, string name, string label, float x, float y, float w, float h, Sprite sprite)
    {
        var rect = Panel(parent, name, sprite, x, y, w, h);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
        Label(rect, "Label", label, 16, 8, w - 32, h - 16, Body, sprite == primary || sprite == danger ? Color.white : UiTheme.TextPrimary, TextAnchor.MiddleCenter);
        return button;
    }

    private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int fontSize, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var rect = Box(parent, name, x, y, w, h); var text = rect.gameObject.AddComponent<Text>();
        text.font = UiTheme.RuntimeFont; text.fontSize = fontSize; text.color = color ?? UiTheme.TextPrimary;
        text.alignment = alignment; text.text = value; text.raycastTarget = false; text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Image Picture(Transform parent, string name, Sprite sprite, float x, float y, float w, float h)
    {
        var image = Box(parent, name, x, y, w, h).gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; return image;
    }

    private static RectTransform Panel(Transform parent, string name, Sprite sprite, float x, float y, float w, float h, Color? color = null)
    {
        var rect = Box(parent, name, x, y, w, h); var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color ?? Color.white; image.type = sprite ? Image.Type.Sliced : Image.Type.Simple; return rect;
    }

    private static RectTransform Node(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform)); if (parent) go.transform.SetParent(parent, false); go.layer = 5; return (RectTransform)go.transform;
    }

    private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h) { var r = Node(parent, name); Box(r, x, y, w, h); return r; }
    private static void Box(RectTransform r, float x, float y, float w, float h) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one; }
    private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    private static Sprite Art(string name, int border = 0)
    {
        var path = "Assets/ArtAssets/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.spriteBorder = new Vector4(border, border, border, border); importer.mipmapEnabled = false;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        if (name == "ui_background_mobile") importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite[] SliceIcons(string assetName, string prefix, int count)
    {
        var path = "Assets/ArtAssets/" + assetName + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.maxTextureSize = 4096; importer.spriteImportMode = SpriteImportMode.Multiple;
        // Symbols occupy one row; exclude the transparent outer margins.
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var previous = provider.GetSpriteRects();
        var slices = new SpriteRect[count];
        for (var i = 0; i < count; i++)
        {
            var name = prefix + "_" + i;
            var cellWidth = 2172f / count;
            slices[i] = new SpriteRect { name = name, rect = new Rect(i * cellWidth, 160, cellWidth, 390), alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f), spriteID = previous.FirstOrDefault(s => s.name == name)?.spriteID ?? GUID.Generate() };
        }
        provider.SetSpriteRects(slices);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(slices.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }
}
