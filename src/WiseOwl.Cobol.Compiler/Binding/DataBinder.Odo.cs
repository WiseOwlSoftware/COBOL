// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>
/// The OCCURS DEPENDING ON half of the data binder (ISO/IEC 1989:2023 §2023:13.18.38): Format-2 clause capture
/// (<see cref="OdoBindOccursSpec"/>) and the post-build resolution + structural-validation pass
/// (<see cref="OdoResolvePass.Run"/>). Partial-class extension over <c>DataBinder</c> — the entry binder stores the array
/// capacity (the MAXIMUM, §2023:8.5.1.8) in <see cref="DataItem.Occurs"/> and the bounds/DEPENDING/KEY surface here.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>Capture an OCCURS clause's structured description: the Format-2 <c>integer-1 TO integer-2</c>
    /// bounds and DEPENDING ON name, plus the ASCENDING/DESCENDING KEY data-names (Formats 1 and 2, §2023:13.18.38
    /// GR3). Returns <see langword="null"/> for a plain keyless fixed table — <see cref="DataItem.Occurs"/>
    /// alone carries those (the dominant case stays allocation-free).</summary>
    private OccursSpec? OdoBindOccursSpec(Core.OccursClauseContext occ, string where, int? maxBound)
    {
        NarrowToDataDivisionFormats(occ, where);
        bool depending = occ.DEPENDING() is not null;
        // ONE list in PHRASE ORDER — ISO §2023:13.18.38.4 GR3 "If more than one data-name-2 is specified, they are
        // specified in descending order of significance", which is what §2023:14.9.37.3 SR11 is a rule about. Splitting
        // the phrase into per-direction lists would lose the relative order of a mixed
        // `ASCENDING KEY IS A B DESCENDING KEY IS C`. The name AND ITS QUALIFIERS (kb/Work PB1018): §2023:13.18.38.3 SR3
        // confines data-name-2 to the OCCURS entry or an entry subordinate to it, but two subordinates may share the
        // name, and then `K OF B` is the only thing that says which one is the key (§2023:8.4.2.2.3 SR1). The capture
        // used to keep the base word alone, and the lookup took the first K under the table.
        var keys = new List<OccursKey>();
        foreach (var kc in occ.occursKeyClause())
        {
            bool descending = kc.DESCENDING() is not null;
            // SCREENED first (kb/Work PB885): §2023:13.18.38.3 SR2 "Data-name-1 and data-name-2 shall not be
            // subscripted" and SR5 "Data-name-2 shall be specified without the subscripting normally required" —
            // a written subscript used to be dropped here in silence. A refused key is not recorded: the
            // refusal is the verdict, and a key named by its written spelling would only draw a second one.
            foreach (var k in kc.dataReference())
            {
                var (keyName, keyQuals) = ClauseDataName(k, $"{where}: OCCURS KEY IS");
                if (!State.Clauses.RefusedClauseOperands.Contains(keyName)) keys.Add(new OccursKey(keyName, keyQuals, descending));
            }
        }

        // Format 4 — a DYNAMIC-capacity table (§2023:13.18.38 Format 4, D9): capture CAPACITY IN / FROM / TO / INITIALIZED
        // (each at most once and in the printed order — NarrowToDataDivisionFormats has refused anything else, so a
        // phrase here is the only one of its kind). ALWAYS returns a spec (a keyless dynamic table still needs IsDynamic recorded,
        // unlike a keyless fixed table where DataItem.Occurs alone suffices). DataItem.Occurs stays null — a dynamic
        // table has no fixed physical capacity; its storage is the out-of-line CobolDynTable.
        if (occ.DYNAMIC() is not null)
        {
            // COBOL-2014 introduction gate: VersionConformancePass ParseArm.VisitOccursClause (rearch 14g.3,
            // recognition — once per source occursClause; a per-DataItem bound-arm walk would over-count TYPE clones).
            string? capName = null; int? fromCap = null; int? toCap = null; bool initialized = false;
            foreach (var ph in occ.occursDynamicPhrase())
            {
                if (ph.CAPACITY() is not null && ph.dataReference() is { } capRef)
                    capName = CapacityRegisterName(capRef, where);
                else if (ph.INITIALIZED() is not null) initialized = true;
                else if (ph.FROM() is not null && ph.integerOperand() is { } fl)
                {
                    if (Constants.IntegerOperandValue(fl, where) is { } from) fromCap = DynamicBoundWithinMaximum(ph, "FROM", from);
                }
                else if (ph.TO() is not null && ph.integerOperand() is { } tl)
                {
                    if (Constants.IntegerOperandValue(tl, where) is { } to) toCap = DynamicBoundWithinMaximum(ph, "TO", to);
                }
            }
            var dyn = new OccursSpec
            {
                Min = fromCap ?? 0, Max = 0, IsDynamic = true,
                CapacityName = capName, InitialCap = fromCap, ExpectedMax = toCap, Initialized = initialized,
            };
            dyn.Keys.AddRange(keys);
            return dyn;
        }
        if (!depending && keys.Count == 0) return null;

        // Each fixed bound is an integer literal or an integer constant-name (§2023:13.10.3 SR2); the caller already
        // resolved the LAST bound (the maximum) via IntegerOperandValue — <paramref name="maxBound"/> — so an
        // unresolvable bound reports exactly once. Only integer-1 of a Format-2 pair resolves here.
        var bounds = occ.integerOperand();
        int max = maxBound ?? 0;
        // Format 2 is `OCCURS integer-1 TO integer-2 … DEPENDING …` (§2023:13.18.38 general formats); `OCCURS n
        // DEPENDING` without TO was REFUSED by NarrowToDataDivisionFormats (kb/Work PB1265) and binds here, under
        // that failed compile, with minimum 1 only so the rest of the entry still binds. Min feeds only the SR16
        // check and the later EC-BOUND-ODO bounds — allocation is ALWAYS Max (§2023:8.5.1.8).
        int min = !depending ? max
            : bounds.Length > 1 ? Constants.IntegerOperandValue(bounds[0], where) ?? 1
            : 1;
        // data-name-1 — a QUALIFIED-DATA-NAME through the ONE data-name-n capture (kb/Work PB885). The whole
        // reference's GetText() stood here: `DEPENDING ON CNT OF G1` became the undefined name `CNTOFG1` (legal
        // source rejected), and `DEPENDING ON WS-TE (2)` drew "not defined" instead of §2023:13.18.38.3 SR2.
        string? depName = null;
        IReadOnlyList<string> depQuals = [];
        if (depending && occ.dataReference() is { } depRef)
            (depName, depQuals) = ClauseDataName(depRef, $"{where}: OCCURS DEPENDING ON");
        var spec = new OccursSpec
        {
            Min = min,
            Max = max,
            DependingName = depName,
            DependingQualifiers = depQuals,
        };
        spec.Keys.AddRange(keys);
        return spec;
    }

    /// <summary>⛔ THE DATA DIVISION'S OCCURS CLAUSE IS FORMAT 1, 2 OR 4 — NARROWED HERE FROM THE GRAMMAR'S SUPERSET
    /// (kb/Work PB1265). <c>occursClause</c> parses every phrase of every format optionally and Format 4's phrases in
    /// any order and number, so that the refusal can name the rule; the report section narrows the same parse to
    /// Format 3 (<c>ReportSectionBinder.cs#ReportOccursOf</c>), and this is the other arm. ISO §2023:13.18.38.2:
    /// <list type="bullet">
    /// <item>Format 2 prints <c>integer-1 TO integer-2 TIMES DEPENDING ON data-name-1</c> with nothing bracketed, so a
    /// TO without DEPENDING and a DEPENDING without TO are each in no data-division format (the bracketed
    /// <c>[ integer-1 TO ]</c> and <c>[ DEPENDING … ]</c> are Format 3's, the report writer's).</item>
    /// <item>STEP integer-3 exists only in Format 3.</item>
    /// <item>Format 4 prints <c>[ CAPACITY IN data-name-3 ] [ FROM integer-4 ] [ TO integer-5 ] [ INITIALIZED ]</c>:
    /// each phrase at most once, and — §2023:5.2.1, "The words, phrases, clauses, punctuation, and operands in each
    /// general format shall be written in the compilation group in the sequence given in the general format, unless
    /// otherwise specified by the rules of that format" — in that order. A repeated FROM used to bind its last value
    /// in silence.</item>
    /// </list>
    /// Each violation is COBOL2789; the entry still binds (under the failed compile) so its other clauses are
    /// checked.</summary>
    private static readonly string[] Format4PhraseNames = ["CAPACITY IN", "FROM", "TO", "INITIALIZED"];

    private void NarrowToDataDivisionFormats(Core.OccursClauseContext occ, string where)
    {
        void Refuse(Antlr4.Runtime.ParserRuleContext at, string what)
        {
            using var _ = Edition.At(at);
            Edition.Error(DiagnosticCatalog.OccursFormatNotPrinted, $"{where}: OCCURS clause — {what}");
        }
        if (occ.DYNAMIC() is not null)
        {
            static int Rank(Core.OccursDynamicPhraseContext p) =>
                p.CAPACITY() is not null ? 0 : p.FROM() is not null ? 1 : p.TO() is not null ? 2 : 3;
            var names = Format4PhraseNames;
            int last = -1;
            foreach (var ph in occ.occursDynamicPhrase())
            {
                int r = Rank(ph);
                if (r == last)
                    Refuse(ph, $"the {names[r]} phrase is written twice; Format 4 prints each of its phrases once "
                        + "(ISO §2023:13.18.38.2: OCCURS DYNAMIC [ CAPACITY IN data-name-3 ] [ FROM integer-4 ] "
                        + "[ TO integer-5 ] [ INITIALIZED ])");
                else if (r < last)
                    Refuse(ph, $"the {names[r]} phrase is written after the {names[last]} phrase; Format 4 prints "
                        + "CAPACITY IN, FROM, TO, INITIALIZED in that order (ISO §2023:13.18.38.2), and \"The words, "
                        + "phrases, clauses, punctuation, and operands in each general format shall be written in the "
                        + "compilation group in the sequence given in the general format\" (ISO §2023:5.2.1)");
                last = Math.Max(last, r);
            }
            return;
        }
        if (occ.occursStepPhrase() is { } step)
            Refuse(step, "the STEP phrase belongs only to the report-writer format, which a report group "
                + "description entry uses (ISO §2023:13.18.38.2 Format 3: OCCURS [ integer-1 TO ] integer-2 TIMES "
                + "[ DEPENDING ON data-name-1 ] [ STEP integer-3 ])");
        bool hasTo = occ.TO() is not null, depending = occ.DEPENDING() is not null;
        if (hasTo != depending)
            Refuse(occ, (hasTo ? "integer-1 TO integer-2 is written without DEPENDING ON data-name-1"
                               : "DEPENDING ON data-name-1 is written without integer-1 TO")
                + "; a data description entry's occurs-depending table is Format 2, which prints "
                + "\"OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1\" with no part optional, and a fixed "
                + "table is Format 1, \"OCCURS integer-2 TIMES\" (ISO §2023:13.18.38.2)");
    }

    /// <summary>⛔ §2023:13.18.38.3 SR29 (kb/Work PB1264): "The implementor shall specify a maximum permissible value for
    /// integer-4 and integer-5. Their values shall not exceed this maximum value." The maximum is the highest
    /// permissible occurrence number of a dynamic-capacity table, <c>CobolDynTable.MaxOccurrences</c>
    /// (docs/CONFORMANCE.md DOC-A.1-60), and a FROM / TO value above it is refused HERE — it used to compile and
    /// give capacity 0, a table smaller than its written minimum. Returns null for a refused value, under an
    /// already-failed compile.</summary>
    private int? DynamicBoundWithinMaximum(Core.OccursDynamicPhraseContext phrase, string which, int? value)
    {
        if (value is not { } v || v <= WiseOwl.Cobol.Runtime.CobolDynTable<byte>.MaxOccurrences) return value;
        using var _ = Edition.At(phrase);
        Edition.Error(DiagnosticCatalog.OccursDynamicBoundAboveMaximum,
            $"OCCURS DYNAMIC {which} {v}: the value exceeds this implementation's maximum of "
            + $"{WiseOwl.Cobol.Runtime.CobolDynTable<byte>.MaxOccurrences:N0} for integer-4 and integer-5 "
            + "(docs/CONFORMANCE.md DOC-A.1-60); ISO §2023:13.18.38.3 SR29: \"Their values shall not exceed this maximum "
            + "value.\"");
        return null;
    }

    /// <summary>The name a <c>CAPACITY IN data-name-3</c> phrase DEFINES (ISO §2023:13.18.38.3 SR30 — "Data-name-3 shall
    /// not be defined elsewhere in the source element", i.e. the phrase is its definition), or null when the
    /// written operand is not a data-name. SR31 — "Data-name-3 shall not be subscripted" — and the §2023:8.4.2.2.2
    /// qualified-data-name shape are the <see cref="ClauseDataName"/> screen; a QUALIFIER is refused here too,
    /// because a defining occurrence names the register and SR30 itself supplies its qualification ("it shall be
    /// treated as though implicitly defined at the same level as the entry containing the OCCURS clause"). The
    /// capture was the whole reference's <c>GetText()</c>, so <c>CAPACITY IN CAP3 (1)</c> silently defined a
    /// register spelled <c>CAP3(1)</c> (kb/Work PB885's sibling sweep).</summary>
    private string? CapacityRegisterName(Core.DataReferenceContext capRef, string where)
    {
        var (name, quals) = ClauseDataName(capRef, $"{where}: OCCURS DYNAMIC CAPACITY IN");
        if (State.Clauses.RefusedClauseOperands.Contains(name)) return null;
        if (quals.Count == 0) return name;
        using var _ = Edition.At(capRef);
        Edition.Error(DiagnosticCatalog.ClauseOperandNotADataName, $"{where}: OCCURS DYNAMIC CAPACITY IN "
            + $"'{WrittenText(capRef)}' is qualified; data-name-3 is DEFINED by the phrase (ISO §2023:13.18.38.3 SR30), "
            + "and a defining occurrence is a bare data-name whose qualification SR30 itself supplies");
        return null;
    }

    /// <summary>The clause that tells the user which entry to edit when a diagnostic's subject is a member the entry
    /// copied in (kb/Work PB1629): empty for an entry the programmer wrote, else the TYPE or SAME AS subject the
    /// diagnostic is positioned at (<see cref="DataItem.ReferenceSite"/>).</summary>
    internal static string ViaReference(DataItem item) =>
        item.ReferenceSite is var site && !ReferenceEquals(site, item)
            ? $" — part of the description '{site.CobolName ?? "FILLER"}' copies in through its TYPE or SAME AS clause"
            : "";

    /// <summary>Is <paramref name="record"/> — and so every data-name subordinate to it — a global name? ISO
    /// §2023:8.4.6.2.2: "A constant-name, file-name, record-name, report-name, screen-name, or type-name described with a
    /// GLOBAL clause is a global name. All data-names and screen-names subordinate to a global name are global
    /// names" — the record's own GLOBAL clause, or its file description's (§2023:13.18.27.3 SR1 b) and d)).</summary>
    internal bool IsGlobalRecord(DataItem record) =>
        record.HasGlobalClause || State.Files.Declared.Any(f => f.IsGlobal && f.Records.Contains(record));

}

/// <summary>⛔ THE ONE ADMISSION OF AN OCCURS DEPENDING ON OBJECT (ISO §2023:13.18.38.3 SR17: "Data-name-1 shall describe an
/// integer"; SR2: "Data-name-1 and data-name-2 shall not be subscripted"; SR20: data-name-1 "shall not occupy a byte
/// position within the range of the first byte position defined by the data description entry containing the OCCURS
/// clause and the last byte position defined by the record description entry containing that OCCURS clause"; kb/Work
/// PB3510). <see cref="OccursSpec.Depending"/> records data-name-1 only when all three facts are false, so no consumer
/// of the link can cycle. Asked by the binder (<c>OdoResolvePass</c>, which also reports each fact) and by the
/// repository decoder (<c>RepositoryCodec</c>), which refuses a record whose link the rules forbid.</summary>
internal readonly record struct DependingAdmission(bool NotInteger, bool Subscripted, bool InTableRange)
{
    /// <summary>The three facts of <paramref name="dep"/> as the object of <paramref name="table"/>'s DEPENDING phrase.</summary>
    public static DependingAdmission Of(DataItem table, DataItem dep) =>
        new(dep.Pic is not { IsIntegerDescription: true }, dep.SubscriptArity > 0, DependingInsideTableRange(table, dep));

    /// <summary>The link may be recorded.</summary>
    public bool Admitted => !NotInteger && !Subscripted && !InTableRange;

    /// <summary>⛔ §2023:13.18.38.3 SR20 over the record's STORAGE AREA, not its entry (kb/Work PB1261). The range is
    /// byte positions — from the table's first to its record's last — and a byte position belongs to an AREA, which
    /// more than one record description can describe: §2023:13.18.33.4 GR3, "Multiple level 1 entries subordinate to a FD
    /// or SD entry represent implicit redefinitions of the same area", and a level-1 REDEFINES. Such records share
    /// one <see cref="RedefinesClass"/> and each begins at the area's first byte, so a counter in a sibling record
    /// sits at its own record offset in the same area. The test compared record ROOTS and so let that counter
    /// through, though it overlays the table's bytes.
    /// <para>Within ONE record, leaf order IS byte order for the canonical storage, so the counter must lie strictly
    /// before the table: its LAST leaf, for data-name-1 "occupies" every byte position of its own extent, and a GROUP
    /// data-name-1 has no leaf of its own — one that contains the table (DEPENDING ON the record that holds it)
    /// occupies every byte of the range, and the test that looked for data-name-1 itself among the leaves passed it
    /// (kb/Work PB3510). Across the records of one area the counter's byte window is compared with the table's:
    /// its offset in its own record against the table's offset and its record's extent (the table at its maximum,
    /// §2023:8.5.1.8 — the allocation every record of the area is described against).</para></summary>
    private static bool DependingInsideTableRange(DataItem table, DataItem dep)
    {
        if (ReferenceEquals(dep.Root, table.Root))
        {
            var leaves = DataBinder.LeavesOf(table.Root).ToList();
            int tableStart = leaves.FindIndex(l => OdoModel.IsWithin(l, table));
            int depLast = leaves.FindLastIndex(l => OdoModel.IsWithin(l, dep));
            return tableStart >= 0 && depLast >= tableStart;
        }
        if (dep.Root.Class is not { } area || !ReferenceEquals(table.Root.Class, area)) return false;
        if (RecordLayout.FirstOccurrenceOffsetOf(table) is not { } first || RecordLayout.OffsetOf(dep) is not { } at)
            return false;
        int recordEnd = RecordLayout.PhysicalWidth(table.Root);
        return at < recordEnd && at + RecordLayout.PhysicalOccurrenceWidth(dep) > first;
    }
}
