using ConsoleAppFramework;
using Cysharp.Diagnostics;

var app = ConsoleApp.Create();
app.UseFilter<LoggerFilter>();
app.Add("", Commands.Run);

await app.RunAsync(args);

static class Commands
{
    /// <summary>
    /// if passed [-- gitflags...], these parameters will be passed to gh repo clone commands
    /// </summary>
    /// <param name="repository">gh repo clone 's first parameter.</param>
    /// <param name="noRecursive">if set this parameter, don't initialize submodule.</param>
    public static async Task<int> Run([Argument] string repository, ConsoleAppContext context, CancellationToken cancellationToken, bool noRecursive = false)
    {
        var root = await GetGitGlobalConfig("repo-get.root", cancellationToken);
        if (string.IsNullOrEmpty(root))
        {
            throw new ArgumentException("Not found `repo-get.root` in git globalconfig. Please set `repo-get.root` by `git config --global repo-get.root <root>`");
        }
        if (BanSpecialCharInPath(root))
        {
            throw new ArgumentException("Cannot resolve path, please remove `$` or `~` from the path or write full-path.");
        }

        var (userName, host) = await GetGitHubAuthInfo(cancellationToken);

        repository = CompleteRepositoryName(repository, userName);

        var cloneTo = Path.Combine(Path.GetFullPath(root), string.IsNullOrEmpty(host) ? "github.com" : host, repository);

        var cloneOptions = context.EscapedArguments!.ToArray();
        if (!noRecursive)
        {
            cloneOptions = [.. cloneOptions, "--recursive"];
        }

        await CloneRepository(repository, cloneTo, cloneOptions, cancellationToken);

        return 0;
    }


    async static ValueTask<string> GetGitGlobalConfig(string key, CancellationToken cancellationToken)
    {
        try
        {
            // NOTE: Currently, we don't support `--all` option, so cannot get multiple values.
            var result = await ProcessX.StartAsync($"git config --global --get {key}")
                .ToTask(cancellationToken);
            return result.FirstOrDefault()?.Trim() ?? string.Empty;
        }
        catch (ProcessErrorException)
        {
            //
        }

        return string.Empty;
    }

    static bool BanSpecialCharInPath(string path)
    {
        return path.Any(x => Path.GetInvalidPathChars().Contains(x) || new[] { '$', '~' }.Contains(x))
            || path.StartsWith('.');
    }

    async static ValueTask<(string userName, string host)> GetGitHubAuthInfo(CancellationToken cancellationToken)
    {
        var userName = string.Empty;
        var host = string.Empty;

        try
        {
            var stdout = await ProcessX.StartAsync("gh auth status -a").ToTask(cancellationToken);
            var authLine = stdout.FirstOrDefault(x => x.Contains("account"));

            if (authLine != null)
            {
                var parts = authLine.Split(' ');
                userName = parts.SkipWhile(x => x != "account").Skip(1).FirstOrDefault() ?? string.Empty;
                host = parts.SkipWhile(x => x != "to").Skip(1).FirstOrDefault() ?? string.Empty;
            }
        }
        catch (ProcessErrorException)
        {
            //
        }

        return (userName, host);
    }

    static string CompleteRepositoryName(string repository, string userName)
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

    async static Task CloneRepository(string repository, string cloneTo, string[] options, CancellationToken cancellationToken)
    {
        var optionsArg = options.Length > 0 ? $"-- {string.Join(" ", options)}" : string.Empty;
        var arguments = $"repo clone {repository} {cloneTo} {optionsArg}";

        var (_, stdOut, stdErr) = ProcessX.GetDualAsyncEnumerable($"gh {arguments}");
        var consumeStdOut = Task.Run(async () =>
            {
                await foreach (var item in stdOut)
                {
                    ConsoleApp.Log(item);
                }
            }, cancellationToken);

        var errorBuffered = new List<string>();
        var consumeStdError = Task.Run(async () =>
            {
                await foreach (var item in stdErr)
                {
                    ConsoleApp.LogError(item);
                }
            }, cancellationToken);

        try
        {
            await Task.WhenAll(consumeStdOut, consumeStdError);
        }
        catch (ProcessErrorException ex)
        {
            if (ex.ExitCode == 0) return;
            throw;
        }

    }

}

internal class LoggerFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) return;

            ConsoleApp.LogError(ex.Message);
            Environment.ExitCode = 1;
        }
    }
}
