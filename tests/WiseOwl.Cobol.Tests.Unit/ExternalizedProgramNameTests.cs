// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// <see cref="ExternalizedProgramName"/>, the key of the AS NESTED callee table (docs/rearchitecture/DESIGN-listing.md section 6.7.1,
/// "Step 6b, slice 6"). A CALL's literal-1 names the program by its externalized name (ISO §2023:14.9.4.3 SR15: "Literal-1 shall be the
/// same as the program-name specified in a PROGRAM-ID paragraph"), and two externalized names are the same name when they are the same
/// COBOL word by the implementor's mapping (§2023:8.3.2.2: "The implementor defines the formation and mapping rules of these names";
/// this implementation's rules are DOC-A.1-68 and DOC-A.1-219, the Annex C fold): the case of a letter does not matter, the hyphen and
/// the digits are kept, and an extended letter is a letter of the name that folds to no basic one. The expected verdicts come from those
/// rules, so the key agrees with the run-time program table that matches the same names.
/// </summary>
public sealed class ExternalizedProgramNameTests
{
    [Theory]
    [InlineData("SUB-P", "sub-p")]
    [InlineData("Sub-P", "SUB-P")]
    [InlineData("PRÜF", "prüf")]
    public void TheSameWord_IsOneKey(string declared, string written)
    {
        var a = new ExternalizedProgramName(declared);
        var b = new ExternalizedProgramName(written);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        var table = new Dictionary<ExternalizedProgramName, int> { [a] = 1 };
        Assert.True(table.ContainsKey(b));
    }

    [Theory]
    [InlineData("SUB-P", "SUBP")]
    [InlineData("SUB-P", "SUB-P2")]
    [InlineData("PRUF", "PRÜF")]
    public void DifferentWords_AreDifferentKeys(string declared, string written) =>
        Assert.NotEqual(new ExternalizedProgramName(declared), new ExternalizedProgramName(written));
}
