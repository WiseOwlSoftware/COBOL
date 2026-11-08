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
/// RENAMES spans) is one reference, the outer place. A current occurrence count (<see cref="AllCount"/>: a loop's, a table SORT's,
/// a whole table's) is no reference: the places it reads are the table's (kb/Work PB4854, PB4855).</para>
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
    /// <summary>What <paramref name="statement"/> WRITES a reference to itself, in source order (an operand a node names in
    /// <see cref="IBoundImplicitOperands"/> is one the program did not write, and is left out); <paramref name="except"/> (nodes the statement
    /// holds, compared by reference) are left out too — the planner takes READ INTO's move and each conditional phrase apart itself.</summary>
    public static Reach Of(BoundStatement statement, IEnumerable<object?>? except = null) => Collect(statement, except ?? []);

    /// <summary>What an operand tree <paramref name="node"/> (a condition, an expression, a set target, a bound operand)
    /// references, in source order.</summary>
    public static Reach OfOperand(object node) => Collect(node, []);

    private static Reach Collect(object root, IEnumerable<object?> except)
    {
        var found = new Reach.Builder();
        var skip = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var part in except)
            if (part is not null) skip.Add(part);   // a part of the statement the caller plans on its own (READ INTO's move, a conditional phrase)
        Visit(root, found, skip, yieldPlace: true, statementsAreDirect: true);
        return found.Build();
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

    private static void Visit(object? node, Reach.Builder found, HashSet<object> skip, bool yieldPlace, bool statementsAreDirect)
    {
        if (node is null) return;
        if (skip.Contains(node))
        {
            // An implied PLACE is no reference of its own, but the positions the program wrote inside it still are: a condition-name
            // `YC (I)` holds its conditional variable as `T (I)`, and the statement explicitly references I (rule 5 d: T (I) references I).
            if (node is Place skippedPlace) VisitFields(skippedPlace, found, skip, yieldPlace: false, statementsAreDirect: false);
            return;
        }
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
                found.AddPlace(new MemberPlace(read.Path, read.Item));   // a subscript or window position that reads an item references it
                return;
            case AllCount count:
                // A current occurrence count reads its places (an OCCURS DEPENDING ON item, a capacity register) for the statement's meaning,
                // never because the program wrote them (AllCount.Places): only the positions inside them are references.
                foreach (var counted in count.Places) VisitFields(counted, found, skip, yieldPlace: false, statementsAreDirect: false);
                return;
            case BoundLinageCounterRef or BoundReportCounterRef:
                found.AddRegister((BoundExpr)node);   // a runtime-sourced register no place stands for, referenced where it is read
                return;
            case Place place:
                if (yieldPlace) found.AddPlace(place);
                // Inside a place only the positions it computes are references (T (I) references I); a wrapped place is the
                // same reference, never a second one.
                VisitFields(node, found, skip, yieldPlace: false, statementsAreDirect: false);
                return;
            case BoundStatement statement:
                // A statement nested in the one asked is a statement of its own (rule 7), but an operand-evaluation step is no statement the
                // program wrote: it evaluates an operand of the one asked (a function activation or object-property GET inside a condition's
                // window), so what it reads is that operand's reference (kb/Work PB4977).
                if (!statementsAreDirect && !statement.OperandEvaluation) return;
                VisitFields(node, found, skip, yieldPlace: true, statementsAreDirect: IsTransparent(statement));
                return;
            default:
                VisitFields(node, found, skip, yieldPlace: true, statementsAreDirect: false);
                return;
        }
    }

    /// <summary>Every field a record stores — the compiler-generated backing fields of its properties included, which a computed
    /// property has none of, so the walk reads state and never derived answers — over the whole base chain.</summary>
    private static void VisitFields(object node, Reach.Builder found, HashSet<object> skip, bool yieldPlace, bool statementsAreDirect)
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

/// <summary>⛔ THE REFERENCES A BOUND STATEMENT OR OPERAND MAKES, of the two kinds a USE FOR DEBUGGING identifier-1 can be (X3.23-1985 USE FOR DEBUGGING
/// general rule 5): a <see cref="Place"/> (every data item, special register and sum counter that has one) and a REGISTER — the runtime-sourced
/// LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER (ISO/IEC 1989:2023 §2023:8.4.3.14, §2023:8.4.3.15), read as a <see cref="BoundLinageCounterRef"/> or
/// <see cref="BoundReportCounterRef"/> because the report writer and the I-O control system own their storage (kb/Work PB4858).</summary>
/// <param name="Places">The places, in source order.</param>
/// <param name="Registers">The register reads, in source order.</param>
internal sealed record Reach(IReadOnlyList<Place> Places, IReadOnlyList<BoundExpr> Registers)
{
    /// <summary>The accumulator a walk fills; handed down as a parameter so the walk itself holds no state.</summary>
    internal sealed class Builder
    {
        private readonly List<Place> _places = [];
        private readonly List<BoundExpr> _registers = [];

        /// <summary>Record a place reference.</summary>
        public void AddPlace(Place place) => _places.Add(place);

        /// <summary>Record a counter-register reference.</summary>
        public void AddRegister(BoundExpr register) => _registers.Add(register);

        /// <summary>The finished reach.</summary>
        public Reach Build() => new(_places, _registers);
    }
}
