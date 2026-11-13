#:sdk Cake.Sdk

// Local build, run from the repository root: `dotnet build/cake.cs` (or `dotnet build/cake.cs -- --target=Test --configuration=Debug`).
// Script arguments go after `--`; otherwise `dotnet run` consumes options such as `--configuration` itself.

var target = Argument("target", "Test");
var configuration = Argument("configuration", "Release");

const string SOLUTION = "src/AtaraxiaAI.slnx";
const string TEST_PROJECT = "src/AtaraxiaAI.Tests/AtaraxiaAI.Tests.csproj";

Task("Restore")
    .Does(() => DotNetRestore(SOLUTION));

Task("Build")
    .IsDependentOn("Restore")
    .Does(() => DotNetBuild(SOLUTION, new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true
    }));

Task("Test")
    .IsDependentOn("Build")
    .Does(() => DotNetTest(TEST_PROJECT, new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true
    }));

RunTarget(target);
