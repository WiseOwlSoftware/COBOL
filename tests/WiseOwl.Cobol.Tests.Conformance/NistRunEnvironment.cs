// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE NIST RUN ENVIRONMENT IS DATA, WRITTEN ONCE (kb/Work PB3199): <c>tests/nist/run-environment.tsv</c>, which this
/// reads for <see cref="NistRunner"/> and <c>scripts/guard-run-group.sh</c> (<c>guard_run_environment</c>) reads for
/// the CLI leg. The two legs used to write the environment in two languages: the shell exported <c>COBOL_SWITCH_1</c>
/// alone, while every program the C# leg ran also had its default locales pinned and its own MCS directory, so a program
/// that depended on the host's locale got a verdict on one leg only. <see cref="NistRunEnvironmentTests"/> holds the file
/// to every variable <see cref="CutRunner.RunExit"/> sets, and <c>scripts/guard-verify.sh --self-test</c> shows a
/// program the shell runs sees each one.
/// <para>The format: one <c>NAME&lt;TAB&gt;VALUE</c> line per variable, NAME upper case, digits and underscore, VALUE
/// non-empty and tab-free, <see cref="RunDirectoryToken"/> in a VALUE the program's run directory; an empty line or one
/// that begins with <c>#</c> is a comment. Anything else is refused, as the shell reader refuses it.</para>
/// </summary>
internal static partial class NistRunEnvironment
{
    /// <summary>The token a VALUE writes for the program's own run directory.</summary>
    internal const string RunDirectoryToken = "{run-directory}";

    /// <summary>The variables <c>tests/nist/run-environment.tsv</c> declares, in file order, the token unreplaced.</summary>
    internal static IReadOnlyList<KeyValuePair<string, string>> Declared { get; } =
        Parse(File.ReadAllLines(TestRepo.Nist("run-environment.tsv")));

    /// <summary>The environment a NIST program run in <paramref name="runDirectory"/> gets, every declared variable with
    /// <see cref="RunDirectoryToken"/> replaced by <paramref name="runDirectory"/>.</summary>
    internal static IReadOnlyDictionary<string, string?> For(string runDirectory) =>
        Declared.ToDictionary(v => v.Key, v => (string?)v.Value.Replace(RunDirectoryToken, runDirectory, StringComparison.Ordinal));

    /// <summary>The variables <paramref name="lines"/> declare; throws <see cref="FormatException"/> naming the first
    /// line that is neither a comment nor <c>NAME&lt;TAB&gt;VALUE</c>, or a name declared twice.</summary>
    internal static IReadOnlyList<KeyValuePair<string, string>> Parse(IEnumerable<string> lines)
    {
        var declared = new List<KeyValuePair<string, string>>();
        int number = 0;
        foreach (string raw in lines)
        {
            number++;
            string line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#') continue;
            int tab = line.IndexOf('\t', StringComparison.Ordinal);
            string name = tab < 0 ? line : line[..tab];
            string value = tab < 0 ? "" : line[(tab + 1)..];
            if (tab < 0 || !VariableName().IsMatch(name) || value.Length == 0 || value.Contains('\t', StringComparison.Ordinal))
                throw new FormatException($"tests/nist/run-environment.tsv line {number} is not NAME<TAB>VALUE: {line}");
            if (declared.Any(v => v.Key == name))
                throw new FormatException($"tests/nist/run-environment.tsv line {number} declares {name} a second time");
            declared.Add(new(name, value));
        }
        return declared;
    }

    /// <summary>An environment variable name as the file writes it (the shell reader's <c>^[A-Z_][A-Z0-9_]*$</c>).</summary>
    [GeneratedRegex("^[A-Z_][A-Z0-9_]*$")]
    private static partial Regex VariableName();
}
