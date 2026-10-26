// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A MUTED SCREEN IS A DIAGNOSTIC NOBODY REPORTS UNLESS ANOTHER WALK ASKS THE SAME QUESTION (kb/Work PB3682), so
/// <c>EditionContext.Muted</c> is not for general use: it is asked only inside the usage walk
/// (<c>DataBinder.UsageInheritanceWalk</c> and <c>UsageInheritanceGroup</c>), where <c>AnsweredByTypeDeclaration</c> and the
/// copy's own provenance show that the walk over the TYPE declaration screens the same fact (ISO §13.18.57.4 GR1 and
/// §13.18.49.4 GR1 make a copy "as though … coded in place", so a defect inside it is the declaration's). A caller anywhere
/// else is a new decision, made here, with its own proof that somebody else reports what it drops.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EditionMutedDriftTests
{
    [Fact]
    public void EditionMuted_IsAskedOnlyByTheUsageWalk()
    {
        var callers = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                              && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                              && !f.EndsWith("EditionContext.cs", StringComparison.Ordinal)))
            foreach (string line in File.ReadAllLines(file).Where(l => l.Contains(".Muted(") && !l.TrimStart().StartsWith("//")
                         && !l.TrimStart().StartsWith("///")))
                callers.Add(Path.GetFileName(file));
        Assert.NotEmpty(callers);
        Assert.All(callers, c => Assert.Equal("DataBinder.cs", c));

        string text = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "DataBinder.cs"));
        int inWalk = 0;
        foreach (string method in new[] { "UsageInheritanceWalk", "UsageInheritanceGroup" })
        {
            var start = Regex.Match(text, $@"^    private void {method}\(", RegexOptions.Multiline);
            Assert.True(start.Success, $"{method} not found — the pin measured nothing");
            var end = Regex.Match(text[start.Index..], @"^    \}\s*$", RegexOptions.Multiline);
            inWalk += Regex.Matches(text.Substring(start.Index, end.Index + end.Length), @"\.Muted\(").Count;
        }
        Assert.Equal(callers.Count, inWalk);
    }
}
