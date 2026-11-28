// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Procedure;
using WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE REPORT DESCRIPTION'S OPERANDS ARE WALKED BY ONE CYCLE-SAFE WALK, AND A MODEL PROPERTY IT DOES NOT READ FAILS HERE
/// (kb/Work PB6349). A GENERATE or TERMINATE evaluates the SOURCE, SUM, PRESENT WHEN and VARYING operands of its report
/// (§2023:14.9.16.4 GR1/GR4/GR5, §2023:14.9.46.4 GR3, §2023:13.18.53.4 GR2, §2023:13.18.54.4 GR3, §2023:13.18.41.4 GR2), and §2023:15.3 AR14 attaches
/// EC-ARGUMENT-FUNCTION to the function reference wherever it is written. The operands live in <see cref="ReportModel"/>, a
/// mutable graph that cycles back to itself and that the statement holds by reference, so the generated statement walk
/// (<see cref="BoundValueWalkDriftTests"/>) does not enter it; <see cref="ReportValueWalk"/> does, and these tests keep it whole:
/// <list type="bullet">
/// <item><see cref="EveryPropertyOfTheReportDescriptionIsHeldByTheWalk"/> — every property of every type of the description
/// that holds a value node, a nested statement list or another description type is read by <c>Holds</c>, except the references
/// named in <see cref="NamedReferences"/>, each of which names a part of the description instead of holding one.</item>
/// <item><see cref="EveryStatementOrValueNodeHoldingTheDescriptionHasADecision"/> — every bound node that holds a description
/// type is decided: walked, or evaluates nothing, so a new statement holding a report cannot be skipped by accident.</item>
/// <item><see cref="AFunctionReferenceInAnyReportOperandDeclaresEcArgumentFunction"/> — the end-to-end rule, one position per row.</item>
/// </list>
/// Each test POPULATES the position with a sentinel and asserts the walk yields that very object (a null-probe cannot tell an
/// unread position from an empty one; <see cref="BoundValueWalkDriftTests"/> records why).
/// </summary>
public sealed class ReportValueWalkDriftTests
{
    private static readonly string ModelNamespace = typeof(ReportModel).Namespace!;

    private static readonly Assembly Compiler = typeof(ReportModel).Assembly;

    /// <summary>⛔ THE REFERENCES THE WALK DOES NOT FOLLOW, each with why. The walk follows containment (a report holds its groups,
    /// a group its lines, a line its fields, an occurrence its source). These properties name another part of the description, or
    /// another report's, so following them would re-enter a parent (the cycle) or read another report's operands as this one's.
    /// The list is closed: a name here that no longer is a property fails <see cref="NamedReferencesAreProperties"/>.</summary>
    private static readonly Dictionary<string, string> NamedReferences = new()
    {
        ["ReportEntryFamily.Report"] = "the report that holds the family: a back reference up the containment",
        ["ReportEntryFamily.Group"] = "the report group the entry is written in: held by the report's Groups",
        ["ReportItemOccurrence.Family"] = "the family that holds the occurrence: a back reference up the containment",
        ["ReportSumModel.Family"] = "the SUM family the counter belongs to: held by the report's SumFamilies",
        ["ReportSumModel.PrintedIn"] = "the report group the counter is printed in: held by the report's Groups",
        ["ReportSumTerm.Upon"] = "the detail groups the term triggers on (ReportDetailRef): a reference to a group of ANOTHER report (§2023:13.18.54.3 SR7), whose operands are that report's own",
    };

    /// <summary>What a bound node that holds a description type does with it, decided once. <c>true</c>: the report's operands
    /// are evaluated when the node runs, so <see cref="ReportValueWalk.EvaluatedBy"/> yields them; <c>false</c>: they are not.</summary>
    private static readonly Dictionary<string, (bool Walked, string Why)> Decisions = new()
    {
        ["BoundGenerate.Report"] = (true, "GENERATE presents the report's groups and evaluates their operands (§2023:14.9.16.4 GR1, GR4, GR5)"),
        ["BoundGenerate.Detail"] = (false, "a group of Report, walked through it"),
        ["BoundGenerate.SummaryDetail"] = (false, "a group of Report, walked through it"),
        ["BoundTerminate.Reports"] = (true, "TERMINATE prints the control footings and the report footing, evaluating their operands (§2023:14.9.46.4 GR3b, GR3c"),
        ["BoundInitiate.Reports"] = (false, "INITIATE zeroes the sum counters and sets LINE-COUNTER and PAGE-COUNTER; it evaluates no operand (§2023:14.9.21.4 GR1)"),
        ["BoundSuppress.Report"] = (false, "SUPPRESS PRINTING inhibits one group's printing; it evaluates no operand (§2023:14.9.45)"),
        ["BoundSuppress.Group"] = (false, "SUPPRESS PRINTING inhibits one group's printing; it evaluates no operand (§2023:14.9.45)"),
        ["BoundReportCounterRef.Report"] = (false, "reading PAGE-COUNTER or LINE-COUNTER reads the engine's counter, not the description's operands"),
    };

    /// <summary>The types of the report description: <see cref="ReportModel"/> and every class of its namespace reachable from it
    /// through a property, an abstract type standing for its concrete types. The nodes of the bound tree are another namespace.</summary>
    internal static readonly IReadOnlySet<Type> ReportModelTypes = Closure();

    private static HashSet<Type> Closure()
    {
        var all = new HashSet<Type> { typeof(ReportModel) };
        var pending = new Stack<Type>([typeof(ReportModel)]);
        while (pending.Count > 0)
            foreach (var p in Properties(pending.Pop()))
                foreach (var leaf in Leaves(p.PropertyType))
                    foreach (var t in ConcreteAndAbstract(leaf))
                        if (all.Add(t)) pending.Push(t);
        return all;
    }

    private static IEnumerable<Type> ConcreteAndAbstract(Type t)
    {
        if (!t.IsClass || t.Namespace != ModelNamespace) yield break;
        yield return t;
        if (t.IsAbstract)
            foreach (var d in Compiler.GetTypes().Where(c => !c.IsAbstract && c.IsSubclassOf(t))) yield return d;
    }

    /// <summary>The types a property type is a collection or option of: <c>List&lt;X&gt;</c>, <c>IReadOnlyList&lt;X&gt;</c>, <c>X?</c>.</summary>
    private static IEnumerable<Type> Leaves(Type t)
    {
        if (t.IsArray) return Leaves(t.GetElementType()!);
        if (Nullable.GetUnderlyingType(t) is { } u) return Leaves(u);
        return t.IsGenericType ? t.GetGenericArguments().SelectMany(Leaves) : [t];
    }

    private static IEnumerable<PropertyInfo> Properties(Type t) =>
        t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetIndexParameters().Length == 0 && !NamedReferences.ContainsKey($"{p.DeclaringType!.Name}.{p.Name}"));

    private static bool IsStatementLeaf(Type t) => typeof(BoundStatement).IsAssignableFrom(t);

    /// <summary>The description types that hold something the walk must reach: a value node, a step list, or such a type.</summary>
    private static readonly IReadOnlySet<Type> Carriers = CarriersOf();

    private static HashSet<Type> CarriersOf()
    {
        var carriers = new HashSet<Type>();
        for (bool grew = true; grew;)
        {
            grew = false;
            foreach (var t in ReportModelTypes)
                if (!carriers.Contains(t) && Properties(t).Any(p => Leaves(p.PropertyType).Any(l =>
                        BoundValueWalkDriftTests.IsValueNode(l) || BoundValueWalkDriftTests.IsStatement(l) || carriers.Contains(l))))
                    grew |= carriers.Add(t);
        }
        return carriers;
    }

    [Fact]
    public void NamedReferencesAreProperties()
    {
        foreach (var name in NamedReferences.Keys)
        {
            var (type, property) = (name[..name.IndexOf('.')], name[(name.IndexOf('.') + 1)..]);
            Assert.True(Compiler.GetTypes().Where(t => t.Namespace == ModelNamespace).Any(t => t.Name == type
                    && t.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) is not null),
                $"{name} is no longer a property of the report description: remove it from NamedReferences");
        }
    }

    /// <summary>The backing a test sets: the auto-property's field, else the private field a computed property falls back to
    /// (<c>PresentWhen</c> of an occurrence is its counter's, else <c>_presentWhen</c>).</summary>
    private static FieldInfo? BackingOf(PropertyInfo p)
    {
        var declaring = p.DeclaringType!;
        return BoundValueWalkDriftTests.Backing(p)
            ?? declaring.GetField("_" + char.ToLowerInvariant(p.Name[0]) + p.Name[1..], BindingFlags.Instance | BindingFlags.NonPublic);
    }

    /// <summary>Hold <paramref name="element"/> in a value of <paramref name="propertyType"/>: the element itself, or a list of one.</summary>
    private static object Shape(Type propertyType, Type elementType, object element)
    {
        if (Leaves(propertyType).Count() == 1 && !propertyType.IsGenericType && !propertyType.IsArray) return element;
        var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        list.Add(element);
        return list;
    }

    private static Type Concrete(Type t) =>
        t.IsAbstract ? Compiler.GetTypes().First(c => !c.IsAbstract && c.IsSubclassOf(t)) : t;

    [Fact]
    public void EveryPropertyOfTheReportDescriptionIsHeldByTheWalk()
    {
        var failures = new List<string>();
        foreach (var type in ReportModelTypes.Where(t => !t.IsAbstract && Carriers.Contains(t)).OrderBy(t => t.Name, StringComparer.Ordinal))
            foreach (var p in Properties(type))
            {
                var held = Leaves(p.PropertyType).FirstOrDefault(l =>
                    BoundValueWalkDriftTests.IsValueNode(l) || BoundValueWalkDriftTests.IsStatement(l) || Carriers.Contains(l));
                if (held is null) continue;
                var label = $"{type.Name}.{p.Name} ({p.PropertyType.Name})";
                if (BackingOf(p) is not { } field) { failures.Add($"{label}: no backing field the test can populate"); continue; }
                if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() is var d
                    && d != typeof(List<>) && d != typeof(IReadOnlyList<>) && d != typeof(Nullable<>))
                { failures.Add($"{label}: a collection shape the test does not build"); continue; }

                object element, expected;
                if (BoundValueWalkDriftTests.IsValueNode(held))
                    element = expected = BoundValueWalkDriftTests.Uninit(
                        held.IsAbstract ? BoundValueWalkDriftTests.LeavesOf(held).First() : held);
                else if (BoundValueWalkDriftTests.IsStatement(held))
                {
                    expected = BoundValueWalkDriftTests.Uninit(typeof(BoundRelational));
                    element = BoundValueWalkDriftTests.IfOf((BoundCondition)expected);
                }
                else element = expected = BoundValueWalkDriftTests.Uninit(Concrete(held));

                object node = BoundValueWalkDriftTests.Uninit(type);
                field.SetValue(node, Shape(p.PropertyType, held, element));
                if (!ReportValueWalk.Holds(node).Any(h => ReferenceEquals(h, expected)))
                    failures.Add($"{label}: {(BoundValueWalkDriftTests.IsValueNode(held) ? "a value node" : "a part of the description")} it holds is not read by ReportValueWalk.Holds");
            }
        Assert.True(failures.Count == 0,
            "ReportValueWalk skips a property of the report description (read it in Holds, or name it in NamedReferences with the reason):\n"
            + string.Join("\n", failures));
    }

    [Fact]
    public void EveryStatementOrValueNodeHoldingTheDescriptionHasADecision()
    {
        var failures = new List<string>();
        var holders = BoundValueWalkDriftTests.LeavesOf(typeof(BoundStatement))
            .Concat(BoundValueWalkDriftTests.ValueRoots.SelectMany(BoundValueWalkDriftTests.LeavesOf));
        var seen = new HashSet<string>();
        foreach (var leaf in holders)
            foreach (var p in leaf.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Length == 0))
            {
                var model = Leaves(p.PropertyType).FirstOrDefault(l => ReportModelTypes.Contains(l) && Carriers.Contains(l));
                if (model is null) continue;
                string key = $"{leaf.Name}.{p.Name}";
                seen.Add(key);
                if (!Decisions.TryGetValue(key, out var decision))
                {
                    failures.Add($"{key}: holds a report description type and no decision says whether the report's operands are evaluated when it runs");
                    continue;
                }
                var report = (ReportModel)BoundValueWalkDriftTests.Uninit(typeof(ReportModel));
                var sentinel = (BoundExpr)BoundValueWalkDriftTests.Uninit(typeof(BoundIntrinsicCall));
                var holding = Holding(sentinel);
                object node = BoundValueWalkDriftTests.Uninit(leaf);
                if (BackingOf(p) is not { } field) { failures.Add($"{key}: no backing field"); continue; }
                bool isReport = model == typeof(ReportModel);
                field.SetValue(node, Shape(p.PropertyType, model, isReport ? holding : Uninit2(model)));
                if (leaf == typeof(BoundGenerate) && p.Name != "Report")
                    BoundValueWalkDriftTests.SetBacking(node, "Report", report);
                bool yielded = node is BoundStatement s && ReportValueWalk.EvaluatedBy(s).Any(n => ReferenceEquals(n, sentinel));
                if (decision.Walked != yielded)
                    failures.Add($"{key}: decided {(decision.Walked ? "walked" : "not walked")} ({decision.Why}) but EvaluatedBy {(yielded ? "yields" : "does not yield")} the report's operand");
            }
        foreach (var stale in Decisions.Keys.Except(seen))
            failures.Add($"{stale}: decided but no bound node has that property: remove it");
        Assert.True(failures.Count == 0, "the bound nodes that hold a report description are not decided:\n" + string.Join("\n", failures));

        static object Uninit2(Type t) => BoundValueWalkDriftTests.Uninit(Concrete(t));
    }

    /// <summary>A report whose one SUM term of another report (<see cref="ReportModel.UponTriggeredTerms"/>) holds <paramref name="operand"/>.</summary>
    private static ReportModel Holding(BoundExpr operand)
    {
        var addend = Make<ReportSumAddend>(("Value", operand));
        var term = Make<ReportSumTerm>(("Addends", new List<ReportSumAddend> { addend }));
        return Make<ReportModel>(("UponTriggeredTerms", new List<ReportSumTerm> { term }));
    }

    private static T Make<T>(params (string Property, object? Value)[] properties) where T : class
    {
        var node = (T)BoundValueWalkDriftTests.Uninit(typeof(T));
        foreach (var (property, value) in properties) BoundValueWalkDriftTests.SetBacking(node, property, value);
        return node;
    }

    private static BoundExpr Function() => (BoundExpr)BoundValueWalkDriftTests.Uninit(typeof(BoundIntrinsicCall));

    private static BoundCondition Relation() =>
        Make<BoundRelational>(("Left", BoundValueWalkDriftTests.FunctionOperand()));

    private static ReportModel ReportOf(ReportGroupModel group) => Make<ReportModel>(("Groups", new List<ReportGroupModel> { group }));

    private static ReportModel ReportOfLine(ReportLineModel line) =>
        ReportOf(Make<ReportGroupModel>(("Lines", new List<ReportLineModel> { line })));

    private static ReportModel ReportOfField(ReportFieldModel field) =>
        ReportOfLine(Make<ReportLineModel>(("Fields", new List<ReportFieldModel> { field })));

    private static ReportFieldModel FieldOf(ReportFieldSource source) =>
        Make<ReportFieldModel>(("Sources", new List<ReportFieldSource> { source }));

    private static ReportModel ReportOfSum(ReportSumModel sum) => Make<ReportModel>(("Sums", new List<ReportSumModel> { sum }));

    /// <summary>§2023:15.3 AR14, one report operand position per row, each reached from the statement that evaluates it: a
    /// function reference in a PRESENT WHEN, SOURCE, SUM, VARYING or CODE operand, or in the steps that compute one, declares
    /// EC-ARGUMENT-FUNCTION on its GENERATE or TERMINATE. Each row was silent before kb/Work PB6349.</summary>
    public static TheoryData<string, Func<BoundStatement>> Positions => new()
    {
        { "GENERATE: line PRESENT WHEN", () => new BoundGenerate(ReportOfLine(Make<ReportLineModel>(("PresentWhen", new List<BoundCondition> { Relation() }))), null, null) },
        { "GENERATE: field PRESENT WHEN", () => new BoundGenerate(ReportOfField(Make<ReportFieldModel>(("PresentWhen", new List<BoundCondition> { Relation() }))), null, null) },
        { "GENERATE: SOURCE identifier", () => new BoundGenerate(ReportOfField(FieldOf(Make<FieldReferenceSource>(("Value", BoundValueWalkDriftTests.FunctionOperand())))), null, null) },
        { "GENERATE: SOURCE expression", () => new BoundGenerate(ReportOfField(FieldOf(Make<FieldComputeSource>(("Value", Function())))), null, null) },
        { "GENERATE: SOURCE operand-evaluation steps", () => new BoundGenerate(ReportOfField(FieldOf(Make<FieldReferenceSource>(("Steps", new List<BoundStatement> { BoundValueWalkDriftTests.IfOf(Relation()) })))), null, null) },
        { "GENERATE: SUM addend", () => new BoundGenerate(ReportOfSum(Make<ReportSumModel>(("Terms", new List<ReportSumTerm> { Make<ReportSumTerm>(("Addends", new List<ReportSumAddend> { Make<ReportSumAddend>(("Value", Function())) })) }))), null, null) },
        { "GENERATE: SUM PRESENT WHEN", () => new BoundGenerate(ReportOfSum(Make<ReportSumModel>(("PresentWhen", new List<BoundCondition> { Relation() }))), null, null) },
        { "GENERATE: VARYING FROM", () => new BoundGenerate(ReportOfField(Make<ReportFieldModel>(("Varyings", new List<ReportVaryingUse> { Make<ReportVaryingUse>(("Counter", Make<ReportVaryingModel>(("From", Function())))) }))), null, null) },
        { "GENERATE: SOURCE of an item occurrence", () => new BoundGenerate(Make<ReportModel>(("ItemOccurrences", new List<ReportItemOccurrence> { Make<ReportItemOccurrence>(("Value", Function())) })), null, null) },
        { "GENERATE: CODE operand-evaluation steps", () => new BoundGenerate(Make<ReportModel>(("Code", Make<ReportCodeModel>(("Steps", new List<BoundStatement> { BoundValueWalkDriftTests.IfOf(Relation()) })))), null, null) },
        { "GENERATE: SUM of another report UPON this report's detail", () => new BoundGenerate(Holding(Function()), null, null) },
        { "TERMINATE: control footing SOURCE", () => new BoundTerminate([ReportOfField(FieldOf(Make<FieldComputeSource>(("Value", Function()))))]) },
    };

    [Theory]
    [MemberData(nameof(Positions))]
    public void AFunctionReferenceInAnyReportOperandDeclaresEcArgumentFunction(string position, Func<BoundStatement> build)
    {
        var names = EcBinder.OperandRaisableNames(build()).ToList();
        Assert.True(names.Contains("EC-ARGUMENT-FUNCTION"), $"{position}: the statement declares {string.Join(",", names)} but not EC-ARGUMENT-FUNCTION");
    }

    /// <summary>The walk enters each node once: a node reached by two paths is read once, and a node that holds itself (a VARYING
    /// counter's <see cref="ReportVaryingModel.Group"/> names the counter) ends the walk.</summary>
    [Fact]
    public void ANodeReachedTwiceOrHeldByItselfIsReadOnce()
    {
        var sentinel = Function();
        var occurrence = Make<ReportItemOccurrence>(("Value", sentinel));
        var family = (ReportSourceFamily)BoundValueWalkDriftTests.Uninit(typeof(ReportSourceFamily));
        BoundValueWalkDriftTests.SetBacking(family, "Occurrences", new List<ReportItemOccurrence> { occurrence });
        var counter = Make<ReportVaryingModel>(("From", sentinel));
        BoundValueWalkDriftTests.SetBacking(counter, "Group", new List<ReportVaryingModel> { counter });
        var report = Make<ReportModel>(("ItemOccurrences", new List<ReportItemOccurrence> { occurrence, occurrence }),
            ("EntryFamilies", new List<ReportEntryFamily> { family, family }),
            ("Groups", new List<ReportGroupModel>()),
            ("Sums", new List<ReportSumModel>()),
            ("SumFamilies", new List<ReportSumFamily>()),
            ("UponTriggeredTerms", new List<ReportSumTerm>()));
        Assert.Single(ReportValueWalk.ValueNodes(report), n => ReferenceEquals(n, sentinel));
        Assert.Single(ReportValueWalk.ValueNodes(counter));
    }
}
