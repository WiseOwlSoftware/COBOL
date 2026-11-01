// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ THE BACKEND CONTRACT (docs/rearchitecture/DESIGN-backend-abstraction.md §6; kb/Work PB4266): the bound tree a
/// second code-generation backend (direct CIL, §3.2) would consume carries no C#. The backend input is measured, not
/// listed (<see cref="BackendInput"/>: every type reachable by field from <c>BoundCompilation</c> inside the neutral
/// layers, so a new node is checked the day it is reached), and every member of it that carries or returns TEXT must be
/// classified in <see cref="BackendContract"/>: NEUTRAL (a COBOL word, an allocated CLR name, a literal value, a run-time datum,
/// diagnostic text or an identity key, §1.1) or a tolerated LEAK naming the §1.3 row and the kb/Work notes that remove
/// it. A member named <c>ClrName</c> or <c>*ClrName</c> is neutral by construction (§1.1, §2.4) and needs no row.
/// </summary>
/// <remarks>
/// <para>
/// The leak rows are a RATCHET: an unclassified member fails (a new text member on the backend input is a decision, made
/// in the change that adds it), a row whose member has left the backend input fails (delete it: the list only shrinks),
/// and a member whose name has the shape of C# text (<see cref="CSharpShapedName"/>, §6 assertion 2) or of a render
/// member (<see cref="RenderName"/>, §6 assertion 1) can never be classified neutral. Assertion 1 is
/// <see cref="NoTextReturningMember_OnTheBackendInput_IsUnclassified"/> (it subsumes the codegen-backend §6 R5 net
/// that held the <c>Place</c> hierarchy alone); assertion 2 is <see cref="NoTextField_OnTheBackendInput_IsUnclassified"/>.
/// </para>
/// <para>
/// The parse-context half is the layering rule's: <c>docs/rearchitecture/architecture-edges.json</c> may not ALLOW an
/// edge from the neutral layers to the parse tree or the binder, so its walk (ArchitectureEdgesDriftTests) pins every
/// such field at its tolerated row (BoundCompilation.Tree and BoundUnit.Ctx, kb/Work PB2398; the binder fields, PB2293's
/// waves). That walk sees product types only, so this fixture refuses the one parse-tree reference it cannot see, a
/// field of an ANTLR runtime type (<see cref="TheBackendInput_HoldsNoParseTree"/>). Assertion 3, two backends producing
/// byte-identical output, lands with the CIL seam proof (§6, §3.3); until then
/// <see cref="TheSeam_HasNoBackendTheContractDoesNotProve"/> fails the day a second backend appears without it.
/// </para>
/// </remarks>
public sealed class BackendContractDriftTests
{
    /// <summary>The directories the neutral layers' sources live in: every type of the backend input is declared in a
    /// file under one of them, which is where a failure points.</summary>
    private static readonly string[] NeutralSourceDirectories =
    [
        TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Model"),
        TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Bound"),
    ];

    /// <summary>The two layers that ARE the neutral model, as the edges file names them.</summary>
    private static readonly string[] NeutralLayers = ["compiler.model", "compiler.bound"];

    /// <summary>The layers the backend input must never reach except through a tolerated edges row.</summary>
    private static readonly string[] ForbiddenLayers = ["frontend.parse", "compiler.binding"];

    /// <summary>§6 assertion 1's render-member names.</summary>
    internal static readonly Regex RenderName = new("^(Read|Write|Render|Emit|AsCs|ToCsharp)", RegexOptions.NonBacktracking);

    /// <summary>§6 assertion 2's C#-spelling names (the L2/L3/L4/L7/L8 shapes): a <c>Cs</c> word (<c>CsName</c>,
    /// <c>CsField</c>, <c>CsMember</c>, a trailing <c>Cs</c>), an access <c>Path</c> or a storage <c>Field</c>.</summary>
    internal static readonly Regex CSharpShapedName = new("Cs([A-Z]|$)|Path|Field", RegexOptions.NonBacktracking);

    private static readonly Lazy<ArchitectureEdges> Edges =
        new(() => ArchitectureEdges.Load(ArchitectureEdgesDriftTests.EdgesFile));

    private static readonly Lazy<BackendInput> Measured = new(() =>
    {
        Assembly compiler = typeof(BoundCompilation).Assembly;
        return BackendInput.Measure(typeof(BoundCompilation),
            t => t.Assembly == compiler && NeutralLayers.Contains(Edges.Value.LayerOf(t.Namespace ?? "")),
            compiler.GetTypes());
    });

    [Fact]
    public void NoTextReturningMember_OnTheBackendInput_IsUnclassified()
    {
        var failures = Check(Measured.Value, BackendContract.Table).Where(f => f.IsMethod).Select(f => f.Text).ToList();
        Assert.True(failures.Count == 0,
            "DESIGN-backend-abstraction §6 assertion 1: a method that returns text on the backend input is a render member "
            + "unless the contract classifies it (" + failures.Count + "):\n  " + string.Join("\n  ", failures) + Advice);
    }

    [Fact]
    public void NoTextField_OnTheBackendInput_IsUnclassified()
    {
        var failures = Check(Measured.Value, BackendContract.Table).Where(f => !f.IsMethod).Select(f => f.Text).ToList();
        Assert.True(failures.Count == 0,
            "DESIGN-backend-abstraction §6 assertion 2: a field or property that carries text on the backend input carries "
            + "C# unless the contract classifies it (" + failures.Count + "):\n  " + string.Join("\n  ", failures) + Advice);
    }

    [Fact]
    public void TheContractTable_IsWellFormed()
    {
        var defects = BackendContract.Table.Defects();
        Assert.True(defects.Count == 0, "the backend contract table is malformed:\n  " + string.Join("\n  ", defects));
    }

    [Fact]
    public void TheBackendInput_HoldsNoParseTree()
    {
        Assembly antlr = typeof(Antlr4.Runtime.ParserRuleContext).Assembly;
        var parseTree = Measured.Value.Exits.Where(e => e.Target.Assembly == antlr)
            .Select(e => $"{e.Member} : {e.Target.FullName}").ToList();
        Assert.True(parseTree.Count == 0,
            "a backend-input field holds an ANTLR parse-tree type, which the edges walk cannot see (it reads product types "
            + "only): carry the fact the backend needs, never the tree (DESIGN-backend-abstraction §1.1, §1.3 L6):\n  "
            + string.Join("\n  ", parseTree));

        var allowed = NeutralLayers
            .SelectMany(layer => ForbiddenLayers.Where(target => Edges.Value.Allows(layer, target)).Select(target => $"{layer} -> {target}"))
            .ToList();
        Assert.True(allowed.Count == 0,
            "architecture-edges.json ALLOWS an edge from the neutral model to the parse tree or the binder, so the edges walk "
            + "no longer pins the backend input's leaks at their tolerated rows: " + string.Join(", ", allowed));
    }

    [Fact]
    public void TheSeam_HasNoBackendTheContractDoesNotProve()
    {
        var backends = typeof(ICodeGenBackend).Assembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(ICodeGenBackend).IsAssignableFrom(t))
            .Select(t => t.FullName!).Order(StringComparer.Ordinal).ToList();
        Assert.True(backends.SequenceEqual([typeof(RoslynBackend).FullName!]),
            "ICodeGenBackend's implementations are " + string.Join(", ", backends) + ": a second backend lands with "
            + "DESIGN-backend-abstraction §6 assertion 3 (a DISPLAY-only BoundCompilation producing byte-identical stdout "
            + "through both), which replaces this assertion");
    }

    [Fact]
    public void EveryBackendInputType_IsDeclaredUnderANeutralSourceDirectory()
    {
        var declared = NeutralSourceDirectories
            .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
            .SelectMany(f => TypeDeclaration.Matches(File.ReadAllText(f)).Select(m => m.Groups["name"].Value))
            .ToHashSet(StringComparer.Ordinal);
        var missing = Measured.Value.Types
            .Select(t => t.IsNested ? OutermostOf(t) : t)
            .Distinct()
            .Where(t => !declared.Contains(t.Name.Split('`')[0]))
            .Select(t => t.FullName!)
            .ToList();
        Assert.True(missing.Count == 0,
            "a backend-input type is placed in a neutral layer by its namespace but declared outside "
            + string.Join(" and ", NeutralSourceDirectories.Select(d => Path.GetRelativePath(TestRepo.Root, d)))
            + ": " + string.Join(", ", missing));
    }

    /// <summary>The walk's reach witness: the structural <c>Place</c> lvalue and its access path (DESIGN-codegen-backend
    /// §2.3), the codegen-backend §6 R5 net's whole population, are all on the measured backend input, so assertion 1
    /// holds them as that net did.</summary>
    [Fact]
    public void TheBackendInput_ReachesEveryPlaceAndAccessPathType()
    {
        var unreached = typeof(Place).Assembly.GetTypes()
            .Where(t => typeof(Place).IsAssignableFrom(t) || typeof(AccessSegment).IsAssignableFrom(t) || t == typeof(AccessPath))
            .Where(t => !Measured.Value.Types.Contains(t))
            .Select(t => t.FullName!)
            .ToList();
        Assert.True(unreached.Count == 0, "the backend-input walk misses Place-hierarchy types: " + string.Join(", ", unreached));
    }

    // ── The planted self-tests: each failure arm fires on a planted backend input, for the right reason. ──

    private sealed record PlantedRoot(PlantedNode Node, Antlr4.Runtime.ParserRuleContext? Tree);

    private abstract record PlantedNode;

    private sealed record PlantedLeaf(string CsField, string CobolName, string ClrName, IReadOnlyList<string> Lines) : PlantedNode
    {
        public string RenderAll() => string.Concat(Lines);
    }

    private sealed record PlantedOther(int Count) : PlantedNode;

    private static BackendInput Planted() =>
        BackendInput.Measure(typeof(PlantedRoot), t => t.DeclaringType == typeof(BackendContractDriftTests) && t.Name.StartsWith("Planted"),
            typeof(BackendContractDriftTests).GetNestedTypes(BindingFlags.NonPublic));

    private static string KeyOf(string member) => BackendInput.KeyOf(typeof(PlantedLeaf), member);

    [Fact]
    public void Planted_TheWalkReachesSubtypesAndRecordsTheParseTreeExit()
    {
        BackendInput input = Planted();
        Assert.Contains(typeof(PlantedLeaf), input.Types);
        Assert.Contains(typeof(PlantedOther), input.Types);
        Assert.Equal(
            [KeyOf("ClrName"), KeyOf("CobolName"), KeyOf("CsField"), KeyOf("Lines"), KeyOf("RenderAll()")],
            input.StringMembers.Where(m => m.Declaring == typeof(PlantedLeaf)).Select(m => m.Key).ToList());
        Assert.Contains(input.Exits, e => e.Target == typeof(Antlr4.Runtime.ParserRuleContext));
    }

    [Fact]
    public void Planted_AnUnclassifiedMember_FailsAndAClrNameDoesNot()
    {
        var failures = Check(Planted(), new ContractTable([new(NeutralKind.CobolWord, [KeyOf("CobolName")])], []));
        Assert.Equal(
            [$"{KeyOf("CsField")}: unclassified", $"{KeyOf("Lines")}: unclassified", $"{KeyOf("RenderAll()")}: unclassified"],
            failures.Select(f => f.Text[..(f.Text.IndexOf(':') + ": unclassified".Length)]).ToList());
        Assert.Equal([false, false, true], failures.Select(f => f.IsMethod).ToList());
    }

    [Fact]
    public void Planted_AToleratedLeakPasses_AndAStaleRowFails()
    {
        var table = new ContractTable(
            [new(NeutralKind.CobolWord, [KeyOf("CobolName")]), new(NeutralKind.LiteralValue, [KeyOf("Lines")])],
            [new("L7", ["PB0"], [KeyOf("CsField"), KeyOf("RenderAll()"), KeyOf("Gone")])]);
        var failures = Check(Planted(), table);
        Assert.Equal([$"{KeyOf("Gone")}: a row names a member the backend input no longer has: delete the row"],
            failures.Select(f => f.Text).ToList());
    }

    [Fact]
    public void Planted_ACSharpShapedOrRenderName_CannotBeNeutral()
    {
        var table = new ContractTable(
            [new(NeutralKind.CobolWord, [KeyOf("CobolName"), KeyOf("CsField"), KeyOf("RenderAll()"), KeyOf("ClrName")])],
            [new("L99", [], [KeyOf("Lines")])]);
        var defects = table.Defects();
        Assert.Equal(4, defects.Count);
        Assert.Contains(defects, d => d.StartsWith(KeyOf("CsField"), StringComparison.Ordinal) && d.Contains("C#-shaped"));
        Assert.Contains(defects, d => d.StartsWith(KeyOf("RenderAll()"), StringComparison.Ordinal) && d.Contains("render member"));
        Assert.Contains(defects, d => d.StartsWith(KeyOf("ClrName"), StringComparison.Ordinal) && d.Contains("neutral by construction"));
        Assert.Contains(defects, d => d.Contains("L99"));
    }

    private const string Advice =
        "\nClassify the member in BackendContract.Table: neutral (with its kind) when it is a COBOL word, "
        + "a CLR name, a literal value, a run-time datum, diagnostic text or an identity key; otherwise carry the fact as "
        + "structure and let each backend spell it (DESIGN-backend-abstraction §1.2). A leak row is tolerated only with its "
        + "§1.3 row and its removers, and a row whose member is gone is deleted.";

    /// <summary>A contract failure, and whether it is §6 assertion 1's (a method) or assertion 2's.</summary>
    internal readonly record struct Failure(string Text, bool IsMethod);

    /// <summary>Check a measured backend input against a contract table.</summary>
    internal static List<Failure> Check(BackendInput input, ContractTable table)
    {
        var failures = new List<Failure>();
        var measured = input.StringMembers.Select(m => m.Key).ToHashSet(StringComparer.Ordinal);
        foreach (BackendMember member in input.StringMembers)
        {
            if (!member.IsMethod && member.Name.EndsWith("ClrName", StringComparison.Ordinal))
            {
                continue;
            }

            if (!table.Classifies(member.Key))
            {
                failures.Add(new($"{member.Key}: unclassified ({member.Declaring.FullName})", member.IsMethod));
            }
        }

        foreach (string key in table.Keys.Where(k => !measured.Contains(k)))
        {
            failures.Add(new($"{key}: a row names a member the backend input no longer has: delete the row", key.EndsWith("()", StringComparison.Ordinal)));
        }

        return failures;
    }

    private static Type OutermostOf(Type type)
    {
        while (type.DeclaringType is { } outer)
        {
            type = outer;
        }

        return type;
    }

    /// <summary>A C# type declaration's name (a <c>record struct</c> names its type after <c>struct</c>).</summary>
    private static readonly Regex TypeDeclaration = new(
        "\\b(class|record|struct|interface|enum)\\s+(struct\\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.NonBacktracking);
}

/// <summary>What a neutral text member is (DESIGN-backend-abstraction §1.1).</summary>
internal enum NeutralKind
{
    /// <summary>A COBOL word or its qualifiers as written: a user-defined word, a keyword, an implementor-name.</summary>
    CobolWord,
    /// <summary>A CLR name (§2.4) held under a name other than <c>ClrName</c>: allocated, synthesized (<c>_T_n</c>,
    /// <c>_P_n</c>) or the allocator's own state and sanitizer.</summary>
    ClrName,
    /// <summary>A decoded literal value or a literal's source text, which both backends scale or encode alike.</summary>
    LiteralValue,
    /// <summary>A COBOL run-time datum (§1.1: <c>EcStatementInfo.Location</c>): an exception name, a location, a
    /// locale tag, an external file name, a run-unit registry key.</summary>
    RuntimeDatum,
    /// <summary>Compile-time or run-time message text in English, never code.</summary>
    DiagnosticText,
    /// <summary>A key the model compares for identity (a same-type or signature key), never emitted as code.</summary>
    IdentityKey,
}

/// <summary>A group of neutral members of one kind.</summary>
internal sealed record NeutralRow(NeutralKind Kind, IReadOnlyList<string> Members);

/// <summary>A tolerated leak: the DESIGN-backend-abstraction §1.3 row that removes it, the kb/Work notes that do (empty
/// when the row has none yet), and its members.</summary>
internal sealed record LeakRow(string Row, IReadOnlyList<string> RemovedBy, IReadOnlyList<string> Members);

/// <summary>The contract's classification of every text member of the backend input.</summary>
internal sealed record ContractTable(IReadOnlyList<NeutralRow> Neutral, IReadOnlyList<LeakRow> Leaks)
{
    /// <summary>The §1.3 rows a leak may name.</summary>
    private static readonly string[] DesignRows = ["L1", "L2", "L3", "L4", "L5", "L6", "L7", "L8", "L9"];

    private static readonly Regex NoteId = new("^PB[0-9]+$", RegexOptions.NonBacktracking);

    public IEnumerable<string> Keys => Neutral.SelectMany(r => r.Members).Concat(Leaks.SelectMany(r => r.Members));

    public bool Classifies(string key) => Keys.Contains(key, StringComparer.Ordinal);

    /// <summary>The table's own defects: a member listed twice, a C#-shaped or render-member name classified neutral,
    /// a <c>*ClrName</c> field listed at all, a leak naming no §1.3 row or a malformed note id.</summary>
    public IReadOnlyList<string> Defects()
    {
        var defects = new List<string>();
        defects.AddRange(Keys.GroupBy(k => k, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => $"{g.Key}: listed {g.Count()} times"));
        foreach (string key in Neutral.SelectMany(r => r.Members))
        {
            bool method = key.EndsWith("()", StringComparison.Ordinal);
            string name = key[(key.LastIndexOf('.') + 1)..].TrimEnd('(', ')');
            if (BackendContractDriftTests.CSharpShapedName.IsMatch(name))
            {
                defects.Add($"{key}: a C#-shaped name (§6 assertion 2) cannot be neutral");
            }
            else if (method && BackendContractDriftTests.RenderName.IsMatch(name))
            {
                defects.Add($"{key}: a render member's name (§6 assertion 1) cannot be neutral");
            }
            else if (!method && name.EndsWith("ClrName", StringComparison.Ordinal))
            {
                defects.Add($"{key}: a ClrName field is neutral by construction (§1.1, §2.4) and takes no row");
            }
        }

        foreach (LeakRow leak in Leaks)
        {
            if (!DesignRows.Contains(leak.Row))
            {
                defects.Add($"leak row {leak.Row} ({string.Join(", ", leak.Members)}): not a DESIGN-backend-abstraction §1.3 row");
            }

            defects.AddRange(leak.RemovedBy.Where(n => !NoteId.IsMatch(n)).Select(n => $"leak row {leak.Row}: '{n}' is not a kb/Work note id"));
        }

        return defects;
    }
}
