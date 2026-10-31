#:sdk Cake.Sdk

// Local build, run from the repository root: `dotnet build/cake.cs` (or `dotnet build/cake.cs -- --target=Test --configuration=Debug`).
// Script arguments go after `--`; otherwise `dotnet run` consumes options such as `--configuration` itself.

var target = Argument("target", "Test");
var configuration = Argument("configuration", "Release");

const string SOLUTION = "src/AtaraxiaAI.slnx";
const string TEST_PROJECT = "src/AtaraxiaAI.Tests/AtaraxiaAI.Tests.csproj";
const string DESKTOP_OUTPUT = "src/AtaraxiaAI.DesktopApp/bin";
const string TARGET_FRAMEWORK = "net10.0";

Task("Restore")
    .Does(() => DotNetRestore(SOLUTION));

Task("Build")
    .IsDependentOn("Restore")
    .Does(() => DotNetBuild(SOLUTION, new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true
    }));

// The desktop app runs Kokoro speech synthesis in a separate worker process, copied beside the
// desktop executable by AtaraxiaAI.DesktopApp.csproj. The probe loads the worker's native ONNX
// runtime, catching a missing worker or a native loading failure that compiling alone would not.
Task("VerifyKokoroWorker")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var workerDirectory = Directory(DESKTOP_OUTPUT) + Directory(configuration) + Directory(TARGET_FRAMEWORK) + Directory("KokoroWorker");
        var worker = MakeAbsolute(workerDirectory)
            .CombineWithFilePath(IsRunningOnWindows() ? "AtaraxiaAI.KokoroWorker.exe" : "AtaraxiaAI.KokoroWorker");
        if (!FileExists(worker))
            throw new Exception($"Kokoro worker was not copied beside the desktop executable: {worker}");

        var probe = MakeAbsolute(Directory("artifacts")).CombineWithFilePath("kokoro-probe.txt");
        EnsureDirectoryExists(probe.GetDirectory());
        if (FileExists(probe)) DeleteFile(probe);

        int exitCode = StartProcess(worker, new ProcessSettings
        {
            Arguments = new ProcessArgumentBuilder().Append("--probe").AppendQuoted(probe.FullPath)
        });
        if (!FileExists(probe))
            throw new Exception($"Kokoro worker exited with code {exitCode} before starting its probe.");

        string result = System.IO.File.ReadAllText(probe.FullPath).Trim();
        if (exitCode != 0 || result != "ready")
            throw new Exception($"Kokoro worker could not load its native ONNX runtime (exit code {exitCode}): {result}");
        Information("Kokoro worker loaded its native ONNX runtime.");
    });

Task("Test")
    .IsDependentOn("VerifyKokoroWorker")
    .Does(() => DotNetTest(TEST_PROJECT, new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true
    }));

RunTarget(target);
