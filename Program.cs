using System.Diagnostics;
using ConsoleAppFramework;

ConsoleApp.Run(args, ([Argument] string repository, ConsoleAppContext context, bool noRecursive = false) =>
{
    var root = GetGitGlobalConfig("repo-get.root");
    if (string.IsNullOrEmpty(root))
    {
        Console.Error.WriteLine("Not found `repo-get.root` in git globalconfig. Please set `repo-get.root` by `git config --global repo-get.root <root>`");
        return;
    }

    var (userName, host) = GetGitHubAuthInfo();

    repository = CompleteRepositoryName(repository, userName);

    var cloneTo = Path.Combine(root, string.IsNullOrEmpty(host) ? "github.com" : host, repository);

    var cloneOptions = context.EscapedArguments!.ToArray();
    if (!noRecursive)
    {
        cloneOptions = [.. cloneOptions, "--recursive"];
    }

    CloneRepository(repository, cloneTo, cloneOptions);
});

string GetGitGlobalConfig(string key)
{
    // NOTE: Currently, we don't support `--all` option, so cannot get multiple values.
    var startInfo = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = $"config --global --get {key}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    return process?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
}

(string userName, string host) GetGitHubAuthInfo()
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "gh",
        Arguments = "auth status -a",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    process?.WaitForExit();

    var userName = string.Empty;
    var host = string.Empty;

    if (process?.ExitCode == 0)
    {
        var output = process.StandardOutput.ReadToEnd().Split('\n');
        var authLine = output.FirstOrDefault(x => x.Contains("account"));

        if (authLine != null)
        {
            var parts = authLine.Split(' ');
            userName = parts.SkipWhile(x => x != "account").Skip(1).FirstOrDefault() ?? string.Empty;
            host = parts.SkipWhile(x => x != "to").Skip(1).FirstOrDefault() ?? string.Empty;
        }
    }

    return (userName, host);
}

string CompleteRepositoryName(string repository, string userName)
{
    if (!repository.Contains("/"))
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentException("Cannot determine the repository owner. Please run `gh auth login` or specify the repository owner name.");
        }
        return $"{userName}/{repository}";
    }
    return repository;
}

void CloneRepository(string repository, string cloneTo, string[] options)
{
    var optionsArg = options.Length > 0 ? $"-- {string.Join(" ", options)}" : string.Empty;
    var arguments = $"repo clone {repository} {cloneTo} {optionsArg}";

    var startInfo = new ProcessStartInfo
    {
        FileName = "gh",
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    process?.WaitForExit();
}