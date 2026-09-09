// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB366 — the invariant that keeps ISO/IEC 1989:2023 §14.9.49.4 GR14 a) TRUE as the OO emitter grows:
/// <b>the Format-4 USE selector's predicate covers EVERY C# type the backend emits for the named COBOL class.</b>
///
/// <para><b>Why this test exists.</b> GR14 a) names both object kinds in ONE clause — the exception object "is a
/// factory object or instance object of object-class-name-1 or of a subclass of object-class-name-1" — but a
/// COBOL class is emitted as TWO DISJOINT C# hierarchies: the instance class (rooted at its base's instance
/// class) and the sibling <c>…__FACTORY</c> singleton (rooted at its base's factory class). The generated
/// <c>__EcObjDispatch</c> tested only the INSTANCE name, so every factory exception object selected NO
/// declarative — silently: nothing ran and nothing said so. One COBOL class-name is a SET of emitted C# types,
/// and <see cref="Compiler.Oo.OoClassSymbol.FactoryOrInstanceCsTypes"/> is the ONE place that says which.</para>
///
/// <para>Both facts are asserted against the GENERATED C# rather than by reading the emitter's source, so a
/// third emitted half (or a renamed one) fails here — at the census, where it is one line to fix — instead of
/// quietly narrowing a user's declarative selection.</para>
/// </summary>
public sealed class Format4UseObjectSelectorDriftTests
{
    private const string Src = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. F4DRIFT.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS F4CBASE
            CLASS F4CSUB.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 U USAGE OBJECT REFERENCE.
        PROCEDURE DIVISION.
        DECLARATIVES.
        BASE-SEC SECTION.
            USE AFTER EXCEPTION OBJECT F4CBASE.
        BASE-P.
            DISPLAY "BASE-HANDLER".
        END DECLARATIVES.
        MAIN SECTION.
        MAIN-P.
            SET U TO F4CBASE.
            RAISE U.
            STOP RUN.
        END PROGRAM F4DRIFT.

        IDENTIFICATION DIVISION.
        CLASS-ID. F4CBASE.
        END CLASS F4CBASE.

        IDENTIFICATION DIVISION.
        CLASS-ID. F4CSUB INHERITS FROM F4CBASE.
        END CLASS F4CSUB.
        """;

    /// <summary>Compile the fixture to C#, paired with the class table the same bind produced.</summary>
    private static (string CSharp, Compiler.Oo.OoClassTable Classes) EmitFixture()
    {
        string path = Path.Combine(Path.GetTempPath(), $"f4drift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, Src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            var comp = emitter.Bind(tree!, new EditionContext(2002));
            return (emitter.EmitBound(comp), comp.OoClasses);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    /// <summary>Every C# type the backend emits FOR a COBOL class is one this class symbol's census names.
    /// `__` cannot occur in a COBOL-derived name, so `F4CBASE` and `F4CBASE__…` are exactly that class's
    /// emitted halves — the census must equal them, in both directions.</summary>
    [Fact]
    public void EveryEmittedHalfOfAClass_IsInTheClassSymbolsCensus()
    {
        var (csharp, classes) = EmitFixture();
        var cls = classes.Find("F4CBASE");
        Assert.NotNull(cls);

        var emitted = Regex.Matches(csharp, @"\bclass\s+(" + cls!.CsName + @"(?:__[A-Z0-9_]+)?)\b\s*:")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(cls.FactoryOrInstanceCsTypes.ToHashSet(StringComparer.Ordinal), emitted);
    }

    /// <summary>The generated §14.9.49.4 GR14 selector tests the class's WHOLE census — the instance half AND
    /// the factory half — so "a factory object or instance object of object-class-name-1" selects the
    /// declarative for either kind. Testing one half is the PB366 wrong answer.</summary>
    [Fact]
    public void ObjDispatchSelector_TestsEveryHalfOfTheNamedClass()
    {
        var (csharp, classes) = EmitFixture();
        var cls = classes.Find("F4CBASE");
        Assert.NotNull(cls);

        var body = Regex.Match(csharp, @"__EcObjDispatch\(object\?\s+__obj\)\s*\{(?<b>.*?)\n\s*\}",
            RegexOptions.Singleline);
        Assert.True(body.Success, $"no generated __EcObjDispatch in:\n{csharp}");
        string predicate = body.Groups["b"].Value;

        foreach (string half in cls!.FactoryOrInstanceCsTypes)
            Assert.Matches(@"\bis\b[^;]*\b" + Regex.Escape(half) + @"\b", predicate);
    }
}
