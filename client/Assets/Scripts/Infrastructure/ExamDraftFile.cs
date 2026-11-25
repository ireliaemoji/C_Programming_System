using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable]
internal sealed class PendingExamAnswer
{
    public long questionId;
    public int selectedOption;
    public long revision;
}

internal sealed class ExamDraftFile
{
    private readonly string path;

    public ExamDraftFile(string server, string username, long examId, long attemptId)
    {
        using var hash = SHA256.Create();
        var scope = Encoding.UTF8.GetBytes($"{server.TrimEnd('/')}\n{username}\n{examId}\n{attemptId}");
        var key = BitConverter.ToString(hash.ComputeHash(scope)).Replace("-", "");
        path = Path.Combine(Application.persistentDataPath, "ExamDrafts", key + ".json");
    }

    public PendingExamAnswer[] Read()
    {
        if (!File.Exists(path)) return Array.Empty<PendingExamAnswer>();
        var draft = JsonUtility.FromJson<Draft>(File.ReadAllText(path));
        return draft?.answers ?? throw new IOException("Invalid exam draft.");
    }

    public void Write(PendingExamAnswer[] answers)
    {
        if (answers.Length == 0)
        {
            File.Delete(path);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write))
        {
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Draft { answers = answers }));
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        if (File.Exists(path)) File.Replace(temporary, path, null);
        else File.Move(temporary, path);
    }

    [Serializable]
    private sealed class Draft
    {
        public PendingExamAnswer[] answers;
    }
}
