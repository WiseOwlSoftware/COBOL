// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>The file organization (ISO/IEC 1989:2023 §12.4.5.10). This slice implements the two sequential shapes;
/// relative/indexed are later G5 slices (registered loud).</summary>
public enum FileOrganization { Sequential, LineSequential, Relative, Indexed }

/// <summary>The access mode (ISO §12.4.5.3). Sequential is the default and the only mode the sequential slice needs.</summary>
public enum FileAccessMode { Sequential, Random, Dynamic }

/// <summary>The SHARING mode (ISO §12.4.5.15 / §14.9.27): how OTHER file connectors may access this file while
/// it is open. <c>None</c> = the implementor default (COBOL.NET: ALL OTHER). NoOther = exclusive.</summary>
public enum SharingMode { None, AllOther, NoOther, ReadOnly }

/// <summary>The LOCK MODE granularity (ISO §12.4.5.9): MANUAL (locks acquired only by an explicit WITH LOCK) vs
/// AUTOMATIC (a READ locks the record). <see cref="Multiple"/> = WITH LOCK ON MULTIPLE RECORDS (else single —
/// any I-O except START releases the prior lock, §12.4.5.9 GR6).</summary>
public enum LockKind { None, Manual, Automatic }
public sealed record LockModeInfo(LockKind Kind, bool Multiple);

/// <summary>
/// A bound file connector (COBOLNET_DESIGN §8): the SELECT clause's properties joined with the FD's record
/// description(s). The FD's record area is a typed field — for multiple <c>01</c>s under one FD they SHARE one
/// storage area (ISO §9.1.2 / §13.18 — the file-edge analogue of REDEFINES), modeled by synthesizing each secondary
/// record as a REDEFINES of the first so the existing <see cref="RedefinesClass"/> tier machinery makes them alias
/// (the singular-pattern rule — no second storage-sharing mechanism). A WRITE/READ moves the record's character
/// image across the on-disk edge.
/// </summary>
public sealed class FileModel
{
    /// <summary>The COBOL file-name (the SELECT / FD name). At emission the multi-unit driver QUALIFIES it with
    /// the owning program's path ("PROG::FILE") to namespace the run-unit-global runtime registry — a file
    /// connector is internal to its program (ISO §8.6.3; two IC-suite units both declare PRINT-FILE). Name
    /// resolution is finished by then (bound nodes hold FileModel references), so the rename is emit-only.</summary>
    public required string CobolName { get; set; }

    /// <summary>The file-name EXACTLY as spelled in the SELECT clause (or the FD/SD when no SELECT names it) —
    /// never qualified, never case-folded: FUNCTION EXCEPTION-FILE / EXCEPTION-FILE-N return "the file-name exactly
    /// as specified in the SELECT clause" (ISO §15.28.4 r1c/r2b, §15.29.4 r1c/r2b; kb/Work PB63), so the connector
    /// is registered with THIS name and the runtime never recovers a display name from the registry KEY (whose
    /// bands — <c>PROG::</c>, <c>::EXT::</c> + the uppercased external name, <c>Class::INST::</c> + a per-object
    /// <c>#N</c> — are an emit-side namespace).</summary>
    public string SelectName { get; init; } = "";

    /// <summary>The ASSIGN clause's TO-phrase target text — literal-1's decoded value or device-name-1 (ISO
    /// §12.4.5.3 GR3 a); resolved to a host path at run time by <c>CobolFile.ResolveHostPath</c>. Defaults to the
    /// file-name when no ASSIGN clause is present. ⛔ EMPTY for the bare <c>ASSIGN USING data-name-1</c> form, which
    /// specifies no device-name-1/literal-1 at all: the connector then registers UNASSOCIATED and
    /// <see cref="AssignUsingName"/>'s content associates it at each OPEN/SORT/MERGE (GR3 b). Defaulting it to the
    /// file-name there is what made a bare-USING file's records land in <c>&lt;file-name&gt;.txt</c>.</summary>
    public string AssignTarget { get; set; } = "";

    /// <summary>The ASSIGN clause's USING data-name-1 as written (ISO §12.4.5.3 GR3 b — dynamic file assignment,
    /// §9.1.21), resolved post-build to <see cref="AssignUsingItem"/>; null when the clause has no USING phrase.
    /// Present on BOTH general-format arms: <c>ASSIGN TO device-name-1 … USING data-name-1</c> and the bare
    /// <c>ASSIGN USING data-name-1</c> — GR3 b applies "when the USING phrase … is specified", with no condition on
    /// the TO phrase, so the USING content wins over literal-1 whenever both are written.</summary>
    public string? AssignUsingName { get; set; }

    /// <summary>The USING data-name-1 reference's IN/OF qualifier words, written order (innermost first); empty when
    /// unqualified. §12.4.5.2 SR7 lets data-name-1 be any alphanumeric item outside this file's FD, so it can need
    /// qualification exactly as a RECORD KEY reference can (§8.4.2.2).</summary>
    public IReadOnlyList<string> AssignUsingQualifiers { get; set; } = [];

    /// <summary>The resolved ASSIGN … USING data item (set post-build, the <see cref="FileStatusItem"/> pattern);
    /// null when the clause has no USING phrase or the name resolves to nothing.</summary>
    public DataItem? AssignUsingItem { get; set; }

    /// <summary>The source position of the USING data-name-1 reference — the cursor §12.4.5.2 SR7's diagnostics
    /// report at, since the rule can only be checked once the data forest is indexed (post-build).</summary>
    public CobolNet.Editions.DiagnosticCursor AssignUsingAt { get; set; }

    /// <summary>The file organization (default SEQUENTIAL).</summary>
    public FileOrganization Organization { get; set; } = FileOrganization.Sequential;

    /// <summary>The access mode (default SEQUENTIAL).</summary>
    public FileAccessMode AccessMode { get; set; } = FileAccessMode.Sequential;

    /// <summary>True for a SELECT OPTIONAL file.</summary>
    public bool Optional { get; set; }

    /// <summary>The FILE STATUS data-name as written, resolved to <see cref="FileStatusItem"/> post-build; null if none.</summary>
    public string? FileStatusName { get; set; }

    /// <summary>The resolved FILE STATUS data item (set post-build), or null if the file has no FILE STATUS clause.</summary>
    public DataItem? FileStatusItem { get; set; }

    /// <summary>The FD's record description(s), in declaration order. The first is the canonical storage area; every
    /// other shares it (synthesized REDEFINES).</summary>
    public List<DataItem> Records { get; } = [];

    /// <summary>The RECORD KEY base data-name as written (ISO §12.4.5.12), resolved post-build to
    /// <see cref="RecordKeyItem"/>; null when absent (the clause is required for ORGANIZATION INDEXED).</summary>
    public string? RecordKeyName { get; set; }

    /// <summary>The RECORD KEY reference's IN/OF qualifier words, written order (innermost first) — identically
    /// named key items under different areas of the record are legal and selected by qualification (ISO
    /// §8.4.2.2; IX215A's three IX-FD3-KEY items). Empty = unqualified.</summary>
    public IReadOnlyList<string> RecordKeyQualifiers { get; set; } = [];

    /// <summary>The resolved prime RECORD KEY item — a data item within the file's record (ISO §12.4.5.12 SR2).</summary>
    public DataItem? RecordKeyItem { get; set; }

    /// <summary>The ALTERNATE RECORD KEY clauses as written, in declaration order: base data-name + IN/OF
    /// qualifiers + WITH DUPLICATES (ISO §12.4.5.6); resolved post-build into <see cref="AlternateKeys"/>.</summary>
    public List<(string Name, IReadOnlyList<string> Qualifiers, bool Duplicates, string? Suppress)> AlternateKeyNames { get; } = [];

    /// <summary>The resolved alternate keys, in declaration order (the runtime key index is the list index).
    /// <c>Suppress</c> is the decoded §12.4.5.6.4 GR6 key suppression value (null = no SUPPRESS WHEN phrase).</summary>
    public List<(DataItem Item, bool Duplicates, string? Suppress)> AlternateKeys { get; } = [];

    // ── §12.4.5.7 COLLATING SEQUENCE (INDEXED record-key collating) — raw capture, resolved post-build ──────────

    /// <summary>Format 1 (file-level) COLLATING SEQUENCE as written: (alphabet-name-1 [ALPHANUMERIC], alphabet-name-2
    /// [NATIONAL]); null when no file-level clause is present. <see cref="FileLevelCollatingCount"/> counts them for
    /// §12.4.5.7.3 SR3 (at most one per file control entry).</summary>
    public (string? Alnum, string? Nat)? FileLevelCollating { get; set; }
    public int FileLevelCollatingCount { get; set; }

    /// <summary>Format 2 (key-level) COLLATING SEQUENCE clauses as written: each names one or more RECORD KEY /
    /// ALTERNATE RECORD KEY items and their alphabet-name-3 (§12.4.5.7.2 Format 2).</summary>
    public List<(IReadOnlyList<string> KeyNames, string Alphabet)> KeyLevelCollating { get; } = [];

    /// <summary>The resolved PRIME key collating sequence (a literal-phrase table or a LOCALE sequence — the
    /// <see cref="AlphabetDef"/> the emitter renders as a runtime <c>CobolCollation</c>); null = native ordinal (no
    /// applicable COLLATING SEQUENCE clause, §12.4.5.3 GR6). Emitted into the runtime registration.</summary>
    public AlphabetDef? PrimeKeyCollation { get; set; }

    /// <summary>The resolved per-alternate-key collating sequences, index-aligned with <see cref="AlternateKeys"/>;
    /// each null = native ordinal.</summary>
    public List<AlphabetDef?> AlternateKeyCollations { get; } = [];

    /// <summary>The RELATIVE KEY data-name as written (ISO §12.4.5.13), resolved post-build; the item lives OUTSIDE
    /// the file's record (SR3) and holds the 1-based relative record number (GR1).</summary>
    public string? RelativeKeyName { get; set; }

    /// <summary>The resolved RELATIVE KEY item (an unsigned integer item, ISO §12.4.5.13 SR2).</summary>
    public DataItem? RelativeKeyItem { get; set; }

    /// <summary>True once an FD was matched to this SELECT (a SELECT with no FD is an error the front-end already
    /// diagnoses; here it simply has no records and is never opened with data).</summary>
    public bool HasFd { get; set; }

    /// <summary>True for an <c>FD … IS EXTERNAL</c> file (ISO §13.18.22.4 GR4a/GR4b): the file connector is an
    /// EXTERNAL file connector — ONE per run unit, shared by every program describing it — and the record data
    /// is external (one record area per run unit). At emission the connector keys the run-unit registry by
    /// <c>"::EXT::" + ExternalName</c> instead of the per-program <c>PROG::FILE</c> qualification, and the record
    /// area re-bases onto a run-unit <c>ExternalStore</c> cell (the WS-EXTERNAL Tier-B machinery). FILE STATUS
    /// items stay per-program (§13.18.22 is about the connector and record data, not the status item).</summary>
    public bool IsExternal { get; set; }

    /// <summary>True for an <c>FD … IS GLOBAL</c> file (ISO §13.18.30 / §13.18.27): the file-name and its
    /// record-names are GLOBAL names — visible in every directly/indirectly contained program; the contained
    /// program's verbs reach the OWNER's one connector and record storage.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>The externalized name of an EXTERNAL file connector — the FD name (ISO §13.18.22.4 GR5; the
    /// grammar carries no <c>AS literal</c> on the FD clause yet). Null for a non-EXTERNAL file.</summary>
    public string? ExternalName { get; set; }

    /// <summary>For a per-object instance file (M2-OO-1i): the name of the emitted object field holding this
    /// connector's per-object minted key (<c>__fkey_&lt;name&gt;</c>). One connector per object instance
    /// (ISO §9.1.4 — implicit CLOSE at object deletion), so the runtime key is minted per object rather than a
    /// static literal. Null for every program / factory / EXTERNAL file, whose connector key IS the static
    /// qualified <see cref="CobolName"/>; <c>EmitText.FileKeyExpr</c> selects between the two.</summary>
    public string? InstanceKeyField { get; set; }

    /// <summary>True when this file is described by an SD (sort-merge file description, ISO §13.4.6): it has no
    /// host storage — only SORT/MERGE/RELEASE/RETURN may reference it (SR3/SR4); its runtime store is the
    /// in-memory <c>CobolSort</c> buffer.</summary>
    public bool IsSortMerge { get; set; }

    /// <summary>The RECORD clause's variable-length model (ISO §13.18.43 — RECORD IS VARYING / RECORD CONTAINS m
    /// TO n), or null when the records are fixed-length. All the record verbs consume it: WRITE/REWRITE/RELEASE
    /// take the record length from the DEPENDING item (GR13a) or the record's own size (GR13b/c) and fail with
    /// I-O status '44' outside [min,max] (GR14); READ/RETURN restore the just-read record's length (GR15).</summary>
    public VaryingRecordInfo? Varying { get; set; }

    /// <summary>The resolved RECORD VARYING … DEPENDING ON data item (set post-build, like
    /// <see cref="FileStatusItem"/>); null when fixed-length or no DEPENDING phrase.</summary>
    public DataItem? VaryingDependingItem { get; set; }

    /// <summary>The report-names of the FD's REPORT(S) clause (ISO §13.18.46), in written order — a report file
    /// is exactly an FD with a non-empty list (§9.1.22; it legally has NO record description entries). The
    /// names resolve to <see cref="ReportModel"/>s post-build (<c>DataBinder.ResolveReports</c>).</summary>
    public List<string> ReportNames { get; } = [];

    /// <summary>The fixed <c>RECORD CONTAINS n</c> character count (ISO §13.18.43 Format 1), or null when absent
    /// or variable-length. A report file's line width prefers it over the computed field extent
    /// (COBOLNET_REPORT_WRITER_DESIGN §4).</summary>
    public int? RecordContains { get; set; }

    /// <summary>The SHARING clause mode (ISO §12.4.5.15; <see cref="SharingMode.None"/> = the implementor default).
    /// An OPEN SHARING phrase overrides it per-OPEN (§14.9.27; carried on the bound OPEN).</summary>
    public SharingMode Sharing { get; set; } = SharingMode.None;

    /// <summary>The LOCK MODE clause (ISO §12.4.5.9), or null when absent (locking off — the single-run-unit
    /// default is no record locking).</summary>
    public LockModeInfo? LockMode { get; set; }

    /// <summary>Whether an I-O-CONTROL APPLY COMMIT clause (ISO §12.4.6.3) names this file — the STATIC,
    /// source-level property the standard calls "subject to an APPLY COMMIT clause", as distinct from
    /// §12.4.6.3.4 GR5's runtime notion of a clause being <i>active</i>. Set by
    /// <c>DataBinder.BindIoControl</c>, which runs after the file control entries are bound (the clause is in a
    /// different paragraph, so this fact is NOT knowable while a SELECT is being bound).
    /// <para>⛔ IT IS A LIVE FACT, NOT A PLACEHOLDER FOR THE DAY A.4.3 IS CLAIMED. COBOLNET1709 declines the
    /// clause, but its descriptor is <c>PermissiveInert: true</c>, so under <c>--permissive</c> the clause is
    /// accepted with a warning and the program compiles — and a syntax rule that keys on subject-hood
    /// (§14.9.27.3 SR8's leading conjunct, read at <c>StatementValidation.CheckOpenSharingAllOther</c>) then has
    /// a program to speak about. Measured, not deduced: before this field existed, an APPLY COMMIT file with
    /// <c>SHARING WITH ALL OTHER</c> and no LOCK MODE clause was rejected COBOLNET1512 under
    /// <c>--std 2023 --permissive</c>, where SR8 exempts it and §12.4.5.9.3 SR1 forbids writing the very LOCK
    /// MODE clause the rejection demands (kb/Work PB319).</para></summary>
    public bool SubjectToApplyCommit { get; set; }

    /// <summary>The LINAGE clause's logical-page model (ISO §13.18.34), or null when the FD has no LINAGE clause.
    /// Its presence generates the file's LINAGE-COUNTER register (§8.4.3.14 / §13.18.34 GR7a) and enables the
    /// WRITE END-OF-PAGE phrases (§14.9.51 SR19). Data-name operands resolve post-build in
    /// <c>DataBinder.ResolveFiles</c> (the <see cref="VaryingDependingItem"/> pattern).</summary>
    public LinageInfo? Linage { get; set; }

    /// <summary>The variable-length minimum record size (ISO §13.18.43 GR9 — an unstated minimum defaults to the
    /// smallest record described for the file, where an occurs-depending table contributes its MINIMUM
    /// occurrences per GR8a); −1 for fixed-length records.</summary>
    public int VaryMin => Varying is { } v ? v.Min ?? (Records.Count == 0 ? 1 : Records.Min(MinRecordSize)) : -1;

    /// <summary>The variable-length maximum record size (ISO §13.18.43 GR10 — an unstated maximum defaults to the
    /// largest record described for the file; an ODO table allocates its maximum, GR8b); −1 for fixed-length
    /// records.</summary>
    public int VaryMax => Varying is { } v ? v.Max ?? Math.Max(1, RecordWidth) : -1;

    /// <summary>The minimum byte size of one record description (ISO §13.18.43 GR8a): the sum over non-redefining
    /// content with every occurs-depending table at its MINIMUM occurrence count (a bare <c>RECORD IS VARYING</c>
    /// over an ODO record — RL211A — has minimum 120, not the 140 max allocation <see cref="DataItem.ImageWidth"/>
    /// reports).</summary>
    private static int MinRecordSize(DataItem item) =>
        item.IsElementary ? item.ImageWidth
        : item.Children.Where(c => c.RedefinesTargetName is null)
            .Sum(c => MinRecordSize(c) * (c.OccursSpec is { DependingName: not null } od ? od.Min : c.Occurs ?? 1));

    /// <summary>The record area's PHYSICAL (codec) width — the max over the FD's records of the extent the
    /// emitted <c>AsImage()</c>/<c>FromImage()</c> spans. (P5.8: <c>ImageWidth</c> under-counted a record whose
    /// REDEFINES redefiner is wider than its target — the codec spans the class-max backing, ISO §13.18.44 /
    /// §13.4.2 — truncating written frames and mis-registering key windows; identical to the old value for every
    /// equal-width record, i.e. the whole prior corpus.)</summary>
    public int RecordWidth => Records.Count == 0 ? 0 : Records.Max(Model.RecordLayout.PhysicalWidth);

    /// <summary>The record description whose view spans the WHOLE record area — the largest one (ISO §13.4.2: the
    /// record area's size is that of the largest record description). Reading a record makes it available in the
    /// whole area, so every area-wide store/read (sequential and keyed READ, sort RETURN) must go through THIS
    /// record's view — a shorter <c>Records[0]</c> window would truncate the splice (ST111A's 50/75/100 FD,
    /// RL106A's 56/102 pair). Null when the FD has no record description.</summary>
    public DataItem? AreaRecord => Records.Count == 0 ? null : Records.MaxBy(r => r.ImageWidth);

    /// <summary>True for either sequential shape (the only organizations this slice can OPEN/READ/WRITE).</summary>
    public bool IsSequential => Organization is FileOrganization.Sequential or FileOrganization.LineSequential;
}

/// <summary>The variable-length record model of a RECORD clause (ISO §13.18.43): the declared minimum/maximum
/// record sizes (null when unstated) and the <c>VARYING … DEPENDING ON</c> data-name (null when none — a
/// <c>RECORD CONTAINS m TO n</c> file varies without a length register).</summary>
/// <param name="VaryingClause">True for FORMAT 2 (<c>RECORD IS VARYING IN SIZE …</c>), false for FORMAT 3
/// (<c>RECORD CONTAINS integer-4 TO integer-5</c>). ⛔ THE TWO FORMATS ARE NOT INTERCHANGEABLE even though both
/// describe variable-length records: §13.18.43.4 GR16 (the READ/RETURN INTO sending size) is stated under the
/// FORMAT 2 heading, and §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b) designate the implied move an alphanumeric group
/// move only "If the file description entry contains a RECORD IS VARYING clause" — a format-3 file's record size
/// is instead "completely defined in the record description entry" (GR18) and its move is classified normally.
/// The two formats were indistinguishable here until kb/Work PB339, which is why a rule keyed on the WORD
/// VARYING had nothing to key on.</param>
public sealed record VaryingRecordInfo(int? Min, int? Max, string? DependingName, bool VaryingClause);

/// <summary>One LINAGE clause operand (ISO §13.18.34 GR6): a fixed literal value (GR6a) or a data-name whose
/// content is read at the GR6b evaluation points (OPEN OUTPUT / WRITE ADVANCING PAGE / page overflow), resolved
/// post-build to <see cref="Item"/> (SR1/SR2 — an elementary unsigned integer not under OCCURS, so a plain
/// name lookup suffices).</summary>
public sealed record LinageOperand(int? Literal, string? DataName)
{
    /// <summary>The resolved data item for the <see cref="DataName"/> form (set post-build); null for a literal.</summary>
    public DataItem? Item { get; set; }
}

/// <summary>The LINAGE clause's four operands (ISO §13.18.34): the page-body size (GR2), the footing start
/// (GR3 — the footing area is [footing, page size] inclusive), and the top/bottom margins (GR4/GR5). A null
/// <see cref="Footing"/>/<see cref="Top"/>/<see cref="Bottom"/> = the phrase is absent (margins 0, GR1; no
/// footing ⇒ no end-of-page condition independent of page overflow, GR1).</summary>
public sealed record LinageInfo(LinageOperand Body, LinageOperand? Footing, LinageOperand? Top, LinageOperand? Bottom)
{
    /// <summary>The non-null operands, for uniform post-build resolution.</summary>
    public IEnumerable<LinageOperand> Operands
    {
        get
        {
            yield return Body;
            if (Footing is not null) yield return Footing;
            if (Top is not null) yield return Top;
            if (Bottom is not null) yield return Bottom;
        }
    }
}
