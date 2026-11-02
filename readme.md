# AtaraxiaAI

AtaraxiaAI is a .NET desktop sandbox for experimenting with machine learning and AI capabilities in a virtual-assistant interface. It brings speech, vision, and small service-backed skills together in an Avalonia UI. The project is experimental; its features and platform support are still evolving.

## What is here now

- An Avalonia desktop app with vision, sound, settings, and log panels.
- Object detection on screen or webcam frames using bundled YOLOv3-tiny assets, plus a Tesseract OCR adapter.
- Windows `System.Speech` recognition for the fixed **“Hey Robot”** command set: jokes, dad jokes, an insult response, and a prototype streaming-availability lookup.
- Speech output through a local Kokoro worker, with Windows system speech as a fallback. Other speech providers have adapters but are not selected by default.
- Local JSON storage for application counters and a rolling log file under the user's local application data directory.

The app does not currently configure a general question-answering provider. The streaming command also uses a hard-coded title, so it is a prototype rather than a general search feature. Vision capture uses Windows-specific DX11 and DirectShow implementations, and speech recognition uses Windows `System.Speech`; the desktop UI framework itself is cross-platform.

## Build and run

Install the .NET 10 SDK (see [`global.json`](global.json)). From the repository root:

```powershell
dotnet build/cake.cs
dotnet run --project src/AtaraxiaAI.DesktopApp/AtaraxiaAI.DesktopApp.csproj
```

The Cake script restores and builds the solution in Release, probes the copied Kokoro worker and its native ONNX runtime, then runs the tests. To run a different target or configuration, pass script arguments after `--`:

```powershell
dotnet build/cake.cs -- --target=Build --configuration=Debug
```

The app's active capture and speech paths are currently most suitable for Windows. Vision requires access to a screen or webcam; speech input requires a compatible audio device. Some service-backed skills require network access and provider credentials. There is no hosted CI; the Cake script is the local verification path.

## Repository layout

| Path | Responsibility |
| --- | --- |
| `src/AtaraxiaAI.Business` | Application behavior and service contracts |
| `src/AtaraxiaAI.Data` | Local JSON persistence |
| `src/AtaraxiaAI.Integrations` | ML models and external service adapters |
| `src/AtaraxiaAI.Presentation.Desktop` | Avalonia views and view models |
| `src/AtaraxiaAI.DesktopApp` | Executable and dependency composition |
| `src/AtaraxiaAI.KokoroWorker` | Separate local speech-synthesis process |
| `src/AtaraxiaAI.Tests` | Automated tests |

See [product specification](docs/product_specification.md) for the current scope and [development conventions](docs/development_conventions.md) for contributor guidance.

## Attributions

- Application icon: [AI](https://www.flaticon.com/free-icon/ai_1693857) by photo3idea_studio.
- Background image: [High Tech Magma Texture Background](https://freesvg.org/high-tech-magma-texture-background) by OpenClipart.
- System log font: [ProFont](https://www.fontsquirrel.com/fonts/ProFontWindows) by SQ Software.
- General font: [Heavy Data](https://www.fontsquirrel.com/fonts/Heavy-Data) by Vic Fieger.
- Streaming data: [Watchmode](https://www.watchmode.com/).
