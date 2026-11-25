using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

public sealed class QuestionImportService
{
    private static readonly string[] Header = ["stem", "optionA", "optionB", "optionC", "optionD", "correctOption", "points", "visibility", "isPublished"];

    public Task<QuestionImportResult> ParseAsync(Stream source, CancellationToken cancellationToken) => ParseAsync(source, string.Empty, cancellationToken);

    public async Task<QuestionImportResult> ParseAsync(Stream source, string fileName, CancellationToken cancellationToken)
    {
        if (Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return await ParseXlsxAsync(source, cancellationToken);
        using var reader = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var errors = new List<string>();
        var rows = new List<QuestionInput>();
        var lineNumber = 0;
        var header = await reader.ReadLineAsync(cancellationToken);
        lineNumber++;
        if (header is null || !SplitCsv(header).Select(x => x.Trim()).SequenceEqual(Header, StringComparer.OrdinalIgnoreCase))
            return new QuestionImportResult(rows, ["第1行表头必须为 stem,optionA,optionB,optionC,optionD,correctOption,points,visibility,isPublished。"]);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = SplitCsv(line);
            AddRow(rows, errors, values, lineNumber);
        }
        return new QuestionImportResult(rows, errors);
    }

    private static Task<QuestionImportResult> ParseXlsxAsync(Stream source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = ReadSharedStrings(archive);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheet is null) return Task.FromResult(new QuestionImportResult([], ["Excel 文件缺少第一个工作表。"]));
        using var sheetStream = sheet.Open();
        var document = XDocument.Load(sheetStream);
        var rows = new List<QuestionInput>();
        var errors = new List<string>();
        var lineNumber = 0;
        foreach (var row in document.Descendants().Where(x => x.Name.LocalName == "row"))
        {
            lineNumber++;
            var values = new string[Header.Length];
            foreach (var cell in row.Elements().Where(x => x.Name.LocalName == "c"))
            {
                var column = ColumnIndex(cell.Attribute("r")?.Value ?? string.Empty);
                if (column < 0 || column >= values.Length) continue;
                var value = cell.Elements().FirstOrDefault(x => x.Name.LocalName == "v")?.Value ?? cell.Descendants().FirstOrDefault(x => x.Name.LocalName == "t")?.Value ?? string.Empty;
                if (cell.Attribute("t")?.Value == "s" && int.TryParse(value, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count) value = sharedStrings[sharedIndex];
                values[column] = value;
            }
            if (lineNumber == 1)
            {
                if (!values.SequenceEqual(Header, StringComparer.OrdinalIgnoreCase)) return Task.FromResult(new QuestionImportResult(rows, ["第1行表头必须为 stem,optionA,optionB,optionC,optionD,correctOption,points,visibility,isPublished。"]));
                continue;
            }
            if (!values.All(string.IsNullOrWhiteSpace)) AddRow(rows, errors, values, lineNumber);
        }
        return Task.FromResult(new QuestionImportResult(rows, errors));
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var result = new List<string>();
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return result;
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        foreach (var item in document.Descendants().Where(x => x.Name.LocalName == "si")) result.Add(string.Concat(item.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value)));
        return result;
    }

    private static int ColumnIndex(string reference)
    {
        var index = 0; var found = false;
        foreach (var character in reference)
        {
            if (!char.IsLetter(character)) break;
            found = true; index = index * 26 + char.ToUpperInvariant(character) - 'A' + 1;
        }
        return found ? index - 1 : -1;
    }

    private static void AddRow(List<QuestionInput> rows, List<string> errors, IReadOnlyList<string> values, int lineNumber)
    {
        if (values.Count != Header.Length) { errors.Add($"第{lineNumber}行列数不正确。" ); return; }
        if (!int.TryParse(values[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var correct) || correct is < 1 or > 4)
        { errors.Add($"第{lineNumber}行 correctOption 必须为1到4。" ); return; }
        var points = 1;
        if (!string.IsNullOrWhiteSpace(values[6]) && (!int.TryParse(values[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out points) || points <= 0))
        { errors.Add($"第{lineNumber}行 points 必须为正整数。"); return; }
        var visibility = string.IsNullOrWhiteSpace(values[7]) ? QuestionVisibilities.TeacherPrivate : values[7].Trim();
        if (visibility is not (QuestionVisibilities.TeacherPrivate or QuestionVisibilities.ClassOnly or QuestionVisibilities.PublicPractice))
        { errors.Add($"第{lineNumber}行 visibility 无效。" ); return; }
        if (!bool.TryParse(values[8], out var published)) published = false;
        if (values.Take(5).Any(string.IsNullOrWhiteSpace)) { errors.Add($"第{lineNumber}行题干或选项为空。" ); return; }
        rows.Add(new QuestionInput(values[0].Trim(), values[1].Trim(), values[2].Trim(), values[3].Trim(), values[4].Trim(), correct, published, visibility, points));
    }

    private static IReadOnlyList<string> SplitCsv(string line)
    {
        var values = new List<string>(); var value = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { value.Append('"'); i++; continue; }
            if (c == '"') { quoted = !quoted; continue; }
            if (c == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); continue; }
            value.Append(c);
        }
        values.Add(value.ToString()); return values;
    }
}

public sealed record QuestionImportResult(IReadOnlyList<QuestionInput> Rows, IReadOnlyList<string> Errors);
