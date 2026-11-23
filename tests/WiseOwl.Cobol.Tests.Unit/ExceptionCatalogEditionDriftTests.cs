// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB5703 — <see cref="EcInfo.IntroducedIn"/> is the edition whose own exception-name list first carries the name
/// (§2002:14.5.12.1, §2014:14.6.12.1, §2023:14.6.13.1): <c>ExceptionNameRule.Window</c> refuses a written name below it
/// (§2002:7.2.19.2 SR2), and <c>CompilerDirectiveWords.IsReserved</c> reserves it in a directive from it (§2002:8.12). A
/// level-3 name the catalog dated too early is legal 2002 source the compiler accepts as an exception-name and refuses
/// as a compilation-variable-name. The expectation is read from the three transcriptions: the first edition whose text
/// names the exception-name at all (each edition's exception table, and the general rules that raise it).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ExceptionCatalogEditionDriftTests
{
    private static HashSet<string> NamesIn(string path)
    {
        Assert.True(File.Exists(path), $"the ISO transcription is missing: {path}");
        return Regex.Matches(File.ReadAllText(path), @"\bEC-[A-Z0-9]+(?:-[A-Z0-9]+)*")
            .Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
    }

    [Fact]
    public void EveryCatalogName_IsIntroducedByTheFirstEditionThatNamesIt()
    {
        // Each held transcription is named by a literal, as the build-targets read audit requires of every repository read.
        var named = new (int Edition, HashSet<string> Names)[]
        {
            (2002, NamesIn(TestRepo.Specs("ISO_COBOL_2002.md"))),
            (2014, NamesIn(TestRepo.Specs("ISO_COBOL_2014.md"))),
            (2023, NamesIn(TestRepo.Specs("ISO_COBOL_2023.md"))),
        };
        var rows = ExceptionCatalog.Level3Rows.ToList();
        var allNames = rows.Select(r => r.Name)
            .Concat(rows.Select(r => r.Level2Parent!).Where(p => p is not null).Distinct());
        var wrong = new List<string>();
        foreach (string name in allNames.Distinct(StringComparer.Ordinal))
        {
            Assert.True(ExceptionCatalog.TryGet(name, out var info), name);
            int expected = named.First(e => e.Names.Contains(name)).Edition;
            if (info.IntroducedIn != expected) wrong.Add($"{name}: catalog {info.IntroducedIn}, first named by {expected}");
        }

        Assert.True(wrong.Count == 0, "IntroducedIn disagrees with the editions' own lists: " + string.Join("; ", wrong));
    }
}
