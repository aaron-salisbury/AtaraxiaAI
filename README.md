Ataraxia AI
===============

Building out a virtual assistant and familiarizing myself with Machine Learning along the way.

Three-tier C# application that uses the Avalonia framework, following the MVVM design pattern, for the presentation layer and .NET libraries for the business & data layers.

Building
--------

Run `dotnet build/cake.cs` from the repository root to restore, build (Release), verify the Kokoro speech worker, and test the solution; it uses [Cake.Sdk](https://cakebuild.net/docs/running-builds/runners/cake-sdk), pinned in `global.json`. Pass script arguments after `--` to change what runs, for example `dotnet build/cake.cs -- --target=Build --configuration=Debug` (without the `--`, `dotnet` consumes `--configuration` itself and the build stays in Release). There is no hosted CI, because GitHub Actions minutes are metered.

Attributions
------------

Application icon "[AI](https://www.flaticon.com/free-icon/ai_1693857)" by photo3idea_studio.

Background image "[High Tech Magma Texture Background](https://freesvg.org/high-tech-magma-texture-background)" by OpenClipart.

System log font "[ProFont](https://www.fontsquirrel.com/fonts/ProFontWindows)" by SQ Software.

General font "[Heavy Data](https://www.fontsquirrel.com/fonts/Heavy-Data)" by Vic Fieger.

Streaming data powered by "[Watchmode](https://www.watchmode.com/)".
