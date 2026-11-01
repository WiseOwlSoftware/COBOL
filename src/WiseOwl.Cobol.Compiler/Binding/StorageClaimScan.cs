// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>A <c>dataAddressIdentifier</c> head (ISO §8.4.3.11): the data-name and the OF/IN qualifiers written after it.</summary>
internal readonly record struct AddressTarget(string Name, List<string> Qualifiers);

/// <summary>The data-name head of one operand an activation may pass BY REFERENCE (or, <paramref name="SlotsOnly"/>, a BY CONTENT
/// operand or RETURNING receiver claimed only when its values ride managed slots). <paramref name="UserFunction"/> names the
/// function a function-call argument belongs to — it is an operand only when THAT function is a user-defined one, a fact the
/// asking element decides (<see cref="StorageClaimScan.ByReferenceOperands"/>) — and is null for every other operand.</summary>
internal readonly record struct ByReferenceOperand(
    string Name, List<string> Qualifiers, bool Invoke, bool SlotsOnly, string? UserFunction = null);

/// <summary>⛔ THE ONE WALK OF A COMPILATION'S PARSE TREE FOR THE STORAGE-CLAIM OPERANDS (kb/Work PB4302). The pointer pass
/// (<c>DataBinder.PtrBindBasedAndAddressables</c>) and the linkage pass (<c>DataBinder.CallBindLinkage</c>) ask, of each source
/// element, which data-names a data-address-identifier (<c>ADDRESS OF x</c>, ISO §8.4.3.11) takes and which operands an
/// activation may pass BY REFERENCE (§14.9.4.4 GR5, §14.9.23.4 GR6) — and a CONTAINING element asks the same of every
/// program it contains, because a contained program references the container's GLOBAL names without describing them again
/// (§13.18.27.4 GR2) and the storage they name is the container's. Asked by walking the subtree, that is one re-walk of every
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
    /// document order: each INVOKE argument passed BY REFERENCE (§14.9.23.4 GR6 a) 1.), each CALL USING operand in a BY REFERENCE
    /// phrase or before any phrase (§14.9.4.4 GR5 — "transitive across the parameters that follow them until another BY CONTENT or BY
    /// REFERENCE phrase is encountered"; GR9 a) 1. — "BY REFERENCE is assumed"), and each argument of a function
    /// <paramref name="isUserFunction"/> says is user-defined, whose identifier argument is passed by reference whenever its formal is
    /// — a fact of the function's header, not of the reference, so every one is a candidate. The predicate is the ASKING element's
    /// (§8.4.6.6: a prototype name or the containing definition's own name), which is why the walk records the function-call
    /// arguments untested. Only an operand that IS one identifier is recorded: a subscript, a reference modifier's operands or an
    /// arithmetic expression's terms are SENDING operands of the reference, never passed, and an expression argument is passed by
    /// content. Also recorded, <see cref="ByReferenceOperand.SlotsOnly"/> (kb/Work PB1940): each CALL USING operand passed BY CONTENT
    /// and each CALL RETURNING receiver — claimed only when it is a group whose values ride managed slots, which crosses only as an area.</summary>
    public IEnumerable<ByReferenceOperand> ByReferenceOperands(IParseTree region, Func<string, bool> isUserFunction)
    {
        var scan = ScanOf(region, out var span);
        for (int i = span.OperandStart; i < span.OperandEnd; i++)
        {
            var operand = scan.Operands[i];
            if (operand.UserFunction is null || isUserFunction(operand.UserFunction)) yield return operand;
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

    private sealed class TreeScan
    {
        public readonly List<AddressTarget> Addresses = [];
        public readonly List<ByReferenceOperand> Operands = [];
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
                    if (HeadOf(target) is { } head) Addresses.Add(new AddressTarget(head.Name, head.Qualifiers));
                    break;

                // An INVOKE argument (§14.9.23.2): BY REFERENCE, or written with no BY phrase — which §14.9.23.4 GR6 a) 1.
                // passes BY REFERENCE whenever the formal is BY REFERENCE and the argument is an identifier.
                case Core.InvokeUsingContext invokeUsing:
                    foreach (var ia in invokeUsing.invokeArgument())
                    {
                        IParseTree? operand = ia.REFERENCE() is not null ? ia.receivingIdentifier()?.dataReference()
                            : ia.CONTENT() is null && ia.VALUE() is null ? ia.arithmeticExpression() : null;
                        if (operand is not null && OperandIdentifier(operand) is { } ih)
                            Operands.Add(new ByReferenceOperand(ih.Name, ih.Qualifiers, Invoke: true, SlotsOnly: false));
                    }
                    break;

                case Core.FunctionCallContext fc when fc.functionArgList() is { } fargs && fc.functionName() is { } fname:
                    string function = fname.GetText();
                    foreach (var farg in fargs.functionArgument())
                        if (OperandIdentifier(farg) is { } h)
                            Operands.Add(new ByReferenceOperand(h.Name, h.Qualifiers, Invoke: false, SlotsOnly: false, function));
                    break;

                case Core.CallReturningPhraseContext ret:
                    if (ret.receivingIdentifier().dataReference() is { } rd && HeadOf(rd) is { } rh)
                        Operands.Add(new ByReferenceOperand(rh.Name, rh.Qualifiers, Invoke: false, SlotsOnly: true));
                    break;

                case Core.CallUsingPhraseContext phrase:
                    // The phrase in force (§14.9.4.4 GR5 — transitive until the next BY phrase): null = BY VALUE.
                    bool? byReference = true;
                    foreach (var arg in phrase.callArgument())
                    {
                        IParseTree? operand;
                        if (arg.callByReference() is { } r) { byReference = true; operand = r.receivingIdentifier()?.dataReference(); }
                        else if (arg.callByContent() is { } c) { byReference = false; operand = c.arithmeticExpression(); }
                        else if (arg.callByValue() is not null) { byReference = null; operand = null; }
                        else operand = byReference is null ? null : arg.arithmeticExpression();
                        if (operand is not null && OperandIdentifier(operand) is { } h)
                            Operands.Add(new ByReferenceOperand(h.Name, h.Qualifiers, Invoke: false, SlotsOnly: byReference is false));
                    }
                    break;
            }
        }

        /// <summary>The identifier an operand consists of, or null when the operand is anything more (an expression, a literal):
        /// the OUTERMOST data reference under it, when its text is the whole operand's.</summary>
        private static (string Name, List<string> Qualifiers)? OperandIdentifier(IParseTree operand) =>
            ParseTreeWalk.SelfAndDescendants(operand).OfType<Core.DataReferenceContext>().FirstOrDefault() is { } dref
            && dref.GetText() == operand.GetText()
                ? HeadOf(dref)
                : null;

        private static (string Name, List<string> Qualifiers)? HeadOf(Core.DataReferenceContext dref)
        {
            if (dref.cobolWord() is not { } head) return null;
            var quals = new List<string>();
            foreach (var suffix in dref.dataReferenceSuffix())
                if (suffix.qualification() is { } q) quals.Add(q.cobolWord().GetText());
            return (head.GetText(), quals);
        }
    }
}
