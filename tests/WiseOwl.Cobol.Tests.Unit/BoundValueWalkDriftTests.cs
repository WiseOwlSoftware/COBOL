// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Runtime.CompilerServices;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Procedure;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY OPERAND POSITION OF THE BOUND TREE IS WALKED BY ONE GENERATED WALK, AND A NEW SHAPE THE GENERATOR CANNOT READ
/// FAILS HERE (kb/Work PB6223, PB6328). ISO §2023:15.3 AR14 attaches EC-ARGUMENT-FUNCTION to the function REFERENCE, so
/// whether a statement's guard is emitted must not depend on WHERE in the tree the reference is held. Two hand-written or
/// partial walks said otherwise: <c>OwnValueParts</c> did not enter a value tuple (a function-identifier CANCEL target,
/// <c>BoundCancel.Targets</c>), and <c>EcBinder.CondHasIntrinsic</c> was a switch with a `_ => false` default that missed a
/// class condition, a range membership and a user-function window. <see cref="BoundValueWalk"/> over the generated
/// <c>BoundStatementTree.ValueChildren</c>, and <c>OwnValueParts</c>, now read every property of every node.
///
/// <para><b>How these tests are sound.</b> A probe that leaves every property null cannot tell an unread position from a
/// read-but-empty one (the generated <c>V</c>/<c>Vz</c> helpers are null-safe; <c>EcArgumentFunctionGateDriftTests</c> records
/// two unsound attempts). Each test here POPULATES the position under test with a sentinel node and asserts the walk
/// yields that very object. <see cref="EveryValueHoldingPropertyIsReadByTheGeneratedWalks"/> does it for every property of
/// every leaf whose type reaches a value node, so a new leaf, a new operand slot or a new container shape the generator
/// does not read (an array, a <c>List</c>, a dictionary, a tuple nested in a record) fails by name.</para>
/// </summary>
public sealed class BoundValueWalkDriftTests
{
    private static readonly Assembly Bound = typeof(BoundStatement).Assembly;

    private static readonly Type[] ValueRoots = [typeof(BoundExpr), typeof(BoundCondition), typeof(BoundOperand), typeof(BoundBoolExpr)];

    private const int MaxDepth = 3;

    private static bool IsValueNode(Type t) => ValueRoots.Any(r => r.IsAssignableFrom(t));

    private static bool IsStatement(Type t) => typeof(BoundStatement).IsAssignableFrom(t);

    private static IEnumerable<Type> LeavesOf(Type root) =>
        Bound.GetTypes().Where(t => !t.IsAbstract && t != root && root.IsAssignableFrom(t)).OrderBy(t => t.Name, StringComparer.Ordinal);

    private static IEnumerable<PropertyInfo> Properties(Type t) =>
        t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Length == 0);

    private static FieldInfo? Backing(PropertyInfo p) =>
        p.DeclaringType!.GetField($"<{p.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static object Uninit(Type t) => RuntimeHelpers.GetUninitializedObject(t);

    private static bool IsValueTuple(Type t) =>
        t.IsGenericType && t.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal);

    /// <summary>⛔ THE ONE NAMED EXCEPTION: the report description models. A <c>ReportModel</c> / <c>ReportGroupModel</c> holds the
    /// report's PRESENT WHEN, SOURCE, SUM and VALUE operands in mutable <c>List&lt;T&gt;</c> properties and in a graph that
    /// cycles back to the nodes that name it (<c>BoundReportCounterRef.Report</c>), so the statement walk holds the model by
    /// reference and does not enter it: a function reference in a report group's SOURCE is not seen by GENERATE's guard.
    /// That is a different mechanism from the operand positions this file pins (the model is not part of the statement's value
    /// tree, and entering it needs a cycle-safe walk over the model), reported for its own fix. The list is closed: a NEW
    /// model type that holds a value node fails the test instead of being skipped.</summary>
    private static readonly HashSet<string> ModelsHeldByReference = ["ReportModel", "ReportGroupModel"];

    /// <summary>Does <paramref name="t"/> mention a value node through a shape the generator is meant to read: the node
    /// itself, a collection of one, a tuple holding one, or a record of the compiler assembly holding one?</summary>
    private static bool Mentions(Type t, int depth, HashSet<Type> seen)
    {
        if (depth > MaxDepth + 1 || !seen.Add(t)) return false;
        if (IsValueNode(t)) return true;
        if (t.IsArray) return Mentions(t.GetElementType()!, depth + 1, seen);
        if (t.IsGenericType)
            return !typeof(Delegate).IsAssignableFrom(t) && t.GetGenericArguments().Any(a => Mentions(a, depth + 1, seen));
        return t.IsClass && t.Assembly == Bound && !IsStatement(t) && !ModelsHeldByReference.Contains(t.Name) && Properties(t).Any(p => Mentions(p.PropertyType, depth + 1, seen));
    }

    /// <summary>Build a value of <paramref name="t"/> that holds a sentinel value node where the generator is meant to
    /// find it, through exactly the shapes <see cref="Mentions"/> accepts. False when <paramref name="t"/> holds none
    /// through a supported shape: that is the failure the test reports.</summary>
    private static bool TryHold(Type t, int depth, out object held, out object sentinel)
    {
        held = sentinel = null!;
        if (depth > MaxDepth) return false;
        if (IsValueNode(t))
        {
            var concrete = t.IsAbstract ? LeavesOf(t).FirstOrDefault() : t;
            if (concrete is null) return false;
            held = sentinel = Uninit(concrete);
            return true;
        }
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(IReadOnlyList<>) || def == typeof(IEnumerable<>) || def == typeof(IReadOnlyCollection<>))
            {
                var e = t.GetGenericArguments()[0];
                if (!TryHold(e, depth + 1, out var eh, out sentinel)) return false;
                var arr = Array.CreateInstance(e, 1);
                arr.SetValue(eh, 0);
                held = arr;
                return true;
            }
            if (IsValueTuple(t))
            {
                object box = Activator.CreateInstance(t)!;
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!TryHold(f.FieldType, depth + 1, out var fh, out sentinel)) continue;
                    f.SetValue(box, fh);
                    held = box;
                    return true;
                }
            }
            return false;
        }
        if (!t.IsClass || t.Assembly != Bound || IsStatement(t)) return false;
        var candidates = t.IsAbstract ? Bound.GetTypes().Where(c => !c.IsAbstract && t.IsAssignableFrom(c)) : [t];
        foreach (var c in candidates)
        {
            object obj = Uninit(c);
            foreach (var p in Properties(c))
            {
                if (Backing(p) is not { } field || !TryHold(p.PropertyType, depth + 1, out var ph, out sentinel)) continue;
                field.SetValue(obj, ph);
                held = obj;
                return true;
            }
        }
        return false;
    }

    /// <summary>For every concrete leaf of a statement or a value root, and every property whose type mentions a value
    /// node: populate that property with a sentinel and assert the generated walk of that leaf yields it. A property the
    /// walk cannot read is named with its type — the author of a new node shape decides how the walk reads it (a shape
    /// the generator already supports, or a generator arm) before the walk can silently skip it.</summary>
    [Fact]
    public void EveryValueHoldingPropertyIsReadByTheGeneratedWalks()
    {
        var failures = new List<string>();
        foreach (var leaf in LeavesOf(typeof(BoundStatement)).Concat(ValueRoots.SelectMany(LeavesOf)))
        {
            bool statement = IsStatement(leaf);
            foreach (var p in Properties(leaf))
            {
                if (!Mentions(p.PropertyType, 0, [])) continue;
                if (!TryHold(p.PropertyType, 0, out var held, out var sentinel))
                {
                    failures.Add($"{leaf.Name}.{p.Name} ({p.PropertyType.Name}): mentions a value node through a shape the generator does not read");
                    continue;
                }
                if (Backing(p) is not { } field) continue;   // computed from other properties, which are tested themselves
                object node = Uninit(leaf);
                field.SetValue(node, held);
                var walked = statement ? ((BoundStatement)node).OwnValueParts() : BoundStatementTree.ValueChildren(node);
                if (!walked.Any(w => ReferenceEquals(w, sentinel)))
                    failures.Add($"{leaf.Name}.{p.Name} ({p.PropertyType.Name}): a value node it holds is not yielded by "
                                 + (statement ? "OwnValueParts" : "ValueChildren"));
            }
        }
        Assert.True(failures.Count == 0, "the generated walks skip an operand position:\n" + string.Join("\n", failures));
    }

    /// <summary>The PB6223 shape by name: a function-identifier CANCEL target is held in a value tuple.</summary>
    [Fact]
    public void CancelYieldsItsDynamicNameFromTheTuple()
    {
        var name = (BoundOperand)Uninit(typeof(BoundComputedOperand));
        var cancel = new BoundCancel([((string?)null, (BoundOperand?)name)], true);
        Assert.Same(name, Assert.Single(cancel.OwnValueParts()));
    }

    private static BoundOperand FunctionOperand()
    {
        var operand = (BoundComputedOperand)Uninit(typeof(BoundComputedOperand));
        SetBacking(operand, "Expr", Uninit(typeof(BoundIntrinsicCall)));
        return operand;
    }

    private static void SetBacking(object target, string property, object? value)
    {
        var f = target.GetType().GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True(f is not null, $"{target.GetType().Name} has no backing field for {property} — the record's shape changed and this guard must follow it");
        f!.SetValue(target, value);
    }

    private static BoundStatement IfOf(BoundCondition condition)
    {
        var stmt = (BoundIf)Uninit(typeof(BoundIf));
        SetBacking(stmt, "Condition", condition);
        return stmt;
    }

    private static T Cond<T>(string property, object? value) where T : BoundCondition
    {
        var c = (T)Uninit(typeof(T));
        SetBacking(c, property, value);
        return c;
    }

    /// <summary>§2023:15.3 AR14, one position per row: the statement's operand-declared names include
    /// EC-ARGUMENT-FUNCTION whatever condition kind or statement holds the function reference. Each row was silent before
    /// kb/Work PB6223 / PB6328 (the relation rows already raised and pin that the new walk keeps them).</summary>
    public static TheoryData<string, Func<BoundStatement>> Positions => new()
    {
        { "relation left", () => IfOf(Cond<BoundRelational>("Left", FunctionOperand())) },
        { "relation right", () => IfOf(Cond<BoundRelational>("Right", FunctionOperand())) },
        { "class condition", () => IfOf(Cond<BoundClassCondition>("Operand", FunctionOperand())) },
        { "user class condition", () => IfOf(Cond<BoundUserClassCondition>("Operand", FunctionOperand())) },
        { "alphabet class condition", () => IfOf(Cond<BoundCodedSetClassCondition>("Operand", FunctionOperand())) },
        { "range subject", () => IfOf(Cond<BoundRangeMembership>("Left", FunctionOperand())) },
        { "range low bound", () => IfOf(Cond<BoundRangeMembership>("Lo", FunctionOperand())) },
        { "range high bound", () => IfOf(Cond<BoundRangeMembership>("Hi", FunctionOperand())) },
        { "per-evaluation window", () => IfOf(Cond<BoundUdfEvaluated>("Inner", Cond<BoundClassCondition>("Operand", FunctionOperand()))) },
        { "negated class condition", () => IfOf(Cond<BoundNot>("Operand", Cond<BoundClassCondition>("Operand", FunctionOperand()))) },
        { "CANCEL target", () => new BoundCancel([((string?)null, (BoundOperand?)FunctionOperand())], true) },
    };

    [Theory]
    [MemberData(nameof(Positions))]
    public void AFunctionReferenceInAnyOperandPositionDeclaresEcArgumentFunction(string position, Func<BoundStatement> build)
    {
        var names = EcBinder.OperandRaisableNames(build()).ToList();
        Assert.True(names.Contains("EC-ARGUMENT-FUNCTION"), $"{position}: the statement declares {string.Join(",", names)} but not EC-ARGUMENT-FUNCTION");
    }
}
