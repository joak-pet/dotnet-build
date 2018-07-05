#load "Command.csx"
using System.Text.RegularExpressions;

private static string RemoveNewLine(this string value)
{    
    string result = Regex.Replace(value, @"\r\n?|\n", "");
    return result;
}

private static string[] ReadAllLines(this string value)
{
    List<string> result = new List<string>();
    var reader = new StringReader(value);
    string line;
    while ((line = reader.ReadLine()) != null)
    {
        result.Add(line);
    }
    return result.ToArray();
}

public class GitRepository
{
    private static Regex tagMatcher = new Regex(@"^.*refs\/tags\/(.*)$", RegexOptions.Compiled);
    
    public GitRepository(string path = null)
    {
        Path = path;
    }

    public string Path { get; }

    public CommandResult Execute(string command)
    {
        if (Path == null)
        {
            return Command.Capture("git", $"{command}");
        }
        return Command.Capture("git", $"-C {Path} {command}");
        
    }
    
    public string GetCurrentCommitHash()
    {        
        return Execute("rev-list --all --max-count=1").StandardOut.RemoveNewLine();        
    }  

    public string GetCurrentShortCommitHash()
    {        
        return Execute("rev-parse --short HEAD").StandardOut.RemoveNewLine();        
    } 

    public string GetLatestTag()
    {                        
        return Execute($"describe --abbrev=0 --tags { GetCurrentCommitHash() }").StandardOut.RemoveNewLine();
    }

    public string GetLatestTagHash()
    {        
        return Execute("rev-list --tags --max-count=1").StandardOut.RemoveNewLine();
    }

    public string GetUrlToPushOrigin()
    {                
        return Execute("remote get-url --push origin").StandardOut.RemoveNewLine();
    }

    private string GetCurrentBranch()
    {
        return Execute("rev-parse --abbrev-ref HEAD").StandardOut.RemoveNewLine().ToLower();    
    }

    public string GetPreviousCommitHash()
    {
        return Execute("rev-list --tags --skip=1 --max-count=1").StandardOut.RemoveNewLine(); 
    }

    public string GetPreviousTag()
    {                
        return Execute($"describe --abbrev=0 --tags { GetPreviousCommitHash() }").StandardOut.RemoveNewLine();;        
    }

    public RepositoryInfo GetRepositoryInfo()
    {
        var urlToPushOrigin = GetUrlToPushOrigin();
        var match = Regex.Match(urlToPushOrigin, @".*.com\/(.*)\/(.*)\.");
        var owner = match.Groups[1].Value;
        var project = match.Groups[2].Value;
        return new RepositoryInfo(){Owner = owner, ProjectName = project};
    }

    public bool AllTagsPushedToOrigin()
    {
        var result = Execute("push --tags --dry-run -n --porcelain").StandardOut;
        return result.ToLower().Contains("new tag");
    }

    //https://stackoverflow.com/questions/2657935/checking-for-a-dirty-index-or-untracked-files-with-git
    public bool HasUntrackedFiles()
    {
        var result = Execute("ls-files --exclude-standard --others").StandardOut.ReadAllLines();
        return result.Any();                
    }

    public bool HasStagedFiles()
    {
        return Execute("diff-index --quiet --cached HEAD --").ExitCode != 0;
    }

    public bool HasUnstagedFiles()
    {
        return Execute("diff-files --quiet").ExitCode != 0;
    }

    public void RequreCleanWorkingTree()
    {
        if (HasStagedFiles())
        {
            throw new InvalidOperationException("git repository contains uncomitted staged files");
        }

        if (HasUnstagedFiles())
        {
            throw new InvalidOperationException("git repository contains unstaged files");
        }

        if (HasUntrackedFiles())
        {
            throw new InvalidOperationException("git repository contains untracked files");
        }
    }

    public string[] GetRemoteTags()
    {
        List<string> result = new List<string>();
        var lines = Execute("ls-remote --tags --q").StandardOut.ReadAllLines();
        foreach (var line in lines)
        {
            var tag = tagMatcher.Match(line).Groups[1].Value;
            result.Add(tag);
        }
        return result.ToArray();
    }


    public bool IsTagCommit()
    {
        var currentTagHash = GetLatestTagHash();
        var currentCommitHash = GetCurrentCommitHash();
        return currentTagHash == currentCommitHash;
    }
}

public static class Git
{        
    public static GitRepository Default = new GitRepository();
    
    public static GitRepository Open(string path)
    {
        return new GitRepository(path);
    }

    public static string GetAccessToken()
    {
        var accessToken = System.Environment.GetEnvironmentVariable("GITHUB_REPO_TOKEN");
        return accessToken;
    }
}

public class RepositoryInfo 
{
    public string Owner {get;set;}    

    public string ProjectName {get;set;}    
}