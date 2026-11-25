using UnityEngine;
using UnityEngine.UI;

public enum UiPage { Login, StudentHome, Exams, Exam, Grades, TeacherHome, Classes, Questions, Publish, Settings }

// Every reference below is authored in UiRoot.prefab. Fixed page controls are
// never rebuilt at runtime; only list templates are cloned for server data.
public sealed class UiLayoutView : MonoBehaviour
{
    public GameObject[] pages;
    public GameObject navigation;
    public Button[] navigationButtons;
    public Image[] navigationIcons;
    public Sprite[] studentIcons, teacherIcons;
    public Text title, userName, status;
    public Image avatar;
    public InputField username, password;
    public Button login, register, logout, refreshExams, heroAction;
    public Button refreshPublished;
    public Button[] shortcuts;
    public Text heroTitle, heroDescription;
    public UiList examList, gradeList, classList, questionList, teacherExamList;
    public Text examTitle, timer, questionProgress, questionStem;
    public Button[] options;
    public Text[] optionLabels;
    public Button previousQuestion, nextQuestion, submitExam, leaveExam;
    public UiList questionNumbers;
    public GameObject questionDrawer;
    public Button openQuestionNumbers, closeQuestionNumbers;
    public GameObject submitDialog;
    public Text submitSummary;
    public Button confirmSubmit, cancelSubmit;
    public Text classCount, questionCount, teacherExamCount;
    public Button[] teacherShortcuts;
    public Button refreshTeacher, createClass, addMembers, importQuestions, publishExam;
    public InputField className, members, importPath;
    public InputField examName, questionIds, studentIds, classIds, delayMinutes, durationMinutes;
    public Text selectedClass, environment;

    public void Show(UiPage page, bool teacher, string usernameValue, Sprite avatarSprite)
    {
        for (var i = 0; i < pages.Length; i++) pages[i].SetActive(i == (int)page);
        navigation.SetActive(page != UiPage.Login && page != UiPage.Exam);
        var labels = teacher
            ? new[] { "总览", "班级与成员", "题库管理", "组卷发布", "已发布考试", "设置" }
            : new[] { "首页", "题库", "练习", "考试", "成绩", "设置" };
        var titles = new[] { "C 语言考试系统", "学生首页", "我的考试", "正在考试", "我的成绩", "教师工作台", "班级与成员", "题库管理", "组卷与发布", "设置" };
        title.text = titles[(int)page];
        userName.text = string.IsNullOrEmpty(usernameValue) ? "欢迎使用" : usernameValue + (teacher ? " · 教师" : " · 学生");
        avatar.sprite = avatarSprite;
        avatar.gameObject.SetActive(page != UiPage.Login);
        for (var i = 0; i < navigationButtons.Length; i++)
        {
            navigationButtons[i].GetComponentInChildren<Text>().text = labels[i];
            navigationIcons[i].sprite = teacher ? teacherIcons[i] : studentIcons[i];
            // These two student flows have no client implementation yet.
            navigationButtons[i].interactable = teacher || (i != 1 && i != 2);
            var selected = teacher
                ? (int)page == new[] { 5, 6, 7, 8, 2, 9 }[i]
                : (int)page == new[] { 1, -1, -1, 2, 4, 9 }[i];
            navigationButtons[i].GetComponent<Image>().color = selected ? new Color(.83f, .91f, 1f) : Color.white;
        }
        submitDialog.SetActive(false);
        questionDrawer.SetActive(name == "Desktop");
    }
}
