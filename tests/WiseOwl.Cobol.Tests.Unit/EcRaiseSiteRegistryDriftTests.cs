// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.CompilerServices;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Procedure;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY EXCEPTION-NAME THE CATALOG QUOTES IS DECLARED BY THE BINDER AND RAISED BY THE RUNTIME — OR IT IS DECLINED, WITH
/// A REASON A READER CAN FOLLOW (kb/Work PB3102, PB1762, PB1037; CONDITIONS_EXCEPTIONS_DESIGN D13).
/// ISO §2023:14.6.13.1.1: "If checking for an exception condition is enabled and an exception status indicator is set as a
/// result of an exception detected during the execution of a statement, the associated exception condition is raised",
/// and "when the general rules for a statement indicate that a specific exception condition exists, it is raised only if
/// checking for that exception condition is enabled". So a name needs BOTH halves, and a name with one is a defect with a
/// shape of its own:
/// <list type="bullet">
/// <item><b>raised but never declared</b> — the PB452 / PB409 / PB326 / PB349 / PB233 / PB1453 shape: the runtime has a
/// raise site whose checking flag no statement ever sets, so <c>&gt;&gt;TURN … CHECKING ON</c> wraps nothing and the
/// declarative never runs, behind a green suite;</item>
/// <item><b>declared but never raised</b> — a dead guard: the statement binds a wrapper for a condition nothing sets;</item>
/// <item><b>neither</b> — a catalogued name no part of the compiler or runtime accounts for (PB1036's four sort-merge names
/// sat in this state for months).</item>
/// </list>
/// A name that is deliberately neither is on <see cref="DeclinedRaiseSites"/>, and every entry names the
/// <c>docs/CONFORMANCE.md</c> row or section that says why. The list is data (<c>tests/doc-facts/declined-raise-sites.json</c>),
/// and <c>scripts/docchecks/facts.py</c> (<c>raise_site_findings</c>) reads the document and fails when the anchor does not
/// mention the name: no compiler test opens a Markdown file (kb/Work PB4453).
///
/// <para>⚠ <b>WHAT "RAISED" MEANS HERE, stated because the honest boundary matters.</b> A raise site is a string literal
/// that IS the name (or a quoted copy of it, in text an emitter writes) in <c>src/WiseOwl.Cobol.Runtime</c> or in the code
/// generator, outside the catalog and the gate tables; the EC-I-O family is raised through the status map
/// (<see cref="ExceptionCatalog.IoEcOfStatus"/> over every two-digit status) and the three names §2023:13.18.34.4 / §2023:14.9.51.4
/// name outright. That proves a raise site EXISTS, not that it is reachable — reachability is the conformance goldens'
/// job, and the cost of this rule is one literal per name.</para>
///
/// <para>⚠ <b>WHAT IT DOES NOT CATCH: A LOST ROW FOR A NAME ANOTHER SOURCE STILL DECLARES.</b> The registry is per NAME
/// (<see cref="EcBinder.AllDeclaredNames"/>), so it fails when a name loses its LAST declaration, never when one
/// statement kind loses its row. It was seen to fail by removing EC-OO-RESOURCE from the INVOKE row (the failure names
/// the name). Deleting the <c>BoundSetEntry</c> row's EC-PROGRAM-NOT-FOUND leaves it GREEN — <c>ProgramNames</c> (the
/// CALL and CANCEL rows), <c>ActivationSiteNames</c> and the address-operand row still declare the name — and that loss is
/// caught by the goldens that run the row: <c>conformance:cases/8.4.3.13-Program-address-identifier/pb1549_set_program_address_fatal_resume</c> (the ISO
/// ADDRESS OF PROGRAM sender) and <c>conformance:cases/_unassigned/pb3102_set_entry_not_found_declared</c> (the ENTRY senders), both
/// seen red with the row deleted (kb/Work PB3102, review finding R1-1).</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class EcRaiseSiteRegistryDriftTests
{
    /// <summary>The names no statement declares and no code raises, each with the CONFORMANCE.md row or section that says why,
    /// read from <c>tests/doc-facts/declined-raise-sites.json</c> (which holds the reasons). An anchor <c>DOC-A.1-n</c> is
    /// that Annex A.1 row; <c>§4</c> is "Documented non-support facilities" (the entry's <c>family</c> is the level-2 name
    /// the section may quote instead); <c>§3:D-x</c> is the §3 determination bullet that begins <c>- **D-x</c>. The
    /// document's side, that the anchor quotes each name, is <c>scripts/docchecks/facts.py</c> (kb/Work PB4453).</summary>
    private static readonly (string[] Names, string Anchor, string? Family)[] DeclinedRaiseSites =
    [
        .. DocFacts.Load("declined-raise-sites.json").GetProperty("sites").EnumerateArray().Select(e => (
            Names: e.GetProperty("names").EnumerateArray().Select(n => n.GetString()!).ToArray(),
            Anchor: e.GetProperty("anchor").GetString()!,
            Family: e.TryGetProperty("family", out var f) ? f.GetString() : null)),
    ];

    /// <summary>The names raised at the ACTIVATION BOUNDARY, in the ACTIVATING element, for a name the propagation machinery
    /// chooses at RUN time (§2023:14.9.18.4 GR1 b) 3a, §2023:14.6.13.1.5 EXIT/GOBACK item 4). The activating statement carries its own
    /// <see cref="EcCheckingProfile"/> (<c>EcBinder.StampActivators</c>), which answers for ANY catalogued name, so no
    /// statement declares these; the raise site is <c>ExceptionState.StageLastName</c> / <c>ConvertObjectPropagationToNamed</c>.
    /// The evidence that each is raised is the raise site itself (<c>Raised</c>), never the register: a test reads no
    /// <c>kb/</c> note (kb/Work PB2878: a register-only change runs no test leg). The notes that built the mechanisms are
    /// PB408 (EC-RAISING-NOT-SPECIFIED) and PB1121 (EC-OO-EXCEPTION).</summary>
    private static readonly string[] RaisedAtTheActivationBoundary = ["EC-RAISING-NOT-SPECIFIED", "EC-OO-EXCEPTION"];

    private static IEnumerable<string> Level3Names =>
        ExceptionCatalog.Level3Rows.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal);

    private static readonly HashSet<string> ComparisonMethods =
        new(["Same", "Equals", "StartsWith", "EndsWith", "Contains", "IndexOf"], StringComparer.Ordinal);

    /// <summary>Is this literal only COMPARED against (<c>CobolNames.Same(name, "EC-X")</c>, <c>name == "EC-X"</c>, a case
    /// label)? Excluding the name from a rule — <c>StageAutomaticPropagation</c> refuses EC-FLOW-GLOBAL-EXIT — is not raising it.</summary>
    private static bool IsComparisonOperand(SyntaxToken token)
    {
        for (var node = token.Parent; node is not null; node = node.Parent)
        {
            switch (node)
            {
                case ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax inv }:
                    string callee = inv.Expression is MemberAccessExpressionSyntax m ? m.Name.Identifier.Text
                        : inv.Expression is IdentifierNameSyntax id ? id.Identifier.Text : "";
                    return ComparisonMethods.Contains(callee);
                case BinaryExpressionSyntax b when b.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression:
                case ConstantPatternSyntax:
                case CaseSwitchLabelSyntax:
                    return true;
                case StatementSyntax or MemberDeclarationSyntax:
                    return false;
            }
        }
        return false;
    }

    /// <summary>The public and internal string constants of <see cref="ExceptionCatalog"/> by field name (<c>McsNoServer</c> to <c>"EC-MCS-NO-SERVER"</c>).</summary>
    private static readonly Lazy<Dictionary<string, string>> CatalogConstants = new(() =>
        typeof(ExceptionCatalog).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f is { IsLiteral: true } && f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!, StringComparer.Ordinal));

    /// <summary>The string literals of a source tree that ARE a name or quote one (<c>"EC-X"</c> inside emitted text).</summary>
    private static HashSet<string> NamesQuotedBy(string root, Func<string, bool> skipFile, HashSet<string> universe)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (rel.StartsWith("obj/", StringComparison.Ordinal) || rel.StartsWith("bin/", StringComparison.Ordinal)
                || rel.StartsWith("Generated/", StringComparison.Ordinal) || skipFile(rel))
                continue;
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            foreach (var token in tree.GetRoot().DescendantTokens())
            {
                // A REFERENCE TO A CATALOG CONSTANT IS A RAISE SITE TOO (kb/Work PB4744): the message control protocol names the condition it ends a statement with
                // as ExceptionCatalog.McsNoServer and its kin, so the spelling lives once, in the catalog, and AsyncMessaging sets what the protocol names.
                if (token.Kind() == SyntaxKind.IdentifierToken && token.Parent is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "ExceptionCatalog" } } access
                    && access.Name.Identifier == token && CatalogConstants.Value.TryGetValue(token.Text, out string? constant)
                    && universe.Contains(constant) && !IsComparisonOperand(token))
                {
                    found.Add(constant);
                    continue;
                }
                if (token.Kind() is not (SyntaxKind.StringLiteralToken or SyntaxKind.InterpolatedStringTextToken
                    or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken))
                    continue;
                string text = token.ValueText;
                if (!text.Contains("EC-", StringComparison.Ordinal)) continue;
                if (IsComparisonOperand(token)) continue;
                if (universe.Contains(text)) { found.Add(text); continue; }
                foreach (string name in universe)
                    if (text.Contains('"' + name + '"', StringComparison.Ordinal))
                        found.Add(name);
            }
        }
        return found;
    }

    /// <summary>The EC-I-O names a connector can set to exist: §2023:9.1.13.1's status map over every two-digit status, and the
    /// three §2023:13.18.34.4 GR6 b) 2 / §2023:14.9.51.4 GR27 a) names a rule gives outright.</summary>
    private static IEnumerable<string> IoRaised()
    {
        for (int i = 0; i < 100; i++)
            if (ExceptionCatalog.IoEcOfStatus(i.ToString("00")) is { } n)
                yield return n;
        yield return ExceptionCatalog.IoLinage;
        yield return ExceptionCatalog.IoEop;
        yield return ExceptionCatalog.IoEopOverflow;
    }

    private static readonly Lazy<HashSet<string>> Raised = new(() =>
    {
        var universe = Level3Names.ToHashSet(StringComparer.Ordinal);
        var raised = NamesQuotedBy(TestRepo.Src("WiseOwl.Cobol.Runtime"), rel => rel == "Exceptions/ExceptionCatalog.cs", universe);
        // The code generator writes raise sites into the program (SEARCH, STRING, SET … TO ADDRESS OF …). The gate table
        // itself (EcEmitter.FatalAmbientGates) names every FLAGGED name and is a declaration, not a raise.
        raised.UnionWith(NamesQuotedBy(TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen"), rel => rel == "EcEmitter.cs", universe));
        raised.UnionWith(IoRaised().Where(ExceptionCatalog.IoMaskNames.Contains));
        return raised;
    });


    [Fact]
    public void EveryCataloguedLevel3Name_IsDeclaredAndRaised_OrDeclinedWithAReason()
    {
        var declared = EcBinder.AllDeclaredNames;
        var declined = DeclinedRaiseSites.SelectMany(d => d.Names.Select(n => (Name: n, d.Anchor))).ToDictionary(d => d.Name, d => d.Anchor);
        var atBoundary = RaisedAtTheActivationBoundary.ToHashSet(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (string name in Level3Names)
        {
            bool isDeclared = declared.Contains(name), isRaised = Raised.Value.Contains(name), isDeclined = declined.ContainsKey(name);
            if (atBoundary.Contains(name))
            {
                if (isDeclared) problems.Add($"{name}: on RaisedAtTheActivationBoundary but an EcBinder table also declares it — remove one");
                if (!isRaised) problems.Add($"{name}: on RaisedAtTheActivationBoundary but no raise site stages it");
            }
            else if (isDeclined)
            {
                if (isDeclared || isRaised)
                    problems.Add($"{name}: on DeclinedRaiseSites ({declined[name]}) but {(isDeclared ? "the binder declares it" : "")}"
                        + $"{(isDeclared && isRaised ? " and " : "")}{(isRaised ? "a raise site exists" : "")} — remove the stale entry");
            }
            else if (isRaised && !isDeclared)
                problems.Add($"{name}: RAISED BUT NEVER DECLARED — a raise site exists, but no EcBinder table (NodeKindRaises, IoRaise, "
                    + "ActivationSiteNames, OperandRaises, AmbientRaises) gives any statement this name, so `>>TURN "
                    + $"{name} CHECKING ON` wraps nothing and the raise is lost (the PB452 / PB1453 shape)");
            else if (isDeclared && !isRaised)
                problems.Add($"{name}: DECLARED BUT NEVER RAISED — an EcBinder table gives statements this name, but no raise site in "
                    + "the runtime or the code generator sets it (a dead guard)");
            else if (!isDeclared && !isRaised)
                problems.Add($"{name}: NEITHER DECLARED NOR RAISED — add the declaration and the raise site, or decline the name on "
                    + "DeclinedRaiseSites with the CONFORMANCE.md row or section that says why");
        }
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void EveryDeclinedEntry_NamesACataloguedName_AndItsAnchorQuotesIt()
    {
        // The anchor's quoting each name is the audit's (scripts/docchecks/facts.py, raise_site_findings): no compiler
        // test opens a Markdown file (kb/Work PB4453). What is the compiler's is checked here.
        var names = Level3Names.ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var (entryNames, anchor, family) in DeclinedRaiseSites)
        {
            if (anchor == "§4" && family is null) problems.Add("a §4 entry names no level-2 family the section may quote instead");
            foreach (string name in entryNames)
            {
                if (!names.Contains(name)) problems.Add($"{name}: not a catalogued level-3 name");
                if (!seen.Add(name)) problems.Add($"{name}: listed twice");
                if (anchor == "§4" && family != ExceptionCatalog.Level2OfName(name))
                    problems.Add($"{name}: its level-2 family is {ExceptionCatalog.Level2OfName(name)}, the entry says {family}");
            }
        }
        foreach (string name in RaisedAtTheActivationBoundary)
        {
            if (!names.Contains(name)) problems.Add($"{name}: not a catalogued level-3 name");
            // The activating statement's profile answers for the name: >>TURN EC-ALL CHECKING ON enables it.
            if (!EcCheckingProfile.FromEvents([(ExceptionCatalog.EcAll, true)]).Enabled(name))
                problems.Add($"{name}: an EcCheckingProfile does not enable it, so the activating statement could not raise it");
        }
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>The I-O family is declared per FILE a statement touches (<see cref="EcBinder.IoFilesOf"/>), and that list was
    /// the one hand-kept enumeration left in the binder (DELETE FILE was the verb once missing from it, kb/Work PB141). A bound
    /// statement carrying a FileModel is an I-O statement unless it is on this list, and each exemption says why.</summary>
    private static readonly Dictionary<string, string> NotAnIoConnector = new(StringComparer.Ordinal)
    {
        ["BoundSort"] = "File is the SD sort file, not a connector; the USING and GIVING files are (EcBinder.IoFilesOf reads them)",
        ["BoundMerge"] = "File is the SD merge file, not a connector; the USING and GIVING files are",
        ["BoundRelease"] = "File is the SD sort file RELEASE writes to; its conditions are the sort-merge names (EcBinder.NodeKindRaises)",
        ["BoundReturn"] = "File is the SD sort file RETURN reads from; its conditions are the sort-merge names (EcBinder.NodeKindRaises)",
    };

    [Fact]
    public void EveryBoundStatementCarryingAFile_IsAnIoStatementTheBinderDeclares()
    {
        var asm = typeof(BoundStatement).Assembly;
        var problems = new List<string>();
        int checkedTypes = 0;
        foreach (var t in asm.GetTypes().Where(t => t is { IsAbstract: false, IsClass: true } && typeof(BoundStatement).IsAssignableFrom(t)))
        {
            var fileProps = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(FileModel)).ToList();
            if (fileProps.Count == 0) continue;
            checkedTypes++;
            if (NotAnIoConnector.ContainsKey(t.Name)) continue;
            var stmt = (BoundStatement)RuntimeHelpers.GetUninitializedObject(t);
            var file = (FileModel)RuntimeHelpers.GetUninitializedObject(typeof(FileModel));
            foreach (var p in fileProps)
                t.GetField($"<{p.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(stmt, file);
            if (!EcBinder.IoFilesOf(stmt).Contains(file))
                problems.Add($"{t.Name} carries a FileModel ({string.Join(", ", fileProps.Select(p => p.Name))}) but EcBinder.IoFilesOf does not "
                    + "name it, so `>>TURN EC-I-O CHECKING ON` over it wraps nothing — add its arm, or exempt it in NotAnIoConnector with the reason");
        }
        Assert.True(checkedTypes >= 10, $"the scan read only {checkedTypes} file-carrying statement types — the reflection scan no longer sees them");
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
