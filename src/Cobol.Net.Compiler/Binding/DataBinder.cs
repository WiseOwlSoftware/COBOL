// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Builds the bound DATA DIVISION model (a forest of <see cref="DataItem"/> trees, one per 01/77 item) from the
/// parse tree, and indexes every named item for reference resolution. Pure syntactic/semantic analysis — no byte
/// layout; the .NET type IS the storage. (Slice scope: WORKING-STORAGE groups + elementary items with fixed
/// OCCURS recorded; FILE/LINKAGE/LOCAL-STORAGE, level-66/88, and REDEFINES follow in later slices.)
/// </summary>
public sealed partial class DataBinder(EditionContext? edition = null)
{
    private int _fillerCounter;
    private int _uidCounter;

    /// <summary>The targeted-edition context (digit caps, bind-time rejection diagnostics). Defaults to the
    /// latest edition for direct test construction; <c>CompilerDriver</c> always supplies the CLI's
    /// <c>--std</c>.</summary>
    public EditionContext Edition { get; } = edition ?? new EditionContext(2023);

    /// <summary>The top-level (01/77) items of WORKING-STORAGE, in source order.</summary>
    public List<DataItem> Roots { get; } = [];

    /// <summary>
    /// Every named item, keyed by COBOL name (case-insensitive) → the list of items with that name. COBOL permits
    /// duplicate data-names disambiguated only by qualification (OF/IN), so this is a MULTIMAP — a single-valued
    /// dictionary would silently drop all but the last (a latent wrong-item bug; COBOLNET_DESIGN §3.5).
    /// </summary>
    public Dictionary<string, List<DataItem>> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>INDEXED BY index-names (case-insensitive) → the C# <c>long</c> field that holds the 1-based
    /// occurrence number (COBOLNET_DESIGN §3.5). A subscript may name an index, so the resolver consults this.</summary>
    public Dictionary<string, string> IndexFields { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Level-88 condition-names (case-insensitive) → the conditions with that name (a list, since names
    /// may be duplicated under different parents and disambiguated by qualification).</summary>
    public Dictionary<string, List<Condition88>> Conditions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Group items referenced as a whole (non-elementary) operand anywhere in the PROCEDURE DIVISION — recorded by
    /// <see cref="ReferenceResolver"/> as it resolves each reference. A group name can only be used as a whole (MOVE
    /// to/from it, DISPLAY it, compare it), so any resolved group reference is a whole-group operand. The bind-time
    /// <c>MarkStoreAsImage</c> pass consults this to decide which numeric-DISPLAY leaves must store their character
    /// image (ISO §14.9 MOVE GR4 — a whole-group move fills without conversion; see <see cref="DataItem.StoreAsImage"/>).
    /// </summary>
    public HashSet<DataItem> WholeGroupReferenced { get; } = [];

    /// <summary>The fully-parsed OPTIONS paragraph (ISO §11.9), program-level context for every later pass — the
    /// binder applies DEFAULT ROUNDED today (a bare ROUNDED phrase uses <see cref="OptionsModel.DefaultRounding"/>);
    /// the remaining clauses are captured for the features that will consume them. Defaults when no OPTIONS.</summary>
    public OptionsModel Options { get; private set; } = OptionsModel.Default;

    /// <summary>All SELECTed files (the SELECT clause joined with its FD records), in source order.</summary>
    public List<FileModel> Files { get; } = [];

    /// <summary>The files keyed by COBOL file-name (case-insensitive), for the binder to resolve OPEN/READ/CLOSE
    /// targets and to map a WRITE/REWRITE record-name back to its owning file.</summary>
    public Dictionary<string, FileModel> FilesByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when SOURCE-COMPUTER declares WITH DEBUGGING MODE (the X3.23-1985 compile-time debug
    /// switch) — consumed by the declaratives binder to decide the USE FOR DEBUGGING posture (compiled but
    /// never triggered vs comment-treated; VCR Table 7 rows 7.9/7.17).</summary>
    public bool DebuggingModeDeclared { get; private set; }

    /// <summary>The compilation group's pass-1 class symbol table (OO deep-dive D1) — set by the run-unit
    /// emitter BEFORE <see cref="Bind"/> so a typed <c>USAGE OBJECT REFERENCE class-name</c> validates its
    /// declared class (§13.18.60.4) against classes defined anywhere in the group. Null only in unit-test
    /// direct construction, which then behaves as an empty group (every typed reference is unknown-class).</summary>
    public OoClassTable? OoClasses { get; set; }

    /// <summary>The unit's REPOSITORY PROPERTY specifier names (§12.3.8) — the §8.4.3.9.3 SR1 gate for
    /// object-property references (case-insensitive per §8.3.2).</summary>
    internal HashSet<string> OoRepositoryProperties { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The unit's REPOSITORY user-function specifiers (§12.3.8 — <c>FUNCTION function-prototype-name</c>
    /// WITHOUT the INTRINSIC phrase): the precondition for a user-function reference, and per §12.3.8.2 GR12
    /// (:14885) the declaration that makes the name refer to the USER-DEFINED function "and not to an intrinsic
    /// function of the same name" — so the binder's user-function dispatch precedes the intrinsic catalog.</summary>
    internal HashSet<string> UserFunctionNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Bind a program unit's DATA DIVISION + the FILE-CONTROL paragraph: the OPTIONS paragraph, the SELECT
    /// clauses, the FILE SECTION records (which share storage with the WORKING-STORAGE roots — they emit as Program
    /// fields), and WORKING-STORAGE; then classify the shared-storage (REDEFINES) classes over the whole forest and
    /// resolve each file's FILE STATUS item.</summary>
    public void Bind(Core.ProgramUnitContext program)
    {
        BindDeclarations(program);
        BindResolve(program);
    }

    /// <summary>The declaration half of <see cref="Bind"/>: OPTIONS/SPECIAL-NAMES/FILE-CONTROL, the FILE /
    /// WORKING-STORAGE / LINKAGE sections, and the PD header formals — everything that ADDS items to the
    /// forest. Split from <see cref="BindResolve"/> so a CLASS unit can bind its METHODS' data sections
    /// (OO deep-dive D3/D6 — <c>DataBinder.Oo.cs</c>) into the same forest BEFORE the post-build passes run
    /// over it (a method item participates in USAGE/SIGN inheritance and object-reference resolution exactly
    /// like program data).</summary>
    internal void BindDeclarations(Core.ProgramUnitContext program)
    {
        Options = OptionsBinder.Bind(program, Edition);   // captured even when there is no WORKING-STORAGE

        // ARITHMETIC mode validity (§11.9.5 / §8.8.1): NATIVE, STANDARD-DECIMAL, and — as of Phase-4 track (e),
        // DEVLOG 611 — plain STANDARD are implemented. STANDARD arithmetic (§8.8.1.2) performs operations in the
        // standard intermediate data item; for FIXED-POINT (non-float) operands that item IS the standard
        // DECIMAL form (§8.8.1.4), so STANDARD and STANDARD-DECIMAL produce identical results — STANDARD routes
        // to the same CobolDec engine (NumericRenderer.StandardDecimal). The two diverge only for FLOATING-POINT
        // operands (STANDARD may use an IEEE-binary intermediate); the float USAGE families are staged loud
        // (PicInfo COBOLNET0899, Phase 6/D16), so no reachable STANDARD program observes the divergence yet —
        // when the float types land they carry the STANDARD-binary-intermediate leg. STANDARD was DROPPED by
        // ISO/IEC 1989:2023 (§8.8.1 names only NATIVE/STANDARD-BINARY/STANDARD-DECIMAL) → a removed-feature error
        // at --std 2023. STANDARD-BINARY is spec-obsolete (§8.8.1.4.1 NOTE 1) and documented-unsupported.
        if (Options.Arithmetic == ArithmeticMode.StandardBinary)
            Edition.Error("COBOLNET0806", "ARITHMETIC IS STANDARD-BINARY is an obsolete feature (ISO §8.8.1.4.1 "
                + "NOTE 1 / Annex F) and is not supported; use NATIVE or STANDARD-DECIMAL");
        else if (Options.Arithmetic == ArithmeticMode.Standard && Edition.DialectLevel >= 2023)
            Edition.Error("COBOLNET0807", "ARITHMETIC IS STANDARD was dropped by ISO/IEC 1989:2023 "
                + "(§8.8.1 defines NATIVE, STANDARD-BINARY, STANDARD-DECIMAL); use STANDARD-DECIMAL");

        // The '85 debug facility's compile-time switch (X3.23-1985 SOURCE-COMPUTER … WITH DEBUGGING MODE; the
        // clause itself is 0902-gated ≥2002 by the EditionValidator): its presence decides whether a USE FOR
        // DEBUGGING declarative section is COMPILED (switch present — the object-time switch is permanently off
        // here, so it never triggers) or treated as comment lines (switch absent — the '85 rule). Token-text
        // scan of the computerAttributes sink, the VisitComputerAttributes pattern (VCR Table 7 rows 7.9/7.17).
        DebuggingModeDeclared = program.environmentDivision()?.configurationSection()?.configurationParagraph()
            .Select(p => p.sourceComputerParagraph()?.computerAttributes())
            .Any(attrs => attrs is not null && Enumerable.Range(0, attrs.ChildCount)
                .Any(i => attrs.GetChild(i).GetText().Equals("DEBUGGING", StringComparison.OrdinalIgnoreCase)))
            ?? false;

        // REPOSITORY PROPERTY specifiers (§12.3.8 :14727-14729) — §8.4.3.9.3 SR1 makes a property-specifier
        // a PRECONDITION of every object-property reference in the unit; captured here, checked at the
        // property-reference desugar (0843). FUNCTION specifiers WITHOUT the INTRINSIC phrase declare
        // user-defined functions (§12.3.8.2 GR12 — the name then refers to the user function, never a
        // same-named intrinsic; the `FUNCTION ALL INTRINSIC` alternative carries no functionName and the
        // per-name INTRINSIC form is excluded by its phrase). CLASS/INTERFACE specifiers stay declarative
        // (names resolve through the group-wide pass-1 table).
        foreach (var re in program.environmentDivision()?.configurationSection()?.configurationParagraph()
                     .Select(p => p.repositoryParagraph()).FirstOrDefault(r => r is not null)
                     ?.repositoryEntry() ?? [])
        {
            if (re.PROPERTY() is not null && re.propertyName() is { } pn)
                OoRepositoryProperties.Add(pn.GetText());
            else if (re.FUNCTION() is not null && re.INTRINSIC() is null && re.functionName() is { } fn)
                UserFunctionNames.Add(fn.GetText());
        }

        SwitchBindSpecialNames(program);           // SPECIAL-NAMES switch clauses → the external-switch registry (ISO §12.3.7)
        BindFileControl(program);                  // SELECT clauses → FileModels (before the FD records bind)
        BindFileSection(program, _rootNames);      // FD records → Roots + FileModel.Records + the shared-area REDEFINES
        BindReportSection(program);                // RD entries → ReportModels (ISO §13.14; DataBinder.Reports.cs)
        BindIoControl(program);                    // I-O-CONTROL: SAME RECORD AREA → cross-file shared record area (§12.4.6.4 GR2)

        if (program.dataDivision()?.workingStorageSection() is { } ws)
            BindEntries(ws.dataDescriptionEntry(), _rootNames);

        // LINKAGE SECTION roots + the PROCEDURE DIVISION header's USING/RETURNING formals (ISO §13.7 / §14.2.2;
        // COBOLNET_INTERPROGRAM_DESIGN D1/D3 — bound into the same forest so every verb works unchanged).
        CallBindLinkage(program, _rootNames);
    }

    /// <summary>The C#-field-name scope at the class level, shared by FILE SECTION records, WORKING-STORAGE
    /// roots, LINKAGE roots — and, in a CLASS unit, every METHOD's data roots (an emitted field/local name is
    /// unique across the whole class, so sibling methods' same-named items can never cross-wire — the legacy
    /// trap-#6 guard at the NAME level).</summary>
    private readonly HashSet<string> _rootNames = new(StringComparer.Ordinal);

    /// <summary>The resolution half of <see cref="Bind"/> — the post-build passes over the COMPLETE forest.</summary>
    internal void BindResolve(Core.ProgramUnitContext program)
    {
        // Post-build (the forest is complete): fix up USAGE INDEX entries (children weren't known at entry bind);
        // apply group-level SIGN clauses (must precede the REDEFINES classification — a SEPARATE sign adds a
        // character position to the item's image width, which feeds the class-max width); then resolve
        // REDEFINES/RENAMES targets, group overlaid items into shared-storage classes and assign each a tier
        // (ISO §13.18.44/§13.18.45; COBOLNET_DESIGN §4). This now covers the FILE SECTION records too (their
        // multi-01 area sharing is a synthesized REDEFINES). Finally resolve each file's FILE STATUS data item.
        ResolveIndexItems();
        InheritUsageClauses();
        InheritSignClauses();
        ResolveRedefines();
        ClassifyRedefinesClasses();
        OdoResolve();   // resolve OCCURS DEPENDING ON data-name-1 + validate §13.18.38 structural rules
        ResolveFiles();
        GateNationalRecords();   // D-N2: the record codec is single-byte — national FD/SD leaves stage loud
        ResolveReports();   // SOURCE/CONTROL/SUM items + owning files + line widths (ISO §13.18.46/.53/.16/.54)
        CallBindExternalAndGlobal(program);   // EXTERNAL 01s → run-unit image backings; GLOBAL 01s collected (ISO §13.18.22 / §13.18.27)
        PtrBindBasedAndAddressables(program); // BASED templates + ADDRESS-OF-taken items → cell backings (ISO §13.18.5 / §8.4.3.11; Phase-4b inc 2)

        // Every FILE record area is filled WITHOUT conversion by READ/RETURN (ISO §9.1.2 — the record area is one
        // character image), so its numeric-DISPLAY leaves store their images exactly like a whole-referenced
        // group's — even when the PROCEDURE DIVISION never names the record as a whole (ST103A reads then tests
        // only a child). MarkStoreAsImage consumes this set after binding.
        foreach (var f in Files)
            foreach (var rec in f.Records)
                if (rec.IsGroup)
                {
                    WholeGroupReferenced.Add(rec);
                    // Flag the leaves NOW (not at the emitter's whole-group pass): statement binding consults
                    // IsCharacterImage — e.g. the SORT binder requires the SD record to be image-storable
                    // (ST102A's all-DISPLAY S-RECORD must not read as a Tier-C island at bind time).
                    MarkImageLeaves(rec);
                }

        static void MarkImageLeaves(DataItem item)
        {
            foreach (var child in item.Children)
            {
                if (child.IsGroup) MarkImageLeaves(child);
                else if (child.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                    child.StoreAsImage = true;   // same rule as the emitter's MarkStoreAsImage (§14.9 MOVE GR4)
            }
        }
    }

    /// <summary>Bind a run of data-description entries (a WORKING-STORAGE section or one FD's records) into the
    /// storage forest: a level-number stack attaches each entry under the nearest open item of a lower level; level-88
    /// becomes a condition-name and level-66 a RENAMES alias. Returns the new top-level (01/77) items, in order — the
    /// caller (an FD) needs them to model the shared record area.</summary>
    private List<DataItem> BindEntries(IEnumerable<Core.DataDescriptionEntryContext> entries, HashSet<string> rootNames)
    {
        var newRoots = new List<DataItem>();
        var stack = new Stack<DataItem>();
        foreach (var entry in entries)
        {
            int.TryParse(entry.levelNumber().GetText(), out int lvl);
            // A level-88 entry is a condition-name on the immediately superior item — not a node in the tree.
            if (lvl == 88)
            {
                if (stack.Count > 0) BindCondition(entry, stack.Peek());
                continue;
            }
            // A level-66 RENAMES entry is a re-grouping alias on the owning record — not a node in the storage tree.
            if (lvl == 66)
            {
                BindRenames(entry);
                continue;
            }

            if (BindEntry(entry) is not { } item) continue;
            item.Uid = _uidCounter++;

            // Level 77 is an INDEPENDENT elementary item (ISO §13.18.38): always top-level, like 01, regardless of its
            // numeric value. Treat it as level 1 for the nesting pop so it attaches as a ROOT — never nested under an
            // open subordinate item just because 77 > that item's level (which would mis-qualify every later reference).
            int nestLevel = item.Level == 77 ? 1 : item.Level;
            while (stack.Count > 0 && stack.Peek().Level >= nestLevel)
                stack.Pop();

            if (stack.Count == 0)
            {
                // A 01/77 emits as a Program-level static field — its C# name must be unique across every root
                // (FILE SECTION records and WORKING-STORAGE alike), so record it in the shared scope.
                item.CsName = Unique(item.CsName, rootNames);
                rootNames.Add(item.CsName);
                Roots.Add(item);
                newRoots.Add(item);
                _lastRoot = item;
            }
            else
            {
                var parent = stack.Peek();
                // A member name need only be unique within its containing struct (the parent's children).
                item.CsName = Unique(item.CsName, parent.Children.Select(c => c.CsName));
                item.Parent = parent;
                parent.Children.Add(item);
            }
            stack.Push(item);
            RegisterName(item);
        }
        return newRoots;
    }

    // ── FILE-CONTROL + FILE SECTION (ISO §12.4.5 / §13.18; COBOLNET_DESIGN §8) ─────────────────────────────────

    /// <summary>Bind the FILE-CONTROL paragraph's SELECT clauses into <see cref="FileModel"/>s (assign target,
    /// organization, access mode, OPTIONAL, FILE STATUS). The FD records attach in <see cref="BindFileSection"/>.</summary>
    private void BindFileControl(Core.ProgramUnitContext program)
    {
        var fc = program.environmentDivision()?.inputOutputSection()?.fileControlParagraph();
        if (fc is null) return;
        foreach (var grp in fc.fileControlClauseGroup())
        {
            if (grp.fileName()?.GetText() is not { } name) continue;
            var file = new FileModel { CobolName = name, AssignTarget = name, Optional = grp.OPTIONAL() is not null };
            foreach (var clauses in grp.fileControlClauses())
            {
                if (clauses.assignClause()?.assignTarget() is { } tgt)
                    file.AssignTarget = tgt.STRINGLIT() is { } s ? DecodeString(s.GetText()) : tgt.GetText();
                else if (clauses.organizationClause() is { } org) file.Organization = MapOrganization(org);
                else if (clauses.accessModeClause() is { } acc) file.AccessMode = MapAccessMode(acc);
                // The BASE word only: an OF/IN-qualified status name (`SQ-FS4-STATUS OF STATUS-GROUP`, SQ133A)
                // would otherwise glue its qualifier into the lookup key (the RENAMES capture pattern).
                else if (clauses.fileStatusClause()?.dataReference() is { } fs)
                    file.FileStatusName = fs.cobolWord()?.GetText() ?? fs.GetText();
                else if (clauses.recordKeyClause()?.dataReference() is { } rk)                                       // ISO §12.4.5.12
                    (file.RecordKeyName, file.RecordKeyQualifiers) = KeyReference(rk);
                else if (clauses.alternateKeyClause() is { } ak)                                                     // ISO §12.4.5.6
                {
                    var (an, aq) = KeyReference(ak.dataReference());
                    file.AlternateKeyNames.Add((an, aq, ak.DUPLICATES() is not null));
                }
                else if (clauses.relativeKeyClause()?.dataReference() is { } rlk)
                    file.RelativeKeyName = KeyReference(rlk).Base;   // ISO §12.4.5.13 SR3 — outside the record
            }
            Files.Add(file);
            FilesByName[name] = file;
        }
    }

    /// <summary>A FILE-CONTROL key reference: the base word plus its IN/OF qualifier words in written order
    /// (innermost first, ISO §8.4.2.2). A raw <c>GetText()</c> would glue qualifiers into the lookup key
    /// (<c>IX-FD3-KEYINIX-FD3-RECKEY-AREA</c>) and the name could never resolve — the FILE STATUS / RENAMES
    /// capture pattern, applied to keys.</summary>
    private static (string Base, IReadOnlyList<string> Quals) KeyReference(Core.DataReferenceContext dref)
    {
        string baseWord = dref.cobolWord()?.GetText() ?? dref.GetText();
        var quals = new List<string>();
        foreach (var s in dref.dataReferenceSuffix())
            if (s.qualification()?.cobolWord() is { } q)
                quals.Add(q.GetText());
        return (baseWord, quals);
    }

    /// <summary>Bind the FILE SECTION's FD records into the storage forest (they emit as Program fields, like
    /// WORKING-STORAGE), attach them to their <see cref="FileModel"/>, and model the shared record area: multiple
    /// <c>01</c>s under one FD occupy ONE area (ISO §9.1.2), so each secondary record is synthesized as a REDEFINES of
    /// the first — the existing tier machinery then makes them alias one backing (the singular-pattern rule).</summary>
    private void BindFileSection(Core.ProgramUnitContext program, HashSet<string> rootNames)
    {
        var fs = program.dataDivision()?.fileSection();
        if (fs is null) return;
        foreach (var fd in fs.fileDescriptionEntry())
        {
            if (fd.fileName()?.GetText() is not { } name) continue;
            var records = BindEntries(fd.dataDescriptionEntry(), rootNames);
            if (!FilesByName.TryGetValue(name, out var file))
            {
                // An FD with no matching SELECT — keep a model so its records still resolve (it is never opened).
                file = new FileModel { CobolName = name };
                Files.Add(file);
                FilesByName[name] = file;
            }
            file.HasFd = true;
            file.Records.AddRange(records);
            for (int i = 1; i < records.Count; i++)
                records[i].RedefinesTarget ??= records[0];   // secondary record shares the first's storage area
            foreach (var clause in fd.fileDescriptionClauses()?.fileDescriptionClause() ?? [])
                if (clause.recordClause() is { } rc)
                    BindRecordClause(rc, file);   // RECORD VARYING / m TO n → FileModel.Varying (ISO §13.18.43)
                else if (clause.linageClause() is { } lc)
                    BindLinageClause(lc, file);   // LINAGE logical-page model → FileModel.Linage (ISO §13.18.34)
                else if (clause.reportClause() is { } rep)
                    // REPORT(S) clause (ISO §13.18.46): the FD hosts these reports — a report FILE (§9.1.22,
                    // legally record-less). Names resolve to ReportModels post-build (ResolveReports).
                    foreach (var rn in rep.reportName())
                        file.ReportNames.Add(rn.GetText());
                else if (clause.fileGlobalExternalClause() is { } ge)
                {
                    // FD IS EXTERNAL / IS GLOBAL (ISO §13.18.22 / §13.18.30): EXTERNAL ⇒ one run-unit file
                    // connector + external record data (GR4a/GR4b), externalized as the FD name (GR5); GLOBAL ⇒
                    // the file-name and record-names are global names, inherited by contained programs.
                    if (ge.EXTERNAL() is not null)
                    {
                        file.IsExternal = true;
                        file.ExternalName = name.ToUpperInvariant();
                    }
                    if (ge.GLOBAL() is not null) file.IsGlobal = true;
                }
        }

        // SD entries (ISO §13.4.6): a sort-merge file's records bind through the SAME entry path as FD records —
        // they emit as Program fields and multi-01 records share ONE area (synthesized REDEFINES, ISO §9.1.2).
        // The SD format admits only the record clause (§13.4.6); DATA RECORDS is an obsolete '85 element DELETED
        // by ISO/IEC 1989:2002 — accepted-inert at 85 (every NIST SD writes it), rejected ≥2002.
        foreach (var sd in fs.sortMergeDescriptionEntry())
        {
            if (sd.fileName()?.GetText() is not { } sdName) continue;
            var sdRecords = BindEntries(sd.dataDescriptionEntry(), rootNames);
            if (!FilesByName.TryGetValue(sdName, out var sdFile))
            {
                sdFile = new FileModel { CobolName = sdName };
                Files.Add(sdFile);
                FilesByName[sdName] = sdFile;
            }
            sdFile.HasFd = true;
            sdFile.IsSortMerge = true;   // referenced only by SORT/MERGE/RELEASE/RETURN (§13.4.6 SR3/SR4)
            sdFile.Records.AddRange(sdRecords);
            for (int i = 1; i < sdRecords.Count; i++)
                sdRecords[i].RedefinesTarget ??= sdRecords[0];
            foreach (var clause in sd.sortMergeDescriptionClauses()?.sortMergeDescriptionClause() ?? [])
            {
                if (clause.recordClause() is { } rc)
                    BindRecordClause(rc, sdFile);
                // (The SD DATA RECORDS 0873 gate MIGRATED to EditionValidator.VisitDataRecordsClause — one
                // enforcement site covering FD AND SD via the shared grammar rule; P2.6 / Table-7 row 7.1.)
            }
        }
    }

    /// <summary>Bind a RECORD clause's variable-length forms into <see cref="FileModel.Varying"/> (ISO §13.18.43:
    /// <c>RECORD IS VARYING [FROM m] [TO n] [DEPENDING ON d]</c> and <c>RECORD CONTAINS m TO n</c> describe
    /// variable-length records; the fixed Format-1 <c>RECORD CONTAINS n</c> leaves it null). Shared by the FD and
    /// SD loops — ONE binding for the clause. The DEPENDING name keeps only the base word (the FILE STATUS
    /// capture pattern) and resolves post-build in <see cref="ResolveFiles"/>.</summary>
    private static void BindRecordClause(Core.RecordClauseContext rc, FileModel file)
    {
        if (rc.VARYING() is null && rc.TO() is null)
        {
            // The fixed Format-1 RECORD CONTAINS n (ISO §13.18.43): captured for the report-file line width
            // (COBOLNET_REPORT_WRITER_DESIGN §4); a record-bearing FD's width still comes from its records.
            if (rc.integerLiteral() is { Length: > 0 } fixedLits && int.TryParse(fixedLits[0].GetText(), out int n0))
                file.RecordContains = n0;
            return;
        }
        var lits = rc.integerLiteral();
        int? lo = lits.Length > 0 ? int.Parse(lits[0].GetText()) : null;
        int? hi = lits.Length > 1 ? int.Parse(lits[1].GetText()) : null;
        if (rc.TO() is not null && lits.Length == 1) { hi = lo; lo = null; }
        string? dep = rc.dataReference() is { } d ? d.cobolWord()?.GetText() ?? d.GetText() : null;
        file.Varying = new VaryingRecordInfo(lo, hi, dep);
    }

    /// <summary>Bind a LINAGE clause into <see cref="FileModel.Linage"/> (ISO §13.18.34: <c>LINAGE IS
    /// {data-name-1 | integer-1} LINES [WITH FOOTING AT {data-name-2 | integer-2}] [LINES AT TOP
    /// {data-name-3 | integer-3}] [LINES AT BOTTOM {data-name-4 | integer-4}]</c>). Each operand is a fixed
    /// literal (GR6a) or a data-name (GR6b — read at the evaluation points); a data-name keeps only the base
    /// word (the FILE STATUS capture pattern) and resolves post-build in <see cref="ResolveFiles"/>. Absent
    /// FOOTING/TOP/BOTTOM phrases stay null (GR1 — margins zero; no footing ⇒ no end-of-page condition
    /// independent of page overflow).</summary>
    private static void BindLinageClause(Core.LinageClauseContext lc, FileModel file)
    {
        static LinageOperand Operand(Core.DataReferenceContext? d, Core.IntegerLiteralContext? i) =>
            i is not null ? new LinageOperand(int.Parse(i.GetText()), null)
            : new LinageOperand(null, d!.cobolWord()?.GetText() ?? d.GetText());
        file.Linage = new LinageInfo(
            Operand(lc.dataReference(), lc.integerLiteral()),
            lc.linageFootingPhrase() is { } f ? Operand(f.dataReference(), f.integerLiteral()) : null,
            lc.linageLinesAtTopPhrase() is { } t ? Operand(t.dataReference(), t.integerLiteral()) : null,
            lc.linageLinesAtBottomPhrase() is { } b ? Operand(b.dataReference(), b.integerLiteral()) : null);
    }

    /// <summary>Bind the I-O-CONTROL paragraph (ISO §12.4.6). A record-area SAME clause (Format 2) makes the listed
    /// files "share a memory area for processing the current logical record … equivalent to an implicit redefinition
    /// of the area with records aligned on the leftmost byte position" (§12.4.6.4 GR2) — modeled by chaining each
    /// listed file's FIRST record as a synthesized REDEFINES of the first LISTED file's first record, exactly the
    /// multi-01-under-one-FD mechanism (the singular-pattern rule): the tier machinery then aliases every record of
    /// every listed file over ONE backing, and READ/WRITE/RELEASE image distribution gives the
    /// record-of-the-most-recently-read-file semantics for free. A sort/merge file may appear in a record-area
    /// clause (SR6 — ST131A's <c>READ FILE3</c> then <c>RELEASE S3</c> with no FROM relies on it). The file-area
    /// (Format 1) and sort-merge-area (Format 3) formats are storage-economy permissions (GR1/GR4 — shared/reusable
    /// ALLOCATION plus open-mode constraints on the program, nothing a typed-native runtime must alias) — bound as
    /// conformant no-ops; MULTIPLE FILE TAPE is obsolete and parsed-and-ignored (grammar note), and so is the
    /// X3.23-1985 RERUN clause (a checkpoint HINT with no program-visible effect — a null rerun facility is
    /// conforming; deleted by ISO 2002, 0902-gated ≥2002 by the EditionValidator, VCR Table 7 row 7.15) —
    /// both skip through the non-SAME `continue` below by design. The SR2–SR11 static
    /// legality checks (report/sort/file-area cross-membership consistency) are the diagnose-correctly track —
    /// staged with the EditionValidator phase, not silently absent by oversight.</summary>
    private void BindIoControl(Core.ProgramUnitContext program)
    {
        var io = program.environmentDivision()?.inputOutputSection()?.ioControlParagraph();
        if (io is null) return;
        foreach (var clause in io.ioControlClause())
        {
            // Format 2 only — SAME RECORD AREA (the RECORD word distinguishes it; SORT/SORT-MERGE are Format 3).
            if (clause.sameClause() is not { } same || same.RECORD() is null) continue;
            DataItem? anchor = null;
            foreach (var fn in same.fileName())
            {
                if (!FilesByName.TryGetValue(fn.GetText(), out var f) || f.Records.Count == 0) continue;
                if (anchor is null) { anchor = f.Records[0]; continue; }
                if (!ReferenceEquals(f.Records[0], anchor))
                    f.Records[0].RedefinesTarget ??= anchor;   // leftmost-aligned over the one area (GR2)
            }
        }
    }

    /// <summary>Resolve each file's FILE STATUS data-name to its item (post-build, once the forest is indexed).</summary>
    private void ResolveFiles()
    {
        foreach (var file in Files)
        {
            if (file.FileStatusName is { } sn && ByName.TryGetValue(sn, out var list) && list.Count > 0)
                file.FileStatusItem = list[0];
            // Keyed organizations: RECORD KEY / ALTERNATE RECORD KEY name items WITHIN the file's record
            // descriptions (ISO §12.4.5.12 SR2 / §12.4.5.6 SR2), possibly IN/OF-qualified (§8.4.2.2 — same-named
            // keys under different areas, IX215A); RELATIVE KEY is OUTSIDE the record (ISO §12.4.5.13 SR3) —
            // a plain name lookup.
            DataItem? InRecords(string keyName, IReadOnlyList<string> quals) =>
                file.Records.Select(r => FindQualified(r, keyName, quals)).FirstOrDefault(x => x is not null)
                ?? (quals.Count == 0 && ByName.TryGetValue(keyName, out var l) && l.Count > 0 ? l[0] : null);
            if (file.RecordKeyName is { } rk) file.RecordKeyItem = InRecords(rk, file.RecordKeyQualifiers);
            foreach (var (altName, altQuals, dups) in file.AlternateKeyNames)
                if (InRecords(altName, altQuals) is { } alt)
                    file.AlternateKeys.Add((alt, dups));
            if (file.RelativeKeyName is { } rl && ByName.TryGetValue(rl, out var rlist) && rlist.Count > 0)
                file.RelativeKeyItem = rlist[0];
            // RECORD VARYING … DEPENDING ON names an integer item outside the record (ISO §13.18.43 SR — the
            // length register WRITE/REWRITE/RELEASE read per GR13a and READ/RETURN set per GR15).
            if (file.Varying?.DependingName is { } vn && ByName.TryGetValue(vn, out var vlist) && vlist.Count > 0)
                file.VaryingDependingItem = vlist[0];
            // LINAGE data-name operands (ISO §13.18.34 GR6b) name elementary unsigned integer items not subject
            // to OCCURS (SR1/SR2) — a plain name lookup, exactly the VaryingDependingItem pattern.
            if (file.Linage is { } lin)
                foreach (var op in lin.Operands)
                    if (op.DataName is { } ln && ByName.TryGetValue(ln, out var llist) && llist.Count > 0)
                        op.Item = llist[0];
        }
    }

    private static FileOrganization MapOrganization(Core.OrganizationClauseContext org)
    {
        var t = org.organizationType();
        if (t is null) return FileOrganization.Sequential;
        if (t.LINE() is not null) return FileOrganization.LineSequential;
        if (t.RELATIVE() is not null) return FileOrganization.Relative;
        if (t.INDEXED() is not null) return FileOrganization.Indexed;
        return FileOrganization.Sequential;
    }

    private static FileAccessMode MapAccessMode(Core.AccessModeClauseContext acc)
    {
        var m = acc.accessMode();
        if (m?.RANDOM() is not null) return FileAccessMode.Random;
        if (m?.DYNAMIC() is not null) return FileAccessMode.Dynamic;
        return FileAccessMode.Sequential;
    }

    /// <summary>VALUE-clause literal/category conformance for national and boolean receivers (ISO §13.18.63,
    /// the COBOLNET0898 band). SR5: a category-national item takes a national literal or a figurative constant
    /// (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE / ZERO, §8.3.3.6 GR1/GR6/GR7). SR10: a category-boolean item
    /// takes a boolean literal or figurative ZERO (no boolean SPACE/QUOTE/HIGH/LOW exists — the §14.9.25.3 SR7
    /// posture). Both directions: an <c>N"…"</c>/<c>B"…"</c> literal seeds no OTHER category. Size: the decoded
    /// content shall not exceed the item's positions (SR5/SR10; alphanumeric receivers keep their historical
    /// truncating store — only the new categories get the strict check).</summary>
    private void ValidateValueCategory(PicInfo pic, string raw, string where)
    {
        bool isNatLit = raw.Length >= 3 && raw[0] is 'N' or 'n' && raw[1] is '"' or '\'';
        bool isBoolLit = raw.Length >= 3 && raw[0] is 'B' or 'b' && raw[1] is '"' or '\'';
        bool isPlainString = raw.Length >= 1 && raw[0] is '"' or '\'';
        bool isNumeric = raw.Length >= 1 && (char.IsAsciiDigit(raw[0]) || raw[0] is '+' or '-' or '.');
        // The part after a leading ALL (GetText concatenates tokens, so `ALL SPACES` → "ALLSPACES",
        // `ALL "AB"` → 'ALL"AB"'). `ALL "literal"` is an alphanumeric ALL-literal (illegal for national/
        // boolean); `ALL SPACES` / `ALL ZEROS` is just the figurative WORD repeated (legal).
        string afterAll = raw.Length > 3 && raw.StartsWith("ALL", StringComparison.OrdinalIgnoreCase)
            ? raw[3..] : raw;
        bool isAllQuoted = !ReferenceEquals(afterAll, raw) && afterAll.Length >= 1 && afterAll[0] is '"' or '\'';
        string word = afterAll.ToUpperInvariant();
        bool isZeroWord = word is "ZERO" or "ZEROS" or "ZEROES";
        bool isNationalFigurative = isZeroWord
            || word is "SPACE" or "SPACES" or "QUOTE" or "QUOTES"
                or "HIGH-VALUE" or "HIGH-VALUES" or "LOW-VALUE" or "LOW-VALUES";
        switch (pic.Category)
        {
            // National: an N"…" literal or a figurative constant (§8.3.3.6 GR1/GR6/GR7 — SPACE/QUOTE/HIGH/
            // LOW/ZERO, incl. their ALL-prefixed forms). Plain strings, B"…", numeric, and ALL "literal" are
            // illegal.
            case PicCategory.National when isPlainString || isBoolLit || isNumeric || isAllQuoted
                    || !(isNatLit || isNationalFigurative):
                Edition.Error("COBOLNET0898", $"{where}: the VALUE of a national data item shall be a national "
                    + "literal (N\"…\") or a figurative constant (ISO §13.18.63 SR5)");
                break;
            case PicCategory.National when isNatLit && DecodeString(raw).Length > pic.Length:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE national literal exceeds the item's "
                    + $"{pic.Length} national positions (ISO §13.18.63 SR5)");
                break;
            // Boolean: a B"…" literal or figurative ZERO (incl. ALL ZEROS) — no boolean SPACE/QUOTE/HIGH/LOW
            // exists (§14.9.25.3 SR7 posture).
            case PicCategory.Boolean when !isBoolLit && !isZeroWord:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE of a boolean data item shall be a boolean "
                    + "literal (B\"…\") or the figurative constant ZERO (ISO §13.18.63 SR10)");
                break;
            case PicCategory.Boolean when isBoolLit && DecodeString(raw).Length > pic.Length:
                Edition.Error("COBOLNET0898", $"{where}: the VALUE boolean literal exceeds the item's "
                    + $"{pic.Length} boolean positions (ISO §13.18.63 SR10)");
                break;
            case not (PicCategory.National or PicCategory.Boolean) when isNatLit || isBoolLit:
                Edition.Error("COBOLNET0898", $"{where}: a {(isNatLit ? "national (N\"…\")" : "boolean (B\"…\")")} "
                    + "literal may seed only a data item of its own category (ISO §13.18.63 SR5/SR10)");
                break;
        }
    }

    /// <summary>Decode a COBOL <c>STRINGLIT</c> (<c>"…"</c> with doubled <c>""</c>) — or a national/boolean
    /// literal (<c>N"…"</c>/<c>B"…"</c>, ISO §8.3.3.5/§8.3.3.4: the prefix letter is part of the token) — to
    /// its character value (the <c>EmitText.DecodeCobolString</c> twin).</summary>
    private static string DecodeString(string raw)
    {
        if (raw.Length >= 3 && raw[0] is 'N' or 'n' or 'B' or 'b' && raw[1] is '"' or '\'')
            raw = raw[1..];
        // Unwrap EITHER delimiter (ISO §8.3.1.2 — the apostrophe form is equal-standing; doubled opening
        // quote = one embedded quote). Keep in sync with the EmitText/StatementBinder twins.
        return raw.Length >= 2 && raw[0] is '"' or '\'' && raw[^1] == raw[0]
            ? raw[1..^1].Replace(new string(raw[0], 2), raw[0].ToString())
            : raw;
    }

    /// <summary>The most-recently-opened 01/77 record, so a following level-66 RENAMES attaches to its owner.</summary>
    private DataItem? _lastRoot;

    /// <summary>Index a named item in the <see cref="ByName"/> multimap (COBOL allows duplicate names disambiguated
    /// only by qualification).</summary>
    private void RegisterName(DataItem item)
    {
        if (item.CobolName is not { } name) return;
        if (!ByName.TryGetValue(name, out var list)) ByName[name] = list = [];
        list.Add(item);
    }

    /// <summary>Bind a level-66 RENAMES entry (ISO §13.18.45): a re-grouping alias <c>RENAMES from [THRU thru]</c>
    /// over a contiguous sibling run of the owning record. It adds no storage (SR2/SR3) — it is attached to the
    /// owning record's <see cref="DataItem.Renames66"/> list (not <see cref="DataItem.Children"/>) and registered for
    /// reference resolution; the FROM/THRU operands are resolved by the post-build pass.</summary>
    private void BindRenames(Core.DataDescriptionEntryContext entry)
    {
        var rc = entry.dataDescriptionBody().renamesClause();
        if (rc is null || entry.dataName()?.GetText() is not { } name || _lastRoot is null) return;
        bool thru = rc.THRU() is not null || rc.THROUGH() is not null;
        var item = new DataItem
        {
            Level = 66,
            CobolName = name,
            CsName = DataItem.Sanitize(name),
            Renames = new RenamesInfo
            {
                // The BASE word only: an OF/IN-qualified operand (`SUB-GRP-1 OF GRP — NC252A RENAMES-TEST-2`)
                // is redundant inside the owning record, and GetText() would glue the suffix into the name.
                FromName = rc.dataReference(0).cobolWord()?.GetText() ?? rc.dataReference(0).GetText(),
                ThruName = thru && rc.dataReference().Length > 1
                    ? rc.dataReference(1).cobolWord()?.GetText() ?? rc.dataReference(1).GetText()
                    : null,
            },
        };
        item.Uid = _uidCounter++;
        item.Parent = _lastRoot;        // owning record — an alias sibling, NOT a storage child
        _lastRoot.Renames66.Add(item);
        RegisterName(item);
    }

    /// <summary>Bind a level-88 condition-name on its conditional variable <paramref name="parent"/>, capturing the
    /// VALUE set (singletons + THRU ranges) as raw operand text (decoded at emit time).</summary>
    private void BindCondition(Core.DataDescriptionEntryContext entry, DataItem parent)
    {
        if (entry.dataName()?.GetText() is not { } name) return;
        var cond = new Condition88 { Name = name, Parent = parent };

        if (entry.dataDescriptionBody().dataDescriptionClauses() is { } clauses)
            foreach (var clause in clauses.dataDescriptionClause())
                if (clause.valueClause() is { } value)
                    foreach (var vi in value.valueItem())
                    {
                        // Numeric operands normalize to dot-decimal form (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a).
                        if (vi.valueClauseRange() is { } range)
                        {
                            // §13.18.63 SR29: THROUGH shall not be specified for a boolean conditional
                            // variable (0898). A national THROUGH range is spec-legal but orders under a
                            // NATIONAL alphabet (SR31) — recognized, staged (0899).
                            if (parent.Pic is { Category: PicCategory.Boolean })
                                Edition.Error("COBOLNET0898", $"condition-name '{name}': THROUGH may not be "
                                    + "specified when the conditional variable is boolean (ISO §13.18.63 SR29)");
                            else if (parent.Pic is { Category: PicCategory.National })
                                Edition.Error("COBOLNET0899", $"condition-name '{name}': a THROUGH range over "
                                    + "a national conditional variable (ordered by the national collating "
                                    + "sequence) is recognized but not yet implemented (Phase 4a residue) — "
                                    + "(ISO §13.18.63 SR31)");
                            else if (parent.Pic is { } rp)
                            {
                                // §13.18.63 SR4/SR5/SR24→SR10: the VALUE literals' category must match the
                                // conditional variable's — the SAME funnel the item-entry VALUE uses.
                                ValidateValueCategory(rp, range.valueClauseOperand(0).GetText(), $"condition-name '{name}'");
                                ValidateValueCategory(rp, range.valueClauseOperand(1).GetText(), $"condition-name '{name}'");
                            }
                            cond.Values.Add((NormalizeIfNumericLiteral(range.valueClauseOperand(0).GetText()),
                                             NormalizeIfNumericLiteral(range.valueClauseOperand(1).GetText())));
                        }
                        else
                            foreach (var op in vi.valueClauseOperand())
                            {
                                // §13.18.63 SR4/SR5/SR24→SR10 (both directions): an N"…"/B"…" literal seeds
                                // only its own category, and a national/boolean conditional variable takes only
                                // its own literal form — the ONE canonical checker (0898 band). Group parents
                                // (Pic null) are a separate leg.
                                if (parent.Pic is { } sp)
                                    ValidateValueCategory(sp, op.GetText(), $"condition-name '{name}'");
                                cond.Values.Add((NormalizeIfNumericLiteral(op.GetText()), null));
                            }
                    }

        if (!Conditions.TryGetValue(name, out var list)) Conditions[name] = list = [];
        list.Add(cond);
    }

    /// <summary>Make <paramref name="name"/> unique within a C# name scope, appending <c>_2</c>, <c>_3</c>, … on collision.</summary>
    private static string Unique(string name, IEnumerable<string> used)
    {
        var set = used as ICollection<string> ?? used.ToList();
        if (!set.Contains(name)) return name;
        for (int n = 2; ; n++)
        {
            string candidate = $"{name}_{n}";
            if (!set.Contains(candidate)) return candidate;
        }
    }

    /// <summary>Bind one data-description entry (skips level-66 RENAMES and level-88 condition names for now).</summary>
    private DataItem? BindEntry(Core.DataDescriptionEntryContext entry)
    {
        if (!int.TryParse(entry.levelNumber().GetText(), out int level)) return null;
        if (level is 66 or 88) return null; // RENAMES / condition-names: later slice.

        string? cobolName = entry.dataName()?.GetText();
        bool isFiller = cobolName is null || cobolName.Equals("FILLER", StringComparison.OrdinalIgnoreCase);
        string csName = isFiller ? $"_filler{_fillerCounter++}" : DataItem.Sanitize(cobolName!);

        string? pictureText = null, usageText = null, rawValue = null, redefinesTargetName = null;
        string? objectClassName = null;   // USAGE OBJECT REFERENCE class-name (null = universal; §13.18.60.4)
        int? occurs = null;
        OccursSpec? occursSpec = null;
        var indexNames = new List<string>();
        SignSpec? ownSign = null;
        bool justified = false, blankWhenZero = false;
        bool binaryUnsigned = false;   // USAGE BINARY-CHAR/... UNSIGNED (SIGNED is the default, ISO §13.18.60.4 GR12)
        bool isBased = false;          // BASED (ISO §13.18.5 — a storage template; Phase-4b increment 2)
        bool hasExternal = false;      // observed for the BASED×EXTERNAL SR (the clause itself binds later)

        if (entry.dataDescriptionBody().dataDescriptionClauses() is { } clauses)
            foreach (var clause in clauses.dataDescriptionClause())
            {
                if (clause.pictureClause()?.PIC_STRING() is { } picTok)
                    pictureText = picTok.GetText();
                else if (clause.basedClause() is not null)
                    isBased = true;   // validated below (§13.16 SR16 placement; the 0881 declaration band)
                else if (clause.externalClause() is not null)
                    hasExternal = true;   // consumed by CallBindExternalAndGlobal; flagged here for the 0881 check
                else if (clause.justifiedClause() is not null)
                    justified = true;   // JUSTIFIED [RIGHT] (ISO §13.18.34 — right-justify alphanumeric receives)
                else if (clause.blankWhenZeroClause() is not null)
                    blankWhenZero = true;   // BLANK [WHEN] ZERO (ISO §13.18.8 — a zero value stores all spaces)
                else if (clause.usageClause() is { } usage)
                {
                    usageText = UsageKeyword(usage);
                    // SIGNED (default) / UNSIGNED on a fixed-width binary usage (ISO §13.18.60.4 GR12) — the
                    // binarySign sibling is a direct child of usageClause in BOTH the full (USAGE IS
                    // BINARY-CHAR SIGNED) and the bare (BINARY-CHAR SIGNED) alternatives.
                    binaryUnsigned = usage.binarySign()?.UNSIGNED() is not null;
                    var oru = usage.usageKeyword()?.objectReferenceUsage();
                    if (oru?.FACTORY() is not null)
                        // OBJECT REFERENCE FACTORY OF class (§13.18.60 :22681) — the factory-object
                        // reference item awaits the universal-reference wave (§16.2.2 FactoryObject).
                        Edition.Error("COBOLNET0899", "USAGE OBJECT REFERENCE FACTORY OF (a factory-object "
                            + "reference, ISO §13.18.60) is recognized but not yet implemented (the "
                            + "universal-reference wave)");
                    else
                        objectClassName = oru?.className()?.GetText();
                }
                else if (clause.redefinesClause() is { } redef)
                    // Capture the target name only; resolution waits until the forest is built (the target is a
                    // prior sibling, but a chain A REDEFINES B REDEFINES C resolves in the post-build pass).
                    redefinesTargetName = redef.dataReference().GetText();
                else if (clause.valueClause() is { } value)
                    rawValue = ExtractValue(value);
                else if (clause.signClause() is { } sign)
                    ownSign = new SignSpec(sign.LEADING() is not null, sign.SEPARATE() is not null);
                else if (clause.occursClause() is { } occ)
                {
                    // Allocate at the table's MAXIMUM occurrence count — the last integer literal (integer-2 for a
                    // Format-2 `n TO m` table, the sole literal for a fixed table) — per ISO §8.5.1.8 (physical
                    // capacity fixed at compile time). The min/DEPENDING/KEY surface is captured in the OccursSpec.
                    if (occ.integerLiteral() is { Length: > 0 } lits && int.TryParse(lits[^1].GetText(), out int n))
                        occurs = n;
                    occursSpec = OdoBindOccursSpec(occ);
                    if (occ.INDEXED() is not null && occ.dataReferenceList() is { } idxList)
                        foreach (var idx in idxList.dataReference())
                            indexNames.Add(idx.GetText());
                }
            }

        // Parse the usage keyword ONCE per entry — ParseUsage carries the W2 loud-guard gates (the 2002+
        // skeleton usages error, ISO §13.18.60), and a re-parse would duplicate their diagnostics.
        string entryWhere = $"data item '{cobolName ?? "FILLER"}'";
        Usage entryUsage = PicInfo.ParseUsage(usageText, Edition, entryWhere, out bool skeletonUsage);

        // A PICTURE-less USAGE INDEX entry is an ELEMENTARY index data item (ISO §13.18.60 — class index, no
        // PICTURE allowed), not a group: synthesize its profile so it emits as a long occurrence-number field.
        // A PICTURE-less SKELETON usage (BINARY-CHAR / POINTER / FLOAT-x / NATIONAL / BIT —
        // legally picture-less per §13.18.60) gets the RECOVERY shape: the compile has already failed, and a
        // Pic-null elementary item NREs the doomed emit on any MOVE receiver (the binary_usage crash,
        // DEVLOG 597) instead of surfacing the 0899/0900. Group headers shed it in ResolveIndexItems.
        // USAGE OBJECT REFERENCE (LIVE — the Phase-3 OO spine): a PICTURE-less elementary reference item
        // (§13.18.60.4; the IndexItem synthesis pattern). PICTURE is prohibited with it — reject loud, never
        // let Analyze classify an incoherent picture-with-reference shape (the W2 silent-misbind rule).
        if (entryUsage is Usage.ObjectReference && pictureText is not null)
        {
            Edition.Error("COBOLNET0812", $"{entryWhere}: PICTURE may not be specified with USAGE OBJECT "
                + "REFERENCE (ISO §13.18.60.4 — an object-reference item is picture-less)");
            pictureText = null;
        }
        // A TYPED reference (spine part 2 — LIVE): the declared class must resolve in the group's pass-1
        // class symbol table (OO deep-dive D1) — its emitted C# field type IS the class's emitted type
        // (PicInfo.ClrType), so an unresolved name would surface as a Roslyn CS0246 on user source (a
        // loud-failure violation). §13.18.60.4: class-name-1 shall reference a class.
        if (entryUsage is Usage.ObjectReference && objectClassName is not null
            && OoClasses?.Find(objectClassName) is null && OoClasses?.FindInterface(objectClassName) is null)
            Edition.Error("COBOLNET0813", $"{entryWhere}: USAGE OBJECT REFERENCE names the unknown class or "
                + $"interface '{objectClassName}' — the declared name of a typed object reference shall be a "
                + "class or interface of the compilation group (ISO §13.18.60.2/.4; separate compilation is "
                + "a later slice)");

        // PICTURE is prohibited on a fixed-width binary usage (ISO §13.16.3 SR8 — the item is picture-less; its
        // width and range are fixed by the usage, §13.18.60.4 GR12). Reject loud, never let Analyze classify an
        // incoherent picture-with-binary shape (the W2 silent-misbind rule; the OBJECT REFERENCE 0812 pattern).
        if (entryUsage is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
            && pictureText is not null)
        {
            Edition.Error("COBOLNET0870", $"{entryWhere}: PICTURE may not be specified with a fixed-width binary "
                + "usage (BINARY-CHAR/-SHORT/-LONG/-DOUBLE) — the item is picture-less (ISO §13.16.3 SR8)");
            pictureText = null;
        }

        // PICTURE is prohibited with USAGE POINTER (§13.18.60.4 — a data-pointer is picture-less; before this
        // gate the entry silently misbound BY ITS PICTURE, the W2 hazard class). The 0881 declaration band.
        if (entryUsage is Usage.Pointer && pictureText is not null)
        {
            Edition.Error("COBOLNET0881", $"{entryWhere}: PICTURE may not be specified with USAGE POINTER — "
                + "a data-pointer item is picture-less (ISO §13.18.60.4)");
            pictureText = null;
        }

        var pic = pictureText is not null
            ? PicInfo.Analyze(pictureText, entryUsage, Edition, entryWhere, ownSign, CurrencyPicSymbol,
                blankWhenZero, explicitUsage: usageText is not null)
            : entryUsage is Usage.Index ? PicInfo.IndexItem
            : entryUsage is Usage.Pointer ? PicInfo.PointerItem
            : entryUsage is Usage.ObjectReference ? PicInfo.ObjectReferenceItem(objectClassName)
            : entryUsage is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
                ? PicInfo.BinaryItem(entryUsage, signed: !binaryUnsigned)
            // A PICTURE-less USAGE NATIONAL/BIT entry is a GROUP header (legal — the usage sheds to
            // subordinates, §13.18.60.4 GR1) or an illegal picture-less elementary item (0881) — unknowable
            // until the forest is complete: ResolveIndexItems adjudicates via these marker shapes.
            : entryUsage is Usage.National ? PicInfo.NationalUsagePending
            : entryUsage is Usage.Bit ? PicInfo.BitUsagePending
            : skeletonUsage ? PicInfo.RecoveryItem : null;

        // Edition gating (the four-compilers rule): a fixed-point picture's digit positions are capped at 18 by
        // COBOL-85 and 31 by 2002+ (ISO §8.3.1.2 / §13.18.40) — reject, never silently mis-store.
        if (pic is { Category: PicCategory.Numeric or PicCategory.NumericEdited, IsFloat: false, Digits: > 0 })
            Edition.CheckDigitCapacity(pic.Digits, $"data item '{cobolName ?? "FILLER"}' (PICTURE {pictureText})");

        // VALUE-clause literal/category conformance for the string-stored 2002 categories (ISO §13.18.63
        // SR5 national / SR10 boolean — the 0898 band, both directions).
        if (rawValue is { } rv && pic is not null) ValidateValueCategory(pic, rv, entryWhere);
        var item = new DataItem
        {
            Level = level,
            CobolName = isFiller ? null : cobolName,
            CsName = csName,
            Pic = pic,
            OwnSign = ownSign,
            OwnUsage = usageText is not null ? entryUsage : null,
            RawValue = rawValue,
            Occurs = occurs,
            OccursSpec = occursSpec,
            RedefinesTargetName = redefinesTargetName,
            Justified = justified,
            BlankWhenZero = blankWhenZero,
        };

        // BASED declaration validation (the 0881 declaration-entry band; Phase-4b increment 2): §13.16 SR16 —
        // a BASED entry is a level-01/77 record-description entry (WS/LS/LINKAGE; the file-subsystem sweep is
        // a named residue); §13.18.5 SRs — REDEFINES and BASED are mutually exclusive (:17215) and a VALUE
        // clause cannot seed storage the item does not own. Violations clear the flag so the item binds as
        // ordinary storage under an already-failed compile (never a half-based state).
        if (isBased)
        {
            if (level is not (1 or 77))
            {
                Edition.Error("COBOLNET0881", $"{entryWhere}: the BASED clause may be specified only in a "
                    + "level-01 or level-77 entry (ISO §13.16 SR16 / §13.18.5)");
                isBased = false;
            }
            else if (redefinesTargetName is not null)
            {
                Edition.Error("COBOLNET0881", $"{entryWhere}: BASED and REDEFINES may not be specified "
                    + "together (ISO §13.18.5 SR)");
                isBased = false;
            }
            else if (hasExternal)
            {
                // §13.16.3 SR5: "The EXTERNAL clause shall not be specified in the same data description
                // entry as the REDEFINES or BASED clause" — without this, BOTH mechanisms would emit a
                // bridge under the ONE BackingCsName (a CS0102 duplicate member, the review finding).
                Edition.Error("COBOLNET0881", $"{entryWhere}: BASED and EXTERNAL may not be specified "
                    + "together (ISO §13.16.3 SR5)");
                isBased = false;
            }
            // A VALUE clause on a BASED entry is LEGAL (its data seeds ALLOCATE … INITIALIZED per §14.9.3
            // GR7's TO-VALUE leg); without INITIALIZED the allocated content is undefined (GR8), so the
            // space-filled cell is conformant — the clause simply has no stored field to seed here.
        }
        item.IsBased = isBased;

        // Register each INDEXED BY index-name as a distinct C# long field (1-based occurrence number, §3.5).
        foreach (var idxName in indexNames)
        {
            item.IndexNames.Add(idxName);
            if (!IndexFields.ContainsKey(idxName))
                IndexFields[idxName] = "_IX_" + IndexFields.Count;
        }
        return item;
    }

    /// <summary>Extract a usage clause's canonical keyword text by TOKEN inspection — never string-stripping.
    /// The full form (<c>USAGE [IS] usageKeyword [binarySign]</c>) carries the keyword in its
    /// <c>usageKeyword</c> child; a bare-keyword alternative (the USAGE word is optional, ISO §13.18.60 general
    /// format) carries the keyword TERMINAL as the clause's FIRST child with an optional <c>binarySign</c>
    /// sibling (SIGNED/UNSIGNED on the BINARY-CHAR/-SHORT/-LONG/-DOUBLE family). The historical
    /// <c>GetText()</c>-and-strip fallback GLUED the sign phrase into the keyword (bare <c>BINARY-CHAR
    /// SIGNED</c> → <c>"BINARY-CHARSIGNED"</c>, which then silently misbound to DISPLAY — the W2 loud-guard
    /// sweep), and even bare <c>DISPLAY</c> survived it only by accident. <c>USAGE OBJECT REFERENCE</c> (a rule,
    /// not one terminal) canonicalizes to <c>"OBJECT REFERENCE"</c> — its class-name operand is not part of the
    /// keyword.</summary>
    private static string UsageKeyword(Core.UsageClauseContext usage)
    {
        if (usage.usageKeyword() is { } kw)
            return kw.objectReferenceUsage() is not null ? "OBJECT REFERENCE" : kw.GetText();
        return usage.GetChild(0).GetText();
    }

    /// <summary>Extract the first VALUE operand's raw source text (literal or figurative constant). THRU ranges /
    /// 88-levels are later. The emitter (<c>FieldEmitter</c>) interprets the text — including figurative constants
    /// such as ZERO/SPACE — against the item's category and width. A numeric literal is normalized to the
    /// canonical dot-decimal form (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a).</summary>
    private string? ExtractValue(Core.ValueClauseContext value)
    {
        var item = value.valueItem().FirstOrDefault();
        return item?.GetText() is { } raw ? NormalizeIfNumericLiteral(raw) : null;
    }

    /// <summary>Resolve PICTURE-less USAGE INDEX entries (ISO §13.18.60) once the forest is complete — entry bind
    /// synthesized an elementary index profile (<see cref="PicInfo.IndexItem"/>) before subordinates were known. An
    /// entry WITH subordinates is a GROUP whose USAGE INDEX merely inherits (GR1 — usage on a group applies to each
    /// elementary item under it): clear the synthesized profile; a PICTURE-less LEAF below it is an index data item
    /// even without its own USAGE clause.</summary>
    private void ResolveIndexItems()
    {
        // Every elementary leaf under a group (for the NATIONAL/BIT group-usage conformance check below).
        static IEnumerable<DataItem> Leaves(DataItem g)
        {
            foreach (var c in g.Children)
                if (c.Children.Count > 0) foreach (var l in Leaves(c)) yield return l;
                else yield return c;
        }

        void Walk(DataItem item, bool inherited, PicInfo? inheritedObjRef)
        {
            bool isIndex = ReferenceEquals(item.Pic, PicInfo.IndexItem) || (inherited && item.Pic is null);
            // USAGE OBJECT REFERENCE inherits the same way (§13.18.60.4 GR1): a group header sheds its
            // synthesized reference profile; a PICTURE-less leaf below takes it (sharing the immutable
            // PicInfo — the declared class flows down with it).
            var objRef = item.Pic is { Category: PicCategory.ObjectReference } p ? p : inheritedObjRef;
            if (item.Children.Count > 0)
            {
                if (ReferenceEquals(item.Pic, PicInfo.IndexItem)) item.Pic = null;   // a group, not an elementary index
                // A skeleton-usage RECOVERY shape on a GROUP header sheds the same way (the usage merely
                // inherits per §13.18.60.4 GR1; a Pic'd "group" would stop grouping — DEVLOG 597).
                if (ReferenceEquals(item.Pic, PicInfo.RecoveryItem)) item.Pic = null;
                // USAGE NATIONAL / BIT on a GROUP header sheds per §13.18.60.4 GR1 — with the SR12/SR5
                // conformance check over the subordinate leaves (each leaf's own PICTURE has already
                // classified it): under NATIONAL a leaf must be national (fine), boolean/numeric (spec-legal
                // national FORMS — staged, the Analyze 0899 legs), never alphabetic/alphanumeric; under BIT
                // every leaf must be boolean (SR5).
                if (ReferenceEquals(item.Pic, PicInfo.NationalUsagePending))
                {
                    item.Pic = null;
                    foreach (var l in Leaves(item))
                        if (l.Pic is { Category: PicCategory.Boolean or PicCategory.Numeric or PicCategory.NumericEdited })
                            Edition.Error("COBOLNET0899", "national-form data (a boolean or numeric item "
                                + $"under a group USAGE NATIONAL) is recognized but not yet implemented "
                                + $"(Phase 4a residue) — data item '{l.CobolName ?? "FILLER"}' "
                                + "(ISO §13.18.60.3 SR12 / §13.18.60.4 GR1)");
                        else if (l.Pic is not null and not { Category: PicCategory.National })
                            Edition.Error("COBOLNET0881", $"data item '{l.CobolName ?? "FILLER"}': USAGE "
                                + "NATIONAL inherited from its group admits boolean, national, "
                                + "national-edited, numeric, and numeric-edited pictures only "
                                + "(ISO §13.18.60.3 SR12 / §13.18.60.4 GR1; §13.18.40.3 SR30)");
                }
                if (ReferenceEquals(item.Pic, PicInfo.BitUsagePending))
                {
                    item.Pic = null;
                    foreach (var l in Leaves(item))
                        if (l.Pic is not null and not { Category: PicCategory.Boolean })
                            Edition.Error("COBOLNET0881", $"data item '{l.CobolName ?? "FILLER"}': USAGE BIT "
                                + "inherited from its group requires a boolean PICTURE (symbol 1 only) "
                                + "(ISO §13.18.60.3 SR5 / §13.18.60.4 GR1)");
                }
                if (item.Pic is { Category: PicCategory.ObjectReference }) item.Pic = null;
                // A synthesized fixed-width binary profile on a GROUP header sheds the same way (the usage
                // merely inherits per §13.18.60.4 GR1). Group-level BINARY-* over PICTURE'd children is a spec
                // corner with no corpus surface (PICTURE is §13.16.3 SR8-illegal on the family) — left to a
                // later slice, mirroring the float-on-group deferral in InheritUsageClauses.
                if (item.Pic is { Category: PicCategory.Numeric, Usage: Usage.BinaryChar or Usage.BinaryShort
                        or Usage.BinaryLong or Usage.BinaryDouble }) item.Pic = null;
                foreach (var c in item.Children) Walk(c, isIndex, objRef);
            }
            else if (ReferenceEquals(item.Pic, PicInfo.NationalUsagePending)
                     || ReferenceEquals(item.Pic, PicInfo.BitUsagePending))
            {
                // A PICTURE-less ELEMENTARY item may not carry USAGE NATIONAL/BIT — they are not among the
                // picture-less usages (§13.18.60.4; contrast INDEX/POINTER/OBJECT REFERENCE/BINARY-x). The
                // recovery shape keeps the doomed emit crash-free (the DEVLOG-597 pattern).
                Edition.Error("COBOLNET0881", $"data item '{item.CobolName ?? "FILLER"}': an elementary item "
                    + $"with USAGE {(ReferenceEquals(item.Pic, PicInfo.BitUsagePending) ? "BIT" : "NATIONAL")} "
                    + "requires a PICTURE clause (ISO §13.18.60.4 — not a picture-less usage)");
                item.Pic = PicInfo.RecoveryItem;
            }
            else if (isIndex && item.Pic is null)
                item.Pic = PicInfo.IndexItem;
            else if (item.Pic is null && objRef is not null)
                item.Pic = objRef;
        }
        foreach (var root in Roots) Walk(root, false, null);
    }

    /// <summary>Apply group-level USAGE clauses to subordinate elementary items (ISO §13.18.60 GR1 — "the USAGE
    /// clause of a group item applies to each elementary item subordinate to it"; the nearest enclosing clause
    /// wins, an item's OWN clause outright). Scope: the binary/packed integer usages (NC107A's
    /// <c>01 U9 USAGE COMPUTATIONAL</c> with PICTURE-only children) — USAGE INDEX inheritance is
    /// <see cref="ResolveIndexItems"/>'s special case (PICTURE-less index items), and a float usage on a group
    /// with PICTUREd children has no NIST surface (left to the float slice). Runs BEFORE
    /// <see cref="InheritSignClauses"/> — a non-DISPLAY item takes the BinaryMinus sign form regardless of any
    /// inherited SIGN clause (§13.18.52 applies only to usage-display items).</summary>
    private void InheritUsageClauses()
    {
        static void Walk(DataItem item, Usage? inherited)
        {
            Usage? effective = item.OwnUsage ?? inherited;
            if (item.OwnUsage is null
                && effective is Usage.Binary or Usage.Packed or Usage.Comp5
                && item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display } pic)
                item.Pic = pic with
                {
                    Usage = effective.Value,
                    SignKind = PicInfo.SignKindFor(effective.Value, pic.Signed, item.OwnSign),
                };
            foreach (var c in item.Children) Walk(c, effective);
        }
        foreach (var root in Roots) Walk(root, null);
    }

    /// <summary>Apply group-level SIGN clauses to subordinate signed numeric DISPLAY items (ISO §13.18.52 GR1–3):
    /// a SIGN on a group applies to every signed numeric item subordinate to it, the NEAREST enclosing clause takes
    /// precedence, and an item's OWN clause (already consumed by <see cref="PicInfo.Analyze"/> at entry bind) wins
    /// outright. Runs BEFORE the REDEFINES classification pass because a SEPARATE sign occupies its own character
    /// position (§13.18.52 GR6a) — it widens the item's image, which feeds the class-max width.</summary>
    private void InheritSignClauses()
    {
        static void Walk(DataItem item, SignSpec? inherited)
        {
            SignSpec? effective = item.OwnSign ?? inherited;
            if (item.OwnSign is null && effective is not null
                && item.Pic is { Category: PicCategory.Numeric, Signed: true, Usage: Usage.Display } pic)
                item.Pic = pic with { SignKind = PicInfo.SignKindFor(pic.Usage, signed: true, effective) };
            foreach (var c in item.Children) Walk(c, effective);
        }
        foreach (var root in Roots) Walk(root, null);
    }

    // ── REDEFINES / RENAMES resolution + classification (post-build, ISO §13.18.44/45) ───────────────────────

    /// <summary>Resolve each item's REDEFINES target name to its <see cref="DataItem"/>, and each level-66 RENAMES
    /// FROM/THRU operand to its item. A REDEFINES target is an unqualified prior entry in the same scope (SR1/SR6); a
    /// RENAMES range names items within the owning record (SR3). Target resolution does not chase chains — the
    /// classification pass walks <see cref="DataItem.RedefinesTarget"/> transitively to the anchor (SR11).</summary>
    private void ResolveRedefines()
    {
        foreach (var item in AllItems())
            if (item.RedefinesTargetName is { } tname)
            {
                IReadOnlyList<DataItem> scope = item.Parent?.Children ?? Roots;
                item.RedefinesTarget = scope.FirstOrDefault(s =>
                    !ReferenceEquals(s, item) && string.Equals(s.CobolName, tname, StringComparison.OrdinalIgnoreCase));
            }

        foreach (var root in Roots)
            foreach (var ren in root.Renames66)
            {
                var info = ren.Renames!;
                info.From = FindDescendantOrSelf(root, info.FromName);
                info.Thru = info.ThruName is { } t ? FindDescendantOrSelf(root, t) : null;
                if (info.From is null || (info.ThruName is not null && info.Thru is null)) continue;
                // The no-THRU alias inherits the renamed item's description (§13.18.45 GR1) — the resolver
                // forwards to the FROM item's place; no span, no synthetic alphanumeric picture.
                if (info.Thru is null) { ren.Pic = info.From.Pic; continue; }

                // The alias spans the record's contiguous leaf run FROM..THRU (§13.18.45 GR1/GR2); the alias item
                // itself reads/writes as one elementary ALPHANUMERIC item of the span's width (its category per
                // GR — a re-grouping, always treated as an alphanumeric data item when referenced as a whole).
                var leaves = new List<DataItem>();
                void Walk(DataItem n) { if (n.IsElementary) leaves.Add(n); else foreach (var c in n.Children) Walk(c); }
                Walk(root);
                int start = leaves.FindIndex(l => ReferenceEquals(l, info.From) || IsUnder(l, info.From));
                DataItem last = info.Thru ?? info.From;
                int end = leaves.FindLastIndex(l => ReferenceEquals(l, last) || IsUnder(l, last));
                if (start < 0 || end < start) continue;
                info.SpanLeaves.AddRange(leaves[start..(end + 1)]);
                ren.Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display,
                    Length: info.SpanLeaves.Sum(l => l.ImageWidth * (l.Occurs ?? 1)), Digits: 0, Scale: 0, Signed: false);
            }

        static bool IsUnder(DataItem leaf, DataItem ancestor)
        {
            for (DataItem? n = leaf; n is not null; n = n.Parent)
                if (ReferenceEquals(n, ancestor)) return true;
            return false;
        }
    }

    /// <summary>Group every redefining entry with the non-redefining anchor it ultimately overlays (SR7/SR11) into a
    /// <see cref="RedefinesClass"/>, mark the anchor canonical and every other member a view, then assign the class a
    /// tier (D &gt; C &gt; B &gt; A) and its class-max width, and propagate view-suppression to each view's
    /// subordinates (SR9 — no VALUE on a subordinate of a redefiner). (COBOLNET_DESIGN §4.2.)</summary>
    private void ClassifyRedefinesClasses()
    {
        var byAnchor = new Dictionary<DataItem, RedefinesClass>();
        foreach (var item in AllItems())
        {
            if (item.RedefinesTarget is null) continue;
            DataItem anchor = item;
            while (anchor.RedefinesTarget is { } t) anchor = t;     // chase the chain to the original (SR11)
            if (!byAnchor.TryGetValue(anchor, out var cls))
            {
                cls = new RedefinesClass { Canonical = anchor };
                cls.Members.Add(anchor);
                anchor.Class = cls;
                byAnchor[anchor] = cls;
            }
            cls.Members.Add(item);
            item.Class = cls;
            item.IsCanonical = false;
        }

        // ONE shared area per overlay nest (ISO §13.18.44 — a REDEFINES nested under another class's member
        // shares THAT storage): a class whose ANCHOR lies inside another class's subtree — its ancestor chain
        // crosses a redefining member or another class's anchor (NC252A's `RDEF8 REDEFINES RDF8` under the view
        // `REDEF11 REDEFINES REDEF10`) — DISSOLVES into the outer class. The outer subtree walk below assigns
        // every nested item its window over the one backing (an inner redefiner starts at its target's
        // already-assigned offset), so a dissolved class needs no members of its own; keeping it would let its
        // later walk re-claim the subtree and emit a backing inside a suppressed view struct (CS0103).
        foreach (var (anchor, cls) in byAnchor.ToList())
            for (var a = anchor.Parent; a is not null; a = a.Parent)
                if (a.RedefinesTargetName is not null || (byAnchor.ContainsKey(a) && !ReferenceEquals(byAnchor[a], cls)))
                {
                    byAnchor.Remove(anchor);
                    break;
                }

        foreach (var cls in byAnchor.Values)
        {
            cls.Tier = ComputeTier(cls, out string? reject);
            cls.RejectReason = reject;
            cls.Width = cls.Members.Max(m => m.ImageWidth * (m.Occurs ?? 1));   // a member table's FULL extent (every occurrence)
            // Each top-level member overlays the area from its start (a REDEFINES begins at the target's first
            // position, SR10); a subordinate accumulates its window offset within the member. Subordinates of any
            // member are themselves views (suppressed field, SR9).
            foreach (var member in cls.Members)
                AssignClassOffsets(member, 0, cls);
            // A Tier-B (string-canonical) numeric-DISPLAY view reads/writes its window through the character pipeline
            // (CobolNum.ParseDisplay / FormatDisplay) — the same StoreAsImage path used for whole-group numeric leaves.
            if (cls.Tier == RedefinesTier.StringCanonical)
                foreach (var leaf in cls.Members.SelectMany(LeavesOf))
                {
                    if (leaf.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                        leaf.StoreAsImage = true;
                    else if (leaf.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Binary or Usage.Packed } bp)
                    {
                        // A fixed-point BINARY/PACKED leaf of a Tier-B class is image-stored too: its window over
                        // the one string backing IS its zoned digit image (ISO §13.18.60 USAGE GR4 — implementor
                        // representation; COBOLNET_DESIGN §14.4). Its profile MUST be rewritten to describe that
                        // zoned storage: every accessor (EmitArithAssign stores, NumericRenderer reads, the
                        // window splice) threads `_P_`, and the leaf's declared BinaryMinus form is VARIABLE
                        // width (a leading '-' only when negative) — FormatDisplay(BinaryMinus) would write a
                        // Digits+1 image into the Digits-wide window and corrupt the value (and every following
                        // leaf). NOTE the observable consequence: DISPLAY of such a leaf shows the zoned
                        // overpunch image (like any signed zoned item), not the '-100' binary-minus form — the
                        // conformant face of the GR4 license. The DigitCount truncation discipline is unchanged
                        // (BINARY truncates by digit count; PACKED's 2n−1 over-capacity digits cannot survive an
                        // image round trip — standard stores never create them without implementor permission).
                        leaf.StoreAsImage = true;
                        leaf.Pic = bp with { SignKind = bp.ImageSignKind };
                    }
                }
        }
    }

    /// <summary>Assign each item in a redefines class its window offset within the class image and its class link; a
    /// top-level member starts at <paramref name="off"/> (0), a subordinate accumulates by preceding-sibling FULL
    /// extents (per-occurrence image width × OCCURS count — every occurrence is part of the layout). A subordinate
    /// that itself REDEFINES a prior sibling takes the TARGET's offset (redefinition begins at the redefined item's
    /// first position, ISO §13.18.44 GR1) and contributes NO width of its own. Every subordinate of a class member
    /// is itself a view (its stored field is suppressed — SR9).</summary>
    private static void AssignClassOffsets(DataItem item, int off, RedefinesClass cls)
    {
        item.ClassOffset = off;
        item.Class = cls;
        int childOff = off;
        foreach (var c in item.Children)
        {
            c.IsCanonical = false;
            // The inner-REDEFINES target is a PRIOR sibling, so its offset is already assigned this walk.
            int cOff = c.RedefinesTarget is { } target ? target.ClassOffset : childOff;
            AssignClassOffsets(c, cOff, cls);
            if (c.RedefinesTarget is null) childOff += c.ImageWidth * (c.Occurs ?? 1);
        }
    }

    /// <summary>Assign a redefines class its tier (COBOLNET_DESIGN §4.2 cascade D &gt; C &gt; B &gt; A). Tier C (the
    /// confined byte[] codec for a genuine mixed-USAGE pun) is not yet implemented, so a class that would be Tier C is
    /// loudly rejected in the interim — a conformant diagnostic on a legal-but-unimplemented construct.</summary>
    private static RedefinesTier ComputeTier(RedefinesClass cls, out string? reject)
    {
        reject = null;
        var leaves = cls.Members.SelectMany(LeavesOf).ToList();

        // Tier C → Rejected (interim): any leaf is float (COMP-1/2 — no fixed decimal-digit width), COMP-5 (its
        // BinaryCapacity discipline stores values EXCEEDING the PICTURE digit count — a Digits-wide character
        // window cannot carry them), or INDEX (no character image, §13.18.60). A DISPLAY + BINARY/PACKED mix is
        // Tier B: under the digit-image representation (ISO §13.18.60 USAGE GR4 — the representation, including
        // the sign, is implementor-defined; COBOLNET_DESIGN §4.2/§14.4) one string backing IS the shared area —
        // exactly §12.4.6.4.4 SAME RECORD AREA GR2's "equivalent to an implicit redefinition of the area, with
        // records aligned on the leftmost byte position". Its binary/packed leaves become zoned windows (the
        // StoreAsImage loop in ClassifyRedefinesClasses). (No pointer/object/strongly-typed items exist in the
        // bound model yet → no Tier-D check.)
        if (leaves.Any(l => l.Pic is { } p && (p.IsFloat
            || p.Usage is Usage.Comp5 or Usage.Index
                or Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble)))
        {
            reject = $"float/COMP-5/BINARY-*/INDEX REDEFINES of '{cls.Canonical.CobolName}' (Tier-C byte path) not yet implemented";
            return RedefinesTier.Rejected;
        }

        // A NATIONAL leaf: §13.18.44 lays the shared area in BYTES, and the documented 2-byte national
        // character (D-N1/D-N2) has no char-window overlay over the single-byte members — recognized, staged
        // loud (Phase 4a residue: per-item byte offsets + UTF-16LE class images). BOOLEAN leaves fall through
        // legitimately (one '0'/'1' char = one byte, D-B1).
        if (leaves.Any(l => l.Pic is { Category: PicCategory.National }))
        {
            reject = $"REDEFINES over national data in '{cls.Canonical.CobolName}' (the 2-byte national "
                + "character has no single-byte char-window overlay) not yet implemented (Phase 4a residue)";
            return RedefinesTier.Rejected;
        }

        // Tier A — every member is an elementary item sharing the canonical's CLR storage type AND its image width:
        // one stored field, the rest pass-throughs (a numeric view reinterprets the shared value via its own scale).
        DataItem canon = cls.Canonical;
        bool allAlias = canon.IsElementary && cls.Members.All(m =>
            m.IsElementary && m.ElementType == canon.ElementType && m.ImageWidth == canon.ImageWidth);
        if (allAlias) return RedefinesTier.Alias;

        // Tier B — DISPLAY-homogeneous: one string canonical of class-max width, each view an (offset,width) accessor.
        return RedefinesTier.StringCanonical;
    }

    /// <summary>The D-N2 byte-surface gate for FILE records: the record codec reads/writes single-byte
    /// characters (Latin-1, <c>SequentialFile</c>), and a national leaf occupies TWO bytes per position under
    /// the documented D-N1 representation — a national leaf in an FD/SD record would silently halve its
    /// positions on disk. Recognized, staged loud (Phase 4a residue: the 2-byte national record layout).
    /// Boolean leaves flow — one '0'/'1' character IS one byte (D-B1).</summary>
    private void GateNationalRecords()
    {
        foreach (var f in Files)
            foreach (var rec in f.Records)
                foreach (var leaf in LeavesOf(rec))
                    if (leaf.Pic is { Category: PicCategory.National })
                        Edition.Error("COBOLNET0899", $"national data in a file record (data item "
                            + $"'{leaf.CobolName ?? "FILLER"}' of record '{rec.CobolName}') is recognized but "
                            + "not yet implemented — the record codec is single-byte and the national "
                            + "character is two bytes (Phase 4a residue; ISO §8.1.2 / §13.18.60.4 GR8)");
    }

    /// <summary>Every item in the WORKING-STORAGE forest, in declaration (pre-order DFS) order.</summary>
    private IEnumerable<DataItem> AllItems()
    {
        static IEnumerable<DataItem> Walk(DataItem d)
        {
            yield return d;
            foreach (var c in d.Children)
                foreach (var x in Walk(c)) yield return x;
        }
        return Roots.SelectMany(Walk);
    }

    /// <summary>The elementary leaves of an item (itself if elementary), in source order.</summary>
    private static IEnumerable<DataItem> LeavesOf(DataItem d)
    {
        if (d.IsElementary) { yield return d; yield break; }
        foreach (var c in d.Children)
            foreach (var l in LeavesOf(c)) yield return l;
    }

    /// <summary>Find an item by COBOL name within a record subtree (the item itself or any descendant).</summary>
    private static DataItem? FindDescendantOrSelf(DataItem root, string name)
    {
        if (string.Equals(root.CobolName, name, StringComparison.OrdinalIgnoreCase)) return root;
        foreach (var c in root.Children)
            if (FindDescendantOrSelf(c, name) is { } f) return f;
        return null;
    }

    /// <summary>Find a (possibly qualified) item within a record subtree: the base name matches the item and
    /// every IN/OF qualifier matches SOME ancestor, in written (innermost→outermost) order with skips allowed —
    /// ISO §8.4.2.2 qualification. Identically-named items under different areas are legal and disambiguated by
    /// their qualifiers (IX215A's three same-named keys).</summary>
    private static DataItem? FindQualified(DataItem root, string name, IReadOnlyList<string> quals)
    {
        if (string.Equals(root.CobolName, name, StringComparison.OrdinalIgnoreCase) && QualsMatch(root, quals))
            return root;
        foreach (var c in root.Children)
            if (FindQualified(c, name, quals) is { } f) return f;
        return null;

        static bool QualsMatch(DataItem item, IReadOnlyList<string> quals)
        {
            int qi = 0;
            for (DataItem? a = item.Parent; a is not null && qi < quals.Count; a = a.Parent)
                if (string.Equals(a.CobolName, quals[qi], StringComparison.OrdinalIgnoreCase)) qi++;
            return qi == quals.Count;
        }
    }
}
