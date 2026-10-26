// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SECOND DECLARATION OF AN UNQUALIFIABLE SPECIAL-NAME IS REFUSED AT THE ONE DECLARATION FUNNEL (kb/Work PB3216).
/// ISO §8.4.2.1 — "In order to use a resource, a statement shall contain a reference that uniquely identifies that
/// resource" (cite.py --check 8.4.2.1) — and an alphabet-name, class-name, locale-name, ordering-name,
/// dynamic-length-structure-name, mnemonic-name or switch condition-name has no qualifier. The ALPHABET, CLASS and
/// switch condition-name registries dropped the second declaration silently (<c>TryAdd</c>, first wins) while LOCALE and
/// DYNAMIC LENGTH STRUCTURE each carried a private refusal. <c>DataBinder.DeclareUserWord</c> now asks
/// <c>UserWordKinds.DeclaredUnqualifiable</c> (COBOL4250), so a NEW SPECIAL-NAMES declaration kind is covered the
/// moment it declares its word through the funnel and joins that set — this test fails when it declares without joining.
/// </summary>
public sealed class SpecialNameRedeclarationDriftTests
{
    /// <summary>The SPECIAL-NAMES kinds that are NOT in the set, and why: a SYMBOLIC CHARACTERS name keeps its own
    /// §12.3.7.3 SR16 a rule, and a switch's condition-name shares its type with the qualifiable level-88 entries, so its
    /// declaration asks <c>CheckUnqualifiableDeclaration</c> explicitly.</summary>
    private static readonly string[] OutsideTheSet = ["SymbolicCharacter", "ConditionName"];

    private static string Switches() => File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.Switches.cs"));

    [Fact]
    public void EveryKindTheSpecialNamesBinderDeclares_IsInTheUnqualifiableSet()
    {
        var declared = Regex.Matches(Switches(), @"DeclareUserWord\([^;]*?UserWordKind\.(\w+)\)")
            .Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.Contains("AlphabetName", declared);   // the scan found the declarations at all
        var missing = declared.Where(k => !OutsideTheSet.Contains(k))
            .Where(k => !UserWordKinds.DeclaredUnqualifiable.Contains(Enum.Parse<UserWordKind>(k)))
            .ToList();
        Assert.True(missing.Count == 0,
            "SPECIAL-NAMES declares these kinds through DeclareUserWord without joining UserWordKinds.DeclaredUnqualifiable, "
            + "so a second declaration of one is silently dropped (kb/Work PB3216): " + string.Join(", ", missing));
    }

    [Fact]
    public void TheSwitchConditionNames_AskTheSameCheck()
    {
        string text = Switches();
        Assert.Contains("CheckUnqualifiableDeclaration(conditionName, UserWordKind.ConditionName)", text);
    }

    [Fact]
    public void NoClauseKeepsAPrivateDuplicateRefusal()
    {
        string text = Switches();
        Assert.DoesNotContain("COBOL1665", text);
        Assert.DoesNotContain("ownDls", text);
        Assert.DoesNotContain("ownNames", text);
    }
}
