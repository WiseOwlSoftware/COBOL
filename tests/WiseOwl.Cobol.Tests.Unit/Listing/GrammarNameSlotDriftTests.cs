// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// Every place the grammar lets the programmer write a NAME carries an ANTLR element label whose prefix classifies it
/// (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Guards"): <c>ref</c>, <c>qual</c>, <c>def</c>, <c>nn</c>, or
/// <c>name</c> in a name rule's own body. The cross-reference's completeness oracle reads each name's class from that
/// label, so a slot with no label, or two slots sharing one, would hide a name from it. The slots come from the
/// generated parser's ATN (<see cref="GrammarNameSlots"/>); nothing lists a slot or a rule.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class GrammarNameSlotDriftTests
{
    /// <summary>Every rule of the COBOL parser has exactly one classified label per name slot.</summary>
    [Fact]
    public void EveryNameSlotOfTheGrammar_CarriesItsOwnClassifyingLabel()
    {
        var analysis = new GrammarNameSlots.Analysis(typeof(CobolParserCore));
        IReadOnlyList<NameSlotFinding> findings = analysis.Findings();
        Assert.True(findings.Count == 0,
            $"{findings.Count} rule(s) of CobolParserCore whose name slots and classified labels are not one for one "
            + "(label each slot with a unique ref/qual/def/nn/name label, DESIGN-listing.md section 6.7.1):"
            + Environment.NewLine + string.Join(Environment.NewLine, findings));
    }

    /// <summary>A literal that names a symbol carries a <c>lit</c> label (section 6.7.1, "The program-name literal"): every such label
    /// holds a <c>literal</c> context, so it labels what it says, and a CALL's and a CANCEL's literal-1, the literals that name programs
    /// (§2023:14.9.4.3 SR2, §2023:14.9.5.3 SR2; §1985:X.5.2.3 SR1, §1985:X.5.3.3 SR1), are among them: the occurrence oracle reads them
    /// as reference slots, so a literal-1 that lost its label would be an unrecorded reference no test saw.</summary>
    [Fact]
    public void EveryLiteralSlotLabel_HoldsALiteral_AndCallAndCancelCarryOne()
    {
        var labels = typeof(CobolParserCore).GetNestedTypes(BindingFlags.Public)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(f => (Type: t, Field: f)))
            .Where(x => GrammarNameSlots.IsLiteralSlot(x.Field.Name)).ToList();
        Assert.All(labels, x => Assert.Equal(typeof(CobolParserCore.LiteralContext), x.Field.FieldType));
        Assert.Contains(labels, x => x.Type == typeof(CobolParserCore.CallTargetContext));
        Assert.Contains(labels, x => x.Type == typeof(CobolParserCore.CancelTargetContext));
    }

    /// <summary>The closure and the reference rules the design names (section 6.7.1) are what the ATN computes: the
    /// name rules hold <c>fileName</c> (one <c>cobolWord</c>) and <c>programName</c> (a <c>cobolWord</c> or a
    /// <c>reservedGatedWord</c>); <c>procedureName</c> and <c>dataReference</c> are reference rules, and the headers
    /// <c>sectionName</c> and <c>paragraphName</c>, whose one element is a transition into <c>procedureName</c>, are
    /// neither. So a check that found no slot at all cannot pass for a clean grammar.</summary>
    [Fact]
    public void TheNameRulesAndReferenceRules_AreComputedFromTheAtn()
    {
        var analysis = new GrammarNameSlots.Analysis(typeof(CobolParserCore));
        Assert.Contains("fileName", analysis.NameRules);
        Assert.Contains("programName", analysis.NameRules);
        Assert.Contains("procedureName", analysis.ReferenceRules);
        Assert.Contains("dataReference", analysis.ReferenceRules);
        Assert.DoesNotContain("sectionName", analysis.NameRules.Concat(analysis.ReferenceRules));
        Assert.DoesNotContain("paragraphName", analysis.NameRules.Concat(analysis.ReferenceRules));
        Assert.Equal(["procedureName"], analysis.SlotsOf("paragraphName").Select(s => s.Target));
        // procedureName: its head and its qualifier, each a cobolWord, an INTEGERLIT or a reservedGatedWord.
        Assert.Equal(6, analysis.SlotsOf("procedureName").Count);
        Assert.Equal(2, analysis.SlotsOf("procedureName").Count(s => s.Target == "INTEGERLIT"));
    }

    /// <summary>The planted failure (section 6.7.1): the same counter over <c>NameSlotFixture.g4</c> reports exactly its
    /// six bad rules, each at the slot that hides, and passes its clean one. So the check can fail on each way a slot
    /// can hide: a reused label, an unlabelled <c>cobolWord</c>, an unlabelled alternative-of-a-slot token, a header
    /// whose transition into a reference rule carries no label, a label not of the name-slot label's form, and a classified
    /// label on an element that is no slot beside an unlabelled slot, whose counts agree (listing step 7 review lead R1-1).</summary>
    [Fact]
    public void TheCounter_ReportsEachPlantedFailure_AtItsSlot()
    {
        Type parser = FixtureParser.Value;
        var analysis = new GrammarNameSlots.Analysis(parser);
        Assert.Equal(["numbered"], analysis.NameRules);
        Assert.Equal(["dataRef", "procName"], analysis.ReferenceRules);

        Dictionary<string, NameSlotFinding> findings = analysis.Findings().ToDictionary(f => f.Rule);
        Assert.Equal(["header", "malformed", "misplaced", "numbered", "reused", "unlabelled"], findings.Keys.Order(StringComparer.Ordinal));

        AssertFinding(findings["reused"], ["cobolWord", "cobolWord"], 1);
        AssertFinding(findings["unlabelled"], ["cobolWord", "cobolWord"], 1);
        AssertFinding(findings["numbered"], ["INTEGERLIT", "cobolWord"], 1);
        AssertFinding(findings["header"], ["procName"], 0);
        AssertFinding(findings["malformed"], ["cobolWord"], 0);
        AssertFinding(findings["misplaced"], ["cobolWord", "cobolWord"], 2);
        Assert.Equal([GrammarNameSlots.Analysis.TokenElement, "cobolWord"], analysis.LabelledElementsOf("misplaced"));

        // The clean rules hold one field per slot: the reference rules' heads and qualifiers included.
        Assert.Equal(2, analysis.SlotsOf("clean").Count);
        Assert.Equal(4, analysis.SlotsOf("procName").Count);
        Assert.Equal(2, analysis.SlotsOf("dataRef").Count);
    }

    private static void AssertFinding(NameSlotFinding finding, string[] targets, int fields)
    {
        Assert.Equal(targets, finding.Slots.Select(s => s.Target).Order(StringComparer.Ordinal));
        Assert.Equal(fields, finding.Fields.Count);
    }

    /// <summary>The fixture's parser, generated by the vendored ANTLR tool the product build uses (the csproj's
    /// <c>AntlrJar</c>) with the product generator's language and encoding flags, compiled in memory and loaded from its
    /// bytes.</summary>
    private static readonly Lazy<Type> FixtureParser = new(GenerateFixtureParser, LazyThreadSafetyMode.ExecutionAndPublication);

    private static Type GenerateFixtureParser()
    {
        string frontend = TestRepo.Src("WiseOwl.Cobol.Frontend");
        string csproj = File.ReadAllText(Path.Combine(frontend, "WiseOwl.Cobol.Frontend.csproj"));
        Match jar = Regex.Match(csproj, "<AntlrJar>([^<]+)</AntlrJar>");
        Assert.True(jar.Success, "WiseOwl.Cobol.Frontend.csproj declares no <AntlrJar>");
        string jarPath = Path.Combine(frontend, jar.Groups[1].Value);
        string fixtureDir = TestRepo.Tests("WiseOwl.Cobol.Tests.Unit", "Listing", "Fixtures");
        string output = ScratchDirectory.Create("name-slot-fixture-");
        try
        {
            var psi = new ProcessStartInfo("java") { WorkingDirectory = fixtureDir };
            foreach (string arg in new[] { "-jar", jarPath, "-encoding", "UTF-8", "-Dlanguage=CSharp", "-no-listener", "-no-visitor",
                                           "-package", "NameSlotFixtureGenerated", "-o", output, "NameSlotFixture.g4" })
                psi.ArgumentList.Add(arg);
            ProcessObservation run = ProcessObserver.ObserveOrThrow(psi);
            Assert.True(run.ExitCode == 0 && !Regex.IsMatch(run.Stdout + run.Stderr, @"(?m)^(warning|error)\("),
                $"ANTLR refused the fixture (exit {run.ExitCode}):{Environment.NewLine}{run.Stdout}{run.Stderr}");

            // The test host's trusted platform assemblies hold the framework and every package the test project takes,
            // the ANTLR runtime the generated parser derives from among them.
            string[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
            CSharpCompilation compilation = CSharpCompilation.Create(
                "NameSlotFixtureGenerated",
                Directory.GetFiles(output, "*.cs").Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f)),
                references.Select(r => MetadataReference.CreateFromFile(r)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var image = new MemoryStream();
            var emitted = compilation.Emit(image);
            Assert.True(emitted.Success, string.Join(Environment.NewLine,
                emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
            Assembly assembly = Assembly.Load(image.ToArray());
            return assembly.GetType("NameSlotFixtureGenerated.NameSlotFixtureParser", throwOnError: true)!;
        }
        finally
        {
            ScratchDirectory.Remove(output);
        }
    }
}
