# Tiny MD Editor

Tiny MD Editor is a lightweight Markdown editor for Windows. It is written in C# and uses Windows Forms, with no web server or cloud dependency.

## Features

- Fixed Markdown formatting toolbar with headings, emphasis, strikethrough, lists, task lists, block quotes, inline and fenced code, links, images, tables, horizontal rules, line breaks, auto-links, footnotes, and escaping.
- Multiple document tabs.
- Built-in side-by-side Markdown preview.
- Open and save Markdown files only.
- Built-in Spanish, English, and French interface options.
- Import GNU gettext `.po` and `.mo` translation catalogs from **About > Settings**.
- Portable Windows x64 build: extract and run, without installing .NET.

## Build from source

Requirements: Windows and the .NET 8 SDK or a newer SDK with the .NET 8 Windows targeting pack.

```powershell
cd v.3
dotnet build .\MarkdownLigero.csproj -c Release
```

Run the development build:

```powershell
dotnet run --project .\v.3\MarkdownLigero.csproj
```

Create a portable Windows x64 package:

```powershell
cd v.3
dotnet publish .\MarkdownLigero.csproj -c Release -r win-x64 --self-contained true -o .\portable
```

## AI assistance disclosure

This project was created and iterated with assistance from OpenAI Codex. Jacobo Feijóo reviewed the requested functionality and owns the project direction and release decisions.

## License

Tiny MD Editor is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE).

## Author

Created by Jacobo Feijóo.
