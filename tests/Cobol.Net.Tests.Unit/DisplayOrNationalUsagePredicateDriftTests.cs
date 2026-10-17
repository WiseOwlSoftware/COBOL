// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

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
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DisplayOrNationalUsagePredicateDriftTests
{
    private static string Code(params string[] segments) => StripComments(File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", .. segments])));

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

    [Fact]
    public void TheStronglyTypedLeafWalk_ExistsOnce_InItemCategory()
    {
        var definitions = new List<string>();
        string root = TestRepo.Src("Cobol.Net.Compiler");
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
