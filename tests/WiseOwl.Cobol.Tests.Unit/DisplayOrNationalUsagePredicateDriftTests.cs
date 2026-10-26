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
/// ⛔ "DESCRIBED IMPLICITLY OR EXPLICITLY AS USAGE DISPLAY OR NATIONAL" IS ONE PREDICATE, AND IT LIVES IN
/// <c>ItemCategory</c> BESIDE <c>UsageOf</c> (kb/Work PB3228). ISO §14.9.43.3 SR1 (STRING: "all identifiers, except
/// identifier-4, shall be described implicitly or explicitly as usage display or national") and §14.9.22.3 SR1/SR2
/// (INSPECT: "an elementary item described implicitly or explicitly as usage display or national") state one sentence.
/// STRING used to re-derive it from <c>Pic.Usage</c> guarded by <c>!IsGroup</c> beside a post-bind <c>StoreAsImage</c>
/// exemption, which is why a <c>GROUP-USAGE BIT</c> group — an elementary item of usage bit by §13.18.29.4 GR1 b) —
/// compiled clean as a STRING operand while INSPECT refused it.
/// <para><b>What this can and cannot see.</b> It pins the SHAPE: each rule's binder reaches the shared predicate, the
/// strongly-typed leaf walk exists once, and STRING no longer reads a storage form as a usage. The behaviour (a bit
/// group refused at identifier-1, -2 and -3, the alphanumeric and national groups still admitted) is the conformance
/// goldens <c>pb3228-string-bit-group-*</c> and <c>pb3228_string_group_usage_controls</c>.</para>
/// <para>The sentence is stated a third time by §8.8.4.2.5 for the numeric integer operand of a relation with a
/// character operand (kb/Work PB3691), asked of the same function reader.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DisplayOrNationalUsagePredicateDriftTests
{
    private static string Code(params string[] segments) => StripComments(File.ReadAllText(TestRepo.Src(["WiseOwl.Cobol.Compiler", .. segments])));

    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");

    [Fact]
    public void StringSr1_AsksTheOnePredicate_AndReadsNoStorageFormAsAUsage()
    {
        string src = Code("Binding", "Procedure", "Verbs", "StringUnstringBinder.cs");
        Assert.Contains("ItemCategory.UsageOutsideDisplayOrNational(", src);
        Assert.DoesNotContain("StoreAsImage", src);
        Assert.DoesNotContain("FirstLeafOutsideDisplayOrNational", src);
        Assert.False(Regex.IsMatch(src, @"pic\.Usage\s+is\s+not"),
            "STRING SR1 re-derives 'usage display or national' from Pic.Usage again; ask ItemCategory (kb/Work PB3228).");
    }

    [Fact]
    public void InspectSr1Sr2_AskTheOnePredicate()
    {
        string src = Code("Binding", "Validation", "StatementValidation.cs");
        Assert.Contains("ItemCategory.IsDisplayOrNationalUsage(", src);
        Assert.False(Regex.IsMatch(src, @"ItemCategory\.UsageOf\(item\)\s+is\s+Usage\.Display"),
            "INSPECT's elementary arm reads the usage itself again; ask ItemCategory.IsDisplayOrNationalUsage.");
    }

    /// <summary>kb/Work PB3665: the same sentence reaches a FUNCTION-IDENTIFIER, which §15.2 describes by its type (an
    /// alphanumeric function is usage display, a national one usage national, a boolean one usage bit, and the numeric,
    /// integer and index functions state none). STRING SR1 and INSPECT SR2 ask the ONE function reader beside the
    /// data-item predicate, and the usage-by-type table is written once, in <c>IntrinsicSig</c>; a binder that
    /// spells the function's usage out itself (or lets the folded FUNCTION LENGTH fall to the numeric-literal arm)
    /// is how STRING came to admit <c>FUNCTION INTEGER(N)</c> and call <c>FUNCTION LENGTH(G)</c> a literal.</summary>
    [Fact]
    public void FunctionIdentifierUsage_IsAskedOfTheOneReader_ByStringAndInspect()
    {
        string str = Code("Binding", "Procedure", "Verbs", "StringUnstringBinder.cs");
        Assert.Contains("IntrinsicArgumentRules.FunctionIsNotDisplayOrNational(", str);
        Assert.True(str.IndexOf("FunctionIsNotDisplayOrNational(", StringComparison.Ordinal)
            < str.IndexOf("BoundNumericLiteral =>", StringComparison.Ordinal),
            "STRING SR1's function arm must precede its numeric-literal arm, or the folded FUNCTION LENGTH is called a literal.");
        Assert.Contains("IntrinsicArgumentRules.FunctionIsNotDisplayOrNational(", Code("Binding", "Validation", "StatementValidation.cs"));
        string inspect = Code("Binding", "Procedure", "Verbs", "InspectBinder.cs");
        Assert.Contains("CheckInspectFunctionUsage(", inspect);
        // SR1's identifier-1 (Format 1 TALLYING-only function) and SR2's keyword-omitted identifier-3..n reach the same reader.
        Assert.Contains("CheckInspectIdentifier1FunctionUsage(", inspect);
        Assert.Contains("KeywordOmittedFunction(dref)", inspect);
        Assert.Contains("IntrinsicSig.ImplicitUsageOf(", Code("Binding", "IntrinsicArgumentRules.cs"));
    }

    /// <summary>kb/Work PB3691 (owner 2026-10-25): §8.8.4.2.5 — "The numeric integer operand shall be an integer literal
    /// or an integer numeric data item of usage display or national" — is the same sentence a third time. The relation
    /// checkpoint's admission of item 6's numeric operand asks the ONE function reader FIRST (before the literal arm,
    /// which the folded FUNCTION LENGTH would otherwise take), and never reads an implementor returned-value
    /// description (<c>SendingValueTemp.FunctionValuePic</c> and its siblings state the usage DISPLAY the implementor
    /// represents the value in, §15.4.1; they are no §15.2 description). An INTEGER function was once admitted "as the
    /// temporary §15.4 makes it", which is how <c>IF FUNCTION INTEGER(N) = "0007"</c> compiled and answered not equal.</summary>
    [Fact]
    public void RelationItem6NumericOperand_AsksTheOneFunctionReader_AndNoReturnedValueDescription()
    {
        string src = Code("Binding", "RelationComparability.cs");
        int reader = src.IndexOf("IntrinsicArgumentRules.FunctionIsNotDisplayOrNational(", StringComparison.Ordinal);
        Assert.True(reader >= 0, "RelationComparability.IsNumericIntegerOperand must ask FunctionIsNotDisplayOrNational (kb/Work PB3691).");
        Assert.True(reader < src.IndexOf("BoundNumericLiteral =>", StringComparison.Ordinal),
            "the function arm must precede the numeric-literal arm, or the folded FUNCTION LENGTH is called an integer literal.");
        Assert.DoesNotContain("FunctionValuePic", src);
        Assert.DoesNotContain("NumericReturnedValuePic", src);
        Assert.DoesNotContain("IntermediatePic", src);
    }

    [Fact]
    public void TheFunctionUsageTable_ExistsOnce()
    {
        var definitions = new List<string>();
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string sep = Path.DirectorySeparatorChar.ToString();
            if (file.Contains($"{sep}obj{sep}", StringComparison.Ordinal)) continue;
            string src = StripComments(File.ReadAllText(file));
            foreach (Match m in Regex.Matches(src, @"static\s+[\w<>?\.]+\s+(?<n>ImplicitUsageOf)\s*\("))
                definitions.Add($"{Path.GetFileName(file)}#{m.Groups["n"].Value}");
            // A second spelling of 'an alphanumeric function has usage display' is the table written again (ConditionBinder
            // and StaticUsageOf each carried one before kb/Work PB3665).
            if (Regex.IsMatch(src, @"PicCategory\.Alphanumeric\s*=>\s*Usage\.Display"))
                definitions.Add($"{Path.GetFileName(file)}#PicCategory.Alphanumeric=>Usage.Display");
        }
        Assert.Equal(["IntrinsicCatalog.cs#ImplicitUsageOf", "IntrinsicCatalog.cs#PicCategory.Alphanumeric=>Usage.Display"],
            definitions.OrderBy(d => d, StringComparer.Ordinal));
    }

    [Fact]
    public void TheStronglyTypedLeafWalk_ExistsOnce_InItemCategory()
    {
        var definitions = new List<string>();
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string sep = Path.DirectorySeparatorChar.ToString();
            if (file.Contains($"{sep}obj{sep}", StringComparison.Ordinal)) continue;
            string src = StripComments(File.ReadAllText(file));
            foreach (Match m in Regex.Matches(src, @"static\s+[\w<>?\.]+\s+(?<n>\w*OutsideDisplayOrNational)\s*\("))
                definitions.Add($"{Path.GetFileName(file)}#{m.Groups["n"].Value}");
        }
        Assert.Equal(["ItemCategory.cs#UsageOutsideDisplayOrNational"], definitions);
    }
}
