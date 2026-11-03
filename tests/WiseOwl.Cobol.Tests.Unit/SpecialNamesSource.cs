// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The source of the SPECIAL-NAMES binder, for the drift tests that scan it. <c>SpecialNamesBinder</c> is one partial
/// class over several files (<c>SpecialNamesBinder.cs</c> and one file per clause family), so a scan that means "the
/// SPECIAL-NAMES binder" reads them all; a new partial is covered by the glob, never by an edit here.
/// </summary>
internal static class SpecialNamesSource
{
    /// <summary>The file names (no directory) of every partial of the SPECIAL-NAMES binder.</summary>
    public static IReadOnlyList<string> Files() =>
        Directory.GetFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding"), "SpecialNamesBinder*.cs")
            .Select(Path.GetFileName).Cast<string>().OrderBy(f => f, StringComparer.Ordinal).ToList();

    /// <summary>The lines of one partial.</summary>
    public static string[] Lines(string file) =>
        File.ReadAllLines(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", file));

    /// <summary>The text of every partial, concatenated.</summary>
    public static string AllText() =>
        string.Join("\n", Files().Select(f => File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", f))));
}
