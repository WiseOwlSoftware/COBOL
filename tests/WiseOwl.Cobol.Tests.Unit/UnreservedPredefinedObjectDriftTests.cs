// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY READER OF THE SELF / SUPER KEYWORD ASKS WHETHER THE EDITION RESERVES IT (kb/Work PB4153; PB655, PB1673).
/// §2023:8.9 reserves SELF and SUPER from COBOL 2002 on. At COBOL-85 a token of either that no entry declared names nothing,
/// and the verdict is the §2023:8.4.2.1 undefined reference (COBOL1639) — never a rule of the 2002 predefined object
/// references (§2023:14.9.39.3 SR8 / SR9, §2023:8.4.3.8.3 SR1 / SR3), which describe a facility the edition does not have.
/// <para>The compiler reads the two words off the parse in a handful of files. Each one that DECIDES something about
/// them (a binder) asks <c>OoBinder.RefuseUnreservedPredefinedObject</c> / <c>UnreservedPredefinedObjectRefusal</c>
/// before it does; each one that only RECOGNIZES the shape (a reduction, a form table) is listed with the reader that
/// guards it. A new file that reads the keyword fails this test until it does one or the other, and an exemption whose
/// file no longer reads the keyword fails it too, so the list cannot rot into a superset.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class UnreservedPredefinedObjectDriftTests
{
    /// <summary>The readers of the keyword that only recognize its shape, each with the guard its caller runs.</summary>
    private static readonly Dictionary<string, string> RecognizersOnly = new()
    {
        ["ExpressionBinder.cs"] = "the arithmetic operand's class screen runs AFTER OoBinder.OoBindOoIdentifier, the OO door that guards; "
            + "RefuseNonReceivable asks UnreservedKeywordReference itself (PB655)",
        ["ReceivingOperandForms.cs"] = "the receiving form table: RefuseNonReceivable asks UnreservedKeywordReference.IsFreeAt (PB655)",
        ["ConditionBinder.cs"] = "SoleOoIdentifier / SolePredefinedObject only reduce an expression to the parse node; their "
            + "consumers (SetSenders.Classify, OoBinder's INVOKE argument, OoBindOoIdentifier) guard",
        ["OoBinder.InlineInvocation.cs"] = "InvocationArg.IsSoleSelf only recognizes the argument; OoBinder's two arg.Self readers guard",
        ["OoBinder.PropertyObject.cs"] = "selects the accessor through OoBinder.OoPredefinedSearchRoot, which guards",
        ["ObjectPropertyResolver.cs"] = "`name OF SUPER` is the qualified SUPER only when `name` is a declared class-name, "
            + "which the edition refuses first (OO is a 2002 introduction); the object of a property is OoPredefinedSearchRoot's",
    };

    private static readonly Regex Reads = new(@"\.SELF\(\)|\.SUPER\(\)|\bselfAndSuper\(\)|\bSelfAndSuperContext\b|\bSELF\(\)|\bSUPER\(\)",
        RegexOptions.Compiled);

    private static readonly Regex Guards = new(@"\b(?:RefuseUnreservedPredefinedObject|UnreservedPredefinedObjectRefusal)\(",
        RegexOptions.Compiled);

    private static string StripLineComments(string text) =>
        string.Join('\n', text.Split('\n').Select(l => l.IndexOf("//", StringComparison.Ordinal) is var i and >= 0 ? l[..i] : l));

    private static IEnumerable<(string Name, string Code)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(p => (Path.GetFileName(p), StripLineComments(File.ReadAllText(p))));

    [Fact]
    public void EveryFileThatReadsSelfOrSuper_GuardsTheEditionOrIsAListedRecognizer()
    {
        var readers = CompilerSources().Where(f => Reads.IsMatch(f.Code)).ToList();
        Assert.NotEmpty(readers);   // the scan still sees the readers
        var unguarded = readers.Where(f => !Guards.IsMatch(f.Code) && !RecognizersOnly.ContainsKey(f.Name))
            .Select(f => f.Name).Order().ToList();
        Assert.True(unguarded.Count == 0, "a file reads the SELF / SUPER keyword and neither asks "
            + "OoBinder.RefuseUnreservedPredefinedObject / UnreservedPredefinedObjectRefusal nor is a listed recognizer: "
            + string.Join(", ", unguarded) + ". At an edition whose §2023:8.9 leaves the word free it is an undeclared name "
            + "(COBOL1639), not the predefined object reference (kb/Work PB4153).");
        var stale = RecognizersOnly.Keys.Except(readers.Select(f => f.Name)).Order().ToList();
        Assert.True(stale.Count == 0, "a recognizer exemption no longer reads SELF / SUPER — remove it: " + string.Join(", ", stale));
        var guardedAndListed = readers.Where(f => Guards.IsMatch(f.Code) && RecognizersOnly.ContainsKey(f.Name))
            .Select(f => f.Name).Order().ToList();
        Assert.True(guardedAndListed.Count == 0, "a listed recognizer also guards — it is a reader, remove the exemption: "
            + string.Join(", ", guardedAndListed));
    }
}
