// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// COBOL7006 (kb/Work PB5085): the words that may not be word-1 or word-2 of a COPY REPLACING phrase —
/// §1985:XII.2.3 SR6 (COPY), §2002:7.1.2.2 SR10 (COPY, the compiler directive indicator, the floating debug indicator,
/// the comment indicator) and §2014:7.2.2.2 SR10 (the same without the debug indicator) — at every edition that carries
/// the word operand forms, including 2023 under <c>--permissive</c>, which still accepts the removed form.
/// </summary>
public sealed class CopyReplacingBarredWordTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("WiseOwlCobol_PB5085_");

    public void Dispose() { ScratchDirectory.Remove(_dir); }

    private DiagnosticBag Run(string operand, int level, bool permissive)
    {
        File.WriteAllText(Path.Combine(_dir, "liba.cpy"), "DISPLAY \"OK\".\n");
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, dialectLevel: level, permissive: permissive);
        ConditionalCompilationProcessor.ProcessWithCopy($"COPY liba REPLACING {operand} BY DISPLAY.\n", copy,
            WiseOwl.Cobol.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: level);
        return bag;
    }

    [Theory]
    [InlineData("COPY", 85, false, true)]
    [InlineData("COPY", 2002, false, true)]
    [InlineData("COPY", 2014, false, true)]
    [InlineData("COPY", 2023, true, true)]    // --permissive keeps the removed word form: the 2014 rule still bars COPY
    [InlineData("COPY", 2023, false, false)]  // strict 2023 refuses the operand once, as COBOL0902
    [InlineData(">>", 85, false, false)]      // 1985 bars only COPY
    [InlineData(">>", 2002, false, true)]
    [InlineData(">>", 2014, false, true)]
    [InlineData(">>", 2023, true, true)]
    [InlineData(">>D", 2002, false, true)]
    [InlineData(">>D", 2014, false, false)]   // the floating debug indicator is a 2002 word only
    [InlineData("WRK", 2002, false, false)]
    public void BarredWord_DrawsCobol7006(string operand, int level, bool permissive, bool expected)
    {
        var diags = Run(operand, level, permissive);
        Assert.Equal(expected, diags.Diagnostics.Any(d => d.Code == "COBOL7006" && d.IsError));
    }
}
