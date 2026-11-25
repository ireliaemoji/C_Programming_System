using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ExamUiValidation
{
    [MenuItem("CExam/UI/Validate and render reference layouts")]
    public static void Run()
    {
        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/validation/ui-previews"));
        Directory.CreateDirectory(output);
        var sizes = new[] { new Vector2Int(2560, 1440), new Vector2Int(1920, 1080), new Vector2Int(1440, 900), new Vector2Int(1280, 720), new Vector2Int(2560, 1080), new Vector2Int(1080, 2160), new Vector2Int(390, 844), new Vector2Int(360, 800), new Vector2Int(430, 932) };
        foreach (var size in sizes)
            Render(size.x, size.y, UiPage.StudentHome, output, false);
        foreach (var page in new[] { UiPage.Login, UiPage.Exams, UiPage.Exam, UiPage.Grades, UiPage.TeacherHome, UiPage.Classes, UiPage.Questions, UiPage.Publish })
        {
            Render(1920, 1080, page, output, false);
            Render(390, 844, page, output, false);
        }
        Render(390, 844, UiPage.StudentHome, output, true);
        Debug.Log("CExam UI validation PASS: 26 renders, nine resolutions, safe area, complete serialized references and button bounds.");
    }

    private static void Render(int width, int height, UiPage page, string output, bool inset)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/UiRoot.prefab");
        var instance = Object.Instantiate(prefab);
        var root = instance.GetComponent<UiRoot>(); root.enabled = false;
        var canvas = root.Canvas;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.enabled = false;
        var cameraObject = new GameObject("UiValidationCamera"); var camera = cameraObject.AddComponent<Camera>();
        var rt = new RenderTexture(width, height, 24); rt.Create(); camera.targetTexture = rt;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = UiTheme.Background; camera.cullingMask = 1 << 5;
        camera.transform.position = new Vector3(0, 0, -10);
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var mobile = height > width;
        var reference = mobile ? UiTheme.MobileReferenceResolution : UiTheme.DesktopReferenceResolution;
        canvas.scaleFactor = Mathf.Min(width / reference.x, height / reference.y);
        foreach (var v in root.Views) v.gameObject.SetActive(v.name == (mobile ? "Mobile" : "Desktop"));
        var view = root.Views.First(v => v.gameObject.activeSelf);
        var so = new SerializedObject(view);
        var property = so.GetIterator();
        while (property.NextVisible(true))
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script" && property.objectReferenceValue == null)
                throw new Exception("Missing serialized UI reference: " + view.name + "/" + property.propertyPath);

        var safe = canvas.GetComponentInChildren<UiSafeArea>(true); safe.enabled = false;
        var safeRect = (RectTransform)safe.transform;
        safeRect.anchorMin = inset ? new Vector2(0, 34f / height) : Vector2.zero;
        safeRect.anchorMax = inset ? new Vector2(1, 1 - 47f / height) : Vector2.one;
        safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
        canvas.transform.Find("Background").GetComponent<Image>().sprite = mobile ? root.Assets.MobileBackground : root.Assets.DesktopBackground;
        var teacher = page >= UiPage.TeacherHome && page != UiPage.Settings;
        view.Show(page, teacher, teacher ? "teacher" : "student", teacher ? root.Assets.TeacherAvatar : root.Assets.StudentAvatar);
        view.examList.gameObject.SetActive(!teacher); view.teacherExamList.gameObject.SetActive(teacher);
        view.examList.Add("C语言基础测试", "4 道单选题 · 请在考试时间内完成", "开始考试", () => { });
        view.classList.Add("C语言基础班", "班级 ID：1　成员：32", "选择班级", () => { });
        view.questionList.Add("#1　下列哪个是正确的 main 函数？", "单选题");
        view.gradeList.Add("C语言阶段测试", "80 分　已提交");
        view.teacherExamList.Add("C语言阶段测试", "4 道题 · 已分配 32 人");
        view.classCount.text = "3"; view.questionCount.text = "120"; view.teacherExamCount.text = "5";
        view.questionProgress.text = "第 1 / 4 题";
        string[] options = { "A. void main()", "B. int main(void)", "C. main(int)", "D. start()" };
        for (var i = 0; i < 4; i++) { view.optionLabels[i].text = options[i]; view.questionNumbers.Add($"{i + 1:00} 未答", "", i == 0 ? "当前" : "选择", () => { }); }
        foreach (var list in instance.GetComponentsInChildren<UiList>(true)) list.itemTemplate.gameObject.SetActive(false);
        Canvas.ForceUpdateCanvases();
        view.GetComponent<UiDesignFit>().Apply();
        foreach (var group in view.GetComponentsInChildren<VerticalLayoutGroup>()) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
        Canvas.ForceUpdateCanvases();
        AssertContained(view.GetComponent<RectTransform>(), camera, width, height);
        foreach (var button in view.GetComponentsInChildren<Button>())
        {
            if (button.GetComponentInParent<ScrollRect>()) continue;
            AssertContained((RectTransform)button.transform, camera, width, height);
            if (((RectTransform)button.transform).rect.width < 80) throw new Exception("Button collapsed: " + button.name);
        }
        camera.Render();
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        var previous = RenderTexture.active; RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply(); RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(output, $"{page}-{width}x{height}{(inset ? "-safe" : "")}.png"), texture.EncodeToPNG());
        Object.DestroyImmediate(texture); camera.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(instance);
    }

    private static void AssertContained(RectTransform rect, Camera camera, int width, int height)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        foreach (var corner in corners)
        {
            var point = camera.WorldToScreenPoint(corner);
            if (point.x < -2 || point.y < -2 || point.x > width + 2 || point.y > height + 2)
                throw new Exception($"UI outside screen: {rect.name} at {point} in {width}x{height}");
        }
    }
}
