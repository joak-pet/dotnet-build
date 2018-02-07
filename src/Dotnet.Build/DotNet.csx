#load "Command.csx"
#load "FileUtils.csx"

using static FileUtils;

public static class DotNet
{
    /// <summary>
    /// Executes the tests in the given path. The path may the full path to a csproj file 
    /// that represents a test project or it may be the 
    /// </summary>
    /// <param name="path"></param>
    public static void Test(string path)
    {
        string pathToProjectFile = FindProjectFile(path);
        if (pathToProjectFile.EndsWith("csproj", StringComparison.InvariantCultureIgnoreCase))
        {
            Command.Execute("dotnet","test " + pathToProjectFile + " --configuration Release");
            return;
        }
        
        if (pathToProjectFile.EndsWith("csx", StringComparison.InvariantCultureIgnoreCase))
        {
            Command.Execute("dotnet", $"script {path}");
            return;
        }
        
        throw new InvalidOperationException($"No tests found at the path {path}");
    }
    
    public static void Pack(string pathToProjectFolder, string pathToPackageOutputFolder)
    {
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet",$"pack {pathToProjectFile} --configuration Release --output {pathToPackageOutputFolder} "); 
    }
    
    public static void Build(string pathToProjectFolder)
    {
        string pathToProjectFile = FindProjectFile(pathToProjectFolder);
        Command.Execute("dotnet","--version");
        Command.Execute("dotnet","restore " + pathToProjectFile);        
        Command.Execute("dotnet","build " + pathToProjectFile + " --configuration Release");  
    }

    public static void Publish(string pathToProjectFolder)
    {
         string pathToProjectFile = FindProjectFile(pathToProjectFolder);
         Command.Execute("dotnet","publish " + pathToProjectFile + " --configuration Release");
    }


    private static string FindProjectFile(string pathToProjectFolder)
    {
        if (GetPathType(pathToProjectFolder) == PathType.File)
        {
            return pathToProjectFolder;
        }
    
        return Directory.GetFiles(pathToProjectFolder, "*.csproj").SingleOrDefault();
    }    
}