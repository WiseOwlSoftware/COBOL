// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Procedure;

/// <summary>
/// ⛔ <b>THE ONE LEFT-TO-RIGHT ARGUMENT ORDER</b> — ISO §8.4.3.2.4 GR2 (kb/Work PB1423): "At the time reference is
/// made to a function, its arguments are evaluated individually in the order specified in the list of arguments,
/// from left to right. An argument being evaluated may itself be a function-identifier or may be an expression
/// containing function-identifiers." It governs every function-identifier — GR1's three name forms are all
/// "a function-identifier" — so the intrinsic call and the user-defined activation share it here.
///
/// <para><b>The defect it removes.</b> A user-defined function activation is a statement-scoped PRE-op
/// (<c>DataBinder.PendingPreOps</c>, hoisted ahead of the statement that consumes its result), and an argument
/// that merely READS state — an identifier, an arithmetic expression, an intrinsic result — is rendered at the
/// consumer, after every hoisted activation. So the later argument's activation ran BEFORE the earlier argument
/// was evaluated: <c>FUNCTION SUM(A, FUNCTION BUMP(A))</c> summed A's value AFTER BUMP had changed it, although
/// the argument to its left is evaluated first.</para>
///
/// <para><b>The mechanism.</b> A <see cref="Window"/> is opened around one function's argument list. Every argument
/// operand bound inside it is recorded with the pending pre-op range it registered. When a LATER argument
/// registered pre-ops (an activation, an inline invocation, a function-bearing subscript store) every earlier
/// argument whose value can still change is MATERIALIZED into its own intermediate result item by the ONE
/// materializer (<see cref="SendingValueTemp.Materialize"/> — the same §14.9.25.4 GR1 temp a MOVE sender gets),
/// and the store is placed on the pending list at the position its argument finished binding — i.e. before the
/// later argument's pre-ops. The list order IS the evaluation order, so no argument can observe a later one.</para>
///
/// <para><b>A held argument keeps its DESCRIPTION (kb/Work PB4124).</b> An intrinsic function reads an argument's description as
/// well as its value (ABS, MAX, MIN, MEDIAN and MEAN return a COPY of an argument's item, §15.59.4, §15.63.4, §15.61.4; DISPLAY
/// and <c>FUNCTION LENGTH</c> measure it), and the intermediate result item an argument is held in has its own digits. A held
/// argument that is a function whose result copies an argument's item (<see cref="NeedsItsArguments"/>) is therefore not held as
/// a value: <see cref="Hold"/> holds ITS arguments and the call is read over them, a pure selection reading what it read at its
/// place. Any other held argument is described by its value (<c>FunctionDeviceText.Field</c>).</para>
///
/// <para><b>An object property is an argument evaluated IN ITS PLACE (kb/Work PB1932).</b> Its value is temp-1,
/// "determined as though the associated get property method were invoked" (§8.4.3.9.4 GR1), so the GET IS the
/// argument's evaluation. Every property reference in a function argument is SENDING — an intrinsic argument always
/// is, and a user function's identifier argument is BY REFERENCE "other than an object property" (§8.4.3.2.4 GR5 a))
/// — so <see cref="Record"/> moves the GETs the argument registered from the statement-level property list (whose
/// wrap runs them ahead of EVERY pre-op of the statement) onto the pending pre-op list at the argument's position.
/// The argument then counts as one that registered pre-ops, an earlier argument it could change is frozen before it,
/// and the activation consuming its temp still follows it.</para>
///
/// <para><b>What is not frozen, and why.</b> A literal or figurative constant cannot change
/// (<see cref="SendingValueTemp.Materialize"/> answers null). A compiler temporary — a user function's result, an
/// earlier snapshot, an object-property or inline-invocation value — is written once, at its own position in the
/// list, and no later argument writes it. A BY REFERENCE identifier argument of a user function designates its
/// storage, and §8.4.3.2.4 GR6a says "the values of argument-1 are made available to the activated function at
/// the time control is transferred" — the caller of <see cref="Window.Freeze"/> never offers it. A BOOLEAN-EXPRESSION
/// argument is stored by COMPUTE Format 2's own store into a boolean item of §8.8.2 rule 10's length
/// (<see cref="SendingValueTemp.MaterializeBoolean"/>); the one shape left alone is a boolean expression whose
/// length is a run-time value, which no intermediate item can describe.</para>
/// </summary>
internal sealed class ArgumentOrder(BinderContext ctx, SendingValueTemp temps,
    Func<int, List<BoundStatement>> drainPropertyGets)
{
    private readonly Stack<Window> _open = new();

    private List<BoundStatement> Pending => ctx.Data.PendingPreOps;

    /// <summary>The pending counts now — the position a pre-op registered next would take, and the count of the
    /// statement's un-drained object-property references.</summary>
    internal (int PreOps, int Properties) Mark => (Pending.Count, ctx.Data.OoPendingPropertyOps.Count);

    /// <summary>Open the window of one function's argument list. Windows nest like the function references do (an
    /// argument may itself be a function-identifier); dispose the window when the argument list is done.</summary>
    internal Window Open()
    {
        var window = new Window(this);
        _open.Push(window);
        return window;
    }

    /// <summary>Record an argument operand just bound in the innermost open window, with the pending counts from
    /// before it bound (<paramref name="before"/>) to now. The GETs of the object properties the argument named move
    /// onto the pending list here, at the argument's position (§8.4.3.9.4 GR1 with §8.4.3.2.4 GR2 — see the class
    /// summary). Outside any window — nothing evaluates arguments — it is ignored.</summary>
    internal void Record(BoundOperand operand, (int PreOps, int Properties) before)
    {
        if (_open.Count == 0) return;
        Pending.AddRange(drainPropertyGets(before.Properties));
        _open.Peek().Entries.Add(new Entry(operand, before.PreOps, Pending.Count));
    }

    internal readonly record struct Entry(BoundOperand Operand, int Before, int After);

    /// <summary>One function's argument list. See <see cref="ArgumentOrder"/>.</summary>
    internal sealed class Window(ArgumentOrder owner) : IDisposable
    {
        internal List<Entry> Entries { get; } = [];

        public void Dispose()
        {
            if (owner._open.Count == 0 || !ReferenceEquals(owner._open.Peek(), this))
                throw new InvalidOperationException("ArgumentOrder windows must close in the reverse order they opened");
            owner._open.Pop();
        }

        /// <summary>Settle the window: for every operand in <paramref name="candidates"/> that a later argument's
        /// pre-ops would otherwise overtake, store its value in an intermediate result item at the position its
        /// argument finished binding, and return the replacement operand (keyed by REFERENCE to the operand as
        /// bound). Operands that need no freeze are absent from the result. The caller passes the operands whose
        /// value is read at the activation — in argument order — and swaps the replacements in.
        /// <para>⛔ <paramref name="describedValue"/> is true when the frozen operand's DESCRIPTION is read as well as its value
        /// (an intrinsic function's argument: <c>FunctionValueField</c>, kb/Work PB4124) and false when only its value is (a
        /// user-defined function's BY CONTENT / BY VALUE argument). Then a nested function whose returned value copies one of its
        /// arguments' items (<see cref="NeedsItsArguments"/>) is NOT frozen as a value, which keeps the value and loses the
        /// item copied: its own arguments are frozen where they are and the call is read after them.</para></summary>
        internal Dictionary<BoundOperand, BoundOperand> Freeze(IEnumerable<BoundOperand> candidates, bool describedValue = false)
        {
            var frozen = new Dictionary<BoundOperand, BoundOperand>(ReferenceEqualityComparer.Instance);
            var wanted = new HashSet<BoundOperand>(candidates, ReferenceEqualityComparer.Instance);
            if (wanted.Count == 0) return frozen;
            // needs[i]: some LATER argument registered pre-ops, so entry i would be read after them.
            var needs = new bool[Entries.Count];
            bool later = false;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                needs[i] = later;
                later |= Entries[i].After > Entries[i].Before;
            }
            int shift = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                var operand = Entries[i].Operand;
                if (!needs[i] || !wanted.Contains(operand) || IsStable(operand)) continue;
                if (owner.Hold(operand, Entries[i].After + shift, describedValue, out int added) is not { } replacement) continue;
                shift += added;
                frozen[operand] = replacement;
            }
            return frozen;
        }

        /// <summary>The intrinsic form: every argument of the bound call settles, and the call is returned with the
        /// frozen operands swapped in (the argument-class screens already ran on the operands as written). ⛔ THE SWAP
        /// CHANGES NO TYPE: the call's §15.2 type (<see cref="BoundIntrinsicCall.ResultType"/>) was decided from the
        /// arguments as written when the call was built, and the <c>with</c> keeps it — a frozen arithmetic expression is
        /// a scale-0 temporary (an integer data item) where the expression it replaced was not one, so a type re-read from
        /// the swapped arguments would depend on whether a LATER argument activated a user-defined function (kb/Work
        /// PB4036; <c>ArgumentOrderDriftTests</c> refuses a reader that re-resolves it).</summary>
        internal BoundExpr Settle(BoundExpr result)
        {
            if (result is not BoundIntrinsicCall call) return result;
            var frozen = Freeze(call.Args, describedValue: true);
            if (frozen.Count == 0) return result;
            return call with { Args = [.. call.Args.Select(a => frozen.GetValueOrDefault(a) ?? a)] };
        }

    }

    private static bool IsStable(BoundOperand operand) => operand is BoundFieldOperand { Place: var place }
        && RootOf(place.Item).IsCompilerTemp;

    private static DataItem RootOf(DataItem item)
    {
        var root = item;
        while (root.Parent is { } p) root = p;
        return root;
    }

    /// <summary>⛔ DOES THIS CALL'S RETURNED VALUE DEPEND ON THE DESCRIPTIONS OF ITS ARGUMENTS, NOT ONLY ON THEIR VALUES (kb/Work
    /// PB4124)? ABS, MAX, MIN, MEDIAN and MEAN return a COPY of an argument's item (<see cref="FunctionDeviceForm.Argument"/>) and
    /// DATE-OF-INTEGER / DAY-OF-INTEGER an unsigned item of fixed digits (<see cref="FunctionDeviceForm.FixedDigits"/>): what a
    /// function that reads the result sees (its value, <c>FUNCTION LENGTH</c>, DISPLAY) is that item, which a numeric
    /// intermediate result item holding the value alone does not keep. Every other numeric function's temporary is described by
    /// its value (<see cref="FunctionDeviceForm.Value"/>), and the intermediate that holds the value holds all of it; a call GnuCOBOL
    /// folds to a literal (<see cref="FunctionDeviceForm.Literal"/>) is no call to hold. <see cref="ReadsArgumentDescriptions"/> names
    /// every form, and <c>ArgumentOrderDriftTests</c> asks it of each member, so the next form says which kind it is.</summary>
    private static bool NeedsItsArguments(BoundOperand operand, out BoundComputedOperand computed, out BoundIntrinsicCall call)
    {
        if (operand is BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Numeric } numericCall } numeric
            && ReadsArgumentDescriptions(numericCall.Sig.DeviceForm))
        {
            (computed, call) = (numeric, numericCall);
            return true;
        }
        (computed, call) = (null!, null!);
        return false;
    }

    /// <summary>Does a function of this device form return a temporary the ARGUMENTS' descriptions decide (a copy of an argument's item,
    /// or an item of fixed digits), rather than one the value alone describes? Total over <see cref="FunctionDeviceForm"/>: a member
    /// it does not name throws, and <c>ArgumentOrderDriftTests</c> fails on it.</summary>
    internal static bool ReadsArgumentDescriptions(FunctionDeviceForm form) => form switch
    {
        FunctionDeviceForm.Argument or FunctionDeviceForm.FixedDigits => true,
        FunctionDeviceForm.Value or FunctionDeviceForm.Literal => false,
        _ => throw new InvalidOperationException($"ArgumentOrder: FunctionDeviceForm.{form} has no statement of whether its temporary reads its arguments' descriptions (kb/Work PB4124)"),
    };

    /// <summary>Hold <paramref name="operand"/> against a later argument's pre-ops, at <paramref name="index"/>: store it
    /// (<see cref="StoreAt"/>), or, when its description is read and it is a function whose result copies an argument
    /// (<see cref="NeedsItsArguments"/>), hold each of ITS arguments instead and answer the call over the held ones - the call is a
    /// pure selection over its arguments, so reading it later over values that cannot change is the call read at its place, and the
    /// item it copies is the item it copied (kb/Work PB4124). Null when nothing needs holding.</summary>
    private BoundOperand? Hold(BoundOperand operand, int index, bool describedValue, out int added)
    {
        added = 0;
        // A table(ALL) argument is no recorded operand (IntrinsicBinder.TryBindAllArgument): the backend ENUMERATES it where the call
        // is rendered, so it cannot be held on its own and the call that names it is held as a value, as it always was.
        if (!describedValue || !NeedsItsArguments(operand, out var computed, out var call)
            || call.Args.Any(a => a is BoundFieldOperand { Place: TableAllPlace }))
            return StoreAt(operand, index, out added);
        var args = new List<BoundOperand>(call.Args.Count);
        bool changed = false;
        foreach (var arg in call.Args)
        {
            if (!IsStable(arg) && Hold(arg, index + added, describedValue, out int stored) is { } held)
            {
                args.Add(held);
                added += stored;
                changed = true;
            }
            else args.Add(arg);
        }
        // ResultType stays the one the arguments as written gave (kb/Work PB4036): `with` copies it.
        return changed ? computed with { Expr = call with { Args = args } } : null;
    }

    /// <summary>Materialize <paramref name="operand"/> with its registered pre-ops (the extent freeze, when there is
    /// one, and the store itself) placed at <paramref name="index"/> — <c>SendingValueTemp</c>'s one placement.</summary>
    private BoundFieldOperand? StoreAt(BoundOperand operand, int index, out int added)
    {
        int start = Pending.Count;
        added = 0;
        // A boolean-expression argument has its own store (COMPUTE Format 2's) and its own length (§8.8.2 rule 10); a
        // run-time length is the one shape no intermediate item can describe, and stays where it was.
        var held = operand is BoundBoolOperand { Expr: var boolean }
            ? ConditionBinder.BoolResultLength(boolean) is int positions and > 0
                ? temps.MaterializeBoolean(boolean, positions, "fnarg", index)
                : null
            : temps.Materialize(operand, "fnarg", index);
        if (held is not { } place) return null;
        added = Pending.Count - start;
        return new BoundFieldOperand(place);
    }
}
