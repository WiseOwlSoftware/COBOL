// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>One storage part of a RENAMES THRU span (kb/Work PB96): a leaf of the record (a REDEFINES view is as
/// good a reader of the storage it overlays as the entry it redefines — NC252A's RDF8-5 THRU RDF8-6 lives inside a
/// double redefinition of an OCCURS 36 table), <paramref name="Occurrence"/> = null for the WHOLE leaf (every
/// occurrence of a table leaf, in order) or the 1-based occurrence the part lies in, and <paramref name="Start"/>
/// (1-based) / <paramref name="Length"/> the character range of that occurrence the alias covers — the whole cell
/// (the ordinary case) or a partial slice when a span boundary lands inside it.</summary>
public sealed record RenamesSpanPart(DataItem Leaf, int? Occurrence, int Start, int Length)
{
    /// <summary>The whole leaf — every occurrence, every character (the composed accessor's fast path).</summary>
    public bool IsWhole => Occurrence is null;

    /// <summary>A part that covers only some characters of its occurrence (renders as the cell's ref-mod view).</summary>
    public bool IsPartial => Occurrence is not null && (Start != 1 || Length != Leaf.ImageWidth);
}

/// <summary>
/// A level-66 RENAMES descriptor (ISO/IEC 1989:2023 §13.18.45): a re-grouping alias over a contiguous run of sibling
/// items, <c>RENAMES data-name-2 [THRU data-name-3]</c>. RENAMES adds no storage — it is a COMPOSED view over the
/// existing fields (GR1 no-THRU = attribute inheritance; GR2 THRU = an alphanumeric group view over the spanned
/// leaves). The names are resolved to items, and the span flattened to its leaves, by the post-build pass.
/// </summary>
public sealed class RenamesInfo
{
    /// <summary>The <c>data-name-2</c> (FROM) operand text as written.</summary>
    public required string FromName { get; init; }

    /// <summary>The <c>data-name-3</c> (THRU) operand text, or <see langword="null"/> for the no-THRU form.</summary>
    public string? ThruName { get; init; }

    /// <summary>The resolved FROM item (set post-build).</summary>
    public DataItem? From { get; set; }

    /// <summary>The resolved THRU item (set post-build; <see langword="null"/> for the no-THRU form).</summary>
    public DataItem? Thru { get; set; }

    /// <summary>The STORAGE parts the THRU span covers, in record storage order (kb/Work PB96): each part is one
    /// NON-redefining leaf of the record and the 1-based character range of it inside the window (a whole leaf is
    /// (1, its width); a boundary that falls inside a leaf — a FROM / THRU that is a partial redefinition of it —
    /// makes a partial part). A REDEFINES view is never a part: it overlays storage the parts already cover.</summary>
    public List<RenamesSpanPart> Span { get; } = [];

    /// <summary>The leaves the rename spans (the parts' leaves) — the strong-type check's view.</summary>
    public IEnumerable<DataItem> SpanLeaves => Span.Select(p => p.Leaf);

    /// <summary>True for a single no-THRU alias (Tier A forward, GR1); false for a THRU span (a Tier-B composition, GR2).</summary>
    public bool IsAlias => ThruName is null;
}

/// <summary>
/// The overlay tier of a redefines class (COBOLNET_DESIGN §4.2; priority cascade D &gt; C &gt; B &gt; A, lattice
/// A ⊑ B ⊑ C ⊑ D, join = max tier). Every member of a class shares one stored canonical backing; the tier decides
/// what that backing is and how each view reads/writes it.
/// </summary>
public enum RedefinesTier
{
    /// <summary>A — identical storage type (same PIC+USAGE, or numeric-over-numeric of the same digit count, or
    /// RENAMES no-THRU): one typed field; every other name is a pass-through over it (a numeric view carries its own
    /// scale/profile, so the shared unscaled value reinterprets for free).</summary>
    Alias,
    /// <summary>B — the whole class is USAGE DISPLAY (alphanumeric / DISPLAY-numeric / numeric-edited / alphabetic):
    /// the canonical is ONE <see cref="string"/> of class-max width; each view is a typed (offset,width) accessor
    /// over it. No bytes. The dominant real case.</summary>
    StringCanonical,
    /// <summary>C — a genuine mixed-USAGE pun (a COMP/COMP-1/2/3/5/INDEX leaf observed cross-view): the canonical is
    /// ONE class-scoped <c>byte[]</c>; each leaf is a typed codec accessor over a (offset,length,usage) window.</summary>
    ByteCanonical,
    /// <summary>D — spec-forbidden / unmodelable (object/pointer/strongly-typed SR12/14; OCCURS DEPENDING ON /
    /// variable-length SR5/17): a loud diagnostic, which is conformant since these are already illegal.</summary>
    Rejected,
}

/// <summary>
/// A storage-sharing equivalence class produced by the post-build classification pass: the items that overlay one
/// storage area (ISO §13.18.44 — a REDEFINES original + every entry that redefines it, directly or transitively).
/// Exactly ONE member is the stored canonical (<see cref="Canonical"/>); every other member is a computed view over
/// the single backing (COBOLNET_DESIGN §4.1 — never two stored fields per area).
/// </summary>
public sealed class RedefinesClass
{
    /// <summary>The non-redefining anchor (the original; SR7/SR11) — the one stored member.</summary>
    public required DataItem Canonical { get; init; }

    /// <summary>The class members (canonical + every redefiner), in source order.</summary>
    public List<DataItem> Members { get; } = [];

    /// <summary>The overlay tier (decides the backing kind + the view accessors). Written ONLY through
    /// <see cref="Classify"/> (P5.11d).</summary>
    public RedefinesTier Tier { get; private set; }

    /// <summary>The class-max image width — characters for <see cref="RedefinesTier.StringCanonical"/>, bytes for
    /// <see cref="RedefinesTier.ByteCanonical"/> (a level-01 non-EXTERNAL original may be redefined larger, SR8).
    /// Written ONLY through <see cref="Classify"/> (P5.11d).</summary>
    public int Width { get; private set; }

    /// <summary>The C# name of the single stored backing field for a Tier-B/Tier-C class.</summary>
    public string BackingCsName => "_redef_" + Canonical.CsName;

    /// <summary>For a BASED class (Phase-4b increment 2, ISO §13.18.5): the C# name of the implicit
    /// data-address pointer field (<c>__addr_X</c>). The backing "field" is then a deref bridge property
    /// (<c>ref CobolPtr.Deref(__addr_X, width).Ref</c>) and every view's window offset is displaced by the
    /// pointer's runtime offset (<c>CobolPtr.OffsetOf</c>) — the ONE place-construction site adds it. Null
    /// for ordinary (stored / external-cell / addressable-cell) classes.</summary>
    public string? BasedPointerField { get; set; }

    /// <summary>The loud-reject reason when <see cref="Tier"/> is <see cref="RedefinesTier.Rejected"/>, else null.
    /// Written ONLY through <see cref="Classify"/> (P5.11d).</summary>
    public string? RejectReason { get; private set; }

    /// <summary>THE one verdict-application site (P5.11d — single-source the tier verdict, DESIGN-data-model §2.3):
    /// exactly two callers exist. <c>DataBinder.ClassifyRedefinesClasses</c> applies the §13.18.44 REDEFINES
    /// verdict once per class (its <c>ComputeTier</c> reason table carries the ISO citations — the
    /// float/COMP-5/BINARY-*/INDEX Tier-C island per §13.18.60, the 2-byte national overlay per D-N1/D-N2, the
    /// dynamic-table reject per §13.18.44 SR5). <c>DataBinder.ForceStringCanonical</c> — the ONE cell-backing
    /// forcer — may RE-classify an already-classified class: EXTERNAL/BASED/ADDRESS-OF re-basing deliberately
    /// overrides the stored-member verdict with the cell-backed Tier-B form (§13.18.22.4 GR5), or rejects when a
    /// leaf has no single-byte character image. No other writer exists; a new tier decision goes through one of
    /// those two, never a scattered assignment.</summary>
    internal void Classify(RedefinesTier tier, int width, string? rejectReason)
    {
        Tier = tier;
        Width = width;
        RejectReason = rejectReason;
    }
}
