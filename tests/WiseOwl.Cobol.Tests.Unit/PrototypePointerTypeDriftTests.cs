// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ NO RELATION COMPARES A RESTRICTED PROGRAM- OR FUNCTION-POINTER'S PROTOTYPE NAME AS WRITTEN (kb/Work PB2464, PB4107):
/// each asks one of the pointer's two resolved restrictions.
/// <para>What such a pointer may CONTAIN is fixed by the SIGNATURE of the prototype it names (ISO §13.18.60.4 GR25 / GR26:
/// "shall contain only … the address of a program with the same signature as that identified by the specified
/// program-prototype-name"), so §14.9.39.3 SR20 / SR22 admit a SET between pointers restricted to two differently named
/// prototypes of one signature, and so do the rules that send to SET (§14.8.2.3.3; §9.3.6 4 c), 5 c), 6). Those ask
/// <c>PrototypeSignatures.SignatureRestriction</c> (the identity of the prototype's signature
/// <c>PrototypeSignatures.StampContentRestrictions</c> attached) or, with the unit's prototype tables in hand,
/// <c>PrototypeSignatures.Same</c>. The identity rules — §9.3.6 3 e), §14.8.2.3.2's "of the same type", §14.8.3.3, the
/// pair-mode checks of §9.3.8.2.3 and the signature graph itself (§13.7.3 SR2, kb/Work PB4491) — compare the USAGE clause's
/// TO phrase by the prototype it NAMES, which is
/// <c>PrototypeSignatures.NamedRestriction</c>: the folded EXTERNALIZED name, so neither a spelling's case nor a
/// prototype's local word decides (kb/Work PB4107). A comparison of <c>PicInfo.RestrictedPrototypeName</c> itself is
/// neither relation: it once refused two prototypes of one signature in a SET-governed relation (kb/Work PB2464), and
/// this test keeps the NEXT relation from comparing it again. Behaviour is pinned by
/// <c>conformance:cases/_unassigned/pb2464_prototype_pointer_signature_type</c>,
/// <c>conformance:cases/_unassigned/pb2464_function_pointer_signature_type</c>,
/// <c>conformance:cases/14.8.2.3.2-Elementary-items-passed-by-reference/pb4107_restricted_pointer_identity_and_signature</c>
/// and the negatives <c>pb2464-typed-pointer-signature-mismatch</c> and <c>pb4107-*</c>.</para>
/// <para>A pointer decoded from another group's repository record is held to the same rule end to end
/// (<see cref="ADecodedPointer_MayContainWhatItsPrototypesSignatureAllows_NeverWhatItsNameDoes"/>; DESIGN-external-repository §6.4).</para>
/// <para>Proven to FAIL before it was trusted: <see cref="ThePatternThisTestKeysOn_FiresOnThePB2464Defect"/> feeds the
/// matcher the exact line the tree carried and asserts it is caught, and feeds it the repaired line and the signature
/// readers and asserts they are not.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class PrototypePointerTypeDriftTests
{
    /// <summary>A COMPARISON over the prototype name: a name-equality call or an operator with the property on either side.</summary>
    private static readonly Regex NameComparison = new(
        @"(?:CobolNames\.Same|string\.Equals|\.Equals|Comparer\.Equals)\s*\([^;]*RestrictedPrototypeName"
        + @"|RestrictedPrototypeName\s*(?:==|!=)|(?:==|!=)\s*[\w.?!]*RestrictedPrototypeName",
        RegexOptions.Compiled);

    [Fact]
    public void NoSourceLineComparesARestrictedPrototypeName()
    {
        var offenders = new List<string>();
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                                 && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;
                if (NameComparison.IsMatch(code))
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{i + 1}  {code}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A source line compares a restricted program- or function-pointer's prototype name AS WRITTEN. A SET-governed "
            + "relation asks what the pointer may contain — PrototypeSignatures.SignatureRestriction, or with the unit's "
            + "prototype tables in hand PrototypeSignatures.Same (ISO §13.18.60.4 GR25 / GR26; §14.9.39.3 SR20 / SR22; kb/Work "
            + "PB2464) — and an identity rule the TO phrase by the prototype it names, PrototypeSignatures.NamedRestriction "
            + "(§9.3.6 3 e); §14.8.2.3.2; §14.8.3.3; kb/Work PB4107):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>A <c>PicInfo</c> record-equality compare as the type model writes it: a <c>with { EditingRules = null, … }</c>
    /// projection, the shape that elides the members a record compare cannot carry.</summary>
    private static readonly Regex ProfileProjection = new(@"with\s*\{\s*EditingRules\s*=\s*null[^}]*\}", RegexOptions.Compiled);

    /// <summary>The restricted-pointer names a <c>PicInfo</c> record compare must not read (kb/Work PB3120, PB4107): the
    /// prototype name as WRITTEN (a program- or function-pointer's TO phrase names the prototype by its folded externalized
    /// name, <c>PrototypeSignatures.NamedRestriction</c>) and the type-name (a data-pointer is typed by the DECLARATION it
    /// names, which the record compare elides).</summary>
    private static readonly string[] RestrictionNames = ["RestrictedPrototypeName", "RestrictedTypeName"];

    /// <summary>⛔ A <c>PicInfo</c> RECORD COMPARE DOES NOT DECIDE A RESTRICTED POINTER'S TYPE (kb/Work PB3120, PB4107).
    /// <c>StrongTypeModel.SameAnalyzedProfile</c> compares two elementary items' analyzed profiles with <c>PicInfo</c>'s own
    /// record equality, which compares <c>RestrictedPrototypeName</c> and <c>RestrictedTypeName</c> by value — the words as
    /// WRITTEN, which §8.5.3.1 does not compare: a prototype is named by its externalized name whatever its local word or
    /// case, and a data-pointer's type is the declaration its type-name resolves to (two declarations of one type-name in
    /// two source elements). Every <c>with { EditingRules = null, … }</c> projection in the compiler must elide BOTH names,
    /// and the comparison reads <c>PrototypeSignatures.NamedRestriction</c> / <c>StrongTypeModel.SameRestriction</c>
    /// instead.</summary>
    [Fact]
    public void EveryPicInfoProfileProjectionElidesTheRestrictedNames()
    {
        var offenders = new List<string>();
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                                 && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            foreach (string missing in ProjectionsMissingARestrictedName(File.ReadAllText(file)))
                offenders.Add($"{Path.GetRelativePath(root, file)}  {missing}");
        Assert.True(offenders.Count == 0,
            "A PicInfo profile projection leaves a restricted-pointer name in the record compare, so a restricted pointer is "
            + "decided by its NAME (kb/Work PB3120) - elide it and ask the pointer's TYPE:\n  " + string.Join("\n  ", offenders));
    }

    private static IEnumerable<string> ProjectionsMissingARestrictedName(string source) =>
        ProfileProjection.Matches(source)
            .Where(m => RestrictionNames.Any(n => !m.Value.Contains(n + " = null", StringComparison.Ordinal)))
            .Select(m => m.Value);

    [Fact]
    public void TheProjectionPin_FiresOnThePB3120Defect()
    {
        // The exact projection StrongTypeModel.SameAnalyzedProfile carried before the repair.
        Assert.NotEmpty(ProjectionsMissingARestrictedName(
            "if ((x with { EditingRules = null, RestrictedTypeDecl = null }) != (y with { EditingRules = null, RestrictedTypeDecl = null }))"));
        // Eliding only the prototype name still leaves a data-pointer's type-name in the compare.
        Assert.NotEmpty(ProjectionsMissingARestrictedName(
            "x with { EditingRules = null, RestrictedTypeDecl = null, RestrictedPrototypeName = null }"));
        Assert.Empty(ProjectionsMissingARestrictedName(
            "x with { EditingRules = null, RestrictedTypeName = null, RestrictedTypeDecl = null, RestrictedPrototypeName = null }"));
    }

    /// <summary>⛔ EVERY GROUP-LEVEL SIGNATURE CHECK RUNS AFTER THE POINTER RESTRICTIONS ARE RESOLVED (kb/Work PB2495). A
    /// restricted program- or function-pointer formal is described by the EXTERNALIZED name of the prototype its TO phrase
    /// names (kb/Work PB4107, PB4491), which <c>BinderDriver.ResolvePointerRestrictions</c> attaches; a check that runs before
    /// it reads the word as written, so it refuses an override, an implementation or a §10.6.2 SR2 prototype / definition
    /// pair whose two units name ONE prototype by two words (a REPOSITORY specifier's AS phrase, §12.3.8.2). Being after the
    /// program units' DATA phase is not enough: the four OO checks once ran ahead of that phase, but
    /// <c>CheckPrototypeSignaturePairs</c> ran inside the REPOSITORY resolver's builder — after it, and still ahead of
    /// <c>ResolvePointerRestrictions</c> — and refused pairs it had to admit.
    /// <para>The pin is EXECUTION order, not text order: a helper defined below <c>Bind</c> can run before a call written
    /// above it, which is exactly where <c>CheckPrototypeSignaturePairs</c> sat. So each check is invoked ONCE in the
    /// compiler, as a top-level statement of <c>BinderDriver.Bind</c>'s body (no loop, branch, lambda or helper between),
    /// as is <c>ResolvePointerRestrictions</c>, and the check's statement follows it. Proven to fail on the pre-repair
    /// shape by <see cref="TheOrderPin_FiresOnThePB2495Defect"/>. Behaviour is pinned by
    /// <c>conformance:cases/10.6-COBOL-compilation-group/pb4491_prototype_pointer_formal_one_prototype</c>.</para></summary>
    [Theory]
    [InlineData("CheckPrototypeSignaturePairs")]
    [InlineData("ValidateInterfaceInheritance")]
    [InlineData("ValidateOverrideSignatures")]
    [InlineData("ValidatePropertyAccessorPairs")]
    [InlineData("ValidateImplements")]
    public void EverySignatureCheckRunsAfterThePointerTypesAreResolved(string check)
    {
        string? violation = OrderViolation(CompilerSources.Value, check);
        Assert.True(violation is null, violation);
    }

    /// <summary>The order pin, proven to fail on the shape the tree carried before kb/Work PB2495:
    /// <c>CheckPrototypeSignaturePairs</c> inside <c>BuildRepositoryResolver</c> (a helper written BELOW <c>Bind</c>, which a
    /// text-offset comparison reads as "after" <c>ResolvePointerRestrictions</c>) and an OO check written ahead of it are
    /// both caught; the repaired shape passes, and a check hidden in a loop or a lambda, or missing, is caught.</summary>
    [Fact]
    public void TheOrderPin_FiresOnThePB2495Defect()
    {
        const string defect = """
            internal sealed class BinderDriver
            {
                public BoundCompilation Bind()
                {
                    OoConformance.ValidateOverrideSignatures(table, edition);
                    foreach (var unit in units) BindUnitData(unit, session);
                    session.Repository = BuildRepositoryResolver(units, classes, session);
                    ResolvePointerRestrictions(units, classes, oo.InterfaceData, session);
                    return null;
                }
                private static RepositoryResolver BuildRepositoryResolver()
                {
                    CheckPrototypeSignaturePairs(units, session.Edition);
                    return null;
                }
            }
            """;
        SyntaxTree[] before = [CSharpSyntaxTree.ParseText(defect)];
        Assert.Contains("not a top-level statement", OrderViolation(before, "CheckPrototypeSignaturePairs"));
        Assert.Contains("BEFORE ResolvePointerRestrictions", OrderViolation(before, "ValidateOverrideSignatures"));

        const string repaired = """
            internal sealed class BinderDriver
            {
                public BoundCompilation Bind()
                {
                    session.Repository = BuildRepositoryResolver(units, classes, session);
                    ResolvePointerRestrictions(units, classes, oo.InterfaceData, session);
                    CheckPrototypeSignaturePairs(units, session.Edition);
                    var ooAdapters = OoConformance.ValidateImplements(table, edition);
                    foreach (var cls in classes) OoConformance.ValidateOverrideSignatures(table, edition);
                    Action later = () => OoConformance.ValidatePropertyAccessorPairs(table, edition);
                    return null;
                }
            }
            """;
        SyntaxTree[] after = [CSharpSyntaxTree.ParseText(repaired)];
        Assert.Null(OrderViolation(after, "CheckPrototypeSignaturePairs"));
        Assert.Null(OrderViolation(after, "ValidateImplements"));
        Assert.Contains("not a top-level statement", OrderViolation(after, "ValidateOverrideSignatures"));
        Assert.Contains("not a top-level statement", OrderViolation(after, "ValidatePropertyAccessorPairs"));
        Assert.Contains("invoked 0 times", OrderViolation(after, "ValidateInterfaceInheritance"));
    }

    private const string Resolver = "ResolvePointerRestrictions";

    private static readonly string[] PinnedMethods =
    [
        Resolver, "CheckPrototypeSignaturePairs", "ValidateInterfaceInheritance", "ValidateOverrideSignatures",
        "ValidatePropertyAccessorPairs", "ValidateImplements",
    ];

    /// <summary>The compiler's sources that name a pinned method, parsed once for every case.</summary>
    private static readonly Lazy<SyntaxTree[]> CompilerSources = new(() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                        && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Select(f => (Path: f, Text: File.ReadAllText(f)))
            .Where(f => PinnedMethods.Any(m => f.Text.Contains(m, StringComparison.Ordinal)))
            .Select(f => CSharpSyntaxTree.ParseText(f.Text, path: f.Path))
            .ToArray());

    /// <summary>Null when <paramref name="check"/> is invoked exactly once in <paramref name="trees"/>, as a top-level
    /// statement of <c>BinderDriver.Bind</c>'s body that follows the one top-level statement invoking
    /// <c>ResolvePointerRestrictions</c>; otherwise why not. The statements directly in one method body run in the order
    /// they are written, so for them, and only for them, text order IS execution order.</summary>
    private static string? OrderViolation(IReadOnlyList<SyntaxTree> trees, string check)
    {
        var bind = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            .SingleOrDefault(m => m.Identifier.Text == "Bind"
                                  && m.Parent is ClassDeclarationSyntax { Identifier.Text: "BinderDriver" });
        if (bind?.Body is not { } body) return "BinderDriver.Bind was not found — the pin measured nothing";

        string? Place(string method, out int index)
        {
            index = -1;
            var calls = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                .Where(i => CalledName(i) == method).ToList();
            if (calls.Count != 1)
                return $"'{method}' is invoked {calls.Count} times in the compiler; the pin needs it exactly once, in BinderDriver.Bind";
            var call = calls[0];
            var statement = call.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
            if (statement?.Parent != body
                || call.Ancestors().TakeWhile(a => a != statement).Any(a => a is AnonymousFunctionExpressionSyntax))
            {
                string host = call.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "no method";
                return $"'{method}' is not a top-level statement of BinderDriver.Bind (it is in {host}, or under a loop, "
                       + "branch, local function or lambda there), so its place in the text says nothing about when it runs";
            }
            index = body.Statements.IndexOf(statement);
            return null;
        }

        if (Place(Resolver, out int resolved) is { } unresolved) return unresolved;
        if (Place(check, out int at) is { } unplaced) return unplaced;
        return at > resolved ? null
            : $"'{check}' runs BEFORE ResolvePointerRestrictions, so it compares restricted prototype-pointers by the word "
              + "each TO phrase WRITES, not by the externalized name of the prototype it names, and refuses two words for one "
              + "prototype (ISO §12.3.8.2; kb/Work PB2495, PB4491)";
    }

    private static string? CalledName(InvocationExpressionSyntax call) => call.Expression switch
    {
        IdentifierNameSyntax id => id.Identifier.Text,
        MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
        _ => null,
    };

    // ── decoded identities (DESIGN-external-repository §6.4 part 3; kb/Work PB2100) ─────────────────────────────────────

    /// <summary>The importing group: a prototype PA of one signature and a program holding a pointer restricted to it.</summary>
    private const string Importer = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PA IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 X PIC 9(4).
               PROCEDURE DIVISION USING X.
               END PROGRAM PA.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. GHOLD.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM PA.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO PA.
               01 P TYPE PT.
               PROCEDURE DIVISION.
                   GOBACK.
               END PROGRAM GHOLD.
               """;

    /// <summary>A publishing group: <paramref name="prototype"/> with a formal of <paramref name="picture"/>, and a program
    /// PUB whose formal is a program-pointer restricted to it.</summary>
    private static string Publisher(string prototype, string picture) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {prototype} IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 X PIC {picture}.
               PROCEDURE DIVISION USING X.
               END PROGRAM {prototype}.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PUB.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   PROGRAM {prototype}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO {prototype}.
               LINKAGE SECTION.
               01 L TYPE PT.
               PROCEDURE DIVISION USING L.
                   GOBACK.
               END PROGRAM PUB.
               """;

    /// <summary>A pointer read from ANOTHER group's published record (rendered into a module and read back,
    /// <c>RepositoryReader</c>) and decoded into the importing group (<c>RepositoryCodec.Decode</c>, which stamps it with
    /// the identity of its prototype's decoded signature) may contain what its prototype's SIGNATURE allows, never what its
    /// name does (ISO §13.18.60.4 GR25: "the address of a program with the same signature as that identified by the
    /// specified program-prototype-name"): a prototype named otherwise with the group's signature shares the group
    /// pointer's content restriction, one named like the group's prototype with another signature gets another, and two
    /// such records decoded in turn keep one each.</summary>
    [Fact]
    public void ADecodedPointer_MayContainWhatItsPrototypesSignatureAllows_NeverWhatItsNameDoes()
    {
        var importer = RepositoryCodecTests.Bind(Importer, 2023);
        var held = importer.Units.Single(u => u.Name == "GHOLD").Data.Forest.WorkingStorageRoots.Single(r => r.CobolName == "P");
        DataItem DecodedPointer(string prototype, string picture)
        {
            var publisher = RepositoryCodecTests.Bind(Publisher(prototype, picture), 2023);
            var record = RepositoryReaderTests.ReadLibrary([RepositoryCodecTests.RecordOf(publisher, publisher.Units.Single(u => u.Name == "PUB"))]).Single();
            return RepositoryCodec.Decode(record).Formals.Single().Item;
        }

        string groupContent = PrototypeSignatures.SignatureRestriction(held)!;
        Assert.StartsWith(PrototypeSignatures.CanonicalPrefix, groupContent, StringComparison.Ordinal);

        var sameSignature = DecodedPointer("PB", "9(4)");
        Assert.Equal(groupContent, PrototypeSignatures.SignatureRestriction(sameSignature));

        var sameName = DecodedPointer("PA", "X(2)");
        Assert.NotEqual(groupContent, PrototypeSignatures.SignatureRestriction(sameName));
        Assert.StartsWith(PrototypeSignatures.CanonicalPrefix, PrototypeSignatures.SignatureRestriction(sameName), StringComparison.Ordinal);

        var again = DecodedPointer("PA", "S9(3)");
        Assert.NotEqual(PrototypeSignatures.SignatureRestriction(sameName), PrototypeSignatures.SignatureRestriction(again));
        Assert.NotEqual(groupContent, PrototypeSignatures.SignatureRestriction(again));
        Assert.Equal(groupContent, PrototypeSignatures.SignatureRestriction(held));   // decoding moved no group pointer
    }

    /// <summary>⛔ NO DESCRIPTION OF A SIGNATURE READS WHAT A POINTER MAY CONTAIN (kb/Work PB4442, PB4491). The pair-mode
    /// relation (<c>ActivationRelations.SignatureDifference</c>: an override, IMPLEMENTS, an interface pair, a property
    /// accessor pair AND §13.7.3 SR2's prototype / definition pair) compares a restricted pointer's USAGE clause, its TO
    /// phrase by the prototype it names, and the signature's key (<c>PrototypeSignatures.SignatureKey</c> /
    /// <c>DescriptionKey</c>) writes the same; <c>ActivationDescription.ContentRestriction</c> is the SET relation's alone.
    /// Were either to read it again, two prototypes that name each other would need a fixed point to compare, and a
    /// prototype <c>USING TO Q1</c> would be admitted for a definition <c>USING TO Q2</c> — every caller checked against the
    /// prototype then passing the definition a pointer its formal refuses (§14.8.2.3.2).</summary>
    [Theory]
    [InlineData("WiseOwl.Cobol.Runtime", "ActivationRelations", "SignatureDifference")]
    [InlineData("WiseOwl.Cobol.Compiler", "PrototypeSignatures", "SignatureKey")]
    [InlineData("WiseOwl.Cobol.Compiler", "PrototypeSignatures", "DescriptionKey")]
    [InlineData("WiseOwl.Cobol.Compiler", "PrototypeSignatures", "SameDescription")]
    public void NoSignatureDescriptionReadsWhatAPointerMayContain(string project, string type, string method)
    {
        var bodies = Directory.EnumerateFiles(TestRepo.Src(project), type + ".cs", SearchOption.AllDirectories)
            .SelectMany(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Where(m => m.Identifier.Text == method && m.Parent is ClassDeclarationSyntax c && c.Identifier.Text == type)
            .ToList();
        Assert.True(bodies.Count > 0, $"{type}.{method} was not found in {project} — the pin measured nothing");
        foreach (var body in bodies) Assert.Null(ContentRead(body));
    }

    /// <summary>The first identifier of <paramref name="method"/>'s body that reads what a pointer may contain, or null.</summary>
    private static string? ContentRead(MethodDeclarationSyntax method) =>
        ((SyntaxNode?)method.Body ?? method.ExpressionBody)?.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Select(i => i.Identifier.Text)
            .FirstOrDefault(n => n is "ContentRestriction" or "PointerRestrictionIdentity" or "SignatureRestriction");

    /// <summary>The pin, proven to fail on the arm PB4442 left and PB4491 removed: the pair-mode relation's
    /// <c>pointersBySignature</c> arm compared two pointers' <c>ContentRestriction</c>s; the repaired arm, which compares the
    /// clauses, passes.</summary>
    [Fact]
    public void TheContentPin_FiresOnThePB4491Defect()
    {
        static MethodDeclarationSyntax Method(string body) => CSharpSyntaxTree.ParseText(
                "static class ActivationRelations { static string? SignatureDifference(ActivationDescription first, ActivationDescription second) { " + body + " } }")
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        Assert.Equal("ContentRestriction", ContentRead(Method(
            "return (pointersBySignature && IsPointerClass(first) && IsPointerClass(second) ? string.Equals(first.ContentRestriction, second.ContentRestriction, StringComparison.Ordinal) : SameElementaryClauses(first, second)) ? null : \"differ\";")));
        Assert.Null(ContentRead(Method("return first.AnyLength == second.AnyLength && SameElementaryClauses(first, second) ? null : \"differ\";")));
    }

    [Fact]
    public void ThePatternThisTestKeysOn_FiresOnThePB2464Defect()
    {
        // The exact line OoConformance.CategoryArmMismatch carried before the repair.
        Assert.Matches(NameComparison, "if (!CobolNames.Same(f.RestrictedPrototypeName, a.RestrictedPrototypeName))");
        Assert.Matches(NameComparison, "string.Equals(receiving.Pic.RestrictedPrototypeName, sending.Pic.RestrictedPrototypeName, StringComparison.Ordinal)");
        Assert.Matches(NameComparison, "if (a.Pic.RestrictedPrototypeName == b.Pic.RestrictedPrototypeName)");
        // The repaired line, and the readers that legitimately name the property, are not caught.
        Assert.DoesNotMatch(NameComparison,
            "if (!string.Equals(PrototypeSignatures.SignatureRestriction(formal), PrototypeSignatures.SignatureRestriction(arg), StringComparison.Ordinal))");
        Assert.DoesNotMatch(NameComparison,
            "? NamedRestriction(pointer.RestrictedPrototype?.ExternalizedName ?? name)");
        Assert.DoesNotMatch(NameComparison, "string? targetProto = receiver.Pic.RestrictedPrototypeName;");
        Assert.DoesNotMatch(NameComparison, "if (item.Pic is { RestrictedPrototypeName: { } name } pic)");
        Assert.DoesNotMatch(NameComparison, "!PrototypeSignatures.Same(host.ProgramSignatureOf(held), named.Signature)");
    }
}
