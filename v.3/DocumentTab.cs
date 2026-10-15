namespace MarkdownLigero;

public sealed class DocumentTab : UserControl
{
    private readonly SplitContainer _split;
    private readonly RichTextBox _preview;
    private bool _loading;
    private bool _isDirty;
    private bool _showPreview = true;

    public TextBox Editor { get; } = new()
    {
        AcceptsReturn = true,
        AcceptsTab = true,
        BorderStyle = BorderStyle.None,
        Dock = DockStyle.Fill,
        Font = new Font("Cascadia Mono", 11F),
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        WordWrap = true
    };

    public string? FilePath { get; private set; }
    public bool IsDirty => _isDirty;
    public string DisplayName => string.IsNullOrWhiteSpace(FilePath) ? "Sin título" : Path.GetFileName(FilePath);
    public string SuggestedFileName => string.IsNullOrWhiteSpace(FilePath) ? "documento.md" : Path.GetFileName(FilePath);
    public int Line => Editor.GetLineFromCharIndex(Editor.SelectionStart) + 1;
    public int Column => Editor.SelectionStart - Editor.GetFirstCharIndexFromLine(Line - 1) + 1;
    public event EventHandler? StateChanged;
    public event EventHandler? CaretChanged;

    public bool ShowPreview
    {
        get => _showPreview;
        set
        {
            _showPreview = value;
            _split.Panel2Collapsed = !value;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public DocumentTab()
    {
        Dock = DockStyle.Fill;
        _preview = new RichTextBox
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            ReadOnly = true,
            WordWrap = true
        };
        _split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.None,
            SplitterDistance = 550,
            SplitterWidth = 5
        };
        _split.Panel1.Padding = new Padding(12);
        _split.Panel2.Padding = new Padding(12);
        _split.Panel1.Controls.Add(Editor);
        _split.Panel2.Controls.Add(_preview);
        Controls.Add(_split);

        Editor.TextChanged += (_, _) =>
        {
            if (!_loading)
                _isDirty = true;
            RenderPreview();
            StateChanged?.Invoke(this, EventArgs.Empty);
        };
        Editor.KeyUp += (_, _) => CaretChanged?.Invoke(this, EventArgs.Empty);
        Editor.MouseUp += (_, _) => CaretChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadDocument(string contents, string? filePath)
    {
        _loading = true;
        Editor.Text = contents;
        FilePath = filePath;
        _isDirty = false;
        _loading = false;
        RenderPreview();
    }

    public void MarkSaved(string path)
    {
        FilePath = path;
        _isDirty = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SurroundSelection(string before, string after, string placeholder)
    {
        var selected = Editor.SelectedText;
        if (string.IsNullOrEmpty(selected))
        {
            Editor.SelectedText = before + placeholder + after;
            Editor.Select(Editor.SelectionStart - after.Length - placeholder.Length, placeholder.Length);
        }
        else
        {
            Editor.SelectedText = before + selected + after;
            Editor.Select(Editor.SelectionStart - (selected.Length + after.Length), selected.Length);
        }
        Editor.Focus();
    }

    public void InsertLink()
    {
        var selected = string.IsNullOrWhiteSpace(Editor.SelectedText) ? "enlace" : Editor.SelectedText;
        Editor.SelectedText = $"[{selected}](https://)";
        var urlStart = Editor.SelectionStart - 9;
        Editor.Select(urlStart, 8);
        Editor.Focus();
    }

    public void InsertImage()
    {
        var selected = string.IsNullOrWhiteSpace(Editor.SelectedText) ? "texto alternativo" : Editor.SelectedText;
        Editor.SelectedText = $"![{selected}](https://)";
        Editor.Select(Editor.SelectionStart - 9, 8);
        Editor.Focus();
    }

    public void InsertCodeBlock()
    {
        var selected = string.IsNullOrWhiteSpace(Editor.SelectedText) ? "código" : Editor.SelectedText;
        Editor.SelectedText = $"```\n{selected}\n```";
        Editor.Select(Editor.SelectionStart - (selected.Length + 4), selected.Length);
        Editor.Focus();
    }

    public void InsertTable()
    {
        const string table = "| Columna 1 | Columna 2 |\n| :--- | ---: |\n| Contenido | Contenido |";
        Editor.SelectedText = table;
        Editor.Select(Editor.SelectionStart - table.Length, table.Length);
        Editor.Focus();
    }

    public void InsertText(string text)
    {
        Editor.SelectedText = text;
        Editor.Focus();
    }

    public void EscapeSelection()
    {
        var selected = string.IsNullOrEmpty(Editor.SelectedText) ? "carácter" : Editor.SelectedText;
        var escaped = string.Concat(selected.Select(character => "\\" + character));
        Editor.SelectedText = escaped;
        Editor.Select(Editor.SelectionStart - escaped.Length, escaped.Length);
        Editor.Focus();
    }

    public void SetHeading(int level)
    {
        var lineIndex = Editor.GetLineFromCharIndex(Editor.SelectionStart);
        var lines = Editor.Lines;
        var current = lines[lineIndex];
        var trim = current.TrimStart();
        while (trim.StartsWith("#", StringComparison.Ordinal)) trim = trim[1..];
        if (trim.StartsWith(" ", StringComparison.Ordinal)) trim = trim[1..];
        lines[lineIndex] = new string('#', level) + " " + trim;
        Editor.Lines = lines;
        var start = Editor.GetFirstCharIndexFromLine(lineIndex);
        Editor.Select(start, lines[lineIndex].Length);
        Editor.Focus();
    }

    public void ToggleLinePrefix(string prefix)
    {
        var selectionStart = Editor.SelectionStart;
        var selectionEnd = selectionStart + Editor.SelectionLength;
        var startLine = Editor.GetLineFromCharIndex(selectionStart);
        var endLine = Editor.GetLineFromCharIndex(selectionEnd);
        if (Editor.SelectionLength > 0 && selectionEnd == Editor.GetFirstCharIndexFromLine(endLine))
            endLine--;

        var lines = Editor.Lines;
        var remove = Enumerable.Range(startLine, endLine - startLine + 1).All(index => lines[index].StartsWith(prefix, StringComparison.Ordinal));
        for (var index = startLine; index <= endLine; index++)
            lines[index] = remove ? lines[index][prefix.Length..] : prefix + lines[index];

        Editor.Lines = lines;
        var newStart = Editor.GetFirstCharIndexFromLine(startLine);
        var newEnd = Editor.GetFirstCharIndexFromLine(endLine) + lines[endLine].Length;
        Editor.Select(newStart, Math.Max(0, newEnd - newStart));
        Editor.Focus();
    }

    private void RenderPreview()
    {
        if (_preview.IsDisposed)
            return;

        _preview.SuspendLayout();
        _preview.Clear();
        foreach (var rawLine in Editor.Lines)
        {
            var line = rawLine;
            var level = 0;
            while (level < line.Length && line[level] == '#')
                level++;

            if (level is > 0 and <= 6 && line.Length > level && line[level] == ' ')
            {
                AppendPreview(line[(level + 1)..], Math.Max(12, 24 - level * 2), FontStyle.Bold);
            }
            else if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                AppendPreview("❝ " + line[2..], 10, FontStyle.Italic, Color.DimGray);
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                AppendPreview("• " + line[2..], 10, FontStyle.Regular);
            }
            else
            {
                AppendPreview(line, 10, FontStyle.Regular);
            }
            _preview.AppendText(Environment.NewLine);
        }
        _preview.Select(0, 0);
        _preview.ResumeLayout();
    }

    private void AppendPreview(string text, float size, FontStyle style, Color? color = null)
    {
        _preview.SelectionFont = new Font("Segoe UI", size, style);
        _preview.SelectionColor = color ?? Color.FromArgb(35, 35, 35);
        _preview.AppendText(text.Replace("**", string.Empty).Replace("`", string.Empty));
    }
}
