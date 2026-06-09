// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Resolves a <c>dataReference</c> parse node to a <see cref="Place"/> — the single entry point every verb uses to
/// turn a COBOL operand into a typed C# lvalue (COBOLNET_DESIGN §3.4). Two phases:
/// <list type="number">
///   <item><b>Syntactic flatten</b> — walk <c>cobolWord dataReferenceSuffix*</c> into the base name, its OF/IN
///         qualifiers, and the subscript / reference-modification token group (a flat SUBSCRIPT-mode stream the
///         binding layer interprets — the same grammar shape the legacy compiler proved over 364 NIST tests).</item>
///   <item><b>Semantic resolve</b> — resolve the (optionally qualified) name to a <see cref="DataItem"/>, interpret
///         the subscripts to C# index expressions, and build the member-access path with each subscript attached to
///         its OCCURS level (outer→inner).</item>
/// </list>
/// Returns <see langword="null"/> when the reference cannot be resolved in this slice — an unknown name, a special
/// register, a reference-modified reference (<c>(s:l)</c> — G2-1c), or a subscript form not yet handled — so the
/// caller emits a loud not-implemented guard rather than silently mis-binding.
/// </summary>
public sealed class ReferenceResolver(DataBinder data)
{
    /// <summary>Resolve <paramref name="dref"/> to a <see cref="Place"/>, or <see langword="null"/> if unsupported here.</summary>
    public Place? Resolve(Core.DataReferenceContext dref)
    {
        // Special registers (LINAGE-/LINE-/PAGE-COUNTER) have no cobolWord base — not handled in this slice.
        if (dref.cobolWord() is not { } baseWord) return null;
        string name = baseWord.GetText();

        var qualifiers = new List<string>();
        Core.SubscriptOrRefModContext? subCtx = null;    // the subscript group (no depth-0 colon)
        Core.SubscriptOrRefModContext? refCtx = null;    // a reference-modification group (start : length)
        bool cleanRefMod = false;                        // the refModPart form (arithmeticExpression : …) — deferred

        void Classify(Core.SubscriptOrRefModContext s) { if (HasDepth0Colon(s)) refCtx ??= s; else subCtx ??= s; }

        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q)
            {
                qualifiers.Add(q.cobolWord().GetText());
                foreach (var sp in q.subscriptPart()) if (sp.subscriptOrRefMod() is { } qs) Classify(qs);
                if (q.refModPart().Length > 0) cleanRefMod = true;
            }
            else if (suffix.refModPart() is not null) cleanRefMod = true;
            else if (suffix.subscriptPart()?.subscriptOrRefMod() is { } s) Classify(s);
        }
        if (cleanRefMod) return null;   // the parsed-arithmetic refModSpec form is a later slice → loud

        DataItem? item = qualifiers.Count > 0 ? ResolveQualified(name, qualifiers) : ResolveUnqualified(name);
        if (item is null) return null;

        List<string> indexExprs = [];
        if (subCtx is not null)
        {
            var (e, isRefMod) = InterpretSubscripts(subCtx);
            if (isRefMod || e is null) return null;   // unsupported subscript form → loud
            indexExprs = e;
        }

        // RENAMES (level 66) resolution lands in a later slice → loud for now (never emit an invalid member access).
        if (item.Renames is not null) return null;

        Place inner;
        if (item.Class is { Tier: RedefinesTier.StringCanonical } sc)
        {
            // Tier B (ISO §13.18.44; COBOLNET_DESIGN §4.2): a typed (offset,width) window over the class's ONE string
            // backing — the canonical too (so exactly one stored member). A subscripted Tier-B view (REDEFINES inside
            // an OCCURS element) is a later slice → only the unsubscripted form here.
            if (indexExprs.Count > 0) return null;
            // The backing is emitted in the canonical's containing struct (FieldEmitter.PhysicalFields), so a NESTED
            // class's backing must be reached through that struct's access path — a bare `_redef_X` resolves only for a
            // top-level (static-field) class. Fail loud if the parent path is unavailable (e.g. it is itself within an
            // OCCURS), rather than emit an unqualified reference that does not exist in scope.
            if (BackingPath(sc) is not { } backing) return null;
            if (item.IsGroup) data.WholeGroupReferenced.Add(item);
            inner = new RedefViewPlace(backing, item.ClassOffset, item.ImageWidth, item);
        }
        else
        {
            // A Tier-A view forwards to the canonical's ONE stored field — the place carries the VIEW's DataItem (so
            // its own Pic/scale/profile drive interpretation) over the canonical's access path, so a numeric view
            // reinterprets the shared unscaled value via its own scale. A not-yet-wired (Tier-C) / Rejected view is loud.
            if (item.Class is { } cls && !item.IsCanonical && cls.Tier != RedefinesTier.Alias)
                return null;
            DataItem accessItem = item.Class is { Tier: RedefinesTier.Alias } ac && !item.IsCanonical
                ? ac.Canonical : item;
            // An unsubscripted reference to an OCCURS table (whole-table op) is a later slice → AccessPath null → loud.
            if (AccessPath(accessItem, indexExprs) is not { } path) return null;
            // A group name can only be used as a whole operand (MOVE/DISPLAY/compare) — record it so the whole-group
            // analysis can decide which numeric-DISPLAY leaves must store their character image (§14.9 MOVE GR4).
            if (item.IsGroup) data.WholeGroupReferenced.Add(item);
            inner = new MemberPlace(path, item);
        }

        if (refCtx is null) return inner;
        // Reference modification is over a character string — alphanumeric / numeric-edited items (incl. a Tier-B view).
        if (item.Pic?.Category is not (PicCategory.Alphanumeric or PicCategory.NumericEdited)) return null;
        var (rm, _) = InterpretSubscripts(refCtx);
        return rm is { Count: > 0 } ? new RefModPlace(inner, rm[0], rm.Count > 1 ? rm[1] : null) : null;
    }

    /// <summary>A <see cref="Place"/> for an already-resolved item with no subscripts (e.g. a level-88's parent),
    /// or <see langword="null"/> if the item is within an OCCURS table (a subscripted reference is then required).</summary>
    public Place? ResolveItem(DataItem item) =>
        AccessPath(item, []) is { } path ? new MemberPlace(path, item) : null;

    /// <summary>The qualified C# access path to a Tier-B/Tier-C class's single stored backing field. The backing is
    /// emitted in the canonical's containing struct, so a NESTED class reaches it through that struct's path
    /// (<c>OUTER.GROUP._redef_X</c>); a top-level class's backing is the bare static field (<c>_redef_X</c>). Returns
    /// <see langword="null"/> when the containing path is unavailable (the canonical is within an OCCURS table).</summary>
    private static string? BackingPath(RedefinesClass cls) =>
        cls.Canonical.Parent is not { } parent ? cls.BackingCsName
        : AccessPath(parent, []) is { } parentPath ? parentPath + "." + cls.BackingCsName
        : null;

    // ── Name resolution ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The item an unqualified name resolves to (first match; COBOL requires qualification to disambiguate).</summary>
    private DataItem? ResolveUnqualified(string name) =>
        data.ByName.TryGetValue(name, out var list) && list.Count > 0 ? list[0] : null;

    /// <summary>
    /// Resolve a qualified reference <c>name OF q[0] OF q[1] …</c> by right-to-left narrowing: resolve the
    /// outermost qualifier, walk inward through each qualifier, then find <paramref name="name"/> within the
    /// innermost qualifier's subtree (ISO §8.4.3.3 qualification).
    /// </summary>
    private DataItem? ResolveQualified(string name, List<string> qualifiers)
    {
        DataItem? scope = ResolveUnqualified(qualifiers[^1]);            // outermost qualifier
        for (int k = qualifiers.Count - 2; k >= 0 && scope is not null; k--)
            scope = FindDescendant(scope, qualifiers[k]);
        return scope is null ? null : FindDescendant(scope, name);
    }

    /// <summary>Find a descendant (direct or nested) of <paramref name="scope"/> with COBOL name <paramref name="name"/>.</summary>
    private static DataItem? FindDescendant(DataItem scope, string name)
    {
        foreach (var child in scope.Children)
        {
            if (string.Equals(child.CobolName, name, StringComparison.OrdinalIgnoreCase)) return child;
            if (FindDescendant(child, name) is { } found) return found;
        }
        return null;
    }

    // ── Access-path construction (subscripts attach to OCCURS levels, outer→inner) ───────────────────────

    /// <summary>
    /// The C# member-access path for an item: a static field at the root, else <c>Parent.Child</c> chained, with
    /// each <paramref name="indexExprs"/> entry inserted as <c>[expr - 1]</c> at its OCCURS level (outermost first).
    /// Returns <see langword="null"/> if the subscript count does not match the table's OCCURS dimension.
    /// </summary>
    private static string? AccessPath(DataItem item, IReadOnlyList<string> indexExprs)
    {
        var chain = new List<DataItem>();
        for (DataItem? n = item; n is not null; n = n.Parent) chain.Add(n);
        chain.Reverse();   // root-first

        int occursLevels = chain.Count(n => n.Occurs is not null);
        if (occursLevels != indexExprs.Count) return null;   // wrong number of subscripts

        var sb = new System.Text.StringBuilder();
        int si = 0;
        foreach (var seg in chain)
        {
            sb.Append(sb.Length == 0 ? seg.CsName : "." + seg.CsName);
            if (seg.Occurs is not null) sb.Append($"[{indexExprs[si++]} - 1]");
        }
        return sb.ToString();
    }

    // ── Subscript interpretation (the flat SUBSCRIPT-mode token stream) ──────────────────────────────────

    /// <summary>
    /// Interpret the flat subscript/ref-mod token sequence. Returns (index expressions, isRefMod): a depth-0
    /// <c>SUB_COLON</c> marks reference modification (handled in a later slice, so the C# list is null). Otherwise
    /// each comma- or multi-space-separated segment is rendered to a C# <c>long</c> index expression; a segment that
    /// cannot be rendered yields a null list (→ the caller fails loud).
    /// </summary>
    /// <summary>True if the flat token stream has a depth-0 <c>SUB_COLON</c> — i.e. it is a reference modification
    /// (<c>start:length</c>) rather than a subscript list.</summary>
    private static bool HasDepth0Colon(Core.SubscriptOrRefModContext ctx)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) return true;
        }
        return false;
    }

    private (List<string>? Exprs, bool IsRefMod) InterpretSubscripts(Core.SubscriptOrRefModContext ctx)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(ctx, tokens);

        int colonIdx = -1;
        for (int i = 0, d = 0; i < tokens.Count; i++)
        {
            int tt = tokens[i].Type;
            if (tt == Core.SUB_LPAREN) d++;
            else if (tt == Core.SUB_RPAREN) { if (d > 0) d--; }
            else if (tt == Core.SUB_COLON && d == 0) { colonIdx = i; break; }
        }
        if (colonIdx >= 0)   // reference modification: start [: length]
        {
            if (RenderSegment(tokens.GetRange(0, colonIdx)) is not { } start) return (null, true);
            var result = new List<string> { start };
            var lengthTokens = tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1);
            if (lengthTokens.Any(t => t.Type != Core.SUB_WS))
            {
                if (RenderSegment(lengthTokens) is not { } len) return (null, true);
                result.Add(len);
            }
            return (result, true);
        }

        var exprs = new List<string>();
        foreach (var seg in SplitSubscriptTokens(tokens))
        {
            if (RenderSegment(seg) is not { } e) return (null, false);
            exprs.Add(e);
        }
        return (exprs, false);
    }

    private static void CollectLeafTokens(IParseTree node, List<IToken> tokens)
    {
        if (node is ITerminalNode term) { tokens.Add(term.Symbol); return; }
        for (int i = 0; i < node.ChildCount; i++) CollectLeafTokens(node.GetChild(i), tokens);
    }

    /// <summary>Split a flat token list into subscript segments on depth-0 comma / multi-space boundaries (a faithful
    /// reduction of the legacy <c>ExpressionBinder.SplitSubscriptTokens</c>: a single space inside a relative
    /// subscript such as <c>I + 1</c> does not split; a separator space before a new operand does).</summary>
    private static List<List<IToken>> SplitSubscriptTokens(List<IToken> tokens)
    {
        var segments = new List<List<IToken>>();
        var current = new List<IToken>();
        int depth = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Type == Core.SUB_LPAREN) { depth++; current.Add(t); continue; }
            if (t.Type == Core.SUB_RPAREN) { if (depth > 0) depth--; current.Add(t); continue; }

            if (depth == 0 && (t.Type == Core.SUB_COMMA || t.Type == Core.SUB_SEMICOLON))
            {
                if (current.Count > 0) { segments.Add(current); current = []; }
                continue;
            }
            if (depth == 0 && t.Type == Core.SUB_WS)
            {
                int next = i + 1;
                while (next < tokens.Count && tokens[next].Type == Core.SUB_WS) next++;
                if (next < tokens.Count && current.Count > 0)
                {
                    var lastNonWs = current.FindLast(x => x.Type != Core.SUB_WS);
                    bool endsWithOperator = lastNonWs is not null &&
                        lastNonWs.Type is Core.SUB_PLUS or Core.SUB_MINUS or Core.SUB_STAR or Core.SUB_SLASH or Core.SUB_POWER;
                    int nextType = tokens[next].Type;
                    if (!endsWithOperator &&
                        nextType is Core.SIGNED_INTEGERLIT or Core.SUB_IDENTIFIER or Core.SUB_INTEGERLIT)
                    {
                        segments.Add(current);
                        current = [];
                        i = next - 1;   // skip consumed WS
                        continue;
                    }
                }
                current.Add(t);
                continue;
            }
            current.Add(t);
        }
        if (current.Count > 0) segments.Add(current);
        return segments;
    }

    /// <summary>Render one subscript segment to a C# <c>long</c> index expression, or <see langword="null"/> if it
    /// uses a form not yet handled (so the caller fails loud). Handles integer literals, data-name / index-name
    /// references, the arithmetic operators, and parentheses — the relative-subscript and simple-index forms.</summary>
    private string? RenderSegment(List<IToken> tokens)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var t in tokens)
        {
            switch (t.Type)
            {
                case Core.SUB_WS: sb.Append(' '); break;
                case Core.SUB_INTEGERLIT or Core.SIGNED_INTEGERLIT: sb.Append(t.Text); break;
                case Core.SUB_PLUS: sb.Append(" + "); break;
                case Core.SUB_MINUS: sb.Append(" - "); break;
                case Core.SUB_STAR: sb.Append(" * "); break;
                case Core.SUB_SLASH: sb.Append(" / "); break;
                case Core.SUB_LPAREN: sb.Append('('); break;
                case Core.SUB_RPAREN: sb.Append(')'); break;
                case Core.SUB_IDENTIFIER:
                    if (ResolveSubscriptName(t.Text) is not { } readExpr) return null;
                    sb.Append(readExpr);
                    break;
                default: return null;   // SUB_STRINGLIT / SUB_DECIMALLIT / SUB_ALL / OF / IN / FUNCTION etc.
            }
        }
        string expr = sb.ToString().Trim();
        return expr.Length == 0 ? null : expr;
    }

    /// <summary>A subscript data-name → its C# read expression: an INDEXED BY index-name (a <c>long</c> field) or a
    /// numeric data item (its place read), or <see langword="null"/> if it is neither.</summary>
    private string? ResolveSubscriptName(string name)
    {
        if (data.IndexFields.TryGetValue(name, out var field)) return field;
        if (ResolveUnqualified(name) is { } item) return AccessPath(item, []);
        return null;
    }
}
