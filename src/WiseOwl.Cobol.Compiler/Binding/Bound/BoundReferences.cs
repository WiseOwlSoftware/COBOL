// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>
/// ⛔ THE DATA REFERENCES A BOUND STATEMENT MAKES ITSELF, read off the tree with no per-node list (X3.23-1985 USE FOR DEBUGGING
/// general rule 5: a debugging section runs "for every statement that explicitly references identifier-1"; rule 7: a statement
/// nested in it is a statement of its own; docs/CONTROL_FLOW_DESIGN.md D20). <see cref="BoundStores"/> is the TOTAL taxonomy of the
/// STORING positions, one arm per statement leaf; this is the same question over EVERY position, sending ones included, and it
/// is answered by the one structural fact every bound node shares — it is an immutable record whose fields hold its operands —
/// so a new statement leaf, a new operand shape and a new place kind are covered the day they are added, with no arm to forget.
/// <para><b>What it yields.</b> Every <see cref="Place"/> the statement holds in an operand position, in source order, plus a
/// <see cref="MemberPlace"/> for each data item a subscript or reference-modification position READS (<c>T (I)</c> references
/// <c>I</c>). A place that wraps another (a reference-modified, group-image or OCCURS-DEPENDING decoration, the leaves a
/// RENAMES spans) is one reference, the outer place.</para>
/// <para><b>What it never enters.</b> A statement NESTED in the one asked — an IF arm, an inline PERFORM body, a conditional
/// phrase's imperative statements — because it is a statement of its own (rule 7); and any type outside the operand model
/// (<see cref="DataItem"/>, file and communication models), whose graphs are not operands. The transparent wrappers one written
/// statement is made of (<see cref="BoundSequence"/>'s operand-evaluation steps, <see cref="BoundImplicitSeries"/>'s members,
/// the EC, activation, receiver-bracket and debug wrappers) are entered: they are the statement. The implicit move of a verb's FROM
/// or INTO phrase (a single <see cref="BoundMove"/> field) is entered too: it is the verb's own operand.</para>
/// <para>The walk is static recursion over parameters and holds no state: the bound tree's layer carries no field of type
/// <see cref="object"/> (<c>BackendContractDriftTests</c>), and the graph it reads is a tree, so it needs no visited set.</para>
/// </summary>
internal static class BoundReferences
{
    /// <summary>The places <paramref name="statement"/> WRITES a reference to itself, in source order (an operand a node names in
    /// <see cref="IBoundImplicitOperands"/> is one the program did not write, and is left out); <paramref name="except"/> (a node the statement
    /// holds, compared by reference) is left out too — the planner takes READ INTO's move apart itself.</summary>
    public static IReadOnlyList<Place> Of(BoundStatement statement, object? except = null) => Collect(statement, except);

    /// <summary>The places an operand tree <paramref name="node"/> (a condition, an expression, a set target, a bound operand)
    /// references, in source order.</summary>
    public static IReadOnlyList<Place> OfOperand(object node) => Collect(node, null);

    private static List<Place> Collect(object root, object? except)
    {
        var found = new List<Place>();
        var skip = new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (except is not null) skip.Add(except);   // a part of the statement the caller plans on its own (READ INTO's move)
        Visit(root, found, skip, yieldPlace: true, statementsAreDirect: true);
        return found;
    }

    /// <summary>The wrappers that are not a statement of their own but a layer of the statement they carry.</summary>
    private static bool IsTransparent(BoundStatement s) =>
        s is BoundSequence or BoundImplicitSeries or BoundEcChecked or BoundActivationSite or BoundReceiverBrackets or BoundDebugTriggered;

    /// <summary>The types the walk enters: the bound tree's own nodes and the operand model (places, access paths and positions).
    /// Everything else — a data item, a file, a communication description — is a graph of its own.</summary>
    private static bool Enters(Type t) =>
        t.Namespace == typeof(BoundStatement).Namespace
        || typeof(Place).IsAssignableFrom(t) || typeof(AccessPath).IsAssignableFrom(t) || typeof(AccessSegment).IsAssignableFrom(t)
        || typeof(Position).IsAssignableFrom(t) || t == typeof(OffsetTerm)
        || typeof(WindowCoding).IsAssignableFrom(t) || typeof(AllCount).IsAssignableFrom(t);

    private static void Visit(object? node, List<Place> found, HashSet<object> skip, bool yieldPlace, bool statementsAreDirect)
    {
        if (node is null || skip.Contains(node)) return;
        var type = node.GetType();
        if (type.IsPrimitive || type.IsEnum || node is string) return;
        if (node is ITuple tuple)   // a tuple list's elements are operands too (CANCEL's (name, operand) pairs)
        {
            for (int i = 0; i < tuple.Length; i++) Visit(tuple[i], found, skip, yieldPlace, statementsAreDirect);
            return;
        }
        if (node is IEnumerable list && !Enters(type))
        {
            foreach (var element in list) Visit(element, found, skip, yieldPlace, statementsAreDirect);
            return;
        }
        if (!Enters(type)) return;
        if (node is IBoundImplicitOperands implied)
            foreach (var operand in implied.ImplicitOperands) skip.Add(operand);   // held for the statement's meaning, never written
        switch (node)
        {
            case PositionItemRead read:
                found.Add(new MemberPlace(read.Path, read.Item));   // a subscript or window position that reads an item references it
                return;
            case Place place:
                if (yieldPlace) found.Add(place);
                // Inside a place only the positions it computes are references (T (I) references I); a wrapped place is the
                // same reference, never a second one.
                VisitFields(node, found, skip, yieldPlace: false, statementsAreDirect: false);
                return;
            case BoundStatement statement:
                if (!statementsAreDirect) return;   // a statement nested in the one asked
                VisitFields(node, found, skip, yieldPlace: true, statementsAreDirect: IsTransparent(statement));
                return;
            default:
                VisitFields(node, found, skip, yieldPlace: true, statementsAreDirect: false);
                return;
        }
    }

    /// <summary>Every field a record stores — the compiler-generated backing fields of its properties included, which a computed
    /// property has none of, so the walk reads state and never derived answers — over the whole base chain.</summary>
    private static void VisitFields(object node, List<Place> found, HashSet<object> skip, bool yieldPlace, bool statementsAreDirect)
    {
        for (var c = node.GetType(); c is not null && c != typeof(object); c = c.BaseType)
            foreach (var field in c.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string)) continue;
                // A single BoundMove-typed field is the implicit move a verb's FROM or INTO phrase performs (WRITE ... FROM x, READ ... INTO x):
                // part of the verb's own statement, whose operands the verb explicitly references (X3.23-1985 USE FOR DEBUGGING general rule 5 a).
                Visit(field.GetValue(node), found, skip, yieldPlace, statementsAreDirect || field.FieldType == typeof(BoundMove));
            }
    }
}
