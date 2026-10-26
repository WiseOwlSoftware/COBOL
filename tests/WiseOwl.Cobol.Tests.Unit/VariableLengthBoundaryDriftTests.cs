// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE VARIABLE-LENGTH ACTIVATION-BOUNDARY DRIFT LOCK (kb/Work PB204).
/// <para>ISO §14.8.2.2 and §14.8.3.2 ADMIT a variable-length group across a Format-2 CALL / CALL RETURNING /
/// INVOKE boundary "subject to compatibility as described in 8.5.1.12". What made that admission a rejection
/// for years was not a missing rule text but a PREDICATE: every boundary screen and emit guard asked
/// <c>IsImageCapable</c>, which is false for such a group BY DEFINITION, so conforming source was refused.
/// The structural fix is that a boundary asks <see cref="WiseOwl.Cobol.Binding.Model.DataItem.BoundaryImageCapable"/>
/// — "fixed record window OR current-extent carrier" — and this test is what keeps that true.</para>
/// <para>Two facts are pinned, and each is a way the mechanism can silently regress:</para>
/// <list type="number">
/// <item>THE CAPABILITY IS DEFINED ONCE. <c>CurrentExtentImageCapable</c> lives on <c>DataItem</c> and nowhere
/// else. It began life inside <c>GroupImageCodec</c> and had to move when a BIND-time screen started asking it;
/// a second definition would let the DISPLAY format and the crossing disagree about which groups have a defined
/// current extent — the exact two-arm shape this repo keeps producing.</item>
/// <item>THE BOUNDARY SCREENS ASK THE BOUNDARY PREDICATE. The two sites that decide whether a group crossing is
/// admitted (<c>ActivationDescriptions.Of</c>'s group arms, which describe a group by both halves of the predicate,
/// and <c>CallEmitter.ArgText</c>'s stage guard) must ask it, and no <c>DataItem</c> comparator beside the relation
/// may screen a group again (kb/Work PB3101). Reverting either one to <c>IsImageCapable</c> restores
/// the defect with no test failing anywhere else — the goldens would still pass, because a REJECTION at bind
/// looks like any other diagnostic to a corpus runner that never compiles the program.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class VariableLengthBoundaryDriftTests
{
    /// <summary>Count occurrences in CODE only — these predicates are discussed at length in doc comments, so a
    /// raw text count is a false positive (the <see cref="DisplayUsageUnionDriftTests"/> convention).</summary>
    private static int CodeOccurrences(string src, string needle)
    {
        var code = new List<string>();
        foreach (string raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("///") || t.StartsWith("//")) continue;
            int i = line.IndexOf("//", StringComparison.Ordinal);
            code.Add(i >= 0 ? line[..i] : line);
        }
        return Regex.Matches(string.Join("\n", code), Regex.Escape(needle)).Count;
    }

    [Fact]
    public void CurrentExtentCapability_IsDefinedExactlyOnce()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        var definitions = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "Generated" + Path.DirectorySeparatorChar)) continue;
            string src = File.ReadAllText(file);
            // A DEFINITION is a property or method declaration of that name; a USE is a member access.
            if (CodeOccurrences(src, "bool CurrentExtentImageCapable") > 0)
                definitions.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }
        Assert.Equal(["Binding/Model/DataItem.cs"], definitions);
    }

    /// <summary>⛔ THE ONE-WAY TWIN (kb/Work PB3358). A variable-length group holding a class pointer/object leaf
    /// has the §14.9.11.4 GR7 DISPLAY format (A.1 item 57) but no §8.5.1.12 carrier, because the carrier's receiving
    /// side stores components back and the leaf's placeholder has no inverse. That is
    /// <c>CurrentExtentTransferImageCapable</c>: defined once, beside <c>CurrentExtentImageCapable</c>, over the SAME
    /// member law (<c>CurrentExtentComposes</c>, one definition with a <c>transfer</c> flag - never a second copy that
    /// could classify a member differently). The consumers that only SEND characters out (DISPLAY, FUNCTION LENGTH
    /// and BYTE-LENGTH) ask it; every consumer that reads the image back keeps the carrier law.</summary>
    [Fact]
    public void CurrentExtentTransferCapability_IsDefinedOnceOverTheSameMemberLaw()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        var transfer = new List<string>();
        var memberLaw = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "Generated" + Path.DirectorySeparatorChar)) continue;
            string src = File.ReadAllText(file);
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (CodeOccurrences(src, "bool CurrentExtentTransferImageCapable") > 0) transfer.Add(relative);
            if (CodeOccurrences(src, "bool CurrentExtentComposes(") > 0) memberLaw.Add(relative);
        }
        Assert.Equal(["Binding/Model/DataItem.cs"], transfer);
        Assert.Equal(["Binding/Model/DataItem.cs"], memberLaw);

        // The one-way consumers ask the transfer capability and never the carrier's.
        foreach (string[] site in new[]
        {
            new[] { "CodeGen", "Verbs", "AcceptDisplayEmitter.cs" },
            new[] { "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs" },
        })
        {
            string src = File.ReadAllText(TestRepo.At(["src", "WiseOwl.Cobol.Compiler", .. site]));
            Assert.Equal(0, CodeOccurrences(src, "CurrentExtentImageCapable"));
        }
        Assert.True(CodeOccurrences(File.ReadAllText(TestRepo.At("src", "WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs")),
                "CurrentExtentTransferImageCapable") > 0,
            "IntrinsicBinder sends a variable-length group's characters out (FUNCTION LENGTH) and must ask CurrentExtentTransferImageCapable (kb/Work PB3358).");

        // The emitters that send a variable-length group's CHARACTERS out - DISPLAY, a function's string argument, the
        // digit decode of a group in a numeric position - read the group through the ONE reader, PlaceRenderer.VarGroupOperandImage,
        // which asks the transfer capability (kb/Work PB3358, PB3424): no consumer spells its own copy of that gate.
        string reader = File.ReadAllText(TestRepo.At("src", "WiseOwl.Cobol.Compiler", "CodeGen", "Roslyn", "PlaceRenderer.cs"));
        Match body = Regex.Match(reader, @"string\? VarGroupOperandImage\(.*?\n\s*: null;", RegexOptions.Singleline);
        Assert.True(body.Success && CodeOccurrences(body.Value, "CurrentExtentTransferImageCapable") > 0,
            "PlaceRenderer.VarGroupOperandImage no longer asks CurrentExtentTransferImageCapable (kb/Work PB3424).");
        foreach (string[] consumer in new[]
        {
            new[] { "CodeGen", "Emit", "OperandText.cs" },
            new[] { "CodeGen", "Emit", "NumericRenderer.cs" },
        })
        {
            string src = File.ReadAllText(TestRepo.At(["src", "WiseOwl.Cobol.Compiler", .. consumer]));
            Assert.True(CodeOccurrences(src, "PlaceRenderer.VarGroupOperandImage(") > 0,
                $"{consumer[^1]} must read a variable-length group's characters through PlaceRenderer.VarGroupOperandImage (kb/Work PB3424).");
        }
        string display = File.ReadAllText(TestRepo.At("src", "WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "AcceptDisplayEmitter.cs"));
        // DISPLAY asks OperandText.AsDeviceString (kb/Work PB3505), whose every operand other than a fixed-point numeric
        // item - a group among them - is OperandText.AsTransferString's.
        string operandText = File.ReadAllText(TestRepo.At("src", "WiseOwl.Cobol.Compiler", "CodeGen", "Emit", "OperandText.cs"));
        Assert.True(CodeOccurrences(display, "OperandText.AsDeviceString(") > 0 && CodeOccurrences(display, "CurrentExtentTransferImageCapable") == 0
                    && CodeOccurrences(operandText, ": AsTransferString(op, num, context)") > 0,
            "DISPLAY must read a group operand through OperandText.AsDeviceString -> AsTransferString, never its own copy of the capability gate (kb/Work PB3424).");

        // The record struct emits the one-way CurrentImage for the wider capability and the carrier pair only for the narrower.
        string emitter = File.ReadAllText(TestRepo.At("src", "WiseOwl.Cobol.Compiler", "CodeGen", "DataDivision", "RecordStructEmitter.cs"));
        Assert.True(CodeOccurrences(emitter, "item.CurrentExtentTransferImageCapable") > 0
                    && CodeOccurrences(emitter, "item.CurrentExtentImageCapable") > 0,
            "RecordStructEmitter must emit CurrentImage() for CurrentExtentTransferImageCapable and the AsVarImage/FromVarImage carrier only for CurrentExtentImageCapable (kb/Work PB3358).");
    }

    [Fact]
    public void BoundaryScreens_AskTheBoundaryPredicate()
    {
        // OoConformance holds no group screen of its own: the pair-mode DataItem comparator, whose argument-group and
        // formal-group arms asked BoundaryImageCapable and VariableLengthCompatibility.Mismatch, is deleted (kb/Work
        // PB3101). Every activation and pair-mode question describes a group through ActivationDescriptions.Of, whose
        // variable-length arm asks CurrentExtentImageCapable before IsImageCapable — the two halves of
        // BoundaryImageCapable — and the §8.5.1.12 compatibility is the run-time relation's walk
        // (ActivationRelations.VariableLengthViolation over the descriptions' atoms).
        string oo = File.ReadAllText(TestRepo.At("src", "WiseOwl.Cobol.Compiler", "Oo", "OoConformance.cs"));
        Assert.Equal(0, CodeOccurrences(oo, "BoundaryImageCapable"));
        Assert.Equal(0, CodeOccurrences(oo, "VariableLengthCompatibility.Mismatch"));
        Assert.Equal(0, CodeOccurrences(oo, "IsImageCapable"));
        string descriptions = File.ReadAllText(
            TestRepo.At("src", "WiseOwl.Cobol.Compiler", "Oo", "ActivationDescriptions.cs"));
        Assert.True(CodeOccurrences(descriptions, "item.CurrentExtentImageCapable") > 0
                    && CodeOccurrences(descriptions, "item.IsImageCapable") > 0,
            "ActivationDescriptions.Of no longer describes a group by both halves of BoundaryImageCapable — a compatible "
            + "variable-length group argument would have no description and be refused (kb/Work PB818).");
        string relations = File.ReadAllText(
            TestRepo.At("src", "WiseOwl.Cobol.Runtime", "Control", "ActivationRelations.cs"));
        Assert.True(CodeOccurrences(relations, "VariableLengthViolation(") > 1,
            "ActivationRelations no longer asks its §8.5.1.12 walk — a variable-length group pair would be judged by "
            + "length alone (kb/Work PB204, PB3101).");

        // CallEmitter: the stage guard that used to refuse the crossing outright.
        string call = File.ReadAllText(TestRepo.At("src", "WiseOwl.Cobol.Compiler", "CodeGen", "Verbs", "CallEmitter.cs"));
        Assert.Equal(1, CodeOccurrences(call, "BoundaryImageCapable"));
        Assert.Equal(0, CodeOccurrences(call, "p.Item.IsGroup && !p.Item.IsImageCapable"));
    }
}
