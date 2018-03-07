#! "netcoreapp2.0"
#r "nuget:Octokit, 0.27.0"
#load "nuget:github-changelog, 0.1.0"
#load "../src/Dotnet.Build/Command.csx"
#load "../src/Dotnet.Build/DotNet.csx"
#load "../src/Dotnet.Build/FileUtils.csx"
#load "../src/Dotnet.Build/NuGet.csx"
#load "../src/Dotnet.Build/Git.csx"
#load "../src/Dotnet.Build/BuildEnvironment.csx"
#load "../src/Dotnet.Build/GitHub-ReleaseManager.csx"
#load "../src/Dotnet.Build/Logger.csx"

using static FileUtils;
using static ChangeLog;
using static ReleaseManagement;

var scriptFolder = GetScriptFolder();
var tempFolder = Path.Combine(scriptFolder, "tmp");
var contentFolder = CreateDirectory(tempFolder, "contentFiles", "csx", "any");


var currentCommitHash = Git.Open(scriptFolder).GetCurrentCommitHash();
WriteLine($"CommitHash {currentCommitHash}");

Copy(Path.Combine(scriptFolder, "..", "src", "Dotnet.Build"), contentFolder);

Copy(Path.Combine(scriptFolder, "Dotnet.Build.nuspec"), Path.Combine(tempFolder, "Dotnet.Build.nuspec"));

string pathToGitHubArtifacts = CreateDirectory(Path.Combine(scriptFolder, "Artifacts", "GitHub"));
var accessToken = System.Environment.GetEnvironmentVariable("GITHUB_REPO_TOKEN");

DotNet.Test(Path.Combine(scriptFolder, "..", "src", "DotNet.Build.Tests", "AllTests.csx"));

string pathToNuGetArtifacts = CreateDirectory(Path.Combine(scriptFolder, "Artifacts", "NuGet"));
NuGet.Pack(tempFolder, pathToNuGetArtifacts);

if (BuildEnvironment.IsSecure)
{
    Logger.Log("Creating release notes");
    string pathToReleaseNotes = Path.Combine(pathToGitHubArtifacts, "ReleaseNotes.md");
    using (StreamWriter sw = new StreamWriter(Path.Combine(pathToGitHubArtifacts, "ReleaseNotes.md")))
    {
        var generator = ChangeLogFrom("seesharper", "dotnet-build", BuildEnvironment.GitHubAccessToken).SinceLatestTag();
        if (!Git.Default.IsTagCommit())
        {
            generator = generator.IncludeUnreleased();
        }
        await generator.Generate(sw);
    }

    if (Git.Default.IsTagCommit())
    {
        Git.Default.RequreCleanWorkingTree();
        await ReleaseManagerFor("seesharper", "dotnet-build", accessToken)
            .CreateRelease(Git.Default.GetLatestTag(), pathToReleaseNotes, Array.Empty<ReleaseAsset>());
        NuGet.Push(pathToNuGetArtifacts);
    }
}











