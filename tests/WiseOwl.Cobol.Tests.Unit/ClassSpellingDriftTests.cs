// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DIAGNOSTIC NAMES AN ISO §2023:8.5.2.1 CLASS THROUGH THE ONE SPELLING, NEVER ITS OWN (kb/Work PB1400). The class
/// lattice (<c>CobolClass</c>) carries two REFINED members a CATEGORY-worded rule needs — numeric-edited of usage display
/// and of usage national — and <c>IntrinsicArgumentRules.Name</c> spells them as the class Table 2 gives them
/// ("alphanumeric (numeric-edited)"). The messages that spelled the enum themselves with
/// <c>ToString().ToLowerInvariant()</c> described a numeric-edited subscript, CALL / CANCEL target, BY VALUE argument or
/// report CODE operand as "class numericediteddeediting"; and <c>ExpressionBinder.NonNumericOperandKind</c> wrote the class OUT as a literal —
/// "group item … (class alphanumeric, ISO §2023:8.5)" — for every group, which told the user a GROUP-USAGE BIT group (class
/// boolean, §2023:8.5.2.5 3)) and a GROUP-USAGE NATIONAL group (class national, §2023:8.5.2.10 3)) were alphanumeric.
/// <para>The class of a place is spelled by <c>IntrinsicArgumentRules.ClassNameOf</c> (or <c>Name</c> over a class the
/// caller already holds); this sweep fails on an interpolated class word built from an enum's <c>ToString()</c>, and
/// on the literal group-class text that stood in the arithmetic-operand screen.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ClassSpellingDriftTests
{
    // "class {x.ToString()…}", "{x.ToString()…} class", ClassOf…(…).ToString() bound to a local, and an interpolated
    // class-named local ({cls.ToString()…}) whose "class " sits on the previous source line.
    private static readonly Regex OwnSpelling = new(
        @"class \{[^{}]*\.ToString\(\)|\{[^{}]*\.ToString\(\)[^{}]*\} class|ClassOf\w*\([^()]*\)\??\.ToString\(\)"
        + @"|\{\w*(?:[cC]ls|Class)\w*(?:\.Value)?\??\.ToString\(\)",
        RegexOptions.CultureInvariant);

    [Fact]
    public void NoDiagnostic_SpellsAClassFromTheEnumName()
    {
        string root = TestRepo.Src(["WiseOwl.Cobol.Compiler"]);
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, line, i)))
            .Where(t => OwnSpelling.IsMatch(t.line))
            .Select(t => $"{Path.GetRelativePath(root, t.f)}:{t.i + 1}: {t.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "spell a class through IntrinsicArgumentRules.Name / ClassNameOf (kb/Work PB1400):\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void TheArithmeticOperandScreen_AsksTheClassItNames()
    {
        string text = File.ReadAllText(TestRepo.Src(["WiseOwl.Cobol.Compiler", "Binding", "Procedure", "ExpressionBinder.cs"]));
        Assert.DoesNotContain("(class alphanumeric, ISO §2023:8.5)", text);
        Assert.DoesNotContain("a reference-modified operand (class alphanumeric", text);
        Assert.Contains("IntrinsicArgumentRules.ClassNameOf(p)", text);
    }

    // Name(…) over ClassOfItem's answer, inline or through a class local, in one interpolation hole.
    private static readonly Regex ItemClassThroughName = new(
        @"ClassOfItem\([^()]*\)\s*is\s*\{\s*\}\s*\w+\s*\?\s*(?:IntrinsicArgumentRules\.)?Name\(|Name\(\s*(?:IntrinsicArgumentRules\.)?ClassOfItem\(",
        RegexOptions.CultureInvariant);

    /// <summary>⛔ A DIAGNOSTIC THAT NAMES THE CLASS OF A DATA ITEM OR AN OPERAND ASKS <c>ClassNameOf</c> (kb/Work PB3192
    /// review R1-1). <c>ClassOfItem</c> answers "alphanumeric" for a strongly-typed group — §2023:15.3 item 2's reading of a
    /// function ARGUMENT — while §2023:8.5.2.1 makes its class the type-name everywhere else; the procedure-header BY VALUE
    /// screen (COBOL1553) spelled <c>Name(ClassOfItem(item))</c> and told the user a strongly-typed formal was of
    /// class alphanumeric. Every BY VALUE class screen (header formal, CALL, INVOKE, user function) and the
    /// operand-class exclusion screen names the class through the family; no source spells Name over ClassOfItem.</summary>
    [Fact]
    public void AClassScreenOverAnItemOrOperand_NamesTheClassThroughClassNameOf()
    {
        string root = TestRepo.Src(["WiseOwl.Cobol.Compiler"]);
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, line, i)))
            // the family's own body is the one place that composes Name over ClassOfItem, after the strong-group arm
            .Where(t => ItemClassThroughName.IsMatch(t.line) && !t.line.Contains("StrongGroupClassName(", StringComparison.Ordinal))
            .Select(t => $"{Path.GetRelativePath(root, t.f)}:{t.i + 1}: {t.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "name an item's class through IntrinsicArgumentRules.ClassNameOf (kb/Work PB3192 R1-1):\n"
            + string.Join("\n", offenders));

        (string[] path, string member)[] screens =
        [
            (["Binding", "ProcedureHeaderScreen.cs"], "public void ByValueClass"),
            (["Binding", "Procedure", "Verbs", "CallBinder.cs"], "private void ValueClassScreen"),
            (["Binding", "Procedure", "Verbs", "OoBinder.cs"], "InvokeByValueOperandClass"),
            (["Binding", "Procedure", "Verbs", "UdfBinder.cs"], "ByValueArgumentClass"),
            (["Binding", "Procedure", "ExpressionBinder.cs"], "OperandClassExcluded"),
        ];
        foreach (var (path, member) in screens)
        {
            string text = File.ReadAllText(TestRepo.Src(["WiseOwl.Cobol.Compiler", .. path]));
            int at = text.IndexOf(member, StringComparison.Ordinal);
            Assert.True(at >= 0, $"{string.Join('/', path)}: anchor '{member}' not found");
            int window = text.IndexOf("IntrinsicArgumentRules.ClassNameOf(", at, StringComparison.Ordinal);
            Assert.True(window >= 0 && window - at < 1200,
                $"{string.Join('/', path)}: the class screen at '{member}' names its class without ClassNameOf");
        }
    }

    [Theory]
    [InlineData("$\"{(IntrinsicArgumentRules.ClassOfItem(item) is { } cls ? IntrinsicArgumentRules.Name(cls) : \"unknown\")} \"", true)]
    [InlineData("$\"is of class {IntrinsicArgumentRules.Name(IntrinsicArgumentRules.ClassOfItem(item)!.Value)}\"", true)]
    [InlineData("$\"is of class {IntrinsicArgumentRules.ClassNameOf(item)}\"", false)]
    [InlineData("var cls = IntrinsicArgumentRules.ClassOfItem(item);", false)]
    public void TheItemClassPattern_CatchesTheFormsItWasWrittenIn(string text, bool offends)
        => Assert.Equal(offends, ItemClassThroughName.IsMatch(text));

}
