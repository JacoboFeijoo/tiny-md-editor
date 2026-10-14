namespace MarkdownLigero;

public sealed class SettingsForm : Form
{
    private readonly UiTranslations _translations;
    private readonly Action _languageChanged;
    public SettingsForm(UiTranslations translations, Action languageChanged)
    {
        _translations = translations; _languageChanged = languageChanged;
        Text = "Ajustes"; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ClientSize = new Size(440, 210);
        var title = new Label { AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(18, 18), Text = "Idioma de la interfaz" };
        var info = new Label { AutoSize = false, Location = new Point(18, 48), Size = new Size(400, 40), Text = "Seleccione un idioma integrado o cargue un catálogo GNU gettext (.po o .mo)." };
        var spanish = new Button { Text = "Español", Location = new Point(18, 98), Size = new Size(120, 32) };
        var english = new Button { Text = "English", Location = new Point(155, 98), Size = new Size(120, 32) };
        var french = new Button { Text = "Français", Location = new Point(292, 98), Size = new Size(120, 32) };
        var load = new Button { Text = "Cargar .po / .mo…", Location = new Point(18, 148), Size = new Size(180, 32) };
        var close = new Button { Text = "Cerrar", DialogResult = DialogResult.OK, Location = new Point(312, 148), Size = new Size(100, 32) };
        spanish.Click += (_, _) => SetBuiltIn("es"); english.Click += (_, _) => SetBuiltIn("en"); french.Click += (_, _) => SetBuiltIn("fr"); load.Click += (_, _) => LoadCatalog();
        Controls.AddRange([title, info, spanish, english, french, load, close]); AcceptButton = close;
    }
    private void SetBuiltIn(string language) { _translations.UseBuiltIn(language); _languageChanged(); }
    private void LoadCatalog()
    {
        using var dialog = new OpenFileDialog { Filter = "Catálogos GNU gettext (*.po;*.mo)|*.po;*.mo", Title = "Cargar traducción" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (_translations.LoadCatalog(dialog.FileName, out var error)) { _languageChanged(); MessageBox.Show(this, "Traducción cargada correctamente.", "Tiny MD Editor", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        else MessageBox.Show(this, $"No se pudo cargar el catálogo.\n\n{error}", "Tiny MD Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
