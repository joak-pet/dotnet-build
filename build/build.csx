#! "netcoreapp2.0"
#load "../src/Dotnet.Build/Command.csx"
#load "../src/Dotnet.Build/FileUtils.csx"
#load "../src/Dotnet.Build/NuGet.csx"
#load "../src/Dotnet.Build/Git.csx"
#load "../src/Dotnet.Build/ReleaseNotes.csx"
#load "../src/Dotnet.Build/GitHub.csx"


using static FileUtils;

var scriptFolder = GetScriptFolder();
var tempFolder = Path.Combine(scriptFolder,"tmp");
var contentFolder = CreateDirectory(tempFolder,"contentFiles","csx","any");

Copy(Path.Combine(scriptFolder,"..","src","Dotnet.Build"), contentFolder);
Copy(Path.Combine(scriptFolder,"Dotnet.Build.nuspec"), Path.Combine(tempFolder,"Dotnet.Build.nuspec"));

string pathToNuGetArtifacts = CreateDirectory(Path.Combine(scriptFolder,"Artifacts","NuGet"));
NuGet.Pack(tempFolder, pathToNuGetArtifacts);

string pathToGitHubArtifacts = CreateDirectory(Path.Combine(scriptFolder,"Artifacts","GitHub"));
ReleaseNotes.Generate(Path.Combine(pathToGitHubArtifacts,"ReleaseNotes.md"));

if(Git.Default.IsTagCommit())
{
     NuGet.Push(pathToNuGetArtifacts);
     GitHub.CreateReleaseDraft(pathToGitHubArtifacts);
}









