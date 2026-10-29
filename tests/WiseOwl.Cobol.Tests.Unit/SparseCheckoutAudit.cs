// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHAT A SPARSE-CHECKOUT CI JOB RUNS, AND WHICH OF IT ITS SPARSE LIST LEAVES OUT (kb/Work PB3787): a workflow job whose
/// checkout lists only some paths runs on a tree without the rest, so a script it runs that imports (or a shell script
/// that names) a file outside the list fails on the runner and on no developer's machine. The audit reads a workflow's
/// jobs, takes each sparse job's list, finds every repository file the job reaches, and reports each one the list does
/// not cover with the chain that reaches it. Used by <see cref="SparseCheckoutDriftTests"/>.
/// </summary>
/// <remarks>
/// <para>What a job REACHES: every token of its steps (comments and the sparse list itself set aside) that names an
/// existing repository file, and the files of a local action (<c>uses: ./…</c>) it calls; from a Python file, the files
/// of every repository module it imports anywhere in its text (an <c>import</c>, <c>from … import</c> or a submodule
/// named by the latter, at module level, in a class body, in a function body or under an <c>if</c>: a lazy import runs
/// on the job's path when its function does, and an over-approximated closure is loud where an under-approximated one
/// is silent, kb/Work PB3810); from a shell script, a PowerShell script or an action, the same token scan of its
/// non-comment text.</para>
/// <para>Python resolves a bare module name through <c>sys.path</c>, which the scripts extend themselves
/// (<c>sys.path.insert(0, str(HERE))</c>, <c>sys.path.append(str(HERE.parent))</c>), so the audit searches the entry
/// script's directory, the importing file's directory and each directory above it. It does not run Python: a module
/// named by <c>importlib</c> or a path string in a Python file is not seen (the two limits of a static read; a
/// <c>subprocess</c> call of another script is the sibling of the second).</para>
/// <para>The list is read as <c>git sparse-checkout</c> reads it in non-cone mode (gitignore patterns: a name covers the
/// file or directory and everything beneath it; <c>*</c>, <c>**</c> and <c>?</c> glob; a leading <c>/</c> anchors), so a
/// job that does not set <c>sparse-checkout-cone-mode: false</c> is refused (the action defaults to cone mode, where
/// the same lines name directories only), and a <c>!</c> pattern is refused as unmodelled.</para>
/// </remarks>
internal static class SparseCheckoutAudit
{
    /// <summary>A job's id and its body, as the workflow declares it.</summary>
    internal sealed record Job(string File, string Id, string Body);

    /// <summary>A sparse-checkout job: its steps' text without the list, and the list's patterns.</summary>
    internal sealed record SparseJob(string File, string Id, string Steps, IReadOnlyList<string> Patterns, bool ConeModeOff);

    /// <summary>One repository file a sparse job reaches, with the file that led to it.</summary>
    internal sealed record Reached(string Path, string Via);

    private static readonly Regex SparseKey = new(@"^(?<ind>[ \t]*)sparse-checkout:[ \t]*(?<value>.*)$", RegexOptions.Multiline | RegexOptions.NonBacktracking);
    private static readonly Regex ConeOff = new(@"^[ \t]*sparse-checkout-cone-mode:[ \t]*false[ \t]*$", RegexOptions.Multiline | RegexOptions.NonBacktracking);
    private static readonly Regex Token = new(@"[A-Za-z0-9_.\-/]+", RegexOptions.NonBacktracking);
    private static readonly Regex LocalAction = new(@"\buses:[ \t]*(?<dir>\./[^\s'""]+)", RegexOptions.NonBacktracking);

    private static readonly Regex ImportStatement = new(@"^(?<ind>[ \t]*)import[ \t]+(?<mods>[^#;\r\n]+)", RegexOptions.NonBacktracking);
    private static readonly Regex FromStatement = new(@"^(?<ind>[ \t]*)from[ \t]+(?<dots>\.*)(?<mod>[\w.]*)[ \t]+import[ \t]+(?<names>[^#\r\n]*)", RegexOptions.NonBacktracking);

    /// <summary>The jobs of one workflow file: those <see cref="DotnetSdkInstallDriftTests.Jobs"/> reads beneath
    /// <c>jobs:</c>, the one reader of a workflow's jobs (kb/Work PB3810: a second reader over the whole file returned
    /// the <c>on:</c> triggers as jobs).</summary>
    internal static IReadOnlyList<Job> Jobs(string file, string workflow) =>
    [.. DotnetSdkInstallDriftTests.Jobs(workflow).Select(j => new Job(file, j.Id, j.Body))];

    /// <summary>The jobs of <paramref name="jobs"/> that give their checkout a sparse list.</summary>
    internal static IReadOnlyList<SparseJob> SparseJobs(IEnumerable<Job> jobs)
    {
        List<SparseJob> found = [];
        foreach (Job job in jobs)
        {
            Match key = SparseKey.Match(job.Body);
            if (!key.Success) continue;
            string[] lines = job.Body.Split('\n');
            int at = Array.FindIndex(lines, l => SparseKey.IsMatch(l));
            int indent = key.Groups["ind"].Length;
            List<string> patterns = [];
            int end = at + 1;
            string value = key.Groups["value"].Value.Trim();
            if (value.Length > 0 && !value.StartsWith('|') && !value.StartsWith('>'))
                patterns.Add(value.Trim('\'', '"'));
            else
            {
                for (; end < lines.Length; end++)
                {
                    string line = lines[end];
                    if (line.Trim().Length > 0 && line.Length - line.TrimStart().Length <= indent) break;
                    if (line.Trim().Length > 0) patterns.Add(line.Trim());
                }
            }
            string steps = string.Join('\n', lines.Take(at).Concat(lines.Skip(end)));
            found.Add(new SparseJob(job.File, job.Id, steps, patterns, ConeOff.IsMatch(job.Body)));
        }
        return found;
    }

    /// <summary>Every repository file <paramref name="job"/> reaches, each once, in the order found. <paramref name="read"/>
    /// gives a repository-relative path's text, or null when no such file exists.</summary>
    internal static IReadOnlyList<Reached> Reach(SparseJob job, Func<string, string?> read)
    {
        List<Reached> reached = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        Queue<(string Path, string Via, string Entry)> work = new();
        string origin = $"{job.File} job `{job.Id}`";
        foreach (string path in TokenFiles(job.Steps, read))
            work.Enqueue((path, origin, Path.GetDirectoryName(path)?.Replace('\\', '/') ?? ""));
        while (work.Count > 0)
        {
            (string path, string via, string entry) = work.Dequeue();
            if (!seen.Add(path)) continue;
            reached.Add(new Reached(path, via));
            string text = read(path)!;
            IEnumerable<string> next = path.EndsWith(".py", StringComparison.Ordinal)
                ? ImportedFiles(path, text, entry, read)
                : path.EndsWith(".sh", StringComparison.Ordinal) || path.EndsWith(".ps1", StringComparison.Ordinal)
                    || path.EndsWith(".yml", StringComparison.Ordinal) || path.EndsWith(".yaml", StringComparison.Ordinal)
                    ? TokenFiles(text, read)
                    : [];
            foreach (string n in next) work.Enqueue((n, path, entry));
        }
        return reached;
    }

    /// <summary>The problems with each sparse job of <paramref name="jobs"/>: a list read in cone mode, an unmodelled
    /// pattern, and each reached file the list leaves out, with the chain that reaches it. Empty when every job is sound.</summary>
    internal static IReadOnlyList<string> Problems(IEnumerable<SparseJob> jobs, Func<string, string?> read)
    {
        List<string> problems = [];
        foreach (SparseJob job in jobs)
        {
            string name = $"{job.File} job `{job.Id}`";
            if (!job.ConeModeOff)
            {
                problems.Add($"{name}: no `sparse-checkout-cone-mode: false` (the action defaults to cone mode, where the list names directories only)");
                continue;
            }
            string? negated = job.Patterns.FirstOrDefault(p => p.StartsWith('!'));
            if (negated is not null)
            {
                problems.Add($"{name}: the pattern `{negated}` is a negation, which this audit does not model");
                continue;
            }
            foreach (Reached r in Reach(job, read))
                if (!Covers(job.Patterns, r.Path))
                    problems.Add($"{name}: `{r.Path}` (reached from {r.Via}) is not in its sparse-checkout list, so the job runs on a tree without it");
        }
        return problems;
    }

    /// <summary>Whether non-cone sparse-checkout <paramref name="patterns"/> include the repository-relative
    /// <paramref name="path"/>: a pattern that matches the path or any directory above it.</summary>
    internal static bool Covers(IReadOnlyList<string> patterns, string path)
    {
        string[] parts = path.Split('/');
        for (int i = 1; i <= parts.Length; i++)
        {
            string prefix = string.Join('/', parts.Take(i));
            if (patterns.Any(p => Matches(p, prefix))) return true;
        }
        return false;
    }

    private static bool Matches(string pattern, string prefix)
    {
        string p = pattern.TrimEnd('/');
        bool anchored = p.StartsWith('/');
        p = p.TrimStart('/');
        anchored |= p.Contains('/');
        string target = anchored ? prefix : prefix[(prefix.LastIndexOf('/') + 1)..];
        string regex = "^" + Regex.Escape(p).Replace(@"\*\*", "\u0001").Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]").Replace("\u0001", ".*") + "$";
        return Regex.IsMatch(target, regex, RegexOptions.NonBacktracking);
    }

    /// <summary>The existing repository files <paramref name="text"/> names, comments set aside: a token with a
    /// <c>/</c> or an extension that <paramref name="read"/> finds, and the <c>action.yml</c> of a local action it
    /// uses.</summary>
    private static IEnumerable<string> TokenFiles(string text, Func<string, string?> read)
    {
        string code = WithoutComments(text);
        foreach (Match m in LocalAction.Matches(code))
        {
            string dir = m.Groups["dir"].Value[2..].TrimEnd('/');
            foreach (string name in new[] { "action.yml", "action.yaml" })
                if (read($"{dir}/{name}") is not null) yield return $"{dir}/{name}";
        }
        foreach (Match m in Token.Matches(code))
        {
            string token = m.Value.StartsWith("./", StringComparison.Ordinal) ? m.Value[2..] : m.Value;
            token = token.TrimEnd('.', '/');
            if (token.Length == 0 || token.StartsWith('/') || token.Contains("..", StringComparison.Ordinal) || !(token.Contains('/') || token.Contains('.'))) continue;
            if (read(token) is not null) yield return token;
        }
    }

    /// <summary>The text without whole-line comments and trailing <c> # …</c> comments (YAML, shell and PowerShell).</summary>
    private static string WithoutComments(string text) =>
        string.Join('\n', text.Split('\n').Select(l =>
        {
            if (l.TrimStart().StartsWith('#')) return "";
            int c = l.IndexOf(" #", StringComparison.Ordinal);
            return c < 0 ? l : l[..c];
        }));

    /// <summary>The repository Python files <paramref name="text"/> (the file at <paramref name="path"/>) contains:
    /// every import statement, a function body's lazy import included, and not a docstring's text.</summary>
    internal static IEnumerable<string> ImportedFiles(string path, string text, string entryDir, Func<string, string?> read)
    {
        string dir = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
        List<string> roots = [entryDir, dir];
        for (string d = dir; d.Length > 0;)
        {
            int cut = d.LastIndexOf('/');
            d = cut < 0 ? "" : d[..cut];
            roots.Add(d);
        }
        roots = [.. roots.Distinct()];

        foreach ((int level, string module, IReadOnlyList<string> names) in Imports(text))
        {
            IReadOnlyList<string> bases = level == 0 ? roots : [Up(dir, level - 1)];
            foreach (string file in Resolve(bases, module, names, read))
                yield return file;
        }
    }

    private static string Up(string dir, int levels)
    {
        for (int i = 0; i < levels; i++) dir = dir.Contains('/') ? dir[..dir.LastIndexOf('/')] : "";
        return dir;
    }

    /// <summary>The module files for <c>module</c> (a dotted name, empty for <c>from . import x</c>) under the first of
    /// <paramref name="bases"/> that holds it, its packages' <c>__init__.py</c> files, and the submodules
    /// <paramref name="names"/> name.</summary>
    private static IEnumerable<string> Resolve(IReadOnlyList<string> bases, string module, IReadOnlyList<string> names, Func<string, string?> read)
    {
        static string Join(string a, string b) => a.Length == 0 ? b : b.Length == 0 ? a : a + "/" + b;
        string rel = module.Replace('.', '/');
        foreach (string root in bases)
        {
            List<string> found = [];
            if (module.Length > 0)
            {
                string? file = read(Join(root, rel + ".py")) is not null ? Join(root, rel + ".py")
                    : read(Join(root, rel + "/__init__.py")) is not null ? Join(root, rel + "/__init__.py")
                    : null;
                if (file is not null)
                {
                    string[] parts = module.Split('.');
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string init = Join(root, string.Join('/', parts.Take(i)) + "/__init__.py");
                        if (read(init) is not null) found.Add(init);
                    }
                    found.Add(file);
                }
            }
            string package = Join(root, rel);
            foreach (string n in names)
            {
                if (read(Join(package, n + ".py")) is not null) found.Add(Join(package, n + ".py"));
                else if (read(Join(package, n + "/__init__.py")) is not null) found.Add(Join(package, n + "/__init__.py"));
            }
            if (found.Count == 0) continue;
            foreach (string f in found) yield return f;
            yield break;
        }
    }

    /// <summary>Each import statement of a Python file, at any depth, as (relative level, dotted module, imported
    /// names).</summary>
    private static IEnumerable<(int Level, string Module, IReadOnlyList<string> Names)> Imports(string text)
    {
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');
        string? quote = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (quote is not null)
            {
                if (line.Contains(quote, StringComparison.Ordinal)) quote = null;
                continue;
            }
            string trimmed = line.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] == '#') continue;
            quote = OpenTripleQuote(line);

            Match import = ImportStatement.Match(line);
            if (import.Success)
            {
                foreach (string part in import.Groups["mods"].Value.Split(','))
                {
                    string module = part.Trim().Split(' ', 2)[0];
                    if (module.Length > 0) yield return (0, module, []);
                }
                continue;
            }
            Match from = FromStatement.Match(line);
            if (!from.Success) continue;
            string names = from.Groups["names"].Value;
            if (names.Contains('(') && !names.Contains(')'))
                for (i++; i < lines.Length; i++)
                {
                    names += " " + lines[i].Split('#')[0];
                    if (lines[i].Contains(')')) break;
                }
            string[] imported = [.. names.Replace("(", " ").Replace(")", " ").Split(',')
                .Select(n => n.Trim().Split(' ', 2)[0]).Where(n => n.Length > 0 && n != "*")];
            yield return (from.Groups["dots"].Length, from.Groups["mod"].Value, imported);
        }
    }

    /// <summary>The triple-quote delimiter a line opens and does not close (the rest of its string lies on the lines
    /// that follow), or null.</summary>
    private static string? OpenTripleQuote(string line)
    {
        string? open = null;
        for (int i = 0; i + 2 < line.Length; i++)
        {
            string three = line.Substring(i, 3);
            if (three is not ("\"\"\"" or "'''")) continue;
            open = open is null ? three : open == three ? null : open;
            i += 2;
        }
        return open;
    }
}
