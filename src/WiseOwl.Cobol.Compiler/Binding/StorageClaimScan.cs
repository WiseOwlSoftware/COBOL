// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Binding.Procedure;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>A <c>dataAddressIdentifier</c> head (ISO §2023:8.4.3.11): the data-name and the OF/IN qualifiers written after it, as
/// written (<see cref="WrittenQualifiedName"/>: the symbol table identifies it, docs/rearchitecture/DESIGN-listing.md section 6.7.1).</summary>
internal readonly record struct AddressTarget(WrittenQualifiedName Written);

/// <summary>The data-name head of one operand an activation may pass BY REFERENCE (or, <paramref name="SlotsOnly"/>, a BY CONTENT
/// operand or RETURNING receiver claimed only when its values ride managed slots).</summary>
internal readonly record struct ByReferenceOperand(WrittenQualifiedName Written, bool Invoke, bool SlotsOnly);

/// <summary>⛔ THE ONE WALK OF A COMPILATION'S PARSE TREE FOR THE STORAGE-CLAIM OPERANDS (kb/Work PB4302). The pointer pass
/// (<c>PtrBindBasedAndAddressablesPass</c>) and the linkage pass (<c>LinkageBinder.CallBindLinkage</c>) ask, of each source
/// element, which data-names a data-address-identifier (<c>ADDRESS OF x</c>, ISO §2023:8.4.3.11) takes and which operands an
/// activation may pass BY REFERENCE (§2023:14.9.4.4 GR5, §2023:14.9.23.4 GR6) — and a CONTAINING element asks the same of every
/// program it contains, because a contained program references the container's GLOBAL names without describing them again
/// (§2023:13.18.27.4 GR2) and the storage they name is the container's. Asked by walking the subtree, that is one re-walk of every
/// contained program's whole subtree from EVERY container above it: a chain of N nested programs walked N×(N+1)/2 programs,
/// through a recursive iterator that charged each node its depth in MoveNext calls on top (558 ms at depth 100, 3,640 ms at 200).
/// <para>This walks the tree ONCE, with an explicit stack, and records the operands in document order together with the span of
/// them under every <c>procedureDivision</c> and every <c>nestedProgram</c> — the only two regions anything asks about — so a
/// question is a slice. The tree is found from the region (its root is the top of its parent chain, and every
/// region of one compilation shares it), so no element needs to be told which walk is its own.</para>
/// <para>One instance lives per compilation (<see cref="BindSession.ClaimScans"/>), because the per-unit binders share one
/// parse tree; an instance built apart (a binder made outside a session) simply walks for itself.</para></summary>
internal sealed class StorageClaimScan
{
    private readonly Dictionary<IParseTree, TreeScan> _byRoot = new(ReferenceEqualityComparer.Instance);

    /// <summary>The data-address-identifier heads under <paramref name="region"/> — a <c>procedureDivision</c> or a
    /// <c>nestedProgram</c> (a contained program's whole subtree) — in document order. ⛔ A <c>setAddressReceiver</c> (SET Format 7's
    /// receiving <c>ADDRESS OF data-name-1</c>) is a different rule and is never yielded: it names a BASED item (kb/Work PB450).</summary>
    public IEnumerable<AddressTarget> AddressTargets(IParseTree region)
    {
        var scan = ScanOf(region, out var span);
        for (int i = span.AddressStart; i < span.AddressEnd; i++) yield return scan.Addresses[i];
    }

    /// <summary>The operands under <paramref name="region"/> (as <see cref="AddressTargets"/>) an activation may pass BY REFERENCE, in
    /// document order: each INVOKE argument passed BY REFERENCE (§2023:14.9.23.4 GR6a.1), each CALL USING operand the one pass-mode
    /// rule (<see cref="CallArgumentModes"/>: §2023:14.9.4.4 GR5 in Format 1, GR9 in Format 2) may pass BY REFERENCE, and each argument
    /// of a function <paramref name="isUserFunction"/> says is user-defined, whose identifier argument is passed by reference whenever
    /// its formal is — a fact of the function's header, not of the reference, so every one is a candidate. Only an operand that IS
    /// one identifier (§2023:14.9.4.4 GR8, <see cref="ConditionBinder.ReduceArgument"/>) is recorded: a subscript, a reference
    /// modifier's operands or an arithmetic expression's terms are SENDING operands of the reference, never passed, and an expression
    /// argument is passed by content. Also recorded, <see cref="ByReferenceOperand.SlotsOnly"/> (kb/Work PB1940): each CALL USING
    /// operand passed BY CONTENT and each CALL RETURNING receiver — claimed only when it is a group whose values ride managed slots,
    /// which crosses only as an area.
    /// <para>⛔ A CALL'S FORMAT 2 FORMALS ARE BOUND AFTER THIS CLAIM, so a keyword-less identifier of a Format-2 CALL is claimed as
    /// GR9 a) 1. passes it to a BY REFERENCE formal, whatever phrase an earlier argument wrote (kb/Work PB3227): a claim the formal
    /// turns out not to need costs a cell, a missing one breaks §2023:14.2.3 GR8's one storage area.</para>
    /// <para>Two facts are the ASKING element's, which is why the walk records the operands they decide untested: whether a function
    /// name is a user-defined function (<paramref name="isUserFunction"/>, §2023:8.4.6.6: a prototype name or the containing
    /// definition's own name), and whether the bare word a CALL without the AS phrase names is a program-prototype-name, which makes
    /// it Format 2 (<paramref name="isProgramPrototype"/>, §2023:8.4.6.8; §2023:14.9.4.2 spelling (b), <c>CallBinder.BindCall</c>).</para></summary>
    public IEnumerable<ByReferenceOperand> ByReferenceOperands(IParseTree region, Func<string, bool> isUserFunction,
        Func<string, bool> isProgramPrototype)
    {
        var scan = ScanOf(region, out var span);
        for (int i = span.OperandStart; i < span.OperandEnd; i++)
        {
            var entry = scan.Operands[i];
            var claim = entry.Asked is not { } asked || asked.NamesContainingProgram
                || (asked.Function ? isUserFunction : isProgramPrototype)(asked.Word)
                ? entry.Claim : entry.Otherwise;
            if (claim is { } operand) yield return operand;
        }
    }

    /// <summary>How many parse trees this instance has walked: one per compilation, however many elements ask and in whatever
    /// order — the figure a test pins, so the cost stays a count and never a clock.</summary>
    internal int TreesWalked => _byRoot.Count;

    private TreeScan ScanOf(IParseTree region, out Span span)
    {
        var root = region;
        while (root.Parent is { } up) root = up;
        if (!_byRoot.TryGetValue(root, out var scan)) _byRoot[root] = scan = TreeScan.Walk(root);
        if (!scan.Spans.TryGetValue(region, out span))
            throw new InvalidOperationException(
                $"StorageClaimScan was asked about a {region.GetType().Name}; only a procedure division or a contained program is a region (kb/Work PB4302)");
        return scan;
    }

    /// <summary>The operands under one region, as half-open index ranges into the walk's two lists.</summary>
    private readonly record struct Span(int AddressStart, int AddressEnd, int OperandStart, int OperandEnd);

    /// <summary>One recorded operand: its <paramref name="Claim"/> when nothing is <paramref name="Asked"/> or the asked word holds,
    /// else <paramref name="Otherwise"/>; null is no claim.</summary>
    private readonly record struct Entry(ByReferenceOperand? Claim, ByReferenceOperand? Otherwise = null, AskedWord? Asked = null);

    /// <summary>A word only the asking element can judge: a function name (<paramref name="Function"/>) or a CALL's bare target word.
    /// <para>⛔ <paramref name="NamesContainingProgram"/>: the CALL's target word is the program-name of a program CONTAINED in the
    /// asking element that contains the CALL (the CALL's own program among them). §2023:8.4.6.8 makes "the program-name of a
    /// containing program definition" a program-prototype-name of the REFERENCING source element, and the asker's own scope
    /// (<see cref="SymbolTable.DeclaresPrototypeName"/>) holds only the asker and the programs containing it, so a container asking
    /// about a contained program's <c>CALL INNERP</c> inside INNERP answered Format 1 while INNERP's own CALL bind selected Format 2
    /// and passed the keyword-less GLOBAL item BY REFERENCE with no cell (kb/Work PB3227 review R1-1). The REPOSITORY half needs no
    /// such fact: a contained program has no configuration section of its own (§2023:12.3.3 SR1) and inherits its container's.</para></summary>
    private readonly record struct AskedWord(string Word, bool Function, bool NamesContainingProgram = false);

    private sealed class TreeScan
    {
        public readonly List<AddressTarget> Addresses = [];
        public readonly List<Entry> Operands = [];
        public readonly Dictionary<IParseTree, Span> Spans = new(ReferenceEqualityComparer.Instance);

        /// <summary>A region's closing: popped after every node under it, it fixes the region's span.</summary>
        private readonly record struct Frame(IParseTree Node, bool Closing, int AddressStart, int OperandStart);

        public static TreeScan Walk(IParseTree root)
        {
            var scan = new TreeScan();
            var pending = new Stack<Frame>();
            pending.Push(new Frame(root, false, 0, 0));
            while (pending.Count > 0)
            {
                var frame = pending.Pop();
                var node = frame.Node;
                if (frame.Closing)
                {
                    scan.Spans[node] = new Span(frame.AddressStart, scan.Addresses.Count, frame.OperandStart, scan.Operands.Count);
                    continue;
                }
                if (node is Core.ProcedureDivisionContext or Core.NestedProgramContext)
                    pending.Push(new Frame(node, true, scan.Addresses.Count, scan.Operands.Count));
                scan.Record(node);
                for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(new Frame(node.GetChild(i), false, 0, 0));
            }
            return scan;
        }

        private void Record(IParseTree node)
        {
            switch (node)
            {
                case Core.DataAddressIdentifierContext { } dai when dai.dataReference() is { } target:
                    // The head name + its OF/IN qualifiers for EVERY operand shape: a subscripted operand forces the same containing
                    // record — the occurrence displacement is a bind-time offset over the ONE cell, never separate storage.
                    if (WrittenQualifiedName.Of(target) is { } head) Addresses.Add(new AddressTarget(head));
                    break;

                // An INVOKE argument (§2023:14.9.23.2): BY REFERENCE, or written with no BY phrase — which §2023:14.9.23.4 GR6a.1
                // passes BY REFERENCE whenever the formal is BY REFERENCE and the argument is an identifier.
                case Core.InvokeUsingContext invokeUsing:
                    foreach (var ia in invokeUsing.invokeArgument())
                    {
                        var operand = ia.REFERENCE() is not null ? ia.receivingIdentifier()?.dataReference()
                            : ia.CONTENT() is null && ia.VALUE() is null
                                ? ConditionBinder.ReduceArgument(ia.literal(), null, ia.booleanExpression(), ia.arithmeticExpression()).Reference
                                : null;
                        if (Head(operand) is { } ih)
                            Operands.Add(new Entry(new ByReferenceOperand(ih, Invoke: true, SlotsOnly: false)));
                    }
                    break;

                case Core.FunctionCallContext fc when fc.functionArgList() is { } fargs && fc.functionName() is { } fname:
                    var function = new AskedWord(fname.GetText(), Function: true);
                    foreach (var farg in fargs.functionArgument())
                        if (Head(ConditionBinder.ReduceArgument(null, null, farg.booleanExpression(), farg.arithmeticExpression()).Reference) is { } h)
                            Operands.Add(new Entry(new ByReferenceOperand(h, Invoke: false, SlotsOnly: false), Asked: function));
                    break;

                case Core.CallReturningPhraseContext ret:
                    if (Head(ret.receivingIdentifier().dataReference()) is { } rh)
                        Operands.Add(new Entry(new ByReferenceOperand(rh, Invoke: false, SlotsOnly: true)));
                    break;

                case Core.CallStatementContext call when call.callUsingPhrase() is { } phrase:
                    RecordCallArguments(call, phrase);
                    break;
            }
        }

        /// <summary>A CALL's USING operands, each claimed by the mode <see cref="CallArgumentModes"/> gives it. Format 2 is selected by
        /// the AS phrase, or, with it omitted, by a bare target word that names a program prototype (§2023:14.9.4.2), which the asking
        /// element judges; a keyword-less identifier whose claim the two formats answer differently is recorded with both.</summary>
        private void RecordCallArguments(Core.CallStatementContext call, Core.CallUsingPhraseContext phrase)
        {
            bool asPhrase = call.callAsPhrase() is not null;
            AskedWord? prototypeWord = !asPhrase && call.callTarget()?.sendingOperand()?.dataReference() is { } target
                && CallBinder.BarePrototypeWord(target) is { } word
                    ? new AskedWord(word.GetText(), Function: false, NamesContainingProgram: SymbolTable.NamesContainedProgramHolding(call, word.GetText()))
                    : null;
            var carried = CobolPassMode.Reference;   // GR5: "the BY REFERENCE phrase is assumed" before the first phrase
            foreach (var arg in phrase.callArgument())
            {
                var carriedIn = carried;
                carried = CallArgumentModes.CarriedAfter(arg, carried);
                if (arg.callByReference() is { } r)
                {
                    if (Head(r.receivingIdentifier()?.dataReference()) is { } rh) Operands.Add(new Entry(Claim(rh, CobolPassMode.Reference)));
                    continue;
                }
                if (arg.callByContent() is { } c)
                {
                    if (Head(ConditionBinder.ReduceArgument(c.literal(), null, c.booleanExpression(), c.arithmeticExpression()).Reference) is { } ch)
                        Operands.Add(new Entry(Claim(ch, CobolPassMode.Content)));
                    continue;
                }
                if (arg.callByValue() is not null
                    || Head(ConditionBinder.ReduceArgument(arg.literal(), null, arg.booleanExpression(), arg.arithmeticExpression()).Reference)
                        is not { } h)
                    continue;
                // Format 2's corresponding formal is not bound yet, so the formal asked is the one that claims most: BY REFERENCE.
                var format2 = Claim(h, CallArgumentModes.KeywordLessIdentifier(formatTwo: true, carriedIn, formalByValue: false));
                var format1 = Claim(h, CallArgumentModes.KeywordLessIdentifier(formatTwo: false, carriedIn, formalByValue: null));
                Operands.Add(asPhrase ? new Entry(format2)
                    : prototypeWord is null || format1 == format2 ? new Entry(format1)
                    : new Entry(format2, format1, prototypeWord));
            }
        }

        /// <summary>The claim a CALL operand passed in <paramref name="mode"/> makes: BY REFERENCE claims its cell, BY CONTENT only when
        /// its values ride managed slots, BY VALUE none (the value crosses).</summary>
        private static ByReferenceOperand? Claim(WrittenQualifiedName head, CobolPassMode mode) => mode switch
        {
            CobolPassMode.Reference => new ByReferenceOperand(head, Invoke: false, SlotsOnly: false),
            CobolPassMode.Content => new ByReferenceOperand(head, Invoke: false, SlotsOnly: true),
            _ => null,
        };

        private static WrittenQualifiedName? Head(Core.DataReferenceContext? operand) =>
            operand is null ? null : WrittenQualifiedName.Of(operand);
    }
}
