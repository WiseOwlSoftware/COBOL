// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A NAME JOINS THE NAME INDEX OF THE SOURCE ELEMENT THAT OWNS ITS RECORD, AT EVERY REGISTRATION (kb/Work PB3192). A
/// method definition is a source element of its own, and its data-names, condition-names, index-names and type-names are
/// local names (ISO §8.4.6.2.2; §13.18.27.3 SR4) that hide its object's of the spelling inside the method (§11.7.4 GR5).
/// They used to join the CLASS's maps as they were bound and move to the method's scope only after the whole method had
/// bound (<c>OoScopeSubtree</c>), while type-names never moved: so a method constant's <c>LENGTH OF W</c> measured the
/// object's <c>W</c>, a method's <c>TYPEDEF</c> spelled like its object's drew "duplicate type-name", and the members of a
/// TYPE copy composed after the move leaked to every sibling method (the backend then failed CS0103). Every registration
/// now asks <c>NameScopeOf</c> (the owner of the item's root, recorded when a method's entry opens the record) — so a
/// copy made at any time lands in the right element — and the move is gone. This pins the funnels: the binder's own
/// data-name and condition-name maps are written only by them (and by the contained-program inheritance of a container's
/// GLOBAL names, which no method has), and the type-name table only through the declaring element's own table.
/// </summary>
public sealed class MethodNameRegistrationDriftTests
{
    private static readonly string BindingDir = TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding");

    /// <summary>The writes into the binder's own data-name and condition-name maps, by file.</summary>
    private static readonly Regex DirectMapWrite =
        new(@"(?<![\w.])(ByName|Conditions)(\[[^\]]+\]\s*=|\.(Add|TryAdd)\()", RegexOptions.Compiled);

    [Fact]
    public void TheBindersOwnNameMaps_AreWrittenOnlyByTheContainedProgramInheritance()
    {
        List<string> offenders = [];
        foreach (var file in Directory.EnumerateFiles(BindingDir, "DataBinder*.cs"))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (DirectMapWrite.IsMatch(lines[i]) && !lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal)
                    && !(Path.GetFileName(file) == "DataBinder.Linkage.cs" && EnclosingMember(lines, i) == "InheritGlobalSubtree"))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }
        Assert.True(offenders.Count == 0, "a data-name or condition-name is written into the binder's own map instead of "
            + "through RegisterName / RegisterCondition, which ask NameScopeOf (kb/Work PB3192):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void EveryRegistrationFunnel_AsksTheOwningElement()
    {
        string text = File.ReadAllText(Path.Combine(BindingDir, "DataBinder.cs"));
        foreach (var funnel in new[] { "RegisterName", "RegisterCondition", "RegisterIndexes", "RegisterTypeDecl" })
        {
            int at = text.IndexOf($"private void {funnel}(", StringComparison.Ordinal);
            Assert.True(at > 0, $"{funnel} is gone — the pin measured nothing");
            int end = text.IndexOf("\n    }", at, StringComparison.Ordinal);
            string body = text[at..end];
            Assert.True(body.Contains("NameScopeOf(", StringComparison.Ordinal) || body.Contains("TypeDeclsOf(ScopeOf(", StringComparison.Ordinal),
                $"{funnel} no longer asks which source element owns the item (NameScopeOf / TypeDeclsOf(ScopeOf(…))).");
        }
        Assert.False(Regex.IsMatch(text, @"(?<![\w.])TypeDecls\.TryAdd\("),
            "a type-name is added to this binder's own table directly instead of through TypeDeclsOf(ScopeOf(…)).");
    }

    [Fact]
    public void NoNameMovesAfterTheMethodBinds()
    {
        foreach (var file in Directory.EnumerateFiles(BindingDir, "*.cs", SearchOption.AllDirectories))
            Assert.DoesNotContain("OoScopeSubtree(", File.ReadAllText(file));
    }

    /// <summary>The name of the member whose body holds line <paramref name="index"/>: the nearest member header above it.</summary>
    private static string EnclosingMember(string[] lines, int index)
    {
        var header = new Regex(@"^    (?:internal |private |public )[^=;(]*?\b(\w+)\(");
        for (int i = index; i >= 0; i--)
            if (header.Match(lines[i]) is { Success: true } m) return m.Groups[1].Value;
        return "";
    }
}
