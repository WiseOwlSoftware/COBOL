// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Cli;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE HAND-WRITTEN USER MANUAL STAYS TRUE (kb/Work PB1610; DESIGN-USER-DOCUMENTATION §4, §5). ISO/IEC 1989:2023
/// §4.2.16 makes <c>docs/manual/</c> the implementation's user documentation, and a manual that names an option the
/// command line lacks, a diagnostic code the catalogue never allocated, a page that does not exist, or a program that no
/// longer compiles is documentation that misleads. Four things the pages repeat from the product are therefore derived
/// from it here, never retyped: every command-line option (<c>Program.BuildParser</c>), every exit status
/// (<c>Program.ExitCode</c>), every diagnostic code (<c>DiagnosticCatalog</c> and the parse-layer
/// <c>DiagnosticDescriptors</c>) and every complete COBOL program in a <c>cobol</c> code block (compiled, check-only, at
/// the default edition). A new option, outcome or page needs its words, and a removed one needs its words removed, or this
/// class is red. The generated chapters of <c>docs/manual/reference/</c> (kb/Work PB1611, PB1612) have drift tests of
/// their own and are not scanned.
/// </summary>
[Trait("Sweep", "tree")]
public sealed partial class UserManualDriftTests
{
    private static readonly string ManualRoot = TestRepo.Docs("manual");

    /// <summary>Every hand-written page: a Markdown file under <c>docs/manual/</c> outside <c>reference/</c> (the
    /// generated chapters), as its path relative to the manual's root with forward slashes.</summary>
    private static IReadOnlyList<string> Pages() =>
    [
        .. Directory.EnumerateFiles(ManualRoot, "*.md", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(ManualRoot, f).Replace('\\', '/'))
            .Where(rel => !rel.StartsWith("reference/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal),
    ];

    private static string Text(string page) => File.ReadAllText(Path.Combine(ManualRoot, page));

    /// <summary>The URL path Starlight serves a page at, relative to the manual's mount: <c>install.md</c> is
    /// <c>install/</c>, <c>user-guide/editions.md</c> is <c>user-guide/editions/</c>, <c>index.md</c> is the root.</summary>
    private static string UrlOf(string page)
    {
        string slug = page[..^".md".Length];
        if (slug == "index") return "";
        if (slug.EndsWith("/index", StringComparison.Ordinal)) slug = slug[..^"/index".Length];
        return slug + "/";
    }

    [GeneratedRegex(@"^---\r?\n(?<head>.*?)\r?\n---\r?\n", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex Frontmatter();

    [GeneratedRegex(@"\]\((?<target>[^)\s]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"^#{1,6}\s+(?<title>.+?)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex Heading();

    [GeneratedRegex(@"\b(?:COBOL|CBL)\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosticCode();

    [GeneratedRegex(@"```cobol\r?\n(?<code>.*?)```", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex CobolBlock();

    /// <summary>The slug Starlight (github-slugger) gives a heading: lower case, punctuation dropped, spaces to hyphens.</summary>
    private static string Slug(string heading)
    {
        var sb = new StringBuilder();
        foreach (char c in heading.Replace("`", "").ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
            else if (c == ' ') sb.Append('-');
        }
        return sb.ToString();
    }

    [Fact]
    public void TheManualExists_AndEveryPageNamesItsTitleDescriptionAndSidebarPosition()
    {
        var pages = Pages();
        Assert.True(pages.Count > 0, "docs/manual/ holds no page");
        var offenders = new List<string>();
        foreach (string page in pages)
        {
            var head = Frontmatter().Match(Text(page));
            if (!head.Success) { offenders.Add($"{page}: no frontmatter"); continue; }
            string h = head.Groups["head"].Value;
            foreach (string key in new[] { "title:", "description:", "order:" })
                if (!h.Split('\n').Any(l => l.TrimStart().StartsWith(key, StringComparison.Ordinal)))
                    offenders.Add($"{page}: frontmatter lacks {key}");
        }
        Assert.True(offenders.Count == 0, "Starlight needs title and description, and the sidebar needs an order:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void EveryRelativeLink_ResolvesToAPageAndAHeading()
    {
        var pages = Pages();
        var byUrl = pages.ToDictionary(UrlOf, StringComparer.Ordinal);
        var offenders = new List<string>();
        foreach (string page in pages)
        {
            foreach (Match m in MarkdownLink().Matches(Text(page)))
            {
                string target = m.Groups["target"].Value;
                if (target.Contains("://", StringComparison.Ordinal) || target.StartsWith("mailto:", StringComparison.Ordinal)) continue;
                string path = target, fragment = "";
                int hash = target.IndexOf('#');
                if (hash >= 0) { path = target[..hash]; fragment = target[(hash + 1)..]; }
                if (path.Length > 0 && !path.EndsWith('/')) { offenders.Add($"{page}: {target}: a page link ends in a slash"); continue; }
                string destination = path.Length == 0 ? UrlOf(page) : Resolve(UrlOf(page), path);
                if (!byUrl.TryGetValue(destination, out string? targetPage)) { offenders.Add($"{page}: {target}: no page at '{destination}'"); continue; }
                if (fragment.Length > 0 && !Heading().Matches(Text(targetPage)).Any(h => Slug(h.Groups["title"].Value) == fragment))
                    offenders.Add($"{page}: {target}: '{targetPage}' has no heading '{fragment}'");
            }
        }
        Assert.True(offenders.Count == 0, "a manual link must reach a hand-written page, written relative with a trailing slash:\n" + string.Join("\n", offenders));
    }

    /// <summary>Resolve a relative page link against the URL path of the page that holds it.</summary>
    private static string Resolve(string fromUrl, string link)
    {
        var parts = new List<string>(fromUrl.Split('/', StringSplitOptions.RemoveEmptyEntries));
        foreach (string seg in link.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (seg == "..") { if (parts.Count > 0) parts.RemoveAt(parts.Count - 1); }
            else if (seg != ".") parts.Add(seg);
        }
        return parts.Count == 0 ? "" : string.Join('/', parts) + "/";
    }

    [Fact]
    public void EveryDiagnosticCodeThePagesName_IsAllocated()
    {
        var known = DiagnosticCatalog.All.Select(d => d.Code)
            .Concat(typeof(Frontend.Diagnostics.DiagnosticDescriptors).GetFields()
                .Where(f => f.FieldType == typeof(Frontend.Diagnostics.DiagnosticDescriptor))
                .Select(f => ((Frontend.Diagnostics.DiagnosticDescriptor)f.GetValue(null)!).Code))
            .Concat(EditionConstructCodes())
            .ToHashSet(StringComparer.Ordinal);
        var offenders = Pages()
            .SelectMany(p => DiagnosticCode().Matches(Text(p)).Select(m => (Page: p, Code: m.Value)))
            .Where(x => !known.Contains(x.Code))
            .Select(x => $"{x.Page}: {x.Code}")
            .Distinct()
            .ToList();
        Assert.True(offenders.Count == 0, "the manual names a diagnostic code no descriptor carries:\n" + string.Join("\n", offenders));
    }

    /// <summary>The codes the edition gates report (<c>COBOL0900</c>, <c>COBOL0902</c>, <c>COBOL0810</c> and the others): each
    /// construct row of the version matrix names its diagnostic, and the gate reports it as a string, not as a catalogue
    /// descriptor.</summary>
    private static IEnumerable<string> EditionConstructCodes() =>
        System.Text.Json.JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("constructs.json")))
            .RootElement.GetProperty("constructs").EnumerateArray()
            .Select(c => c.GetProperty("diagnosticCode").GetString()!);

    /// <summary>The page every option of the command line is documented on.</summary>
    private const string CommandLinePage = "user-guide/command-line.md";

    [Fact]
    public void TheCommandLinePage_NamesEveryOptionOfTheCommandLine_AndNoOther()
    {
        var (command, _) = Program.BuildParser();
        // A long option's name is its first spelling (`-o, --output` is named `-o`), so read the aliases too.
        var options = command.Options.SelectMany(o => o.Aliases.Append(o.Name))
            .Where(n => n.StartsWith("--", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        string text = Text(CommandLinePage);
        var missing = options.Where(o => !text.Contains($"`{o}", StringComparison.Ordinal)).Order().ToList();
        Assert.True(missing.Count == 0, $"{CommandLinePage} does not document: {string.Join(", ", missing)}");
        // Only a backticked token that starts with -- is an option; a COBOL phrase in prose never does.
        var named = Regex.Matches(text, @"`(?<name>--[a-z][a-z-]*)", RegexOptions.CultureInvariant).Select(m => m.Groups["name"].Value).ToHashSet(StringComparer.Ordinal);
        var invented = named.Where(n => !options.Contains(n)).Order().ToList();
        Assert.True(invented.Count == 0, $"{CommandLinePage} documents options the command line lacks: {string.Join(", ", invented)}");
    }

    [Fact]
    public void TheCommandLinePage_ListsEveryExitStatus()
    {
        // Program.ExitCode maps each compile outcome; 64 is RunCompile's usage error for --run of a library.
        var statuses = Enum.GetValues<CompilerDriver.Outcome>().Select(o => Program.ExitCode(o)).Append(64).ToHashSet();
        var documented = Regex.Matches(Text(CommandLinePage), @"^\| (?<status>\d+) \|", RegexOptions.Multiline | RegexOptions.CultureInvariant)
            .Select(m => int.Parse(m.Groups["status"].Value, CultureInfo.InvariantCulture)).ToHashSet();
        Assert.True(statuses.SetEquals(documented),
            $"{CommandLinePage}'s exit status table lists {string.Join(", ", documented.Order())}; the command returns {string.Join(", ", statuses.Order())}");
    }

    [Fact]
    public void EveryCompleteProgramInTheManual_Compiles()
    {
        string dir = ScratchDirectory.Create("usermanual");
        try
        {
            var offenders = new List<string>();
            int programs = 0;
            foreach (string page in Pages())
            {
                int n = 0;
                foreach (Match block in CobolBlock().Matches(Text(page)))
                {
                    string code = block.Groups["code"].Value;
                    string first = code.Split('\n').First(l => l.Trim().Length > 0).Trim();
                    // A complete program starts with its IDENTIFICATION DIVISION or a directive before it; a fragment does not.
                    if (!first.StartsWith("IDENTIFICATION DIVISION", StringComparison.Ordinal) && !first.StartsWith(">>", StringComparison.Ordinal)) continue;
                    programs++;
                    string src = Path.Combine(dir, $"{page.Replace('/', '_')}_{n++}.cob");
                    File.WriteAllText(src, code.Replace("\r\n", "\n"), new UTF8Encoding(false));
                    var result = CompilerDriver.Compile(new CompilerDriver.Options(src, CheckOnly: true));
                    if (!result.Success) offenders.Add($"{page}, program starting '{first}': {string.Join(" | ", result.Errors)}");
                }
            }
            Assert.True(programs > 0, "no complete COBOL program in the manual: the example scan matched nothing");
            Assert.True(offenders.Count == 0, "a complete program printed in the manual does not compile:\n" + string.Join("\n", offenders));
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
