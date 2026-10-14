# Tiny MD Editor application source

This folder contains the C# Windows Forms application source code.

The repository-level [README](../README.md) contains the project overview, build instructions, contribution policy, AI-assistance disclosure, and licensing information.

## Local commands

```powershell
dotnet build .\MarkdownLigero.csproj -c Release
dotnet publish .\MarkdownLigero.csproj -c Release -r win-x64 --self-contained true -o .\portable
```

`portable` is a generated distribution folder and is deliberately not committed to Git.
