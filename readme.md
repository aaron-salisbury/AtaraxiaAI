# AtaraxiaAI

AtaraxiaAI is a .NET desktop sandbox for experimenting with machine learning and AI capabilities in a virtual-assistant interface. It brings speech, vision, and small service-backed skills together in an Avalonia UI. The project is experimental; its features and platform support are still evolving.

## What is here now

- An Avalonia desktop app with vision, sound, settings, and log panels.
- Object detection on screen or webcam frames using bundled YOLOv3-tiny assets, plus a Tesseract OCR adapter.
- Speech recognition for the **“Hey Robot”** command set (jokes, dad jokes, an insult response, and a prototype streaming-availability lookup) from a source chosen in settings: System (Windows `System.Speech`, the default) or Vosk. Vosk runs offline and downloads its US English model (about 131 MB) the first time it is chosen, showing progress in the settings overlay. It listens to the microphone or, with the sound card capture source, to what the computer is playing.
- Speech output from a source chosen in settings: System (Windows speech, the default) or Kokoro. Kokoro runs locally in the app and downloads its model (about 326 MB) the first time it is chosen, showing progress in the settings overlay. Kokoro offers 28 American and British English voices, filtered by gender, and choosing one plays a short sample. If a source cannot be prepared, the current one stays in use; Windows system speech is the fallback.
- Recognizer and speech choices kept between launches in `desktop-settings.json`, and a rolling log file, under the user's local application data directory.

The app does not currently configure a general question-answering provider. The streaming command also uses a hard-coded title, so it is a prototype rather than a general search feature. Vision capture uses Windows-specific DX11 and DirectShow implementations, and the default speech recognition uses Windows `System.Speech`; the desktop UI framework itself is cross-platform.

## Build and run

Install the .NET 10 SDK (see [`global.json`](global.json)). From the repository root:

```powershell
dotnet build/cake.cs
dotnet run --project src/AtaraxiaAI.DesktopApp/AtaraxiaAI.DesktopApp.csproj
```

The Cake script restores and builds the solution in Release, then runs the tests. To run a different target or configuration, pass script arguments after `--`:

```powershell
dotnet build/cake.cs -- --target=Build --configuration=Debug
```

The app's active capture and speech paths are currently most suitable for Windows. Vision requires access to a screen or webcam; speech input requires a compatible audio device. Some service-backed skills require network access and provider credentials. There is no hosted CI; the Cake script is the local verification path.

## Repository layout

| Path | Responsibility |
| --- | --- |
| `src/AtaraxiaAI.Business` | Application behavior and service contracts |
| `src/AtaraxiaAI.Integrations` | ML models and external service adapters |
| `src/AtaraxiaAI.Presentation.Desktop` | Avalonia views and view models |
| `src/AtaraxiaAI.DesktopApp` | Executable and dependency composition |
| `src/AtaraxiaAI.Tests` | Automated tests |

See [product specification](docs/product_specification.md) for the current scope and [development conventions](docs/development_conventions.md) for contributor guidance.

## Attributions

- Application icon: [AI](https://www.flaticon.com/free-icon/ai_1693857) by photo3idea_studio.
- Background image: [High Tech Magma Texture Background](https://freesvg.org/high-tech-magma-texture-background) by OpenClipart.
- System log font: [ProFont](https://www.fontsquirrel.com/fonts/ProFontWindows) by SQ Software.
- General font: [Heavy Data](https://www.fontsquirrel.com/fonts/Heavy-Data) by Vic Fieger.
- Streaming data: [Watchmode](https://www.watchmode.com/).
