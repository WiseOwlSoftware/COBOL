// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary><c>INITIALIZE</c> (ISO §14.9.20), expanded at BIND time into the spec's series of implicit elementary
/// MOVEs (§14.9.20 GR4) — there is no runtime INITIALIZE: each action is one per-elementary store (the full MOVE
/// conversion/editing/padding/truncation rules apply at emit, through the ONE MOVE path) or a per-occurrence loop
/// over a table dimension (GR5b2 — every occurrence of a table element is a possible receiving operand). Multiple
/// identifier-1 expand in source order as separate statements (GR3); elementary receivers within a group appear in
/// definition order (GR8).</summary>
public sealed record BoundInitialize(IReadOnlyList<InitializeAction> Actions) : BoundStatement;

/// <summary>One step of an expanded INITIALIZE.</summary>
public abstract record InitializeAction;

/// <summary>One implicit elementary MOVE (ISO §14.9.20 GR4): <paramref name="Source"/> stores into
/// <paramref name="Target"/> under the MOVE rules (§14.9.25 — conversion, editing, JUSTIFIED/padding, truncation).</summary>
public sealed record InitializeStore(Place Target, BoundOperand Source) : InitializeAction;

/// <summary>The per-occurrence expansion of ONE OCCURS dimension (ISO §14.9.20 GR5b2): the body repeats for
/// <paramref name="Var"/> = 1‥<paramref name="Count"/>; nested dimensions nest loops, outermost first (the loop
/// variable is spliced into each body place's subscript position).</summary>
public sealed record InitializeLoop(string Var, int Count, IReadOnlyList<InitializeAction> Body) : InitializeAction;

/// <summary>A receiver the binder could not materialize as a typed place — the backend emits a loud runtime
/// guard (COBOLNET_DESIGN §1.4), never a silent skip.</summary>
public sealed record InitializeErrorAction(string Feature) : InitializeAction;

/// <summary>The INITIALIZE data categories (ISO §14.9.20.2 category-name, per §8.5.2 class/category) — the
/// COBOL-85 five. The 2002+ categories (BOOLEAN, NATIONAL[-EDITED], DATA-/FUNCTION-/PROGRAM-POINTER,
/// OBJECT-REFERENCE; MESSAGE-TAG 2023) arrive with their lexer tokens in the edition-gated grammar fragments.</summary>
public enum InitializeCategory { Alphabetic, Alphanumeric, AlphanumericEdited, Numeric, NumericEdited }

public sealed partial class StatementBinder
{
    private int _initializeLoopVar;   // program-unique loop-variable counter (__iniN — `__` cannot occur in COBOL names)

    /// <summary>What the statement's phrases select, threaded through the expansion: the receiver filter
    /// (ISO §14.9.20 GR5c) and the sending-operand precedence VALUE → REPLACING → category default (GR6).
    /// <paramref name="ValueCategory"/> null with <paramref name="ToValue"/> means <c>ALL TO VALUE</c> (a bare
    /// <c>TO VALUE</c> ≡ ALL, §14.9.20.2 note 2).</summary>
    private readonly record struct InitializeSpec(
        bool WithFiller, bool ToValue, InitializeCategory? ValueCategory,
        IReadOnlyList<(InitializeCategory Cat, BoundOperand Value)> Replacements, bool ToDefault);

    /// <summary>Bind INITIALIZE (ISO §14.9.20). The COBOL-85 surface — identifier-1‥n (full data references:
    /// qualification AND subscripts) and the REPLACING phrase — binds completely; the 2002+ phrases (WITH FILLER,
    /// [ALL | category] TO VALUE, the THEN connective, THEN TO DEFAULT — Annex E additions) are edition-gated and
    /// bind for 2002+ targets. The whole receiver expansion happens here; the emitter renders each store through
    /// the canonical MOVE path.</summary>
    private BoundStatement BindInitialize(Core.InitializeStatementContext ini)
    {
        bool withFiller = ini.FILLER() is not null;
        var toValue = ini.initializeCategoryToValue();
        var replacing = ini.initializeReplacingPhrase();
        bool toDefault = ini.initializeDefaultPhrase() is not null;

        if (data.Edition.DialectLevel < 2002)
        {
            // The post-85 surface was introduced by ISO/IEC 1989:2002 (§14.9.20 / Annex E) — rejected at --std 85.
            string need = $"it requires --std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})";
            if (withFiller)
                data.Edition.Error("COBOLNET0830", $"INITIALIZE … WITH FILLER — the FILLER phrase was introduced by ISO/IEC 1989:2002 (§14.9.20); {need}");
            if (toValue is not null)
                data.Edition.Error("COBOLNET0831", $"INITIALIZE … TO VALUE — the VALUE phrase was introduced by ISO/IEC 1989:2002 (§14.9.20); {need}");
            if (toDefault)
                data.Edition.Error("COBOLNET0832", $"INITIALIZE … TO DEFAULT — the DEFAULT phrase was introduced by ISO/IEC 1989:2002 (§14.9.20); {need}");
            if (replacing?.THEN() is not null)
                data.Edition.Error("COBOLNET0833", $"INITIALIZE … THEN REPLACING — the THEN connective was introduced by ISO/IEC 1989:2002 (§14.9.20); {need}");
        }

        var replacements = new List<(InitializeCategory Cat, BoundOperand Value)>();
        if (replacing is not null)
            foreach (var item in replacing.initializeReplacingItem())
            {
                InitializeCategory cat = InitializeCategoryOf(item.initializeCategory());
                if (replacements.Any(r => r.Cat == cat))
                {
                    // ISO §14.9.20.3 SR6: the same category shall not be repeated in a REPLACING phrase.
                    data.Edition.Error("COBOLNET0834",
                        $"INITIALIZE REPLACING repeats category {cat} (ISO §14.9.20.3 SR6 — each category at most once)");
                    continue;
                }
                BoundOperand value = item.literal() is { } lit ? LiteralOperand(lit)
                    : item.dataReference() is { } sref ? FieldOperand(sref)
                    : new BoundOperandError("INITIALIZE REPLACING sending operand");
                replacements.Add((cat, value));
            }

        var spec = new InitializeSpec(withFiller, toValue is not null,
            toValue?.initializeCategory() is { } vc ? InitializeCategoryOf(vc) : null, replacements, toDefault);

        var actions = new List<InitializeAction>();
        foreach (var dref in ini.dataReferenceList().dataReference())
            BindInitializeTarget(dref, spec, actions);   // GR3 — per identifier-1, in source order
        return new BoundInitialize(actions);
    }

    /// <summary>Expand one identifier-1 (ISO §14.9.20 GR5): resolve its FULL data reference (qualification +
    /// subscripts — the legacy binder's name-only resolution was a gap, not behavior), then walk its subtree in
    /// definition order (GR8) collecting the per-elementary stores. identifier-1 itself MAY have / sit under a
    /// REDEFINES (GR5a3's exclusion applies only BELOW it).</summary>
    private void BindInitializeTarget(Core.DataReferenceContext dref, in InitializeSpec spec, List<InitializeAction> actions)
    {
        if (refs.Resolve(dref) is not { } place)
        {
            // ISO §14.9.20.3 SR5: identifier-1 shall not have a RENAMES clause (a level-66 entry — NC401M territory).
            string name = dref.cobolWord()?.GetText() ?? dref.GetText();
            if (data.ByName.TryGetValue(name, out var named) && named.Any(i => i.Renames is not null))
                data.Edition.Error("COBOLNET0835",
                    $"INITIALIZE '{name}' — identifier-1 shall not have a RENAMES clause (ISO §14.9.20.3 SR5)");
            actions.Add(new InitializeErrorAction($"INITIALIZE target '{dref.GetText()}'"));
            return;
        }
        if (place is RefModPlace)
        {
            // A reference-modified identifier-1 is the single receiver, of category alphanumeric (ISO §8.4.2.4 —
            // a reference-modifier defines a unique alphanumeric data item); no VALUE clause attaches to it.
            if (InitializeSender(InitializeCategory.Alphanumeric, rawValue: null, spec) is { } src)
                actions.Add(new InitializeStore(place, src));
            return;
        }
        InitializeCursor? cursor = place switch
        {
            MemberPlace mp => new InitializeMemberCursor(mp.Path, mp.MemberItem),
            RedefViewPlace rv => new InitializeViewCursor(rv.Backing, rv.OffsetExpr, rv.ViewItem.ClassOffset, rv.ViewItem, ""),
            _ => null,
        };
        if (cursor is null)
        {
            actions.Add(new InitializeErrorAction($"INITIALIZE target '{dref.GetText()}' (unsupported place kind)"));
            return;
        }
        ExpandInitialize(cursor, spec, actions, identifier1: true);
    }

    /// <summary>The recursive receiver walk (ISO §14.9.20 GR5), in definition order (GR8). Exclusions: GR5a2 —
    /// an explicit-or-implicit FILLER elementary item (a null <see cref="DataItem.CobolName"/>) unless WITH FILLER
    /// (identifier-1 itself is always named, so the test applies only to contained items); GR5a3 — a subordinate
    /// whose entry has REDEFINES, and with it its whole subtree (level-66 entries are not storage children,
    /// §13.18.45); GR5a1 — items that are not valid MOVE receivers (an index data item, §14.9.25.3 SR). A child
    /// with an OCCURS clause expands one loop per dimension (GR5b2 — every occurrence).</summary>
    private void ExpandInitialize(InitializeCursor cur, in InitializeSpec spec, List<InitializeAction> actions,
        bool identifier1 = false)
    {
        DataItem item = cur.Item;
        if (item.IsElementary)
        {
            if (!identifier1 && item.CobolName is null && !spec.WithFiller) return;            // GR5a2
            if (InitializeItemCategory(item) is not { } cat) return;                            // GR5a1
            if (InitializeSender(cat, item.RawValue, spec) is not { } source) return;           // GR5c — left unchanged
            actions.Add(new InitializeStore(cur.ToPlace(), source));
            return;
        }
        if (!item.IsGroup) return;
        foreach (var child in item.Children)
        {
            if (!(child.IsGroup || child.IsElementary)) continue;                               // no storage
            if (child.RedefinesTargetName is not null || child.Renames is not null) continue;   // GR5a3
            if (cur.Child(child) is not { } childCur)
            {
                actions.Add(new InitializeErrorAction(
                    $"INITIALIZE receiver '{child.CobolName ?? "FILLER"}' (unwired REDEFINES storage tier)"));
                continue;
            }
            if (child.Occurs is { } n)
            {
                string v = $"__ini{_initializeLoopVar++}";
                var body = new List<InitializeAction>();
                ExpandInitialize(childCur.Indexed(v), spec, body);
                if (body.Count > 0) actions.Add(new InitializeLoop(v, n, body));                // GR5b2
            }
            else
                ExpandInitialize(childCur, spec, actions);
        }
    }

    /// <summary>The sending operand for one possible receiver, or null when it is NOT a receiving operand and is
    /// left unchanged (ISO §14.9.20 GR5c — e.g. <c>REPLACING NUMERIC…</c> alone touches no non-numeric item).
    /// Precedence (GR6): the VALUE phrase (category match + a data-item-format VALUE clause, GR5c1b/GR6a3) → the
    /// REPLACING operand of the matching category (GR6b) → the category default (GR6c — figurative ZEROES for
    /// numeric/numeric-edited [the EDITED zero through MOVE editing, never spaces], alphanumeric SPACES for
    /// alphabetic/alphanumeric/alphanumeric-edited), the default applying only under TO DEFAULT or when neither
    /// VALUE nor REPLACING is specified (GR5c3/c4 — the bare-85 form defaults everything not excluded).</summary>
    private static BoundOperand? InitializeSender(InitializeCategory cat, string? rawValue, in InitializeSpec spec)
    {
        if (spec.ToValue && (spec.ValueCategory is null || spec.ValueCategory == cat) && rawValue is { } raw)
            return InitializeValueOperand(raw);
        foreach (var (rcat, value) in spec.Replacements)
            if (rcat == cat) return value;
        if (spec.ToDefault || (!spec.ToValue && spec.Replacements.Count == 0))
            return cat is InitializeCategory.Numeric or InitializeCategory.NumericEdited
                ? new BoundFigurative('Z')
                : new BoundFigurative('S');
        return null;
    }

    /// <summary>The INITIALIZE category of an elementary item (ISO §8.5.2 via §14.9.20.2 category-name), or null
    /// when the item is excluded (GR5a1 — an index data item is not a valid MOVE receiver; only SET stores it,
    /// §14.9.39 GR2b). Alphanumeric-edited = an X/A picture with insertion symbols; alphabetic = an all-A picture
    /// (the COBOL-85 alphabetic-edited category folds into alphanumeric-edited per the 2023 categorization).</summary>
    private static InitializeCategory? InitializeItemCategory(DataItem item) => item.Pic switch
    {
        { Usage: Usage.Index } => null,
        { Category: PicCategory.Numeric } => InitializeCategory.Numeric,
        { Category: PicCategory.NumericEdited } => InitializeCategory.NumericEdited,
        { Category: PicCategory.Alphanumeric, EditMask: not null } => InitializeCategory.AlphanumericEdited,
        { Category: PicCategory.Alphanumeric, IsAlphabetic: true } => InitializeCategory.Alphabetic,
        { Category: PicCategory.Alphanumeric } => InitializeCategory.Alphanumeric,
        _ => null,
    };

    /// <summary>Decode a grammar category-name, handling BOTH the two-token (<c>ALPHANUMERIC EDITED</c> /
    /// <c>NUMERIC EDITED</c>) and the hyphenated one-token (<c>ALPHANUMERIC-EDITED</c> / <c>NUMERIC-EDITED</c>)
    /// spellings (ISO §14.9.20.2 writes the hyphenated form; the corpus uses both).</summary>
    private static InitializeCategory InitializeCategoryOf(Core.InitializeCategoryContext cat) =>
        cat.ALPHABETIC() is not null ? InitializeCategory.Alphabetic
        : cat.ALPHANUMERIC_EDITED() is not null ? InitializeCategory.AlphanumericEdited
        : cat.NUMERIC_EDITED() is not null ? InitializeCategory.NumericEdited
        : cat.ALPHANUMERIC() is not null
            ? (cat.EDITED() is not null ? InitializeCategory.AlphanumericEdited : InitializeCategory.Alphanumeric)
        : cat.EDITED() is not null ? InitializeCategory.NumericEdited
        : InitializeCategory.Numeric;

    /// <summary>The bound sending operand for a VALUE-qualified receiver (ISO §14.9.20 GR6a3 — "a literal that,
    /// when moved to the receiving-operand with a MOVE statement, produces the same result as … the VALUE clause"):
    /// the raw VALUE text decodes to the figurative / ALL-literal / string / numeric operand the MOVE path already
    /// renders, so TO VALUE re-produces the program-start state through MOVE semantics.</summary>
    private static BoundOperand InitializeValueOperand(string raw)
    {
        string t = raw.Trim();
        if (InitializeFigurativeKind(t) is { } kind) return new BoundFigurative(kind);
        if (t.StartsWith("ALL", StringComparison.OrdinalIgnoreCase) && t.Length > 3)
        {
            string rest = t[3..].TrimStart();
            // ALL "literal" repeats to the receiver width (§8.3.3.6.4 GR2); ALL <figurative-word> ≡ the bare word.
            if (rest.Length >= 2 && rest[0] == '"' && rest[^1] == '"') return new BoundAllLiteral(DecodeCobolString(rest));
            if (InitializeFigurativeKind(rest) is { } k) return new BoundFigurative(k);
        }
        if (t.Length >= 2 && t[0] == '"' && t[^1] == '"') return new BoundStringLiteral(DecodeCobolString(t));
        return new BoundNumericLiteral(t);
    }

    /// <summary>A figurative-constant word's <see cref="BoundFigurative"/> kind, or null when the text is not a
    /// figurative word (ISO §8.3.1.2 — the singular/plural forms are identical).</summary>
    private static char? InitializeFigurativeKind(string word) => word.ToUpperInvariant() switch
    {
        "ZERO" or "ZEROS" or "ZEROES" => 'Z',
        "SPACE" or "SPACES" => 'S',
        "HIGH-VALUE" or "HIGH-VALUES" => 'H',
        "LOW-VALUE" or "LOW-VALUES" => 'L',
        "QUOTE" or "QUOTES" => 'Q',
        "NULL" or "NULLS" => 'N',
        _ => null,
    };

    // ── Receiver cursors: extend the RESOLVED identifier-1 place member-by-member ────────────────────────────

    /// <summary>A bind-time lvalue cursor over the receiving subtree: it extends the already-resolved identifier-1
    /// <see cref="Place"/> (so a qualified / subscripted identifier-1 expands from its built access path — no
    /// re-resolution), materializing each elementary receiver as a typed place. <see cref="Child"/> returns null
    /// for a storage form not yet wired (the caller fails loud).</summary>
    private abstract record InitializeCursor(DataItem Item)
    {
        public abstract InitializeCursor? Child(DataItem child);
        public abstract InitializeCursor Indexed(string indexVar);
        public abstract Place ToPlace();
    }

    /// <summary>A plain member-access cursor, mirroring <c>ReferenceResolver.AccessPath</c>: <c>CsName</c> segments
    /// chained with <c>.</c>, each OCCURS level routed through the ref-returning <c>CobolTable.At</c> (benign
    /// subscripting, ISO §8.4.2.3.4 GR2). Entering a Tier-B (string-canonical) REDEFINES class — whose ONE stored
    /// string backing lives in the containing struct (COBOLNET_DESIGN §4.2) — switches to a
    /// <see cref="InitializeViewCursor"/>; an unwired Tier-C / Rejected class yields null (loud).</summary>
    private sealed record InitializeMemberCursor(string Path, DataItem Item) : InitializeCursor(Item)
    {
        public override InitializeCursor? Child(DataItem child)
        {
            if (child.Class is { } cls)
            {
                if (cls.Tier == RedefinesTier.StringCanonical && child.IsCanonical)
                    return new InitializeViewCursor($"{Path}.{cls.BackingCsName}",
                        child.ClassOffset.ToString(), child.ClassOffset, child, "");
                if (cls.Tier != RedefinesTier.Alias || !child.IsCanonical) return null;
            }
            return new InitializeMemberCursor($"{Path}.{child.CsName}", child);
        }

        public override InitializeCursor Indexed(string indexVar) =>
            this with { Path = $"CobolTable.At({Path}, {indexVar})" };

        public override Place ToPlace() => new MemberPlace(Path, Item);
    }

    /// <summary>A cursor inside a Tier-B REDEFINES class: every receiver is a (offset, width) character window over
    /// the class's ONE string backing. The window offset = the entry place's offset expression + (this item's
    /// in-class offset − the entry item's) + Σ (indexVar − 1) × per-occurrence width for each OCCURS level crossed
    /// (ISO §13.18.44 — a redefined table lays its occurrences end-to-end in the one backing; the same arithmetic
    /// as <c>ReferenceResolver.PlaceForItem</c>).</summary>
    private sealed record InitializeViewCursor(
        string Backing, string BaseExpr, int BaseOffset, DataItem Item, string OccursTerms) : InitializeCursor(Item)
    {
        public override InitializeCursor? Child(DataItem child) =>
            ReferenceEquals(child.Class, Item.Class) ? this with { Item = child } : null;

        public override InitializeCursor Indexed(string indexVar) =>
            this with { OccursTerms = $"{OccursTerms} + ({indexVar} - 1) * {Item.ImageWidth}" };

        public override Place ToPlace()
        {
            int delta = Item.ClassOffset - BaseOffset;
            return new RedefViewPlace(Backing,
                $"{BaseExpr}{(delta != 0 ? $" + {delta}" : "")}{OccursTerms}", Item.ImageWidth, Item);
        }
    }
}
