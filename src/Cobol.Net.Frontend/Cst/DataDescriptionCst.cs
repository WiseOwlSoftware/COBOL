// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Cst;

using Core = CobolParserCore;

/// <summary>
/// Typed façade over <see cref="Core.DataDescriptionEntryContext"/> (rearchitecture PHASE 04, Group C) — the
/// surface <c>DataBinder.BindEntry</c> reads instead of raw positional <c>GetText()</c>. Thin, 1:1 with the
/// grammar rule, no semantic state. <see cref="Clauses"/> yields a <see cref="DataDescriptionClauseCst"/> per
/// clause whose TEXT-bearing reads are typed; a caller keeps the presence-only clause predicates (BASED / EXTERNAL
/// / TYPEDEF / JUSTIFIED / SYNC / SIGN / USAGE / OCCURS …) raw via <see cref="DataDescriptionClauseCst.Context"/>
/// so their else-if order and edition-gate side effects stay byte-identical.
/// </summary>
public readonly struct DataDescriptionCst(Core.DataDescriptionEntryContext ctx)
{
    public Core.DataDescriptionEntryContext Context => ctx;

    /// <summary>The level number as an <see cref="int"/>, or <see langword="null"/> when it does not parse
    /// (the caller returns early on null — byte-identical to the <c>int.TryParse</c> guard).</summary>
    public int? Level => int.TryParse(ctx.levelNumber().GetText(), out int n) ? n : null;

    /// <summary>The data-name text, or <see langword="null"/> when the entry is FILLER (no <c>dataName</c>) —
    /// the null vs non-null distinction drives the FILLER test, so this MUST stay nullable.</summary>
    public string? Name => ctx.dataName()?.GetText();

    /// <summary>The clauses of the entry body, in source order (empty when the body has no clause list).</summary>
    public IReadOnlyList<DataDescriptionClauseCst> Clauses
    {
        get
        {
            var list = ctx.dataDescriptionBody().dataDescriptionClauses()?.dataDescriptionClause();
            if (list is null || list.Length == 0) return [];
            var wrapped = new DataDescriptionClauseCst[list.Length];
            for (int i = 0; i < list.Length; i++) wrapped[i] = new DataDescriptionClauseCst(list[i]);
            return wrapped;
        }
    }

    public SourceSpan Span => SourceSpan.Of(ctx);

    public static implicit operator DataDescriptionCst(Core.DataDescriptionEntryContext c) => new(c);
}

/// <summary>Typed façade over one <see cref="Core.DataDescriptionClauseContext"/>. TEXT-bearing accessors are
/// typed (each replacing one <c>GetText()</c> cluster in <c>BindEntry</c>); the presence-only clause predicates
/// stay raw on <see cref="Context"/>.</summary>
public readonly struct DataDescriptionClauseCst(Core.DataDescriptionClauseContext ctx)
{
    /// <summary>The raw clause context — for the presence-only <c>xxxClause() is [not] null</c> predicates.</summary>
    public Core.DataDescriptionClauseContext Context => ctx;

    /// <summary>The PICTURE string text, or <see langword="null"/> when this is not a picture clause.</summary>
    public string? PictureText => ctx.pictureClause()?.PIC_STRING()?.GetText();

    /// <summary>The <c>TYPE IS type-name</c> referenced type-name text (§13.18.57; D17), or null.</summary>
    public string? TypeRefName => ctx.typeClause()?.IDENTIFIER()?.GetText();

    /// <summary>The REDEFINES target's <c>dataReference</c> text, or null.</summary>
    public string? RedefinesTargetName => ctx.redefinesClause()?.dataReference().GetText();

    /// <summary>The USAGE OBJECT REFERENCE class-name text (null = universal, or not an object-reference usage).</summary>
    public string? ObjectClassName => ctx.usageClause()?.usageKeyword()?.objectReferenceUsage()?.className()?.GetText();

    // (The canonical USAGE keyword text and the VALUE-operand normalization stay in DataBinder's shared
    // UsageKeyword / ExtractValue helpers — reused by BOTH BindEntry and DataBinder.Reports.cs, so the façade does
    // NOT fork a second copy of that computation. This clause façade exposes only the leaf text reads BindEntry
    // migrated in Group C; the report-writer partial migrates in P7.)

    /// <summary>The OCCURS maximum occurrence count — the LAST integer literal (integer-2 of a <c>n TO m</c>
    /// table, or the sole literal), or <see langword="null"/> when absent/unparseable (§8.5.1.8). Not an occurs
    /// clause → null.</summary>
    public int? OccursMax =>
        ctx.occursClause()?.integerLiteral() is { Length: > 0 } lits && int.TryParse(lits[^1].GetText(), out int n)
            ? n : null;

    /// <summary>The OCCURS INDEXED BY index-name texts, in order (empty when absent). The caller keeps the
    /// <c>INDEXED()</c> presence guard raw.</summary>
    public IReadOnlyList<string> IndexNames
    {
        get
        {
            var idxList = ctx.occursClause()?.dataReferenceList();
            if (idxList is null) return [];
            var refs = idxList.dataReference();
            var names = new string[refs.Length];
            for (int i = 0; i < refs.Length; i++) names[i] = refs[i].GetText();
            return names;
        }
    }

    /// <summary>The first VALUE operand's RAW source text (literal or figurative constant), or null. Numeric-literal
    /// normalization stays binder-side (it is binder logic, not a text read).</summary>
    public string? FirstValueText => ctx.valueClause()?.valueItem().FirstOrDefault()?.GetText();

    public static implicit operator DataDescriptionClauseCst(Core.DataDescriptionClauseContext c) => new(c);
}
