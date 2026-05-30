# Print Maestro

Desktop Windows app for **batch sequential printing** of PDF, Office, image, and text documents.

> Repository: [github.com/dz0l/PrintMaestro](https://github.com/dz0l/PrintMaestro)  
> Version: **0.1.0** (MVP scaffold)

---

## Distribution channels

| Channel | Format | Audience |
|---------|--------|----------|
| **Alpha** | Portable **ZIP** | developers, quick local checks |
| **Beta** | **Inno Setup** `Setup.exe` | testers from GitHub Releases |
| **Production** | Signed **MSIX** | general users (Windows 11 App Installer) |

ClickOnce is **not** used.

---

## Features (v0.1)

- Print queue (up to 1000 files)
- Add files and folders via dialog
- Printer, paper size (A4/A3), orientation, copies, duplex
- Virtualized queue list
- Switch **Language** in Settings (gear icon in the title bar)
- Settings and history in `%AppData%\PrintMaestro\`
- Update check via GitHub Releases
- Fluent Design UI (WPF-UI, Mica)

Document printing and worker processes are **in development**.

---

## Supported formats (target)

| Category | Extensions |
|----------|------------|
| PDF | `.pdf` |
| Microsoft Office | `.doc`, `.docx`, `.xls`, `.xlsx` (requires Office) |
| Text | `.txt` |
| Images | `.png`, `.jpg`, `.jpeg`, `.bmp`, `.heic`, `.tiff`, `.webp` |

---

## Requirements

- Windows 10 1809 (17763) or later / Windows 11
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (ZIP and Setup.exe builds)
- Microsoft Office for Office formats
- HEIF codecs for HEIC (optional)

---

## Installation

### Beta — Setup.exe (recommended for testers)

1. Download `PrintMaestro-x.x.x-Setup.exe` from [Releases](https://github.com/dz0l/PrintMaestro/releases).
2. Run the installer (SmartScreen may show “Unknown publisher” — choose **Run anyway**).
3. Launch **Print Maestro** from the Start menu.

Requires .NET 10 Desktop Runtime if not already installed.

### Alpha — Portable ZIP

1. Download `PrintMaestro-x.x.x-win-x64.zip` from Releases.
2. Extract anywhere and run `PrintMaestro.exe`.

### Production — MSIX (when signed builds are published)

1. Download `PrintMaestro-x.x.x-x64.msix` from Releases.
2. Double-click — Windows App Installer installs in one step.

### Build from source

```powershell
git clone https://github.com/dz0l/PrintMaestro.git
cd PrintMaestro
dotnet build PrintMaestro.slnx -c Release
dotnet run --project src/PrintMaestro/PrintMaestro.csproj -c Release
```

Build packages locally:

```powershell
./scripts/publish-portable.ps1 -Configuration Release   # ZIP (alpha)
./scripts/publish-inno.ps1 -Configuration Release       # ZIP + Setup.exe (beta)
./scripts/publish-msix.ps1 -Configuration Release       # MSIX (needs VS MSIX tooling)
```

---

## Updates

The app is **offline-first**; network is used for updates only.

- On startup, GitHub Releases API is queried (`dz0l/PrintMaestro`).
- Update asset priority: **MSIX** → **Setup.exe** → **ZIP**.
- Disable: `%AppData%\PrintMaestro\settings.json` → `"checkUpdatesOnStartup": false`.

---

## Localization

- Default language: **English**
- Alternative: **Russian** (Settings → Language)
- Translation files: `Assets/Localization/locale.en.json`, `locale.ru.json`

Locale is auto-detected from Windows UI language on first launch.

---

## Data locations

| Path | Purpose |
|------|---------|
| `%AppData%\PrintMaestro\settings.json` | Settings (incl. locale) |
| `%AppData%\PrintMaestro\history.db` | Print history (100 entries) |
| `%AppData%\PrintMaestro\logs\` | Logs |
| `%AppData%\PrintMaestro\thumbnails\` | Thumbnail cache |

---

## License

MIT — see [LICENSE](LICENSE).

---

## Feedback

[github.com/dz0l/PrintMaestro/issues](https://github.com/dz0l/PrintMaestro/issues)
