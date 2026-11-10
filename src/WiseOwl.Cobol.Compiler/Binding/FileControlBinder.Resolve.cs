// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// The post-build half of the file binder (ISO §2023:12.4.5 FILE-CONTROL clauses, §2023:13.18.34 LINAGE; DESIGN-architecture-review §8.3 (1);
/// kb/Work PB2296 M7): <see cref="ResolveFiles"/>, the <c>FilesResolved</c> pass's body, resolves each file's FILE STATUS, RECORD
/// KEY, ALTERNATE KEY, RELATIVE KEY, ASSIGN USING and LINAGE operands to data items once the data division is built, and applies
/// each clause's own syntax rules over the resolved item. The SELECT entry and the FD / SD binding are the other partials.
/// </summary>
public sealed partial class FileControlBinder
{
    /// <summary>Post-build (once the forest is indexed), resolve every file's data-name clause operands — FILE
    /// STATUS, the keys, ASSIGN … USING, RECORD … DEPENDING ON and the LINAGE operands — and run the syntax
    /// rules of the file control entry and of the LINAGE clause that need a RESOLVED data item (design D19/D25).
    /// Every operand goes through <see cref="DataBinder.ResolveClauseOperand"/> or <see cref="DataBinder.QualifiedCandidates"/>: ONE
    /// ISO §2023:8.4.2.2 resolution, never a per-clause first-match lookup (kb/Work PB489).</summary>
    internal void ResolveFiles()
    {
        foreach (var file in _host.State.Files.Declared)
        {
            if (file.FileStatusName is { } sn)
            {
                file.FileStatusItem = _host.ResolveClauseOperand(sn, file.FileStatusQualifiers, "FILE STATUS", file.FileStatusAt);
                ScreenFileStatusItem(file);   // §2023:12.4.5.8.3 — the clause's own syntax rules over the resolved item
            }
            // Keyed organizations: RECORD KEY / ALTERNATE RECORD KEY name items WITHIN the file's record
            // descriptions (ISO §2023:12.4.5.12 SR2 / §2023:12.4.5.6 SR2), possibly IN/OF-qualified (§2023:8.4.2.2 — same-named
            // keys under different areas, IX215A); RELATIVE KEY is OUTSIDE the record (ISO §2023:12.4.5.13 SR3) —
            // a plain name lookup.
            // ⛔ THE KEY CLAUSES KEEP THEIR OWN SELECTION, and it is not the uniqueness rule: SR2 puts the key
            // INSIDE one of this file's record descriptions, so where §2023:8.4.2.2 leaves several candidates the one
            // in a record of THIS file is the one the clause means, and an operand that resolves outside them is
            // the case SR2's own FileControlKeyRules row reports. What changed with kb/Work PB489 is only WHERE
            // the candidates come from — the one §2023:8.4.2.2 resolver, so the file-name qualifier (§2023:8.4.2.2.2
            // Format 1's file-report-qualifier) works here too, instead of a private record-subtree walk.
            // ⛔ AND THE SELECTION IS COUNTED (kb/Work PB978): SR2 narrows the set to this file's records when any
            // survivor lies there, and what remains must be ONE — two same-named keys in this file's records, or
            // two outside them with none inside, are §2023:8.4.2.2.3 SR1's ambiguity, never the first declared.
            HashSet<string> refusedKeys = new(CobolNames.Comparer);
            DataItem? InRecords(string keyName, IReadOnlyList<string> quals, string face, Editions.DiagnosticCursor at)
            {
                // ⛔ ALREADY REFUSED AT CAPTURE — one fault, one verdict (kb/Work PB481). ClauseDataName records a
                // refused operand AS WRITTEN (`IX-KEY(1:3)`), which no data item is named, so the key screen's SR2
                // row then added "references nothing described in this program" under COBOL0863 — a false
                // second verdict about an operand whose only fault is its shape. The RELATIVE KEY arm below never
                // had the cascade because ResolveClauseOperand already asks this set; the two record-key arms
                // resolve through their own SR2 selection and now ask it too (feedback_two_arm_dispatch).
                if (_host.State.Clauses.RefusedClauseOperands.Contains(keyName)) { refusedKeys.Add(keyName); return null; }
                var cands = _host.QualifiedCandidates(keyName, quals, Model.Scope.Program);
                var inFile = cands.Where(i => RecordLayout.IsInRecordOfFile(file, i));
                using var __ = Edition.At(at);
                string written = DataBinder.WrittenQualified(keyName, quals);
                // A name the REPORT SECTION declares is not "nothing described" either (kb/Work PB3768's sibling arm):
                // §2023:13.8.6.2.3 bars the key clause from referring to a report item, the ONE refusal says so, and
                // SR2's row stays silent about an operand already refused. It is asked before the ambiguity, which it
                // leaves standing unless a report entry of a NEARER source element hides the data items (kb/Work
                // PB3767's order, kept by the lookup itself since PB4196, ISO §2023:8.4.6.2.1 3) a)); a key item of the file's own
                // record is never hidden.
                if (inFile.Count == 0 && cands.Count != 1
                    && _host.ReportSection.ReportEntryReferenceRefused(cands, $"{face} '{written}'", keyName, quals))
                {
                    refusedKeys.Add(keyName);
                    return null;
                }
                var hit = _host.UniqueOrReportAmbiguous(inFile.Count > 0 ? inFile : cands, face, written, out bool ambiguous);
                if (ambiguous && hit is null) refusedKeys.Add(keyName);
                return hit;
            }
            // ⛔ ONE RESOLUTION FOR BOTH KEY FORMS (kb/Work PB1099 Q1): data-name-1 is the key's one segment, and
            // each data-name-2 of a SOURCE phrase is one segment of the concatenation (§2023:12.4.5.12.4 GR2 /
            // §2023:12.4.5.6.4 GR2), resolved by the same SR2 selection. A key any of whose operands references nothing
            // describable has no value to build, and that operand's SR2 row reports it.
            RecordKey? KeyOf(string keyName, IReadOnlyList<string> quals, RecordKeySource? source, string face,
                             Editions.DiagnosticCursor at, Action<DataItem?> dataName1)
            {
                if (source is null)
                {
                    var item = InRecords(keyName, quals, face, at);
                    dataName1(item);
                    return item is null ? null : RecordKey.OfItem(item);
                }
                foreach (var op in source.Operands)
                    op.Item = InRecords(op.Name, op.Qualifiers, $"{face} SOURCE", op.At);
                return source.Operands.All(o => o.Item is not null)
                    ? RecordKey.OfSource(source.Name, [.. source.Operands.Select(o => o.Item!)]) : null;
            }
            if (file.RecordKeyName is { } rk)
                file.PrimeKey = KeyOf(rk, file.RecordKeyQualifiers, file.RecordKeySource, "RECORD KEY", file.RecordKeyAt, _ => { });
            foreach (var clause in file.AlternateKeyNames)
            {
                clause.SuppressWhen = ReadSuppressWhen(clause);
                if (KeyOf(clause.Name, clause.Qualifiers, clause.Source, "ALTERNATE RECORD KEY", clause.At,
                        item => clause.Item = item) is { } alt)
                    file.AlternateKeys.Add((alt, clause.Duplicates, KeySuppressionOf(alt, clause.SuppressWhen)));
            }
            ResolveFileCollating(file);   // §2023:12.4.5.7 — per-key collating weights (needs the resolved keys)
            if (file.RelativeKeyName is { } rl)
                file.RelativeKeyItem = _host.ResolveClauseOperand(rl, file.RelativeKeyQualifiers, "RELATIVE KEY", file.RelativeKeyAt);
            if (file.Padding is { DataName: { } pn } pad)
            {
                pad.Item = _host.ResolveClauseOperand(pn, pad.Qualifiers, "PADDING CHARACTER", pad.At);
                ScreenPaddingDataName(pad);
            }
            ResolveAssignUsing(file);   // §2023:12.4.5.3 GR3 b + §2023:12.4.5.2 SR7
            // ⛔ THE FILE CONTROL ENTRY'S OWN SYNTAX RULES, screened HERE — at the ENTRY, for EVERY declared file,
            // whatever the procedure division does with it. They used to run from KeyedIoBinder on the first keyed
            // VERB that named the file, so a program that only OPENed and CLOSEd a file whose keys break them
            // compiled clean (kb/Work PB699). One table, one screen: FileControlKeyRules.
            FileControlKeyRules.Screen(file, Edition, refusedKeys);
            // ⛔ The RECORD clause's own size syntax rules (ISO §2023:13.18.43.3 SR3/SR4/SR5/SR9), screened HERE for
            // the same reason and by the same shape: they are rules of the file description ENTRY, and SR3/SR4
            // compare the clause's integers against §2023:13.18.43.4 GR8's byte counts of the record descriptions,
            // which are settled only once the forest is bound. Until kb/Work PB721 nothing checked them at all
            // and `RECORD IS VARYING IN SIZE FROM 20 TO 5` compiled clean at every edition.
            // RECORD VARYING … DEPENDING ON data-name-1 — the length register WRITE/REWRITE/RELEASE read and
            // READ/RETURN set (§2023:13.18.43.4) — is resolved FIRST, because SR6 (an elementary unsigned integer in the
            // working-storage, local-storage or linkage section) is one of the screen's rows (kb/Work PB858).
            if (file.Varying?.DependingName is { } vn)
                file.VaryingDependingItem = _host.ResolveClauseOperand(
                    vn, file.Varying.DependingQualifiers, "RECORD DEPENDING ON", file.Varying.DependingAt);
            RecordClauseRules.Screen(file, Edition);
            ResolveLinage(file);   // §2023:13.18.34 — the logical-page operands and §2023:13.18.34.3's syntax rules
        }
        ResolveApplyCommit();   // §2023:12.4.6.3.3 — its identifiers, and SR9's FILE STATUS / LINAGE / RECORD items resolved above
    }

    /// <summary>Screen the FILE STATUS clause's data-name-1 against the clause's own syntax rules (ISO §2023:12.4.5.8.3) —
    /// the FILE-CONTROL twin of <see cref="ResolveLinage"/>'s operand screen, run post-build for the same reason: an
    /// operand's OCCURS ancestry, category, size and section are known only once the data forest is bound.
    /// <para>⛔ NONE OF THE FOUR RULES HAD A SITE (kb/Work PB1080), so every item compiled, and what happened next was
    /// the emitter's accident: an OCCURS item died at OPEN ("not resolvable to storage"), a <c>PIC 99</c> item
    /// crashed the C# backend (CS0029), a BASED item died EC-DATA-PTR-NULL at OPEN, and <c>PIC X</c> /
    /// <c>PIC X(3)</c> / <c>PIC N(2)</c> / an FD record / a dynamic-length or variable-length item ran with a
    /// truncated, padded or wrong-category status.</para>
    /// <list type="bullet">
    /// <item>SR1 — <i>"Data-name-1 shall not be subject to any OCCURS clauses"</i>: <see cref="RecordLayout.IsSubjectToOccurs"/>,
    /// the reader §2023:13.18.34.3 SR1 and the key clauses' SR1 share.</item>
    /// <item>SR4 — <i>"shall not be subject to a BASED clause in its data description"</i>: the item or a group it is
    /// subordinate to is BASED.</item>
    /// <item>SR3 — <i>"shall not reference a dynamic-length elementary item or a variable-length group"</i> (the one
    /// screen of that pair, <see cref="VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup"/>).</item>
    /// <item>SR2 — <i>"a two-character data item of the category alphanumeric, defined in the working-storage,
    /// local-storage, or linkage section"</i>. The category is <see cref="ItemCategory.IsAlphanumeric"/>, so a
    /// two-character alphanumeric GROUP qualifies — §2023:13.18.29.4 GR3 makes a group with no GROUP-USAGE clause "an
    /// alphanumeric group item", the reading §2023:12.4.5.2 SR7's ASSIGN USING operand already takes. "Two-character" is
    /// the item's size in character positions, fixed: a group whose size an OCCURS DEPENDING ON subordinate varies
    /// is not a two-character data item. The section half reads <see cref="DataItem.Section"/>, the fact the item
    /// carries from its root — so a GLOBAL item a contained program sees keeps the section its own program described
    /// it in.</item>
    /// </list>
    /// One verdict per item, in the order a fix would address them (the first rule that fails).</summary>
    private void ScreenFileStatusItem(FileModel file)
    {
        if (file.FileStatusItem is not { } item) return;   // unresolved — reported by the resolution, one verdict
        using var _ = Edition.At(file.FileStatusAt);
        string face = DataBinder.WrittenQualified(file.FileStatusName!, file.FileStatusQualifiers);
        string? fault =
            RecordLayout.IsSubjectToOccurs(item)
                ? "is subject to an OCCURS clause; data-name-1 shall not be subject to any OCCURS clauses "
                  + "(ISO §2023:12.4.5.8.3 SR1)"
            : SubjectToBased(item)
                ? "is subject to a BASED clause; data-name-1 shall not be subject to a BASED clause in its data "
                  + "description (ISO §2023:12.4.5.8.3 SR4)"
            : VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup(item) is { } variableShape
                ? $"is {variableShape}; data-name-1 shall not reference a dynamic-length elementary item or a "
                  + "variable-length group (ISO §2023:12.4.5.8.3 SR3)"
            : FileStatusShapeFault(item) is { } shape
                ? $"is {shape}; data-name-1 shall reference a two-character data item of the category alphanumeric, "
                  + "defined in the working-storage, local-storage, or linkage section (ISO §2023:12.4.5.8.3 SR2)"
            : null;
        if (fault is not null)
            Edition.Error(DiagnosticCatalog.FileStatusItemRule, $"file '{file.SelectName}': FILE STATUS '{face}' {fault}");
    }

    /// <summary>Which conjunct of §2023:12.4.5.8.3 SR2 the FILE STATUS item breaks, as the phrase the diagnostic prints;
    /// null when it satisfies all three (category, size, section) — asked in the rule's own word order.</summary>
    private string? FileStatusShapeFault(DataItem item) =>
        !ItemCategory.IsAlphanumeric(item) ? $"{ItemCategory.Face(item)}, not of the category alphanumeric"
        : DataItem.HasOdoOnOrBeneath(item) ? "a group whose size an OCCURS DEPENDING ON clause varies, not a two-character data item"
        : item.ByteWidth != 2 ? $"{item.ByteWidth} character position{(item.ByteWidth == 1 ? "" : "s")} long, not a two-character data item"
        : item.Section is { } s and not (EntrySection.WorkingStorage or EntrySection.LocalStorage or EntrySection.Linkage)
            ? $"defined in the {DataBinder.SectionWords(s)}"
        : null;

    /// <summary>The item, or a group it is subordinate to, carries a BASED clause (§2023:13.18.5 — "subject to").</summary>
    private static bool SubjectToBased(DataItem item)
    {
        for (DataItem? p = item; p is not null; p = p.Parent)
            if (p.IsBased) return true;
        return false;
    }

    /// <summary>Read an ALTERNATE RECORD KEY clause's SUPPRESS WHEN literal-1 (ISO §2023:12.4.5.6.2) — null when the
    /// clause has none, or when the operand is no literal at all (reported here, once).
    /// <para>⛔ ONE READING, THROUGH THE ONE LITERAL-POSITION CHOKEPOINT (kb/Work PB1072). The phrase used to be
    /// read as <c>CobolLiteral.Decode(literal.GetText())</c>, which returns a figurative constant or an ALL literal
    /// UNCHANGED as its source spelling: <c>SUPPRESS WHEN SPACES</c> suppressed the six-letter word "SPACES" and
    /// indexed every blank key, and a constant-name or symbolic-character — both legal literal-1 spellings
    /// (§2023:13.10.3 SR2; §2023:8.3.3.6.3 SR1 with §2023:8.3.3.6.2 Format 7) — did not parse at all. <see cref="DataBinder.RawValueOperandText"/>
    /// is the VALUE clause's and the EDITING phrase's own reader: it screens the literal position, substitutes a
    /// constant-name's and a symbolic-character's literal, and folds a concatenation expression (§2023:8.8.3.3 GR3), so
    /// what reaches <see cref="WiseOwl.Cobol.CodeGen.FigurativeConstants.Classify"/> here is always ONE literal's raw text.</para>
    /// <para>What is recorded is the operand's SHAPE, which is what §2023:12.4.5.6.3 SR7 screens
    /// (<c>FileControlKeyRules</c>); the key-sized value is <see cref="KeySuppressionOf"/>'s.</para></summary>
    private SuppressWhenOperand? ReadSuppressWhen(AlternateKeyClause clause)
    {
        if (!SuppressWhenOperands.TryGetValue(clause, out var op)) return null;
        using var _ = Edition.At(op);
        if (_host.RawValueOperandText(op, $"ALTERNATE RECORD KEY '{clause.Name}'", DataBinder.LiteralPosition.SuppressWhen) is not { } raw)
            return null;
        string written = DataBinder.WrittenText(op);
        var form = WiseOwl.Cobol.CodeGen.FigurativeConstants.Classify(raw, includeNull: false);
        if (form.Kind is { } kind)
            return new(written, SuppressWhenForm.Figurative, null, "", kind);
        if (form.AllLiteral is { } literal1)   // ALL literal-1, and a symbolic-character's ALL "c" substitution
            return new(written, SuppressWhenForm.AllLiteral, CobolLiteral.ClassOf(literal1), CobolLiteral.Decode(literal1), null);
        return CobolLiteral.ClassOf(raw) is { } cls
            ? new(written, SuppressWhenForm.Literal, cls, CobolLiteral.Decode(raw), null)
            // A numeric literal, or the figurative constant NULL, which has no character value.
            : new(written, SuppressWhenForm.NotAlphanumericOrNational, null, "", null);
    }

    /// <summary>The §2023:12.4.5.6.4 GR6 key suppression value of <paramref name="key"/>, in the key's own character
    /// positions — the operand the relation condition compares the key with (§2023:12.4.5.6.4 GR4: equality "based on
    /// the collating sequence used for the file according to the rules for a relation condition"). Null when
    /// there is no SUPPRESS WHEN phrase or the key is no key §2023:12.4.5.6.3 SR2 admits (already refused).
    /// <list type="bullet">
    /// <item>A keyword figurative is repeated to the key's size (§2023:8.3.3.6.4 GR2 — a figurative "compared with" a
    /// fixed-length data item is associated with it, NOTE 1), its character taken in the key's category through
    /// <see cref="WiseOwl.Cobol.CodeGen.FigurativeConstants.FillChar"/> — the fill a <c>MOVE</c> of the same figurative into the key
    /// stores, so a program that moves SPACES into the key is exactly the program that suppressed it.</item>
    /// <item>ALL literal-1 is repeated the same way (GR2 again).</item>
    /// <item>A literal SHORTER than the key is extended with spaces (§2023:8.8.4.2.7 2) — national spaces for a national
    /// key, which the encoder writes as the byte pair <c>00 20</c>); a LONGER one is cut back to the key's size when
    /// every excess position is a space (equal under the same rule) and otherwise kept whole — no key value can
    /// equal it, so it never suppresses.</item>
    /// </list>
    /// An operand §2023:12.4.5.6.3 SR7 refuses still gets a value: the screen's error has already failed the compile.</summary>
    private KeySuppression? KeySuppressionOf(RecordKey key, SuppressWhenOperand? operand)
    {
        // A SOURCE-form key has the class and category of data-name-2 (§2023:12.4.5.6.3 SR6), and its character positions
        // are its segments' (GR2's concatenation) — SR2 makes every segment the same category, so one unit applies.
        if (operand is null || !ItemCategory.IsAlphanumericOrNational(key.CategorySource)) return null;
        bool national = !ItemCategory.IsAlphanumeric(key.CategorySource);
        int positions = key.Segments.Sum(s => s.ByteWidth / (s.OperandPic?.BytesPerPosition ?? 1));   // the ONE unit reader (kb/Work PB546)
        var category = national ? PicCategory.National : PicCategory.Alphanumeric;
        string value = operand.Form switch
        {
            SuppressWhenForm.Figurative => new string(WiseOwl.Cobol.CodeGen.FigurativeConstants.FillChar(
                operand.FigurativeKind!.Value, _host.State.Configuration.Collating, category, _host.State.Configuration.NationalCollating), positions),
            SuppressWhenForm.AllLiteral when operand.Characters.Length > 0 =>
                string.Concat(Enumerable.Repeat(operand.Characters, positions / operand.Characters.Length + 1))[..positions],
            _ when operand.Characters.Length <= positions => operand.Characters.PadRight(positions),
            _ when operand.Characters.AsSpan(positions).IndexOfAnyExcept(' ') < 0 => operand.Characters[..positions],
            _ => operand.Characters,
        };
        return new KeySuppression(value, national);
    }

    /// <summary>Resolve the LINAGE clause's data-name operands and screen the clause's own syntax rules
    /// (ISO §2023:13.18.34.3) — the FD-clause twin of <see cref="ResolveFileCollating"/>, run post-build because an
    /// operand's OCCURS ancestry is unknown until the data forest is indexed.
    /// <para>SR1, <i>"Data-name-1, data-name-2, data-name-3, and data-name-4 shall not be subject to any OCCURS
    /// clauses"</i>, is screened here. It had NO site at all: <c>LINAGE IS T-LINES LINES</c> over a table element
    /// compiled clean and died at OPEN OUTPUT with a runtime "not resolvable to storage" throw, which is a
    /// process kill where the standard requires a compile-time rejection (kb/Work PB489/PB524, measured). The
    /// WRITTEN subscript form — <c>LINAGE IS T-LINES (2) LINES</c> — is refused earlier, at the capture, because
    /// §2023:13.18.34.2 prints data-name-1 and a subscript is not part of a qualified-data-name.</para>
    /// <para>SR2, <i>"Data-name-1, data-name-2, data-name-3, and data-name-4 shall reference elementary unsigned
    /// numeric integer data items"</i>, and SR3, <i>"Integer-2 shall not be greater than integer-1"</i>, are the
    /// other two tests of the SAME screen (kb/Work PB524). Neither had a site: the emitter ASSUMED SR2
    /// (<c>SequentialIoEmitter</c>'s "scale 0 by SR2's elementary-unsigned-integer rule, with a defensive
    /// rescale"), so an alphanumeric "007" was read as page size 7, a PIC 9V9 5.5 was truncated to 5, and a signed
    /// or SR3-breaking page reached run time and threw a CLR exception out of the page evaluation. SR2's
    /// "integer" is <see cref="PicInfo.IsIntegerDescription"/> — §2023:5.5's fixed-point, no-fraction reading, the one
    /// predicate every integer-data-item rule reads, which also refuses a floating-point item and an index item
    /// (class index, never numeric). SR3 compares the two LITERALS only: with a data-name on either side the
    /// relation is a value at run time and §2023:13.18.34.4 GR3 is its rule.</para>
    /// <para>SR4 (integer-3 / integer-4 may be zero) is a permission, honoured by the §2023:5.5 1) integer-n screen
    /// (<c>IntegerOperandPass</c>), which is where a ZERO integer-1 or integer-2 is refused.</para>
    /// </summary>
    private void ResolveLinage(FileModel file)
    {
        if (file.Linage is not { } lin) return;
        foreach (var op in lin.Operands)
        {
            if (op.DataName is not { } name) continue;   // GR6 a) — a literal operand has no data item
            op.Item = _host.ResolveClauseOperand(name, op.Qualifiers, "LINAGE clause operand", op.At);
            if (op.Item is not { } item) continue;   // unresolved — reported by the resolution, one verdict
            using var _ = Edition.At(op.At);
            if (RecordLayout.IsSubjectToOccurs(item))
                Edition.Error(DiagnosticCatalog.LinageClauseOperandRule,
                    $"LINAGE clause operand '{name}' is subject to an OCCURS clause; data-name-1, data-name-2, "
                    + "data-name-3, and data-name-4 shall not be subject to any OCCURS clauses "
                    + "(ISO §2023:13.18.34.3 SR1)");
            else if (LinageOperandShapeFault(item) is { } fault)
                Edition.Error(DiagnosticCatalog.LinageClauseOperandRule,
                    $"LINAGE clause operand '{name}' is {fault}; data-name-1, data-name-2, data-name-3, and "
                    + "data-name-4 shall reference elementary unsigned numeric integer data items "
                    + "(ISO §2023:13.18.34.3 SR2)");
        }
        // SR3 — both operands are literals, so the relation is known now and is a syntax rule.
        if (lin.Body.Literal is int page && lin.Footing is { Literal: int footing } foot && footing > page)
        {
            using var _ = Edition.At(foot.At);
            Edition.Error(DiagnosticCatalog.LinageClauseOperandRule,
                $"LINAGE clause: FOOTING AT {footing} is greater than the page size {page}; integer-2 shall not be "
                + "greater than integer-1 (ISO §2023:13.18.34.3 SR3)");
        }
    }

    /// <summary>Which conjunct of §2023:13.18.34.3 SR2 — "elementary unsigned numeric integer" — the item breaks, as
    /// the phrase the diagnostic prints; null when it satisfies all four. Asked in the rule's own word order so
    /// the FIRST failing word is the one named.</summary>
    private static string? LinageOperandShapeFault(DataItem item) =>
        item.IsGroup ? "a group item, not an elementary item"
        : item.Pic is not { Category: PicCategory.Numeric } pic ? $"{ItemCategory.Face(item)}, not a numeric data item"
        : pic.Usage is Usage.Index ? "an index data item (class index), not a numeric data item"
        : !pic.IsIntegerDescription
            ? (pic.IsFloat ? "a floating-point numeric data item, not an integer data item"
                : "a numeric data item with digits to the right of the decimal point, not an integer data item")
        : pic.Signed ? "a signed numeric data item, not an unsigned one"
        : null;

    /// <summary>
    /// Resolve the ASSIGN … USING data-name-1 (ISO §2023:12.4.5.3 GR3 b — dynamic file assignment, §2023:9.1.21) and enforce
    /// §2023:12.4.5.2 SR7, <i>"Data-name-1 shall reference an alphanumeric data item and shall not be subordinate to the
    /// file description entry for file-name-1."</i> Both halves are checked, and the SECOND is the one a program can
    /// silently lose to: an operand inside the file's own record area is overwritten by every READ, so its content at
    /// the next OPEN would be file data rather than a file name.
    /// <para>Category: §2023:9.1.21 says the same thing in the concepts ("The USING phrase references an alphanumeric data
    /// item"). An ALPHANUMERIC GROUP item qualifies — §2023:13.18.29.4 GR3 makes a group with no GROUP-USAGE clause "an
    /// alphanumeric group item" — while a bit or national group does not, and neither does a category-ALPHABETIC
    /// (PIC A) item, which §2023:8.8.4.2.4 keeps a class of its own ("A class alphabetic operand shall be treated as
    /// though it were an operand of class alphanumeric" is a COMPARISON rule, not a category identity).</para>
    /// </summary>
    private void ResolveAssignUsing(FileModel file)
    {
        if (file.AssignUsingName is not { } dynName) return;
        using var _ = Edition.At(file.AssignUsingAt);
        // The §2023:8.4.2.2 candidate set (kb/Work PB489 — one resolver); SR7's own verdict below speaks for a
        // reference that survives nothing, so this site reports the category sentence rather than a second
        // uniqueness one.
        // Counted (kb/Work PB978): two survivors are §2023:8.4.2.2.3 SR1's ambiguity, reported once by the ONE verdict.
        var candidates = _host.QualifiedCandidates(dynName, file.AssignUsingQualifiers, Model.Scope.Program);
        string written = DataBinder.WrittenQualified(dynName, file.AssignUsingQualifiers);
        // A name the REPORT SECTION declares is not undescribed (kb/Work PB3768): §2023:13.8.6.2.3 bars every clause of a
        // data division section other than the report section from referring to a report item, so the ONE refusal
        // speaks (as ResolveClauseOperand does for the other file clauses) before "no such data item is described",
        // and before the ambiguity, which it leaves standing unless a report entry of a NEARER source element hides the
        // data items (kb/Work PB3767's order, kept by the lookup itself since PB4196, ISO §2023:8.4.6.2.1 3) a)). The subject is the head of the refusal, so it
        // names the phrase as written, never `ASSIGN … USING` (kb/Work PB4160: a head that elides the statement renders
        // `ASSIGN . USING` on the console).
        if (candidates.Count != 1
            && _host.ReportSection.ReportEntryReferenceRefused(candidates, $"ASSIGN USING '{written}'", dynName, file.AssignUsingQualifiers)) return;
        DataItem? item = _host.UniqueOrReportAmbiguous(candidates, "ASSIGN USING", written, out bool ambiguous);
        if (ambiguous && item is null) return;
        if (item is null || !ItemCategory.IsAlphanumeric(item))
        {
            Edition.Error(DiagnosticCatalog.AssignUsingNotAlphanumeric,
                $"file '{file.SelectName}': ASSIGN … USING '{dynName}' shall reference an alphanumeric data item "
                + (item is null ? "and no such data item is described" : $"— '{dynName}' is {ItemCategory.Face(item)}")
                + " (ISO §2023:12.4.5.2 SR7)");
            return;
        }
        // "shall not be subordinate to the file description entry for file-name-1" — subordinate means anywhere
        // under one of THIS file's record descriptions (a record description entry is subordinate to the FD).
        for (DataItem? a = item; a is not null; a = a.Parent)
            if (file.Records.Any(r => ReferenceEquals(r, a)))
            {
                Edition.Error(DiagnosticCatalog.AssignUsingInOwnRecord,
                    $"file '{file.SelectName}': ASSIGN … USING '{dynName}' shall not be subordinate to the file "
                    + $"description entry for '{file.SelectName}' (ISO §2023:12.4.5.2 SR7)");
                return;
            }
        file.AssignUsingItem = item;
    }
}
