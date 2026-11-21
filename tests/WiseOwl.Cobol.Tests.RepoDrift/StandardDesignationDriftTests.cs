// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>⛔ A 1985 CITATION NAMES NO ISO STANDARD (kb/Work PB6013, PB6010). The 1985 text is American National Standard
/// X3.23-1985; no ISO standard has a <c>§1985</c> clause, so a message saying <c>ISO §1985:VII.2.7.3 SR1</c> sends the user to a
/// document that does not exist. A citation chosen by edition is written whole in each arm of <c>EditionInfo.Cite</c> (the one
/// choice of a citation by edition): the 1985 arm <c>§1985:…</c>, the later arms <c>ISO §2023:…</c>, never a shared
/// <c>"ISO " +</c> prefix outside it. This RATCHET lists every source line that still types the designation by hand, in either
/// form: a literal <c>ISO §1985</c>, or an <c>ISO</c> written in front of a <c>Cite(</c> call (which renders <c>ISO §1985</c>
/// at 85). Moving a message to the rule lowers the count and the test then demands the new, lower <see cref="Unmoved"/>; a new
/// line raises it and fails. Comments are not messages and are not counted.</summary>
[Trait("Sweep", "tree")]
public sealed class StandardDesignationDriftTests
{
    /// <summary>The C# sources the messages are built in (declared, kb/Work PB3531).</summary>
    private static readonly TreeScope Sources = new(Under: ["src/"], Kinds: [".cs"]);

    /// <summary>The count of unmoved lines on this tree (kb/Work PB6013 stays open while it is above zero).</summary>
    private const int Unmoved = 440;

    private static readonly Regex HandTyped = new(@"ISO §1985|ISO \{[^}]*\bCite\(|""ISO "" \+ [^;]*\bCite\(", RegexOptions.Compiled);

    [Fact]
    public void NoMessageTypesAnIsoDesignationBeforeA1985Citation()
    {
        var lines = new List<string>();
        foreach (string path in TrackedTree.Files(Sources))
        {
            string[] text = File.ReadAllLines(Path.Combine(TestRepo.Root, path));
            for (int i = 0; i < text.Length; i++)
            {
                string line = text[i].TrimStart();
                if (line.StartsWith("//", StringComparison.Ordinal)) continue;
                if (HandTyped.IsMatch(line)) lines.Add($"{path}:{i + 1}: {line}");
            }
        }
        Assert.True(lines.Count == Unmoved,
            $"{lines.Count} source lines type a standard's designation before a citation that can be 1985's (expected {Unmoved}; "
            + "write each arm of EditionInfo.Cite whole, \"§1985:…\" and \"ISO §2023:…\", and set Unmoved to the new count):\n  "
            + string.Join("\n  ", lines));
    }
}
