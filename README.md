# Print Maestro

Desktop Windows app for **batch sequential printing** of PDF, Office, image, and text documents.

> Repository: [github.com/dz0l/BPM](https://github.com/dz0l/BPM)  
> Version: **1.0.0**

---

## Distribution channels

| Channel | Format | Audience |
|---------|--------|----------|
| **Alpha** | Portable **ZIP** | developers, quick checks |
| **Public release (v1.0)** | **Inno Setup** `Setup.exe` | recommended for all users from GitHub |
| **Fallback** | ZIP | no install wizard |
| **MSIX** | optional | internal QA only (unsigned/self-signed) |

No commercial code signing is required for v1.0. Windows SmartScreen may show “Unknown publisher” — choose **Run anyway**. ClickOnce is **not** used.

---

## Features (v1.0)

- Print queue (up to 1000 files)
- Add files and folders via dialog
- Printer, paper size (A4/A3), orientation, copies, duplex
- Virtualized queue list
- Switch **Language** in Settings (gear icon in the title bar)
- Settings and history in `%AppData%\PrintMaestro\`
- Update check via GitHub Releases
- Fluent Design UI (WPF-UI, Mica)

- Sequential batch printing for PDF, images, text, and Office documents (via worker processes)
- Print history (last 100 jobs) and named print profiles
- Settings: language, profile management, About / third-party licenses

---

## Supported formats

| Category | Extensions |
|----------|------------|
| PDF | `.pdf` |
| Microsoft Office | `.doc`, `.docx`, `.xls`, `.xlsx` (requires Office; PowerPoint — позже) |
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

### Recommended — Setup.exe

1. Download `PrintMaestro-x.x.x-Setup.exe` from [Releases](https://github.com/dz0l/BPM/releases).
2. If SmartScreen warns about an unknown publisher, click **Run anyway** / **More info** → **Run anyway**.
3. Complete the installer; launch **Print Maestro** from the Start menu.

Requires [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) if not already installed.

### Portable ZIP

1. Download `PrintMaestro-x.x.x-win-x64.zip` from Releases.
2. Extract anywhere and run `PrintMaestro.exe`.

### Build from source

```powershell
git clone https://github.com/dz0l/BPM.git
cd BPM
dotnet build PrintMaestro.slnx -c Release
dotnet run --project src/PrintMaestro/PrintMaestro.csproj -c Release
```

Build packages locally:

```powershell
./scripts/build-release.ps1 -Configuration Release  # ZIP + Setup.exe + SHA256
./scripts/publish-portable.ps1 -Configuration Release   # ZIP only
./scripts/publish-inno.ps1 -Configuration Release       # ZIP + Setup.exe
./scripts/publish-msix.ps1 -Configuration Release       # MSIX (needs VS MSIX tooling)
```

Publish to GitHub Releases (creates assets automatically):

```powershell
git tag v1.0.0
git push origin v1.0.0
```

---

## Updates

The app is **offline-first**; network is used for updates only.

- On startup, GitHub Releases API is queried (`dz0l/BPM`).
- Update asset priority: **Setup.exe** → **ZIP** → MSIX (if present).
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

[github.com/dz0l/BPM/issues](https://github.com/dz0l/BPM/issues)
