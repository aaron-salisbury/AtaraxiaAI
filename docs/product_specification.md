# AtaraxiaAI Product Specification

## Product summary

AtaraxiaAI is an experimental ML and AI sandbox presented as a desktop virtual assistant. Its purpose is to try local models, device input, speech interaction, and external service adapters in one application. This document describes the implementation currently in the repository, not a committed future feature set.

## Intended users and workflow

The primary user is a developer or enthusiast exploring assistant features. They launch the desktop app, watch initialization in the log panel, enable sound or vision, choose an available capture source in settings, and observe spoken responses or annotated vision frames. The app is a prototype, so setup and behavior depend on local devices and provider availability.

## Current capabilities

| Area | Current behavior |
| --- | --- |
| Desktop shell | Avalonia UI with vision feed, sound and vision toggles, settings, and in-app logs. |
| Vision | Bundled YOLOv3-tiny object detection on screen or webcam frames. Screen capture uses DX11; webcam capture uses DirectShow. A Tesseract OCR adapter exists in the integration layer. |
| Speech input | `System.Speech` recognition on Windows. The fixed wake phrase is “Hey Robot”; the recognizer loads a grammar for the implemented commands. |
| Speech output | A Speech Source setting chooses System (Windows speech, default) or Kokoro. Kokoro runs in-process and downloads its model on first selection with visible progress; switching away cancels the download, which resumes later. Sources with a choice of voices (currently Kokoro) reveal a voice gender filter (Male, Female, Both) and a voice list; choosing a voice plays a short sample. A source that cannot be prepared leaves the current one in use, and Windows system speech is the fallback. |
| Skills | Joke and dad-joke retrieval, an insult response, and a prototype Watchmode streaming lookup. These service-backed operations depend on their providers. |
| Storage and logs | Desktop preferences (speech source, voice gender filter, and voice) in `desktop-settings.json`, downloaded speech models, and a rolling Serilog file, all in the user's local application data directory. The app keeps no business data. |

## Current limits

- No general answer provider is configured, so arbitrary questions do not receive a generated answer through the shipped integration factory.
- The streaming-availability command looks up a hard-coded movie title; it does not yet extract a title from speech.
- The UI is built with cross-platform Avalonia, but active vision capture and speech-recognition implementations are Windows-specific. Other operating systems are not established as supported end-to-end.
- Only speech choices are saved between launches; the vision and sound capture choices reset to their defaults.
- The Watchmode prototype counts its API usage for the current session only. It has no API key configured, so it does not run.
- Speech capture settings are exposed in the UI, while the active `System.Speech` recognizer uses the default input device and does not switch sources through that setting.
- This is an experimental desktop application, not a hosted AI service, trained model platform, or production assistant.

## Technical shape

The solution targets .NET 10. `Business` owns assistant behavior and service contracts; `Integrations` implements model and provider adapters; `Presentation.Desktop` contains Avalonia UI and keeps desktop preferences; and `DesktopApp` composes and runs the application. The local Cake build restores, builds, and runs MSTest tests.

## Product direction

The near-term direction is to make experiments more reliable and configurable: clearer provider setup, working general question answering if a provider is chosen, speech command parameters such as movie titles, and validation of platform support. These are possible next steps, not current capabilities.
