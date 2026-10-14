using System.Buffers.Binary;
using System.Text;

namespace MarkdownLigero;

public sealed class UiTranslations
{
    private Dictionary<string, string> _catalog = new(StringComparer.Ordinal);
    public string Translate(string source) => _catalog.TryGetValue(source, out var translated) && !string.IsNullOrWhiteSpace(translated) ? translated : source;
    public void UseBuiltIn(string language) => _catalog = language switch { "en" => English(), "fr" => French(), _ => new Dictionary<string, string>(StringComparer.Ordinal) };

    public bool LoadCatalog(string path, out string error)
    {
        try { _catalog = Path.GetExtension(path).Equals(".mo", StringComparison.OrdinalIgnoreCase) ? ReadMo(File.ReadAllBytes(path)) : ReadPo(File.ReadAllLines(path)); error = string.Empty; return true; }
        catch (Exception ex) { error = ex.Message; return false; }
    }

    private static Dictionary<string, string> ReadPo(IEnumerable<string> lines)
    {
        var catalog = new Dictionary<string, string>(StringComparer.Ordinal); string? id = null; var value = new StringBuilder(); var field = 0;
        void Commit() { if (!string.IsNullOrEmpty(id) && value.Length > 0) catalog[id] = value.ToString(); id = null; value.Clear(); field = 0; }
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("msgid ", StringComparison.Ordinal)) { Commit(); id = Unquote(line[6..]); field = 1; }
            else if (line.StartsWith("msgstr ", StringComparison.Ordinal)) { value.Append(Unquote(line[7..])); field = 2; }
            else if (line.StartsWith('"') && field == 1 && id is not null) id += Unquote(line);
            else if (line.StartsWith('"') && field == 2) value.Append(Unquote(line));
        }
        Commit(); return catalog;
    }
    private static string Unquote(string text) { text = text.Trim(); if (text.Length >= 2 && text[0] == '"' && text[^1] == '"') text = text[1..^1]; return text.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\"); }

    private static Dictionary<string, string> ReadMo(byte[] bytes)
    {
        if (bytes.Length < 28) throw new InvalidDataException("El archivo .mo es demasiado pequeño.");
        var little = BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x950412de;
        if (!little && BinaryPrimitives.ReadUInt32BigEndian(bytes) != 0x950412de) throw new InvalidDataException("El archivo no es un catálogo GNU gettext .mo válido.");
        uint Read(int offset) => little ? BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4)) : BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
        var count = Read(8); var originals = Read(12); var translations = Read(16);
        if (count > 100_000 || originals + count * 8 > bytes.Length || translations + count * 8 > bytes.Length) throw new InvalidDataException("Las tablas del archivo .mo no son válidas.");
        var catalog = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < count; index++)
        {
            var sourceLength = Read((int)originals + index * 8); var sourceOffset = Read((int)originals + index * 8 + 4);
            var translatedLength = Read((int)translations + index * 8); var translatedOffset = Read((int)translations + index * 8 + 4);
            if (sourceOffset + sourceLength > bytes.Length || translatedOffset + translatedLength > bytes.Length) continue;
            var source = Encoding.UTF8.GetString(bytes, (int)sourceOffset, (int)sourceLength).Split('\0')[0];
            var translated = Encoding.UTF8.GetString(bytes, (int)translatedOffset, (int)translatedLength).Split('\0')[0];
            var context = source.IndexOf('\u0004'); if (context >= 0) source = source[(context + 1)..];
            if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(translated)) catalog[source] = translated;
        }
        return catalog;
    }

    private static Dictionary<string, string> English() => new(StringComparer.Ordinal)
    {
        ["Archivo"] = "File", ["Nuevo"] = "New", ["Abrir…"] = "Open…", ["Guardar"] = "Save", ["Guardar como…"] = "Save as…", ["Cerrar pestaña"] = "Close tab", ["Salir"] = "Exit", ["Edición"] = "Edit", ["Deshacer"] = "Undo", ["Cortar"] = "Cut", ["Copiar"] = "Copy", ["Pegar"] = "Paste", ["Seleccionar todo"] = "Select all", ["Ver"] = "View", ["Vista previa"] = "Preview", ["Acerca de"] = "About", ["Información"] = "Information", ["Ajustes…"] = "Settings…", ["Documento sin guardar — se guardará como .md"] = "Unsaved document — it will be saved as .md", ["Línea {0}, columna {1}"] = "Line {0}, column {1}", ["Abrir archivos Markdown"] = "Open Markdown files", ["Guardar Markdown"] = "Save Markdown", ["No se pudo abrir el archivo."] = "The file could not be opened.", ["No se pudo guardar el archivo."] = "The file could not be saved.", ["¿Guardar los cambios en \"{0}\"?"] = "Save changes to \"{0}\"?", ["Negrita"] = "Bold", ["Cursiva"] = "Italic", ["Negrita y cursiva"] = "Bold and italic", ["Tachado"] = "Strikethrough", ["Lista con viñetas"] = "Bulleted list", ["Lista numerada"] = "Numbered list", ["Lista de tareas"] = "Task list", ["Cita"] = "Quote", ["Código en línea"] = "Inline code", ["Bloque de código"] = "Code block", ["Enlace"] = "Link", ["Imagen"] = "Image", ["Tabla"] = "Table", ["Regla horizontal"] = "Horizontal rule", ["Salto de línea"] = "Line break", ["Enlace automático"] = "Autolink", ["Nota al pie"] = "Footnote", ["Escapar Markdown"] = "Escape Markdown", ["Escapar caracteres Markdown"] = "Escape Markdown characters", ["Título de nivel {0}"] = "Heading level {0}"
    };
    private static Dictionary<string, string> French() => new(StringComparer.Ordinal)
    {
        ["Archivo"] = "Fichier", ["Nuevo"] = "Nouveau", ["Abrir…"] = "Ouvrir…", ["Guardar"] = "Enregistrer", ["Guardar como…"] = "Enregistrer sous…", ["Cerrar pestaña"] = "Fermer l’onglet", ["Salir"] = "Quitter", ["Edición"] = "Édition", ["Deshacer"] = "Annuler", ["Cortar"] = "Couper", ["Copiar"] = "Copier", ["Pegar"] = "Coller", ["Seleccionar todo"] = "Tout sélectionner", ["Ver"] = "Affichage", ["Vista previa"] = "Aperçu", ["Acerca de"] = "À propos", ["Información"] = "Informations", ["Ajustes…"] = "Paramètres…", ["Documento sin guardar — se guardará como .md"] = "Document non enregistré — il sera enregistré en .md", ["Línea {0}, columna {1}"] = "Ligne {0}, colonne {1}", ["Abrir archivos Markdown"] = "Ouvrir des fichiers Markdown", ["Guardar Markdown"] = "Enregistrer le Markdown", ["No se pudo abrir el archivo."] = "Impossible d’ouvrir le fichier.", ["No se pudo guardar el archivo."] = "Impossible d’enregistrer le fichier.", ["¿Guardar los cambios en \"{0}\"?"] = "Enregistrer les modifications de \"{0}\" ?", ["Negrita"] = "Gras", ["Cursiva"] = "Italique", ["Negrita y cursiva"] = "Gras et italique", ["Tachado"] = "Barré", ["Lista con viñetas"] = "Liste à puces", ["Lista numerada"] = "Liste numérotée", ["Lista de tareas"] = "Liste de tâches", ["Cita"] = "Citation", ["Código en línea"] = "Code en ligne", ["Bloque de código"] = "Bloc de code", ["Enlace"] = "Lien", ["Imagen"] = "Image", ["Tabla"] = "Tableau", ["Regla horizontal"] = "Règle horizontale", ["Salto de línea"] = "Saut de ligne", ["Enlace automático"] = "Lien automatique", ["Nota al pie"] = "Note de bas de page", ["Escapar Markdown"] = "Échapper Markdown", ["Escapar caracteres Markdown"] = "Échapper les caractères Markdown"
    };
}
