using System.Text;

namespace MarkdownLigero;

public sealed class MainForm : Form
{
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, DrawMode = TabDrawMode.OwnerDrawFixed, Padding = new Point(16, 4) };
    private readonly ToolStripStatusLabel _fileStatus = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _positionStatus = new() { TextAlign = ContentAlignment.MiddleRight };
    private readonly UiTranslations _translations = new();
    private ToolStripButton? _previewButton;
    private MenuStrip? _menu;
    private ToolStrip? _toolbar;

    public MainForm()
    {
        Text = "Tiny MD Editor";
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 520);
        Size = new Size(1180, 720);
        Font = new Font("Segoe UI", 9F);
        var status = new StatusStrip { SizingGrip = false };
        status.Items.AddRange([_fileStatus, new ToolStripStatusLabel("  "), _positionStatus]);
        Controls.Add(_tabs);
        Controls.Add(status);
        _tabs.SelectedIndexChanged += (_, _) => UpdateUi();
        _tabs.ControlAdded += (_, _) => UpdateUi();
        _tabs.ControlRemoved += (_, _) => UpdateUi();
        _tabs.DrawItem += DrawTab;
        _tabs.MouseDown += TabsMouseDown;
        BuildChrome();
        CreateDocument();
    }

    private string T(string source) => _translations.Translate(source);

    private void BuildChrome()
    {
        if (_menu is not null) { Controls.Remove(_menu); _menu.Dispose(); }
        if (_toolbar is not null) { Controls.Remove(_toolbar); _toolbar.Dispose(); }
        _menu = CreateMenu();
        _toolbar = CreateToolbar();
        Controls.Add(_toolbar);
        Controls.Add(_menu);
        UpdateUi();
    }

    private MenuStrip CreateMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };
        var file = new ToolStripMenuItem(T("Archivo"));
        file.DropDownItems.Add(Item("Nuevo", Keys.Control | Keys.N, (_, _) => CreateDocument()));
        file.DropDownItems.Add(Item("Abrir…", Keys.Control | Keys.O, (_, _) => OpenFiles()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("Guardar", Keys.Control | Keys.S, (_, _) => SaveCurrent()));
        file.DropDownItems.Add(Item("Guardar como…", Keys.Control | Keys.Shift | Keys.S, (_, _) => SaveCurrent(true)));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("Cerrar pestaña", Keys.Control | Keys.W, (_, _) => CloseCurrentDocument()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("Salir", Keys.None, (_, _) => Close()));

        var edit = new ToolStripMenuItem(T("Edición"));
        edit.DropDownItems.Add(Item("Deshacer", Keys.Control | Keys.Z, (_, _) => ActiveDocument?.Editor.Undo()));
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(Item("Cortar", Keys.Control | Keys.X, (_, _) => ActiveDocument?.Editor.Cut()));
        edit.DropDownItems.Add(Item("Copiar", Keys.Control | Keys.C, (_, _) => ActiveDocument?.Editor.Copy()));
        edit.DropDownItems.Add(Item("Pegar", Keys.Control | Keys.V, (_, _) => ActiveDocument?.Editor.Paste()));
        edit.DropDownItems.Add(Item("Seleccionar todo", Keys.Control | Keys.A, (_, _) => ActiveDocument?.Editor.SelectAll()));

        var markdown = new ToolStripMenuItem("Markdown");
        markdown.DropDownItems.Add(Item("Negrita", Keys.Control | Keys.B, (_, _) => Surround("**", "**", "texto en negrita")));
        markdown.DropDownItems.Add(Item("Cursiva", Keys.Control | Keys.I, (_, _) => Surround("*", "*", "texto en cursiva")));
        markdown.DropDownItems.Add(Item("Negrita y cursiva", Keys.None, (_, _) => Surround("***", "***", "texto destacado")));
        markdown.DropDownItems.Add(Item("Tachado", Keys.None, (_, _) => Surround("~~", "~~", "texto tachado")));
        markdown.DropDownItems.Add(new ToolStripSeparator());
        for (var level = 1; level <= 6; level++) { var headingLevel = level; markdown.DropDownItems.Add(Item($"Título {level}", Keys.None, (_, _) => ActiveDocument?.SetHeading(headingLevel))); }
        markdown.DropDownItems.Add(new ToolStripSeparator());
        markdown.DropDownItems.Add(Item("Lista con viñetas", Keys.Control | Keys.L, (_, _) => PrefixLines("- ")));
        markdown.DropDownItems.Add(Item("Lista numerada", Keys.None, (_, _) => PrefixLines("1. ")));
        markdown.DropDownItems.Add(Item("Lista de tareas", Keys.None, (_, _) => PrefixLines("- [ ] ")));
        markdown.DropDownItems.Add(Item("Cita", Keys.Control | Keys.Q, (_, _) => PrefixLines("> ")));
        markdown.DropDownItems.Add(Item("Código en línea", Keys.Control | Keys.K, (_, _) => Surround("`", "`", "código")));
        markdown.DropDownItems.Add(Item("Bloque de código", Keys.None, (_, _) => ActiveDocument?.InsertCodeBlock()));
        markdown.DropDownItems.Add(Item("Enlace", Keys.Control | Keys.U, (_, _) => ActiveDocument?.InsertLink()));
        markdown.DropDownItems.Add(Item("Imagen", Keys.None, (_, _) => ActiveDocument?.InsertImage()));
        markdown.DropDownItems.Add(Item("Tabla", Keys.None, (_, _) => ActiveDocument?.InsertTable()));
        markdown.DropDownItems.Add(Item("Regla horizontal", Keys.None, (_, _) => ActiveDocument?.InsertText("\n---\n")));
        markdown.DropDownItems.Add(Item("Salto de línea", Keys.None, (_, _) => ActiveDocument?.InsertText("  \n")));
        markdown.DropDownItems.Add(Item("Enlace automático", Keys.None, (_, _) => Surround("<", ">", "https://")));
        markdown.DropDownItems.Add(Item("Nota al pie", Keys.None, (_, _) => ActiveDocument?.InsertText("[^1]\n\n[^1]: Nota")));
        markdown.DropDownItems.Add(Item("Escapar Markdown", Keys.None, (_, _) => ActiveDocument?.EscapeSelection()));

        var view = new ToolStripMenuItem(T("Ver"));
        var preview = Item("Vista previa", Keys.F6, (_, _) => TogglePreview());
        preview.CheckOnClick = true; preview.Checked = ActiveDocument?.ShowPreview ?? true;
        preview.CheckedChanged += (_, _) => { if (_previewButton is not null && _previewButton.Checked != preview.Checked) _previewButton.Checked = preview.Checked; };
        view.DropDownItems.Add(preview);

        var about = new ToolStripMenuItem(T("Acerca de"));
        about.DropDownItems.Add(Item("Información", Keys.None, (_, _) => ShowAbout()));
        about.DropDownItems.Add(Item("Ajustes…", Keys.None, (_, _) => ShowSettings()));
        menu.Items.AddRange([file, edit, markdown, view, about]);
        return menu;
    }

    private ToolStrip CreateToolbar()
    {
        var toolbar = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, RenderMode = ToolStripRenderMode.System, CanOverflow = true };
        toolbar.Items.Add(Button("B", "Negrita", (_, _) => Surround("**", "**", "texto en negrita"), true));
        toolbar.Items.Add(Button("I", "Cursiva", (_, _) => Surround("*", "*", "texto en cursiva"), true));
        toolbar.Items.Add(Button("BI", "Negrita y cursiva", (_, _) => Surround("***", "***", "texto destacado"), true));
        toolbar.Items.Add(Button("S", "Tachado", (_, _) => Surround("~~", "~~", "texto tachado")));
        toolbar.Items.Add(new ToolStripSeparator());
        for (var level = 1; level <= 6; level++) { var headingLevel = level; toolbar.Items.Add(Button($"H{level}", $"Título de nivel {level}", (_, _) => ActiveDocument?.SetHeading(headingLevel))); }
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(Button("•", "Lista con viñetas", (_, _) => PrefixLines("- ")));
        toolbar.Items.Add(Button("1.", "Lista numerada", (_, _) => PrefixLines("1. ")));
        toolbar.Items.Add(Button("☐", "Lista de tareas", (_, _) => PrefixLines("- [ ] ")));
        toolbar.Items.Add(Button("❝", "Cita", (_, _) => PrefixLines("> ")));
        toolbar.Items.Add(Button("`", "Código en línea", (_, _) => Surround("`", "`", "código")));
        toolbar.Items.Add(Button("```", "Bloque de código", (_, _) => ActiveDocument?.InsertCodeBlock()));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(Button("🔗", "Enlace", (_, _) => ActiveDocument?.InsertLink()));
        toolbar.Items.Add(Button("▣", "Imagen", (_, _) => ActiveDocument?.InsertImage()));
        toolbar.Items.Add(Button("▦", "Tabla", (_, _) => ActiveDocument?.InsertTable()));
        toolbar.Items.Add(Button("—", "Regla horizontal", (_, _) => ActiveDocument?.InsertText("\n---\n")));
        toolbar.Items.Add(Button("↵", "Salto de línea Markdown", (_, _) => ActiveDocument?.InsertText("  \n")));
        toolbar.Items.Add(Button("<>", "Enlace automático", (_, _) => Surround("<", ">", "https://")));
        toolbar.Items.Add(Button("¹", "Nota al pie", (_, _) => ActiveDocument?.InsertText("[^1]\n\n[^1]: Nota")));
        toolbar.Items.Add(Button("\\", "Escapar caracteres Markdown", (_, _) => ActiveDocument?.EscapeSelection()));
        toolbar.Items.Add(new ToolStripSeparator());
        _previewButton = new ToolStripButton(T("Vista previa")) { CheckOnClick = true, Checked = ActiveDocument?.ShowPreview ?? true, DisplayStyle = ToolStripItemDisplayStyle.Text };
        _previewButton.CheckedChanged += (_, _) => TogglePreview();
        toolbar.Items.Add(_previewButton);
        return toolbar;
    }

    private ToolStripMenuItem Item(string source, Keys shortcut, EventHandler action) => new(T(source), null, action) { ShortcutKeys = shortcut };
    private ToolStripButton Button(string text, string tooltip, EventHandler action, bool bold = false) => new(text, null, action) { ToolTipText = T(tooltip), Font = bold ? new Font("Segoe UI", 9F, FontStyle.Bold) : new Font("Segoe UI", 9F) };
    private DocumentTab? ActiveDocument => _tabs.SelectedTab?.Tag as DocumentTab;

    private void CreateDocument(string? contents = null, string? filePath = null)
    {
        var document = new DocumentTab(); document.LoadDocument(contents ?? string.Empty, filePath);
        document.StateChanged += (_, _) => UpdateUi(); document.CaretChanged += (_, _) => UpdateUi();
        var page = new TabPage { Tag = document }; page.Controls.Add(document); _tabs.TabPages.Add(page); _tabs.SelectedTab = page; UpdateUi(); document.Editor.Focus();
    }
    private void OpenFiles()
    {
        using var dialog = new OpenFileDialog { Filter = "Archivos Markdown (*.md;*.markdown)|*.md;*.markdown", Multiselect = true, Title = T("Abrir archivos Markdown") };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var path in dialog.FileNames)
        {
            var existing = FindDocument(path); if (existing is not null) { _tabs.SelectedTab = existing; continue; }
            try { CreateDocument(File.ReadAllText(path), path); } catch (Exception ex) { MessageBox.Show(this, $"{T("No se pudo abrir el archivo.")}\n\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
    private TabPage? FindDocument(string path) => _tabs.TabPages.Cast<TabPage>().FirstOrDefault(page => string.Equals((page.Tag as DocumentTab)?.FilePath, path, StringComparison.OrdinalIgnoreCase));
    private void SaveCurrent(bool saveAs = false) { if (ActiveDocument is not null) SaveDocument(ActiveDocument, saveAs); }
    private bool SaveDocument(DocumentTab document, bool saveAs = false)
    {
        var path = document.FilePath;
        if (saveAs || string.IsNullOrWhiteSpace(path)) { using var dialog = new SaveFileDialog { Filter = "Archivo Markdown (*.md)|*.md", DefaultExt = "md", AddExtension = true, FileName = document.SuggestedFileName, Title = T("Guardar Markdown") }; if (dialog.ShowDialog(this) != DialogResult.OK) return false; path = dialog.FileName; }
        if (!string.Equals(Path.GetExtension(path), ".md", StringComparison.OrdinalIgnoreCase)) path += ".md";
        try { File.WriteAllText(path, document.Editor.Text, new UTF8Encoding(false)); document.MarkSaved(path); UpdateUi(); return true; } catch (Exception ex) { MessageBox.Show(this, $"{T("No se pudo guardar el archivo.")}\n\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
    }
    private void CloseCurrentDocument()
    {
        if (_tabs.SelectedTab is { } page)
            CloseDocument(page);
    }

    private void CloseDocument(TabPage page)
    {
        if (!ConfirmClose(page.Tag as DocumentTab))
            return;

        _tabs.TabPages.Remove(page);
        page.Dispose();
        if (_tabs.TabCount == 0)
            CreateDocument();
    }

    private void DrawTab(object? sender, DrawItemEventArgs e)
    {
        var page = _tabs.TabPages[e.Index];
        var bounds = _tabs.GetTabRect(e.Index);
        var selected = e.State.HasFlag(DrawItemState.Selected);
        using var background = new SolidBrush(selected ? SystemColors.ControlLightLight : SystemColors.Control);
        e.Graphics.FillRectangle(background, bounds);
        TextRenderer.DrawText(e.Graphics, page.Text, Font, new Rectangle(bounds.X + 8, bounds.Y + 3, bounds.Width - 30, bounds.Height - 6), SystemColors.ControlText, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var close = GetCloseButtonBounds(e.Index);
        using var pen = new Pen(SystemColors.ControlDarkDark, 1.6F);
        e.Graphics.DrawLine(pen, close.Left + 4, close.Top + 4, close.Right - 4, close.Bottom - 4);
        e.Graphics.DrawLine(pen, close.Right - 4, close.Top + 4, close.Left + 4, close.Bottom - 4);
    }

    private void TabsMouseDown(object? sender, MouseEventArgs e)
    {
        for (var index = 0; index < _tabs.TabPages.Count; index++)
        {
            if (!GetCloseButtonBounds(index).Contains(e.Location))
                continue;

            _tabs.SelectedIndex = index;
            CloseDocument(_tabs.TabPages[index]);
            return;
        }
    }

    private Rectangle GetCloseButtonBounds(int index)
    {
        var tab = _tabs.GetTabRect(index);
        return new Rectangle(tab.Right - 21, tab.Top + (tab.Height - 16) / 2, 16, 16);
    }
    private bool ConfirmClose(DocumentTab? document)
    {
        if (document is null || !document.IsDirty) return true;
        var answer = MessageBox.Show(this, string.Format(T("¿Guardar los cambios en \"{0}\"?"), document.DisplayName), Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        return answer switch { DialogResult.Yes => SaveDocument(document), DialogResult.No => true, _ => false };
    }
    private void Surround(string before, string after, string placeholder) => ActiveDocument?.SurroundSelection(before, after, placeholder);
    private void PrefixLines(string prefix) => ActiveDocument?.ToggleLinePrefix(prefix);
    private void TogglePreview() { if (ActiveDocument is not null && _previewButton is not null) ActiveDocument.ShowPreview = _previewButton.Checked; }
    private void ShowAbout() => MessageBox.Show(this, "Tiny MD Editor\n\nHecho por Jacobo Feijóo bajo licencia GPL-3.0.\n\nEn Acerca de > Ajustes… puede seleccionar Español, English o Français, o cargar un catálogo GNU gettext .po/.mo.", T("Acerca de"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    private void ShowSettings() { using var dialog = new SettingsForm(_translations, ApplyLanguage); dialog.ShowDialog(this); }
    private void ApplyLanguage() { BuildChrome(); UpdateUi(); }
    private void UpdateUi()
    {
        foreach (TabPage page in _tabs.TabPages) if (page.Tag is DocumentTab document) page.Text = document.IsDirty ? $"{document.DisplayName} *" : document.DisplayName;
        if (ActiveDocument is { } active) { _fileStatus.Text = string.IsNullOrEmpty(active.FilePath) ? T("Documento sin guardar — se guardará como .md") : active.FilePath; _positionStatus.Text = string.Format(T("Línea {0}, columna {1}"), active.Line, active.Column); if (_previewButton is not null && active.ShowPreview != _previewButton.Checked) _previewButton.Checked = active.ShowPreview; }
        else { _fileStatus.Text = string.Empty; _positionStatus.Text = string.Empty; }
    }
    protected override void OnFormClosing(FormClosingEventArgs e) { foreach (TabPage page in _tabs.TabPages) if (!ConfirmClose(page.Tag as DocumentTab)) { e.Cancel = true; return; } base.OnFormClosing(e); }
}
