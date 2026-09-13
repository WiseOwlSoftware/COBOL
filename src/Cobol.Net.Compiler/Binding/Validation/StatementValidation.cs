// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics.CodeAnalysis;

using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Validation;

using Core = CobolParserCore;

/// <summary>
/// The edition-INVARIANT syntax-rule check catalog lifted out of the verb binders (P7 Step 10; the phase
/// doc's §Step 10). The contract (fixed at 10c, the AS-BUILT PLAN's convention): every <c>Check*</c> is a
/// PURE check — it reports to the ONE sink (<c>data.Edition</c>) with byte-identical message text and
/// returns the verdict (<c>true</c> = conformant); the CALLING verb binder owns all control flow (error
/// placeholders, statement aborts, operand rewrites). Edition GATING never lives here — the
/// <c>VersionConformancePass</c> is the sole edition funnel (DESIGN-version-conformance-pipeline); the
/// residual inline gates move VERBATIM with their verb binders until Exec Step E folds them.
/// </summary>
internal sealed class StatementValidation(DataBinder data)
{
    // ── THE STATEMENT-OPERAND SCREENS THAT USED TO BE RUN-TIME LOUDS (kb/Work PB236) ─────────────────────────
    //    `BoundUnsupported` was the carrier for THREE incompatible jobs — a genuine feature DEFERRAL, an
    //    ill-formed OPERAND, and an illegal PLACEMENT — and StatementEmitter rendered all three as the same
    //    `NotImplemented.Run(...)`. The two user-error jobs belong at COMPILE time, per ISO §4.2.2 ¶2 ("An
    //    implementation shall provide a warning mechanism that optionally may be invoked by the user at compile
    //    time to indicate violations of the general formats and the explicit syntax rules of standard COBOL"),
    //    and they belong HERE, in the one syntax-rule check catalog, not at each verb binder.

    /// <summary>⛔ THE ONE file-name → <see cref="FileModel"/> resolution for a STATEMENT operand (kb/Work
    /// PB236). Seven binder sites — UNLOCK, OPEN, CLOSE, READ, DELETE, DELETE FILE and START — each wrote its
    /// own <c>FilesByName.TryGetValue</c> and its own unreported <c>BoundUnsupported</c>, so a word naming no
    /// file connector compiled clean at every edition and either aborted the run unit or, on an unexecuted
    /// path, said nothing at all. Each of those statements' general formats writes the operand as
    /// <c>file-name-1</c>, and ISO §8.4.2.1 fixes what a name must do: "In order to use a resource, a statement
    /// shall contain a reference that uniquely identifies that resource." A word that identifies no file
    /// connector identifies no resource.
    /// <para>The diagnostic is the EXISTING <see cref="DiagnosticCatalog.UndefinedReference"/> (COBOLNET1639),
    /// not a new code: "this source element defines no such name" is ONE rule, and the file-name space was
    /// simply missing from the place that already reports it for data-names (feedback_one_rule_one_place). A
    /// new statement that names a file gets the diagnostic by calling this, not by remembering to write
    /// one.</para></summary>
    /// <param name="name">The file-name as written.</param>
    /// <param name="verb">The statement, for the message, e.g. "OPEN".</param>
    /// <param name="file">The resolved model when this returns true.</param>
    /// <returns>true when the name identifies a file connector; false after reporting.</returns>
    public bool ResolveFile(string name, string verb, [NotNullWhen(true)] out FileModel? file)
    {
        if (data.FilesByName.TryGetValue(name, out file)) return true;
        data.Edition.Error(DiagnosticCatalog.UndefinedReference,
            $"'{name}' is not defined as a file — {verb} names file-name-1 in its general format, and no SELECT "
            + "or file description entry in this source element gives that name, so the statement's reference "
            + "identifies no resource (ISO §8.4.2.1: \"a statement shall contain a reference that uniquely "
            + "identifies that resource\"). Check the spelling, or add the file to the FILE-CONTROL paragraph.");
        return false;
    }

    /// <summary>⛔ THE ONE <c>procedure-name</c> OPERAND RULE (kb/Work PB390), reported for every statement
    /// whose general format prints that operand: PERFORM, GO TO (both formats), ALTER, RESUME AT and the
    /// SORT/MERGE INPUT/OUTPUT PROCEDURE phrases. The resolution itself is
    /// <see cref="Procedure.ProcedureTableBuilder.ResolveProcedureOperand"/> — §8.4.2.2's in-section →
    /// global → section-name order, ONE implementation — and this is where its failure becomes a DIAGNOSTIC
    /// instead of a bound node.
    /// <para>⛔ THE STAGE WAS THE DEFECT. Eight sites each turned "this name resolves to nothing" into a
    /// <c>BoundUnsupported</c>, which the emitter renders as <c>NotImplemented.Run(...)</c>: a misspelled
    /// <c>PERFORM</c> COMPILED, shipped an assembly, and aborted the run unit with an unhandled .NET exception
    /// telling the user that COBOL.NET had not implemented a feature — the diagnosis sent to the wrong party,
    /// at the wrong time, and on an unexecuted path never sent at all. ISO §4.2.2 ¶2 puts violations of "the
    /// general formats and the explicit syntax rules" in the compile-time mechanism.</para>
    /// <para>The diagnostic is the EXISTING <see cref="DiagnosticCatalog.UndefinedReference"/> (COBOLNET1639)
    /// and no new code — "this source element defines no such name" is ONE rule (ISO §8.4.2.1), and the
    /// procedure-name space was simply missing from the place that already reports it for data-names and (since
    /// kb/Work PB236) for file-names. SORT's own private copy of this message folded into it here.</para>
    /// <para>The scope half is quoted because it is the half a reader disbelieves: §8.4.6.1 lists paragraph-name
    /// and section-name among the words that "may be referenced only by statements in the source element in
    /// which the user-defined word is declared", which is why a CONTAINED program's paragraph, and a sibling
    /// METHOD's, are correctly not found — a method definition begins with an identification division
    /// (§11.7.1), so it is a contained source unit (§3.165) and thus its own source element (§3.164).
    /// ⛔ NOT §11.7 ALONE, which is where the deleted <c>OoScopeHint</c> pointed: that clause is the
    /// METHOD-ID paragraph, and the only scoping rule it states (§11.7.4 GR5) is about words defined in the
    /// method's DATA DIVISION — a real clause answering a different question.</para></summary>
    /// <param name="refText">The procedure-name as written, for the message.</param>
    /// <param name="head">Its head word (the qualifier stripped), for the data-name discrimination.</param>
    /// <param name="verb">The statement or phrase, e.g. "PERFORM" or "SORT INPUT PROCEDURE".</param>
    /// <param name="rule">The caller's own syntax rule quoted with its citation, or "" where the statement
    /// states none of its own (GO TO and ALTER rest on §8.4.2.1 alone).</param>
    /// <param name="methodLocal">True inside a method body — resolution is confined there (ISO §11.7).</param>
    /// <returns>Always false: the operand is refused, HAVING REPORTED.</returns>
    public bool RejectProcedureName(string refText, string head, string verb, string rule, bool methodLocal)
    {
        bool isDataName = data.Symbols.TryResolve(head, data.ActiveScope, out _);
        data.Edition.Error(DiagnosticCatalog.UndefinedReference,
            $"{verb} '{refText}' names no procedure: no paragraph or section in this source element carries "
            + "that name, so the reference identifies no resource (ISO §8.4.2.1 — \"In order to use a resource, "
            + "a statement shall contain a reference that uniquely identifies that resource\"; §8.4.6.1 — a "
            + "paragraph-name or section-name \"may be referenced only by statements in the source element in "
            + "which the user-defined word is declared\")"
            + (isDataName
                ? $"; '{head}' is declared as a DATA ITEM, and a procedure-name is a different class of "
                  + "user-defined word (§8.3.2.2)"
                : "")
            + (methodLocal
                ? "; resolution inside a method is METHOD-LOCAL — a method definition begins with an "
                  + "identification division (ISO §11.7.1) so it is a contained source unit (§3.165) and "
                  + "therefore its own source element (§3.164: \"source unit excluding any contained source "
                  + "units\"), which is what the scope rule above is measured against — so the paragraphs of "
                  + "sibling methods and of the driver program are not visible here"
                : "")
            + (rule.Length > 0 ? $". {rule}" : ""));
        return false;
    }

    /// <summary>⛔ ISO §14.9.39.3 SR6 — "Condition-name-1 shall be associated with a conditional variable"
    /// (kb/Work PB390), the Format-4 <c>SET condition-name-1 … TO TRUE</c> operand rule. COBOL has TWO kinds of
    /// condition-name (§8.4.4): one associated with a CONDITIONAL VARIABLE (a level-88 entry) and one
    /// associated with the status of an EXTERNAL SWITCH (a SPECIAL-NAMES <c>ON STATUS IS</c> / <c>OFF STATUS
    /// IS</c> name, §12.3.7). SR6 admits the first kind only — the second is set through Format 3,
    /// <c>SET mnemonic-name-1 TO ON|OFF</c>, whose SR5 requires an external switch "the status of which may be
    /// altered".
    /// <para>⛔ THE OLD MESSAGE DENIED THE FACT THE RULE TURNS ON. A switch-status name reached a
    /// <c>BoundUnsupported</c> reading "SET 'SW-ON' TO TRUE (not a condition-name)" — but SW-ON IS a
    /// condition-name; what it is not is a condition-name of a conditional VARIABLE, which is exactly what SR6
    /// says. Told it was not a condition-name at all, a user looks for a missing level-88 instead of reaching
    /// for Format 3. The rule's own discriminating case was also its silent one: it compiled clean and aborted
    /// the run unit.</para></summary>
    /// <param name="refText">The operand as written.</param>
    /// <param name="switchName">The implementor's external-switch name when the operand IS a switch-status
    /// condition-name, else null.</param>
    /// <returns>Always false: the operand is refused, HAVING REPORTED.</returns>
    public bool RejectSetConditionName(string refText, string? switchName)
    {
        if (switchName is not null)
            data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                $"SET '{refText}' TO TRUE — '{refText}' IS a condition-name, but the SPECIAL-NAMES kind: it "
                + $"names the status of external switch {switchName} (ISO §12.3.7), and §8.4.4.1 distinguishes "
                + "that kind from the level-88 kind, which is the one associated with a conditional variable. "
                + "\"Condition-name-1 shall be associated with a conditional variable\" (ISO §14.9.39.3 SR6), "
                + "so this operand is not admitted in Format 4. Set the switch through Format 3 — "
                + "SET <mnemonic-name> TO ON or OFF (SR5).");
        else
            data.Edition.Error(DiagnosticCatalog.UndefinedReference,
                $"SET '{refText}' TO TRUE — '{refText}' is not a condition-name: no level-88 entry and no "
                + "SPECIAL-NAMES switch-status name in this source element declares it, so the reference "
                + "identifies no resource (ISO §8.4.2.1), and §14.9.39.3 SR6 requires a condition-name "
                + "associated with a conditional variable.");
        return false;
    }

    /// <summary>The CORRESPONDING group-operand rule, ONE screen for its three spellings (kb/Work PB236):
    /// MOVE §14.9.25.3 SR12 — "Identifier-3 and identifier-4 shall specify group data items and shall not be
    /// reference-modified" — and ADD §14.9.2.3 SR6 / SUBTRACT §14.9.44.3 SR6 — "Identifier-4 and identifier-5
    /// shall be alphanumeric group items, national group items, variable-length groups, or strongly-typed group
    /// items and shall not be described with level-number 66." The two spellings differ (only the arithmetic
    /// ones exclude level-66 and enumerate the admitted group kinds), so the message names the rule the CALLER
    /// is under rather than reciting all three at everyone.
    /// <para>⛔ THE LEVEL-66 CASE GETS ITS OWN REASON. A RENAMES entry has <c>Pic</c> null and no
    /// <c>Children</c>, so <see cref="DataItem.IsGroup"/> is false for it and it used to be reported as an
    /// "elementary operand" — rejected for a reason the rule does not give. It is excluded BY NAME, and the
    /// message says so.</para>
    /// <para>⛔ AND THE OPERAND IS A <see cref="Place"/>, NOT A <see cref="DataItem"/>, BECAUSE SR12's SECOND
    /// HALF IS ABOUT THE REFERENCE (kb/Work PB390). "…and shall not be reference-modified" cannot be asked of
    /// the resolved item: a reference modifier rides on the <see cref="RefModPlace"/> DECORATOR and
    /// <c>PlaceDecorator.Item</c> answers with the INNER item, so a ref-modified group passed the group test
    /// and the prohibition went unchecked — the same blindness kb/Work PB347 measured at RELEASE. What caught
    /// it instead was an accident of the access-path factory (<c>CorrAccess.Create</c>'s default arm), under a
    /// storage-shape message naming neither the rule nor reference modification; and once that factory started
    /// switching on <c>Place.Undecorated</c> (kb/Work PB393) the accident stopped catching it at all, so
    /// <c>MOVE CORRESPONDING G1(1:3) TO G2</c> silently moved the WHOLE group with the modifier discarded. The
    /// arithmetic spellings need no separate rule for it: §8.4.3.3.4 GR6 makes the result of reference
    /// modification an ELEMENTARY data item ("The unique data item is considered to be an elementary data item
    /// without the JUSTIFIED clause"), which is none of the four group kinds their SR6 admits.</para></summary>
    /// <param name="operand">The resolved operand, DECORATIONS INTACT.</param>
    /// <param name="refText">The operand as written, for the message.</param>
    /// <param name="verb">MOVE | ADD | SUBTRACT.</param>
    /// <param name="clause">The caller's own rule, e.g. "§14.9.2.3 SR6".</param>
    /// <returns>true when the operand is admitted.</returns>
    public bool CheckCorrespondingGroupOperand(Place operand, string refText, string verb, string clause)
    {
        for (Place p = operand; p is PlaceDecorator d; p = d.Inner)
            if (d is RefModPlace)
            {
                data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                    $"{verb} CORRESPONDING operand '{refText}' is reference-modified — the operands of a "
                    + "CORRESPONDING statement are group items, and reference modification yields an "
                    + "elementary data item (ISO §8.4.3.3.4 GR6: \"The unique data item is considered to be an "
                    + $"elementary data item without the JUSTIFIED clause\"), so it is not one (ISO {clause}). "
                    + "Name the group itself, or MOVE the modified reference to a matching item.");
                return false;
            }
        DataItem item = operand.Item;
        if (item.Renames is not null)
        {
            data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                $"{verb} CORRESPONDING operand '{refText}' is described with level-number 66 — the operands "
                + $"\"shall not be described with level-number 66\" (ISO {clause})");
            return false;
        }
        if (item.IsGroup) return true;
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
            $"{verb} CORRESPONDING operand '{refText}' is an elementary item — both operands shall be group "
            + $"items (ISO {clause})");
        return false;
    }

    /// <summary>⛔ THE ONE <c>record-name-1</c> OPERAND RULE, shared by the three statements whose general
    /// format prints that operand: WRITE (ISO §14.9.51.3 SR5), REWRITE (§14.9.35.3 SR1) and RELEASE
    /// (§14.9.32.3 SR1). All three say the same thing — the operand shall be "the name of a logical record" in
    /// a file description entry, and it "may be qualified". Returns the owning file, or false HAVING REPORTED;
    /// the shape is <see cref="ResolveFile"/>'s, so a fourth verb naming a record inherits the rule instead of
    /// re-deriving it.
    /// <para>⛔ THE PREDICATE IS IDENTITY, NOT CONTAINMENT (kb/Work PB347). What this replaced —
    /// <c>SequentialIoBinder.FileOfRecord</c> — walked the reference's data item UP to its top-level 01
    /// (<c>while (root.Parent is { } p) root = p;</c>) and asked whether THAT ROOT was one of a file's records,
    /// so the predicate actually enforced was <i>the reference lies somewhere INSIDE a record</i>. Every
    /// subordinate item of a record passed it, at all three verbs, silently: on an 8-byte SD whose record is
    /// <c>05 SR-KEY PIC X(3) / 05 SR-DATA PIC X(5)</c>, <c>RELEASE SR-DATA</c> compiled with no diagnostic at
    /// any <c>--std</c> and injected SR-DATA's 5-byte image, space-extended to the SD's 8, into the sorted
    /// result as a record the program never released — the released width is taken from the REFERENCE, so a
    /// wrong operand produces a wrong-WIDTH record rather than anything loud. The upward walk survives here as
    /// the EXPLANATION rather than the verdict: naming the record the operand is subordinate to is the one
    /// thing that tells the user what to write instead.</para>
    /// <para>⛔ AND record-name-1 IS NOT AN IDENTIFIER, so it is not reference-modifiable. A record-name is a
    /// user-defined word (§8.3.2.2.25, "A record-name identifies a record described in a record description
    /// entry"), and §5.2.4's operand table gives that operand type — "User-defined word, including
    /// qualification and subscripting if needed" — qualification and subscripting, and nothing else.
    /// §8.4.3.3.3 SR5 permits reference modification only "anywhere an identifier referencing a data item of
    /// class alphanumeric, boolean, or national is permitted", and its NOTE spells the consequence out:
    /// "Because the references to data items are restricted to identifiers, where data-name-n is used in a
    /// general format or syntax rule, then reference-modification is not permitted." The printed RELEASE format
    /// (rendered from the PDF, not read from the OCR) writes <c>record-name-1</c>, not <c>identifier-1</c>.
    /// A reference modifier rides on the <see cref="RefModPlace"/> DECORATOR and leaves <see cref="Place.Item"/>
    /// untouched, which is precisely why the containment test could not see it: <c>RELEASE SRT-REC(1:3)</c>
    /// released a 3-byte record into an 8-byte sort file.</para>
    /// <para>The diagnostic is the EXISTING <see cref="DiagnosticCatalog.StatementOperandRule"/> (COBOLNET1757)
    /// and no new code: that descriptor's own contract is that "the CODE is the identity of the MECHANISM (a
    /// bind-time operand refusal), the MESSAGE carries the rule", and this IS that mechanism. ISO §4.2.2 ¶2
    /// puts the indication at compile time; before this, WRITE and REWRITE staged it to
    /// <c>BoundUnsupported</c>, so <c>WRITE WS-REC</c> got a COBOLNET1756 deferral WARNING — the compiler's own
    /// gap — for what is the source's error.</para></summary>
    /// <param name="record">The resolved operand.</param>
    /// <param name="refText">The operand as written, for the message.</param>
    /// <param name="verb">WRITE | REWRITE | RELEASE.</param>
    /// <param name="rule">The caller's own syntax rule, quoted verbatim with its citation.</param>
    /// <param name="file">The owning file when this returns true.</param>
    /// <returns>true when the reference IS a logical record of a file description entry.</returns>
    public bool ResolveRecordName(Place record, string refText, string verb, string rule,
                                  [NotNullWhen(true)] out FileModel? file)
    {
        file = null;
        for (Place p = record; p is PlaceDecorator d; p = d.Inner)
            if (d is RefModPlace)
            {
                data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                    $"{verb} '{refText}' — record-name-1 is a record-name, not an identifier, so it shall not "
                    + "be reference-modified: ISO §5.2.4 gives a \"User-defined word, including qualification "
                    + "and subscripting if needed\" those two decorations and no other, and §8.4.3.3.3 SR5 "
                    + "permits reference modification only \"anywhere an identifier referencing a data item of "
                    + $"class alphanumeric, boolean, or national is permitted\". {rule}. Write the record-name "
                    + "alone; to send part of a record, use the FROM phrase or a MOVE.");
                return false;
            }
        if (FileWhoseRecordIs(record.Item) is { } owner) { file = owner; return true; }
        DataItem root = record.Item;
        while (root.Parent is { } up) root = up;
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
            $"{verb} '{refText}' — {rule}"
            + (!ReferenceEquals(root, record.Item) && FileWhoseRecordIs(root) is { } containing
                ? $"; '{refText}' is subordinate to '{root.CobolName}', which is the logical record of "
                  + $"'{containing.CobolName}' — name that record, not one of its items"
                : $"; '{refText}' is not a logical record of any file description entry"));
        return false;
    }

    /// <summary>The file whose <see cref="FileModel.Records"/> contain this item — the IDENTITY test behind
    /// <see cref="ResolveRecordName"/>, asked of the reference's own item and (for the message only) of its
    /// top-level record. The second sweep is an inherited GLOBAL FD's record (ISO §13.18.30 — the record-names
    /// of a GLOBAL FD are GLOBAL names): the owning file is a CONTAINER's <see cref="FileModel"/>, present in
    /// this unit only through the <c>FilesByName</c> merge (<c>CallBindUnit</c>), so a contained program's
    /// WRITE/REWRITE of the owner's record resolves to the owner's ONE connector (IC233A's family; never a
    /// second mapping mechanism).</summary>
    private FileModel? FileWhoseRecordIs(DataItem item)
    {
        foreach (var f in data.Files)
            if (f.Records.Contains(item)) return f;
        foreach (var f in data.FilesByName.Values)
            if (f.Records.Contains(item)) return f;
        return null;
    }

    /// <summary>ISO §14.9.32.3 SR1's SECOND half — "…in a SORT-MERGE file description entry". The first half
    /// ("shall be the name of a logical record", and its no-reference-modification corollary) is
    /// <see cref="ResolveRecordName"/>'s, shared with WRITE and REWRITE; this is the part RELEASE alone has,
    /// and it is asked only of a reference that already IS a logical record — so <paramref name="file"/> is
    /// never null and the old "this name is not a record of any file description entry" arm has moved to the
    /// one place that can also say WHICH record the operand was subordinate to (kb/Work PB347). Only the STAGE
    /// was ever wrong here (kb/Work PB236): on a path the flow skipped, the program compiled AND ran to normal
    /// completion with no message at any time.</summary>
    public bool CheckReleaseRecord(FileModel file, string refText)
    {
        if (file.IsSortMerge) return true;
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
            $"RELEASE '{refText}' — record-name-1 shall be the name of a logical record in a sort-merge (SD) "
            + $"file description entry (ISO §14.9.32.3 SR1); '{refText}' is a record of '{file.CobolName}', "
            + "which is described by an FD");
        return false;
    }

    /// <summary>ISO §14.9.34.3 SR1 — "File-name-1 shall be described by a sort-merge file description entry in
    /// the data division." The RETURN twin of <see cref="CheckReleaseRecord"/> (kb/Work PB236). The
    /// "not declared at all" case is NOT this rule and never reaches here: it is
    /// <see cref="ResolveFile"/>'s §8.4.2.1 verdict, and conflating the two told a user whose file simply had
    /// an FD that the name was undefined.</summary>
    public bool CheckReturnFile(FileModel file)
    {
        if (file.IsSortMerge) return true;
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
            $"RETURN '{file.CobolName}' — file-name-1 shall be described by a sort-merge (SD) file description "
            + "entry in the data division (ISO §14.9.34.3 SR1); this file is described by an FD");
        return false;
    }

    /// <summary>A SORT/MERGE operand list the statement's GENERAL FORMAT does not print (kb/Work PB236) —
    /// §14.9.24.2's MERGE format prints <c>USING file-name-2 {file-name-3}…</c> (two names minimum) and
    /// requires one of <c>OUTPUT PROCEDURE</c> or <c>GIVING</c>; §14.9.40.3's table-sort rules fix what a key
    /// may be. ISO §4.2.2 ¶2 puts violations of "the general formats and the explicit syntax rules" in the
    /// compile-time warning mechanism, and these used to be run-time louds.</summary>
    public bool RejectStatementOperand(string message)
    {
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule, message);
        return false;
    }

    /// <summary>⛔ THE ONE ENTRY for the <c>SEARCH ALL</c> Format-2 operand rules, ISO §14.9.37.3 SR7–SR13
    /// (kb/Work PB445) — seven consecutive syntax rules that read the same two things and so were unenforced
    /// together. The rules themselves live in <see cref="SearchAllFormat2Rules"/> because they are predicates over
    /// a MODEL (the ordered OCCURS KEY phrase, and the WHEN decomposed into its Format-2 operands) rather than
    /// field tests, and the model is what makes the next Format-2 rule a predicate instead of a tree walk; this
    /// entry keeps the check catalog's single front door.
    /// <para>It takes a PARSE CONTEXT, unlike its neighbours here, and it has to: SR8 and SR9 are rules about the
    /// subscript AS WRITTEN — "shall be subscripted by the first index-name … shall not be followed by a '+' or a
    /// '–'" — a fact the bound operand has already erased. The <see cref="ReferenceResolver"/> comes from the
    /// caller for the same reason (the raw subscript segments are its to read).</para></summary>
    public bool CheckSearchAllFormat2(Core.SearchAllStatementContext s, DataItem table, ReferenceResolver refs) =>
        new SearchAllFormat2Rules(data, refs, this).Check(s, table);

    // ── INSPECT (ISO §14.9.22.3) — lifted at 10c ─────────────────────────────────────────────────────────────

    /// <summary>SR5 — a TALLYING counter shall be an elementary numeric data item.</summary>
    public bool CheckInspectTallyCounter(Place counter)
    {
        if (counter.Item.Pic is { Category: PicCategory.Numeric }) return true;
        data.Edition.Error("COBOLNET0847", $"INSPECT TALLYING counter '{counter.Item.CobolName}' shall "
            + "be an elementary numeric data item (ISO §14.9.22.3 SR5)");
        return false;
    }

    /// <summary>SR7 — with REPLACING CHARACTERS, literal-3 shall be ONE character (an identifier-5 of another
    /// size is the runtime GR15 case — the runtime uses its first character, deterministic).</summary>
    public bool CheckInspectCharactersReplacement(BoundOperand rep)
    {
        if (rep is not BoundStringLiteral { Value.Length: not 1 } bad) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT REPLACING CHARACTERS BY a {bad.Value.Length}-"
            + "character literal — literal-3 shall be one character in length (ISO §14.9.22.3 SR7)");
        return false;
    }

    /// <summary>SR6 — non-figurative literal-1 / literal-3 of unequal size is illegal (statically known, so
    /// diagnosed at compile time; the identifier-size mismatch is the runtime GR14 EC case). Called on the
    /// no-figurative-expansion path — the figurative rewrite (bind logic) stays in the binder.</summary>
    public bool CheckInspectReplacingSize(BoundOperand pat, BoundOperand rep, bool figurative)
    {
        if (pat is not BoundStringLiteral lp || rep is not BoundStringLiteral lr || figurative
            || lp.Value.Length == lr.Value.Length) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT REPLACING: literal '{lp.Value}' and replacement "
            + $"'{lr.Value}' differ in size (ISO §14.9.22.3 SR6 — equal size unless the replacement is figurative)");
        return false;
    }

    /// <summary>SR9 — CONVERTING literal-4 / literal-5 of unequal size (equal size unless literal-5 is
    /// figurative; the figurative expansion stays in the binder).</summary>
    public bool CheckInspectConvertingSize(BoundOperand from, BoundOperand to, bool figurative)
    {
        if (from is not BoundStringLiteral lf || to is not BoundStringLiteral lt || figurative
            || lf.Value.Length == lt.Value.Length) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT CONVERTING: '{lf.Value}' and '{lt.Value}' differ in "
            + "size (ISO §14.9.22.3 SR9 — equal size unless literal-5 is figurative)");
        return false;
    }

    /// <summary>SR2 — an INSPECT identifier operand shall be an elementary usage-display item.</summary>
    public bool CheckInspectOperandUsage(Place p, string refText)
    {
        if (!(p.Item.IsGroup || p.Item.Pic is { Usage: not Usage.Display })) return true;
        data.Edition.Error("COBOLNET0847", $"INSPECT operand '{refText}' shall be an elementary "
            + "usage-display item (ISO §14.9.22.3 SR2)");
        return false;
    }

    // ── MOVE (ISO §14.9.25.3) — lifted at 10e ────────────────────────────────────────────────────────────────

    /// <summary>ISO §14.9.25.3 SR2 (data-model D17): if a receiving operand is a strongly-typed group, the sending
    /// operand shall be a group item of the SAME type (§8.5.3.3 — a strong record accepts only a same-type whole-record
    /// source; its individual fields are still set by ordinary field MOVEs, and a strong-type SENDER to a non-strong
    /// receiver is permitted per Table 16). A mismatch → COBOLNET1533.</summary>
    /// <param name="implicitOf">The PHRASE whose implicit move is being screened — the <c>… FROM</c> of
    /// RELEASE/WRITE/REWRITE, the <c>… INTO</c> of READ/RETURN (kb/Work PB348) — or null for the explicit
    /// MOVE statement. It is reported because the other three MOVE screens report it: a <c>RETURN … INTO</c>
    /// into a strong group used to say only "MOVE to strongly-typed group", sending the programmer looking for
    /// a MOVE statement that is not in their source.</param>
    public bool CheckStrongMove(BoundOperand source, IReadOnlyList<Place> receivers,
                                ImplicitMovePhrase? implicitOf = null)
    {
        bool ok = true;
        DataItem? sender = source is BoundFieldOperand sf ? sf.Place.Item : null;
        foreach (var r in receivers)
        {
            if (!StrongTypeModel.IsStrongGroup(r.Item)) continue;
            if (sender is null || !StrongTypeModel.SameStrongType(sender, r.Item))
            {
                ok = false;
                data.Edition.Error(DiagnosticCatalog.StrongMoveMismatch, "MOVE to strongly-typed group "
                    + $"'{r.Item.CobolName ?? r.Item.CsName}': the sending operand shall be a group item of the same "
                    + $"type (ISO §14.9.25.3 SR2 / §8.5.3.3){ImplicitMovePhrase.Via(implicitOf)}");
            }
        }
        return ok;
    }

    /// <summary>ISO §14.9.25.3 SR9 (kb/Work PB393): "If identifier-1 or identifier-2 references a variable-length
    /// group then these groups shall be compatible groups as specified in 8.5.1.12, Variable-length groups." The
    /// relation itself is the ONE <see cref="VariableLengthCompatibility"/> module (built for the §14.8.2.2 /
    /// §14.8.3.2 activation-boundary crossing, kb/Work PB204) — this is the MOVE statement's application of it,
    /// never a second copy.
    /// <para>⛔ A NON-GROUP sender is a violation, not a fall-through. §8.5.1.12.1 states the prohibition in
    /// terms of the OTHER OPERAND — "a variable-length group is not equivalent to an alphanumeric data item and
    /// may not undergo a comparison or a move operation, in either direction, explicitly or otherwise, unless the
    /// other operand is a compatible group" — so <c>MOVE SPACES TO a-variable-length-group</c>, a literal, a
    /// function result, a reference-modified operand (§8.4.3.3.4 GR6 makes it an ELEMENTARY alphanumeric item)
    /// and a level-66 RENAMES alias (§13.18.45 composes ONE elementary item) are each refused. INITIALIZE is the
    /// statement that fills such a group (§14.9.20.4 GR7/GR10).</para>
    /// <para>No edition gate is needed and none is written: a variable-length group can only be DECLARED from
    /// COBOL-2014 (the DYNAMIC LENGTH clause §13.18.19 and OCCURS Format 4 §13.18.38), so the screen is
    /// unreachable below 2014 by construction rather than by a predicate that could drift.</para></summary>
    public bool CheckVariableLengthMove(BoundOperand source, IReadOnlyList<Place> receivers,
                                        ImplicitMovePhrase? implicitOf = null)
    {
        // The sending DATA ITEM, when the sender is one at all. A ref-modified or RENAMES place is deliberately
        // NOT unwrapped to its underlying item — those places ARE elementary alphanumeric operands by rule.
        DataItem? sender = source switch
        {
            BoundFieldOperand { Place: not (RefModPlace or RenamesPlace) } sf => sf.Place.Item,
            BoundCurrentRecord { Area: not (RefModPlace or RenamesPlace) } cr => cr.Area.Item,
            _ => null,
        };
        bool ok = true;
        foreach (var r in receivers)
        {
            var recv = r is RefModPlace or RenamesPlace ? null : r.Item;
            bool variable = (recv is not null && VariableLengthCompatibility.IsVariableLength(recv))
                || (sender is not null && VariableLengthCompatibility.IsVariableLength(sender));
            if (!variable) continue;
            string? why = sender is null || recv is null
                ? $"the {(recv is null ? "receiving" : "sending")} operand is not a group item: a "
                  + "variable-length group may move only to or from a compatible GROUP (ISO §8.5.1.12.1)"
                : VariableLengthCompatibility.Mismatch(sender, recv);
            if (why is null) continue;
            ok = false;
            string vl = recv is not null && VariableLengthCompatibility.IsVariableLength(recv)
                ? recv.CobolName ?? recv.CsName : sender!.CobolName ?? sender.CsName;
            data.Edition.Error(DiagnosticCatalog.MoveVariableLengthIncompatible,
                $"MOVE involving the variable-length group '{vl}': {why} — ISO §14.9.25.3 SR9 requires the two "
                + $"groups to be compatible as specified in §8.5.1.12{ImplicitMovePhrase.Via(implicitOf)}");
        }
        return ok;
    }


    // ── The … INTO phrase's OWN syntax rules (READ §14.9.30.3 · RETURN §14.9.34.3) ───────────────────────────

    /// <summary>
    /// ⛔ <b>THE ONE ADMISSIBILITY SCREEN FOR AN <c>… INTO</c> RECEIVER</b> — READ (ISO §14.9.30.3 SR1 and SR2)
    /// and RETURN (§14.9.34.3 SR2 and SR3), four syntax rules over two verbs, reached from the ONE place all
    /// three INTO arms funnel through (<c>MoveBinder.BindIntoPhrase</c>), so the next INTO-bearing verb inherits
    /// the rule by construction rather than by an edit.
    ///
    /// <para><b>What it enforces.</b> (1) The admissibility pair, which is the whole of the rule: the phrase is
    /// admitted when the description entry has at most one record description (arm a), OR when identifier-1 AND
    /// EVERY record-name of the file <i>"describe an alphanumeric group item or an elementary item of category
    /// alphanumeric or category national"</i> (arm b). (2) The strongly-typed receiver's record-area COUNT.
    /// Both rules' wording, and the one place they differ, ride <see cref="IntoPhraseRules"/>.</para>
    ///
    /// <para><b>Why arm b) is worded over ALL the record-names.</b> Several record descriptions share ONE record
    /// area (§13.4.2), and the phrase moves <i>the record area</i> (§14.9.30.4 GR4 b). Which description the area
    /// currently holds is a run-time fact, so the only way the move's category can be known at compile time is
    /// for every description — and the receiver — to be a shape a group move copies without conversion.</para>
    ///
    /// <para><b>What this screen deliberately does NOT check.</b> The second sentence of §14.9.30.3 SR2 /
    /// §14.9.34.3 SR3 (<i>"shall be a strongly-typed group item of the same type as identifier-1"</i>) is the same
    /// predicate over the same pair that §14.9.25.3 SR2 already applies to this move's SENDER — which IS the
    /// record area — and <see cref="CheckStrongMove"/> reports it as COBOLNET1533, naming the phrase. A second
    /// copy here is one rule in two places, this repository's most reproducible defect. The COUNT obligation is
    /// checked here because nothing else can express it: with several record areas the MOVE screen inspects only
    /// <c>FileModel.AreaRecord</c>, so a file whose LARGEST record happens to be the right type passed
    /// everything (kb/Work PB337, probe 5).</para>
    ///
    /// <para><b>No edition gate, and none is needed</b> (feedback_four_editions_one_compiler — the question was
    /// asked, not skipped). The admissibility rule is an all-editions rule, present since COBOL-85. The two
    /// shapes that make the 2023 wording NARROWER than its '85 ancestor — <i>alphanumeric</i> group item rather
    /// than any group item, and category <i>national</i> — cannot be DECLARED below the edition that introduced
    /// them (TYPEDEF/TYPE and USAGE NATIONAL are COBOL-2002, DYNAMIC LENGTH is COBOL-2014), so one predicate is
    /// per-edition-correct by construction rather than by a gate that could drift. §13.4.5.3 SR3's record-less FD
    /// is likewise unobservable: the binder materializes the implied record area (FILES design D18), so arm a)
    /// answers the same at every edition.</para>
    /// </summary>
    /// <param name="file">The file whose record area is the phrase's sender.</param>
    /// <param name="receiver">identifier-1, as resolved.</param>
    /// <param name="rules">The verb's own rules row.</param>
    /// <returns>true when the phrase conforms; false after reporting.</returns>
    public bool CheckIntoReceiver(FileModel file, Place receiver, IntoPhraseRules rules)
    {
        int count = file.Records.Count;
        // Arm a) of the admissibility rule and the strong-receiver rule impose THE SAME condition, which is why
        // one predicate answers both: READ's read "no record description entry or only one" and "at most one
        // record area" (count ≤ 1); RETURN's read "only one" and "exactly one" (count = 1).
        bool admittedByCount = rules.AdmitsRecordCount(count);

        // ── §14.9.30.3 SR2 / §14.9.34.3 SR3, the COUNT obligation. A strongly-typed identifier-1 is unreachable
        //    below COBOL-2002 (the TYPE clause's edition), so this arm needs no gate of its own.
        //    The receiver's PLACE KIND is deliberately not filtered the way arm b) filters it below: a
        //    reference-modified or RENAMES view of a strongly-typed group — which §8.4.3.3.4 GR6 would make an
        //    elementary alphanumeric item, outside this rule's reach — cannot BE written. Measured, not assumed:
        //    `READ f INTO WS-STRONG(1:4)` draws COBOLNET1647 (§8.4.3.3.3 SR1, reference modification of a
        //    strongly-typed group item) and a RENAMES over one draws COBOLNET1532 (§13.18.57.3 SR4), both before
        //    this screen runs. A guard for a shape nothing can reach is a guard nothing can contradict. ──
        if (StrongTypeModel.IsStrongGroup(receiver.Item))
        {
            // ⛔ THE ADMISSIBILITY RULE IS NOT ALSO REPORTED FOR A STRONG RECEIVER, and that is a derivation, not
            // a preference: §13.18.29.4 GR3 excludes a strongly-typed group from the alphanumeric group items, so
            // arm b) can NEVER admit one, leaving arm a) — which is the condition just tested. For this receiver
            // the two rules say the same thing and SR2/SR3 is the one that names the reason; emitting both is
            // emitting one fact twice.
            if (admittedByCount) return true;
            data.Edition.Error(DiagnosticCatalog.IntoPhraseStrongReceiverRecordAreas,
                $"{rules.Statement} '{ReceiverFace(receiver)}': identifier-1 is a strongly-typed group item, so "
                + $"{rules.StrongReceiverCount} ({rules.StrongReceiverCite}) — {EntryFace(file, rules, count)}. The record descriptions "
                + "share one record area, so which type the area holds is not decidable from the statement.");
            return false;
        }

        // ── §14.9.30.3 SR1 / §14.9.34.3 SR2, arm a) then arm b). ──
        if (admittedByCount) return true;
        // BOTH halves of arm b), and the FIRST offender of each is kept so the message names WHICH item failed
        // rather than restating the rule. identifier-1 is reported in preference to a record: it is the operand
        // the programmer chose, and a record-name they may not have written.
        DataItem? badRecord = file.Records.FirstOrDefault(r => !AdmitsIntoRecord(r));
        bool badReceiver = !AdmitsIntoOperand(receiver);
        if (badRecord is null && !badReceiver) return true;

        string why = badReceiver
            ? $"identifier-1 '{ReceiverFace(receiver)}' is {ItemCategory.Face(receiver.Item)}"
            : $"the record '{badRecord!.CobolName ?? "FILLER"}' is {ItemCategory.Face(badRecord)}";
        data.Edition.Error(DiagnosticCatalog.IntoPhraseReceiverNotAdmissible,
            $"{rules.Statement} '{ReceiverFace(receiver)}': {rules.AdmissibilityCite} admits the phrase on two "
            + $"grounds and neither holds — a) {rules.AdmissibilityArmA}, but {EntryFace(file, rules, count)}; or b) identifier-1 and "
            + "ALL record-names associated with file-name-1 \"describe an alphanumeric group item or an "
            + $"elementary item of category alphanumeric or category national\", but {why}. Write the statement "
            + "without the INTO phrase and MOVE from the record-name you mean.");
        return false;
    }

    /// <summary>Arm b) applied to identifier-1. A REFERENCE-MODIFIED or level-66 RENAMES receiver is admitted
    /// WITHOUT reading the item behind it: §8.4.3.3.4 GR6 makes the unique data item a ref-mod identifies "an
    /// elementary item of class and category alphanumeric" whatever identifier-1 is described as, and a RENAMES
    /// alias composes ONE elementary alphanumeric view (§13.18.45) — so asking the underlying entry's category
    /// would reject a legal <c>READ F INTO WS-NUM(1:4)</c> on a rule that does not reach it. The same reading
    /// <see cref="CheckVariableLengthMove"/> takes of the same two place kinds.</summary>
    private static bool AdmitsIntoOperand(Place p) =>
        p is RefModPlace or RenamesPlace || AdmitsIntoRecord(p.Item);

    /// <summary>Arm b) applied to ONE record-name — "an alphanumeric group item or an elementary item of category
    /// alphanumeric or category national", which is exactly <see cref="ItemCategory.IsAlphanumericOrNational"/>:
    /// its group arm IS §13.18.29.4 GR3's alphanumeric group item and its national arm covers both the elementary
    /// national item and the GR2 b) national group. Never a second category walk.</summary>
    private static bool AdmitsIntoRecord(DataItem item) => ItemCategory.IsAlphanumericOrNational(item);

    /// <summary>How identifier-1 names itself in these two diagnostics.</summary>
    private static string ReceiverFace(Place p) => p.Item.CobolName ?? p.Item.CsName;

    /// <summary>How the description entry names itself in these two diagnostics. A method rather than a local,
    /// so the string is built on the DIAGNOSTIC path only — <see cref="CheckIntoReceiver"/> runs on every INTO
    /// phrase in the source element and almost all of them conform.</summary>
    private static string EntryFace(FileModel file, IntoPhraseRules rules, int count) =>
        $"this {rules.EntryFace} for '{file.CobolName}' has {count}";

    // ── Arithmetic composite of operands (ISO §14.7 rule 2) — lifted at 10e ──────────────────────────────────

    /// <summary>The per-edition COMPOSITE-OF-OPERANDS check (ISO §14.7 rule 2, NATIVE arithmetic, the four
    /// arithmetic statements ONLY — COMPUTE expressions are explicitly exempt, §8.8.1.2 r7): the hypothetical item
    /// superimposing the statement's fixed-point operands aligned on their decimal points shall not exceed the
    /// edition's digit cap (18 at COBOL-85; the 2023 text says 31). Float/binary-native operands are excluded
    /// (rule 2b — the composite is then over the remaining operands).</summary>
    public bool CheckComposite(string verb, IEnumerable<BoundExpr> operands, IEnumerable<Receiver> receivers)
    {
        if (data.Options.Arithmetic != ArithmeticMode.Native) return true;   // §14.7 r2 applies to native only
        int maxInt = 0, maxFrac = 0;
        void Shape(int digits, int scale)
        {
            maxInt = Math.Max(maxInt, digits - scale);   // a negative (P-scaled) scale ADDS integer positions
            maxFrac = Math.Max(maxFrac, Math.Max(0, scale));
        }
        // ISO §14.7.7 rule 2b: a data item of usage binary-char/-short/-long/-double is EXCLUDED from the composite
        // (the composite is then over the OTHER operands, still capped at 31) — like a floating-point operand. Those
        // four picture-less usages carry Category Numeric + IsFloat false, so without this guard they were wrongly
        // superimposed (BINARY-DOUBLE contributes up to 20 integer digits) and pushed a conforming program past 31
        // (COBOLNET0805). COMP-5 is NOT in rule 2b's list and stays counted; float usages are already IsFloat. (CA6.)
        // Category NUMERIC-EDITED is IN the composite (kb/Work PB155): a MULTIPLY/DIVIDE GIVING resultant is a
        // composite member (§14.9.26.3 SR4 counts ALL operands; §14.9.12.3 SR4 excludes only REMAINDER) and
        // §14.9.26.3/§14.9.12.3 SR2 admit it as numeric-edited — requiring Category Numeric silently dropped it.
        // A floating-point-FORM edited mask (IsFloatEdited) is excluded as a DOCUMENTED READING, not rule 2b's
        // text (its list names usages and float literals, never an edited form): the composite is a
        // superimposition "aligned on their decimal points" (§14.7.7 r2's own definition) and an E-form mask
        // has no fixed decimal point to align — the same property that puts every listed exclusion outside
        // the superimposition. Its DigitPositions is 0 anyway, so inclusion would contribute nothing.
        static bool InComposite(PicInfo p) =>
            p is { Category: PicCategory.Numeric or PicCategory.NumericEdited, IsFloat: false, IsFloatEdited: false }
            && p.Usage is not (Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble);
        void OfExpr(BoundExpr e)
        {
            switch (e)
            {
                case BoundNumRef { Place.Item.Pic: { } p } when InComposite(p):
                    var (d, s) = data.StoredShapeOf(p);   // the ONE fixed-point geometry (edited: mask-derived)
                    Shape(d, s);
                    break;
                // §14.7.7 rule 2a/2b: a FLOATING-POINT literal is excluded from the composite — counting the
                // E-form's characters here rejected legal `ADD 1.5E+3 TO <PIC 9(28)>` (kb/Work PB155).
                case BoundNumLiteral lit when !CobolNet.Common.NumericLiteral.IsFloatingPointForm(lit.Text):
                    string t = lit.Text.TrimStart('+', '-');
                    int dot = t.IndexOf('.');
                    Shape(t.Count(char.IsAsciiDigit), dot < 0 ? 0 : t.Length - dot - 1);
                    break;
            }
        }
        foreach (var e in operands) OfExpr(e);
        foreach (var r in receivers)
            if (r.Place.Item.Pic is { } rp && InComposite(rp))
            {
                var (rd, rs) = data.StoredShapeOf(rp);
                Shape(rd, rs);
            }

        // The cap is 31 at EVERY edition (ISO §14.7 rule 2a — the 2023 text). A COBOL-85-specific tightening to
        // 18 was considered and REFUTED by the conformance corpus itself: CCVS-85 NC101A multiplies 9(3)V9(3) by
        // 9(18) (composite 21) as a deliberate SIZE ERROR test, and every conforming '85 implementation accepts
        // it — so the 18-digit figure does not govern the composite (it caps '85 PICTURE/literal capacity only).
        int composite = maxInt + maxFrac;
        if (composite <= 31) return true;
        data.Edition.Error("COBOLNET0805",
            $"{verb}: the composite of operands spans {composite} digits ({maxInt} integer + {maxFrac} fraction); "
            + "ISO/IEC 1989 caps the composite of operands at 31 digits (§14.7 rule 2)");
        return false;
    }

    // ── INITIALIZE (ISO §14.9.20.3) — lifted at 10e ──────────────────────────────────────────────────────────

    /// <summary>§14.9.20.3 SR6 ("the same category shall not be repeated in a REPLACING phrase") AND §5.2.6.4
    /// ("any single alternative shall be specified only once") — ONE check, because a category-name is a SET of
    /// category words and the two rules are the same ban at two scopes: SR6 across the phrase's items, §5.2.6.4
    /// within one item's category-name. <paramref name="namedSoFar"/> is the caller's running union.</summary>
    public bool CheckInitializeCategoryUnique(InitializeCategorySet namedSoFar, InitializeCategory cat)
    {
        if (!namedSoFar.Contains(cat)) return true;
        data.Edition.Error("COBOLNET0834",
            $"INITIALIZE REPLACING repeats the category-name {InitializeCategories.Spelling(cat)} "
            + "(ISO §14.9.20.3 SR6 — each category at most once in the phrase; §5.2.6.4 — each alternative at "
            + "most once within one category-name)");
        return false;
    }

    /// <summary>ISO §14.9.20.2 + §5.2.6.3 — the VALUE phrase's <c>{ ALL | category-name }</c> is a BRACE, and
    /// "one of the alternatives contained within the braces shall be explicitly specified or is implicitly
    /// selected": neither is implicitly selected here, so a bare <c>TO VALUE</c> is not conforming source. It was
    /// accepted (and read as ALL) on the strength of a §14.9.20.2 "note 2" the clause does not carry — kb/Work
    /// PB415. The binder recovers as ALL after this diagnostic, so one statement yields one message.</summary>
    public void CheckInitializeValueChoicePresent() =>
        data.Edition.Error(DiagnosticCatalog.InitializeValueChoiceMissing,
            "INITIALIZE ... TO VALUE requires ALL or a category-name before VALUE (ISO §14.9.20.2 — the braced "
            + "choice is mandatory, §5.2.6.3)");

    /// <summary>ISO §14.9.20.3 SR3 — "for each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or
    /// PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase, identifier-2 shall be
    /// specified". literal-1 is what the rule excludes, because §14.9.20.4 GR4 makes the implicit statement a SET
    /// for exactly those five categories and no SET format (§14.9.39) admits a literal sending operand.</summary>
    public void CheckInitializeReplacingSetCategoryIdentifier(InitializeCategorySet cats, string literalText)
    {
        foreach (var cat in InitializeCategories.All)
            if (cats.Contains(cat) && InitializeCategories.IsSetForm(cat))
            {
                data.Edition.Error(DiagnosticCatalog.InitializeReplacingPointerNeedsIdentifier,
                    $"INITIALIZE REPLACING {InitializeCategories.Spelling(cat)} ... BY the literal {literalText}: "
                    + "identifier-2 shall be specified (ISO §14.9.20.3 SR3 — the implicit statement for this "
                    + "category is a SET, §14.9.20.4 GR4, and no SET format admits a literal sending operand)");
                return;   // one message per REPLACING item, naming the first offending category
            }
    }

    /// <summary>ISO §14.9.20.3 SR4 for the SET-form categories — "a SET statement with identifier-2 as the sending
    /// operand and an item of the specified category as the receiving operand shall be valid". §14.9.39's pointer
    /// and object-reference formats admit only a sending operand of the receiver's own category, so category
    /// agreement is the necessary condition; an object-reference pair's §9.3.8.2 class conformance is the OO
    /// lane's rule and is not re-stated here.</summary>
    public void CheckInitializeReplacingSetCategoryAgrees(
        InitializeCategory cat, InitializeCategory? senderCat, string senderText)
    {
        string spelled = InitializeCategories.Spelling(cat);
        data.Edition.Error(DiagnosticCatalog.InitializeReplacingSetCategoryMismatch,
            $"INITIALIZE REPLACING {spelled} ... BY {senderText}, which is "
            + (senderCat is { } sc ? $"an item of category {InitializeCategories.Spelling(sc)}" : "not a data item")
            + $": ISO §14.9.20.3 SR4 requires that `SET <a {spelled} item> TO identifier-2` be valid, and "
            + "§14.9.39 admits only a sending operand of the receiver's own category");
    }

    /// <summary>SR5 — identifier-1 shall not have a RENAMES clause (a level-66 entry).</summary>
    public bool CheckInitializeTargetRenames(string name, IReadOnlyList<DataItem> named)
    {
        if (!named.Any(i => i.Renames is not null)) return true;
        data.Edition.Error("COBOLNET0835",
            $"INITIALIZE '{name}' — identifier-1 shall not have a RENAMES clause (ISO §14.9.20.3 SR5)");
        return false;
    }

    // ── Sequential file I/O (ISO §14.9.27 / §14.9.51) — lifted at 10h ───────────────────────────────────────

    /// <summary>§14.9.27.3 SR8 — OPEN … SHARING WITH ALL OTHER (clause or phrase) requires a LOCK MODE clause.</summary>
    /// <summary>§13.4.6.3 SR3/SR4 — a sort-merge (SD) file-name may be referenced ONLY by SORT / MERGE /
    /// RELEASE / RETURN (and a SORT/MERGE USING/GIVING). Every other input-output statement rejects at BIND
    /// time; the old posture was a patchwork — a bind error at UNLOCK, a runtime loud stage at
    /// OPEN/WRITE/READ/REWRITE, and NOTHING at CLOSE / DELETE / DELETE FILE, where the statement then ran
    /// against an UNREGISTERED connector and the fail-open registry reported the SUCCESSFUL '00' (kb/Work
    /// PB140). Returns the message (for the caller's BoundUnsupported placeholder), or null for a non-SD
    /// file.</summary>
    public string? ScreenSortMergeFile(FileModel file, string verb)
    {
        if (!file.IsSortMerge) return null;
        string msg = $"{verb} may not name the sort-merge file '{file.CobolName}' — an SD file-name may "
            + "appear only in SORT/MERGE/RELEASE/RETURN (ISO §13.4.6.3 SR3/SR4)";
        data.Edition.Error(DiagnosticCatalog.SortMergeFileInIoStatement, msg);
        return msg;
    }

    /// <summary>⛔ THE ONE SCREEN for the L1–L3 "phrase written where this statement's syntax rules forbid it"
    /// leniency family (kb/Work PB144). Three syntax rules across READ/REWRITE/DELETE say a phrase "shall not be
    /// specified" in a particular access mode or organization, and all three were TOLERATED unconditionally with
    /// a "CCVS-lenient" comment and no strict arm — so at <c>--std 2023</c> strict the compiler accepted source
    /// the standard forbids, silently (measured, not assumed: a REWRITE with INVALID KEY on both a
    /// sequential-organization and a relative-sequential-access file compiled clean and printed no diagnostic).
    /// <para>The severity decision routes through <see cref="EditionContext.Removed"/> — THE policy seam, which
    /// already carries documented-dialect-leniency gating as well as removed-construct gating: an ERROR under
    /// strict, a WARNING under <c>--permissive</c> with the bind UNCHANGED, so the CCVS-85 corpus that motivated
    /// the leniency keeps compiling and keeps its existing semantics. Never a local <c>Permissive</c> test and
    /// never a parallel <c>Lenient()</c> method.</para>
    /// <para>Returns true when the phrase is forbidden (the caller may still bind it — under permissive it must,
    /// and under strict the compile has already failed), so a call site reads as a screen, not a branch.</para></summary>
    /// <param name="forbidden">Whether the rule's condition holds — computed by the caller, since only it knows
    /// the statement's own access-mode/organization test.</param>
    /// <param name="phrase">The phrase as the program wrote it, e.g. "INVALID KEY".</param>
    /// <param name="verb">The statement, e.g. "REWRITE".</param>
    /// <param name="because">The rule's own condition, quoted from it, e.g. "a file with sequential organization".</param>
    /// <param name="citation">The §/SR, e.g. "ISO §14.9.35.3 SR2".</param>
    public bool ScreenForbiddenPhrase(bool forbidden, string phrase, string verb, string because, string citation)
    {
        if (!forbidden) return false;
        data.Edition.Removed(DiagnosticCatalog.IoPhraseForbiddenHere.Code,
            $"the {phrase} phrase shall not be specified for a {verb} statement that references {because} "
            + $"({citation})");
        return true;
    }

    /// <summary>⛔ THE ONE ENFORCEMENT OF ISO §14.9.30.3 SR6 — "None of the phrases ADVANCING, AT END, NEXT,
    /// NOT AT END, or PREVIOUS shall be specified if ACCESS MODE RANDOM is specified in the file control entry
    /// for file-name-1." Both READ binder arms call THIS: the rule is about the file control entry's access
    /// mode, which every organization has, and a copy per arm is how §14.9.30.3's other phrase rules came to be
    /// enforced on one arm only (kb/Work PB334). ⚠ On the SEQUENTIAL-organization arm it is now
    /// DEFENCE IN DEPTH, not the only guard: §12.4.5.5.2 SR2 ("The DYNAMIC and RANDOM phrases shall not be
    /// specified for a sequential file") is screened at the FILE CONTROL ENTRY as of kb/Work PB692
    /// (<c>DataBinder.BindFileControl</c>, COBOLNET1858), so <c>IsSequential &amp;&amp; AccessMode is Random</c>
    /// no longer reaches a statement at all. It stays because it is the STATEMENT's own rule and costs a
    /// field test — never because the entry rule might be missing.
    /// <para>The message names EVERY phrase the statement actually wrote — the at-end bracket carries choice
    /// indicators (§5.2.6.4), so AT END and NOT AT END can both be present and both are named.</para></summary>
    public bool CheckReadRandomAccessPhrases(FileModel file, bool advancing, bool atEnd, bool notAtEnd,
        bool next, bool previous)
    {
        if (file.AccessMode != FileAccessMode.Random) return true;
        var present = new List<string>();
        if (advancing) present.Add("ADVANCING");
        if (atEnd) present.Add("AT END");
        if (notAtEnd) present.Add("NOT AT END");
        if (next) present.Add("NEXT");
        if (previous) present.Add("PREVIOUS");
        if (present.Count == 0) return true;
        ScreenForbiddenPhrase(true, string.Join(" / ", present), "READ",
            "a file whose file control entry specifies ACCESS MODE RANDOM", "ISO §14.9.30.3 SR6");
        return false;
    }

    /// <summary>⛔ THE ONE ENFORCEMENT OF ISO §14.9.30.3 SR10 — "The KEY phrase may be specified only if
    /// ORGANIZATION IS INDEXED is specified in the file control entry for file-name-1." The rule names an
    /// ORGANIZATION, so it cannot live on one organization's binder arm: it was enforced only in
    /// <c>KeyedIoBinder.BindRead</c> (which reaches RELATIVE) while <c>SequentialIoBinder.BindRead</c> — the arm
    /// every SEQUENTIAL and LINE SEQUENTIAL file takes — never called <c>readKey()</c> at all, so the phrase was
    /// parsed and dropped without a word (kb/Work PB334). Returns true when the organization admits the
    /// phrase.</summary>
    public bool CheckReadKeyOrganization(FileModel file)
    {
        if (file.Organization == FileOrganization.Indexed) return true;
        data.Edition.Error("COBOLNET0864", $"READ … KEY on '{file.CobolName}': the KEY phrase may be "
            + "specified only when ORGANIZATION IS INDEXED (ISO §14.9.30.3 SR10)");
        return false;
    }

    /// <summary>⛔ THE ONE SCREEN for the MIRROR of <see cref="ScreenForbiddenPhrase"/>: a phrase the printed
    /// general format shows WITHOUT brackets, and that the program omitted (kb/Work PB350). ISO §5.2.6.2 makes
    /// brackets the ONLY convention that lets a portion of a format be omitted and §5.2.2 makes an underlined
    /// keyword required subject to those conventions, so an unbracketed phrase line is MANDATORY — a grammar
    /// that writes it <c>phrase?</c> under-rejects, which is the falsely-PERMISSIVE twin of the transcription's
    /// falsely-restrictive OCR bias.
    /// <para>⚠ THIS IS A HARD ERROR AT EVERY EDITION, deliberately NOT routed through
    /// <see cref="EditionContext.Removed"/> the way its forbidden-phrase mirror is. That seam exists because the
    /// CCVS-85 corpus really does write the phrases §14.9.30.3 SR6 forbids, and tolerating them leaves the bind
    /// UNCHANGED. Nothing is lenient about the omission this screen catches: measured over
    /// <c>tests/conformance</c> and <c>tests/nist</c>, every RETURN in the corpus writes its AT END, and a
    /// permissive arm would keep alive exactly the wrong answer PB350 reports — control falling THROUGH a
    /// RETURN at end of data onto an undefined record area (§14.9.34.4 GR3).</para>
    /// <para>⚠ It is screened HERE and not in the grammar on purpose. The grammar already produces a tree the
    /// binder can read, and a bind-time diagnostic can NAME the omitted phrase, the statement and the clause,
    /// where an ANTLR parse error can only report an unexpected token. Making the phrase mandatory in the
    /// grammar would also make the §14.9.34.3 SR4 reversed spelling report as a token-level surprise. The
    /// grammar rule carries a comment pointing here; <c>RequiredFormatPhraseDriftTests</c> re-derives from the
    /// transcription that RETURN's AT END is still the only §14.9 format line this applies to.</para>
    /// <para>Returns true when the phrase was omitted, so a call site reads as a screen, not a branch. The
    /// caller may still bind the statement — the compile has already failed.</para></summary>
    /// <param name="omitted">Whether the phrase is absent — computed by the caller, which alone knows how its
    /// own parse tree spells the phrase's presence.</param>
    /// <param name="phrase">The phrase as the format prints it, e.g. "AT END".</param>
    /// <param name="verb">The statement, e.g. "RETURN".</param>
    /// <param name="because">Why the format makes it mandatory, in the format's own terms.</param>
    /// <param name="citation">The §, e.g. "ISO §14.9.34.2".</param>
    public bool ScreenOmittedRequiredPhrase(bool omitted, string phrase, string verb, string because, string citation)
    {
        if (!omitted) return false;
        data.Edition.Error(DiagnosticCatalog.FormatRequiredPhraseOmitted,
            $"the {phrase} phrase is required on every {verb} statement and is omitted — {because} "
            + $"({citation}; brackets are the only convention that makes a portion of a general format "
            + "omissible, ISO §5.2.6.2)");
        return true;
    }

    /// <summary>⛔ THE ONE AND ONLY ENFORCEMENT OF ISO §14.9.27.3 SR8 — "When file-name-1 is not subject to an
    /// APPLY COMMIT clause, then if the sharing phrase is omitted from the OPEN statement and the ALL phrase is
    /// specified in the SHARING clause of the file control entry for file-name-1 or if the ALL phrase is
    /// specified on the OPEN statement, the LOCK MODE clause shall be specified in the file control entry for
    /// file-name-1." The rule is a SYNTAX RULE OF THE OPEN STATEMENT about file-name-1, so the OPEN binder is
    /// its only possible home; a second copy at the file control entry (deleted, kb/Work PB319) had to drop the
    /// antecedent — the antecedent names an OPEN statement the SELECT cannot see, and its leading conjunct names
    /// an I-O-CONTROL clause bound later — and so rejected legal source while double-reporting the violations.
    /// <para>THE ANTECEDENT IS WRITTEN OUT IN FULL, IN THE RULE'S OWN ORDER. (1) The leading conjunct: a file
    /// subject to an APPLY COMMIT clause is EXEMPT, and the exemption is load-bearing rather than decorative
    /// because §12.4.5.9.3 SR1 forbids writing a LOCK MODE clause "for a file that is the subject of an APPLY
    /// COMMIT clause" — without the exemption the two rules would make such a file unwritable. It is reachable
    /// today: COBOLNET1709 declines the clause but is <c>PermissiveInert</c>, so <c>--permissive</c> compiles the
    /// program. (2) The two disjuncts collapse to ONE test on the EFFECTIVE sharing mode, which is what the
    /// caller passes: <c>sharing ?? file.Sharing</c> is the ALL phrase when the OPEN group wrote one and the
    /// file control entry's clause when it did not (§14.9.27.4 GR23), which is exactly "[ALL on the OPEN]" OR
    /// "[phrase omitted AND the SELECT says ALL]". A phrase that is present and is NOT ALL satisfies neither
    /// disjunct, and a file with no OPEN statement is never asked at all — both are legal.</para>
    /// <para>Pure: returns false when the rule is violated so a call site reads as a screen.</para></summary>
    public bool CheckOpenSharingAllOther(FileModel file, SharingMode? effectiveSharing)
    {
        if (file.SubjectToApplyCommit) return true;   // SR8's leading conjunct — cf. §12.4.5.9.3 SR1
        if (!(effectiveSharing is SharingMode.AllOther && file.LockMode is null)) return true;
        data.Edition.Error("COBOLNET1512", $"OPEN of file '{file.CobolName}' with SHARING WITH ALL OTHER "
            + "requires the file to have a LOCK MODE clause (ISO §14.9.27.3 SR8)");
        return false;
    }

    /// <summary>§14.9.27.3 SR5 — <i>"The NO REWIND phrase may be specified only for sequential files."</i>
    /// The EXACT twin of the CLOSE rule §14.9.6.3 SR1 that <c>SequentialIoBinder.BindClose</c> already enforces
    /// (COBOLNET1693), which is why the phrase's OPEN half went unchecked for so long: one rule written in the
    /// standard twice, once per statement, and only the CLOSE spelling had a screen (kb/Work PB317/PB318).
    /// <para>The predicate is ORGANIZATION, not access mode — §9.1.7.2 puts record sequential and line
    /// sequential both under sequential organization, and <see cref="FileModel.IsSequential"/> is the same
    /// predicate the CLOSE arm tests, so the two arms cannot drift apart.</para>
    /// <para>It is also what makes §14.9.27.4 GR11 answerable: GR11 keys on the storage medium, the medium is
    /// <c>PhysicalFileCategory</c>, and a relative or indexed file is category (d) Non-sequential — a category
    /// for which neither GR11 nor GR12 defines a NO REWIND effect. Rejecting the source is the standard's own
    /// answer, not a deferral.</para></summary>
    public bool CheckOpenNoRewindOrganization(FileModel file)
    {
        if (file.IsSequential) return true;
        data.Edition.Error(DiagnosticCatalog.OpenNoRewindOrganization,
            $"OPEN '{file.CobolName}' WITH NO REWIND — the phrase may be specified only for sequential files "
            + "(ISO §14.9.27.3 SR5)");
        return false;
    }

    /// <summary>§14.9.27.3 SR6 — <i>"The NO REWIND phrase may be specified only when the INPUT or OUTPUT phrase
    /// is specified."</i> The rule pairs the phrase with the open mode of its own group, and §14.9.27.4 GR12 a)
    /// corroborates it by naming only EXTEND as the mode that suppresses the beginning-of-file positioning the
    /// phrase talks about: I-O and EXTEND have no rewind semantics to decline (kb/Work PB317/PB318).</summary>
    public bool CheckOpenNoRewindOpenMode(FileModel file, BoundOpenMode mode)
    {
        if (mode is BoundOpenMode.Input or BoundOpenMode.Output) return true;
        data.Edition.Error(DiagnosticCatalog.OpenNoRewindOpenMode,
            $"OPEN {(mode is BoundOpenMode.IO ? "I-O" : "EXTEND")} '{file.CobolName}' WITH NO REWIND — the "
            + "phrase may be specified only when the INPUT or OUTPUT phrase is specified (ISO §14.9.27.3 SR6)");
        return false;
    }

    /// <summary>§14.9.51 SR19 (the silent-drop bug class) — the END-OF-PAGE / NOT END-OF-PAGE phrase requires
    /// a LINAGE clause in the file's file description entry.</summary>
    public bool CheckWriteEopLinage(FileModel file)
    {
        if (file.Linage is not null) return true;
        data.Edition.Error("COBOLNET0860", $"WRITE … END-OF-PAGE on file '{file.CobolName}', whose file "
            + "description entry has no LINAGE clause (ISO §14.9.51 SR19)");
        return false;
    }

    /// <summary>§14.9.51 SR18 — ADVANCING PAGE and END-OF-PAGE shall not both be specified in one WRITE.</summary>
    public bool CheckWriteEopAdvancingPage(bool advancingPage)
    {
        if (!advancingPage) return true;
        data.Edition.Error("COBOLNET0861", "WRITE … ADVANCING PAGE with an END-OF-PAGE phrase: the two "
            + "shall not both be specified in a single WRITE statement (ISO §14.9.51 SR18)");
        return false;
    }

    /// <summary>§14.9.51.3 SR17 — <i>"The BEFORE and AFTER phrases shall not both be specified if the PAGE
    /// phrase is specified."</i> The pair itself is a COBOL-2023 addition (Annex §E.3.3 item 2, gated in
    /// <c>VersionConformancePass</c>); this is the one SYNTAX rule that narrows it, and it lives here beside
    /// SR18 and SR19 rather than inside the WRITE binder's operand plumbing (kb/Work PB712 — the old
    /// two-ADVANCING-phrase grammar checked SR17 as one arm of a malformed-pair test that also caught shapes
    /// §14.9.51.2 Format 1 cannot print).</summary>
    public bool CheckWriteBeforeAfterPage(bool bothWords, bool advancingPage)
    {
        if (!(bothWords && advancingPage)) return true;
        data.Edition.Error(DiagnosticCatalog.WriteBeforeAfterAdvancingPage,
            "WRITE … BEFORE AFTER ADVANCING PAGE: the BEFORE and AFTER phrases shall not both be specified if "
            + "the PAGE phrase is specified (ISO §14.9.51.3 SR17)");
        return false;
    }

    /// <summary>§14.9.51 SR13 — with a LINAGE clause, the ADVANCING phrase shall not name a SPECIAL-NAMES
    /// mnemonic (the caller resolves the mnemonic test through the per-unit registry).</summary>
    public bool CheckWriteAdvancingMnemonic(FileModel file, bool advancingNamesMnemonic)
    {
        if (!(file.Linage is not null && advancingNamesMnemonic)) return true;
        data.Edition.Error(DiagnosticCatalog.IoStatementOperandRule, $"WRITE … ADVANCING mnemonic-name on file '{file.CobolName}', whose "
            + "file description entry contains a LINAGE clause (ISO §14.9.51 SR13)");
        return false;
    }

    // ── The relational-operand SR checkpoint (ISO §8.8.4.2.2 / §8.8.4.2.3; lifted from ConditionBinder's
    //    CheckedRelational at P7 Step 10t/3 — the 10o deviation-(b) pure-lift discharged). ────────────────────

    /// <summary>The edition-invariant SR checks that ride the ONE <c>BoundRelational</c> checkpoint — reached
    /// by every relation (IF, EVALUATE pairings/ranges, PERFORM UNTIL, SEARCH WHEN, sole-operand conditions).
    /// A PURE emission check (no verdict — the caller always builds the node; the checks are side-effect
    /// diagnostics): class-boolean comparability (§8.8.4.2.2 Format 2 — boolean operands compare only with a
    /// boolean or the figurative ZERO, equality only — COBOLNET0844), and the strongly-typed-group rules
    /// (§8.8.4.2.3 SR1: same type both sides — COBOLNET1533; SR4: a strong group with a boolean/object/pointer
    /// leaf is equality-only — COBOLNET1535 <c>strong-compare-ordering</c>; plus the §8.8.4.2.12 signed-leaf
    /// ordering stage — COBOLNET0899 <c>strong-group-ordering-signed-leaf</c>, P10 Step 16).</summary>
    public void CheckRelationalOperands(BoundOperand left, string op, BoundOperand right)
    {
        // ⛔ The class of an operand is asked of the ONE classifier (IntrinsicResultType.OperandCategory — total
        // over literals, fields, ref-mod views, groups, ALL literals, boolean expressions and COMPUTED operands),
        // never re-derived here (kb/Work PB68): the local switch that stood here had no arm for a
        // BoundComputedOperand wrapping a BOOLEAN-result intrinsic, so `IF FUNCTION BOOLEAN-OF-INTEGER(544, 6) =
        // B"100000"` — exactly the §8.8.4.2.8 boolean comparison — was rejected as a class mix, while the mirror
        // against an ALPHANUMERIC literal (illegal) was accepted and evaluated. §15.13.1 makes the function's type
        // boolean; §15.2 item 2 its class and category; the classifier already knew.
        static bool IsBoolOperand(BoundOperand o) => IntrinsicResultType.OperandCategory(o) is PicCategory.Boolean;
        bool lb = IsBoolOperand(left), rb = IsBoolOperand(right);
        if (lb || rb)
        {
            static bool BoolCompatible(BoundOperand o) => o is BoundFigurative { Kind: 'Z' } || IsBoolOperand(o);
            if (!(BoolCompatible(left) && BoolCompatible(right)))
                data.Edition.Error("COBOLNET0844", "a boolean operand may be compared only with another "
                    + "boolean operand or the figurative constant ZERO (ISO §8.8.4.2.2; §8.8.4.2.1 F1 "
                    + "SR2/SR3 exclude class boolean from the general relation)");
            else if (op is not ("==" or "!="))
                data.Edition.Error("COBOLNET0844", "boolean operands compare for equality only — an ordering "
                    + "relation is not defined for class boolean (ISO §8.8.4.2.2 Format 2)");
        }
        // §8.8.4.2.3 SR1 (data-model D17): if either operand is a strongly-typed group, both shall be of the same
        // type (§8.5.3.3). This is the ONE relation checkpoint, so it also covers EVALUATE pairings/ranges,
        // PERFORM UNTIL, and SEARCH WHEN.
        DataItem? sl = left is BoundFieldOperand fl ? fl.Place.Item : null;
        DataItem? sr = right is BoundFieldOperand fr ? fr.Place.Item : null;
        if ((sl is { } && StrongTypeModel.IsStrongGroup(sl)) || (sr is { } && StrongTypeModel.IsStrongGroup(sr)))
        {
            if (sl is null || sr is null || !StrongTypeModel.SameStrongType(sl, sr))
                data.Edition.Error(DiagnosticCatalog.StrongCompareMismatch, "a strongly-typed group may be compared only with a group of the "
                    + "same type (ISO §8.8.4.2.3 SR1 / §8.5.3.3)");
            // §8.8.4.2.3 SR4: a strong group whose elementary items include class boolean, message-tag,
            // object, or pointer may be compared only for EQUALITY or INEQUALITY — an ordering relation on
            // such a group is a syntax error (the complete spec rule, not a stage; P10 Step 16 reclassified
            // the former "not implemented" framing — message-tag has no greenfield class yet, so the test
            // covers the three modeled categories).
            else if (op is not ("==" or "!=") && (ContainsNonOrderableLeaf(sl) || ContainsNonOrderableLeaf(sr)))
                data.Edition.Error(DiagnosticCatalog.StrongCompareOrdering, "a strongly-typed group containing a boolean, object-reference, "
                    + "or pointer element may be compared only for equality or inequality "
                    + "(ISO §8.8.4.2.3 SR4 — no ordering relation is defined for such a group)");
            // §8.8.4.2.12 (P10 Step 16, staged loud): strongly-typed groups order ELEMENT BY ELEMENT, a signed
            // numeric pair comparing ALGEBRAICALLY (§8.8.4.2.4) — the whole-group character-image comparison
            // the emitter performs cannot honor that for a SIGNED leaf (overpunch/separate-sign images do not
            // order algebraically). EQUALITY stays image-based for every same-type shape (a fixed profile's
            // value→image map is injective, so image-equal ⟺ element-equal), and an ordering over
            // unsigned-numeric/alphanumeric/national leaves is image-order == element-order (equal-width
            // digit/character columns, §8.8.4.2.7) — both fully implemented.
            else if (op is not ("==" or "!=") && sl is { } && (ContainsSignedNumericLeaf(sl) || ContainsSignedNumericLeaf(sr!)))
                data.Edition.Error(DiagnosticCatalog.StrongGroupOrderingSignedLeaf, "an ordering relation between "
                    + "strongly-typed groups containing a SIGNED numeric elementary item requires the "
                    + "element-by-element algebraic comparison of ISO §8.8.4.2.12/§8.8.4.2.4 — recognized but "
                    + "not yet implemented (the image comparison carries equality and unsigned orderings only)");
        }
    }

    /// <summary>True when a group (or elementary) item has any leaf of class boolean / object-reference / pointer —
    /// the categories that make a strongly-typed group comparable only for equality (ISO §8.8.4.2.3 SR4).</summary>
    private static bool ContainsNonOrderableLeaf(DataItem item)
    {
        if (item.IsElementary)
            return item.Pic?.Category is PicCategory.Boolean or PicCategory.ObjectReference or PicCategory.Pointer;
        foreach (var c in item.Children)
            if (ContainsNonOrderableLeaf(c)) return true;
        return false;
    }

    /// <summary>True when a group (or elementary) item has any SIGNED fixed-point numeric leaf — the one shape
    /// whose §8.8.4.2.12 element-by-element ordering (algebraic per element, §8.8.4.2.4) diverges from the
    /// whole-group character-image ordering the emitter performs (P10 Step 16 staged residue).</summary>
    private static bool ContainsSignedNumericLeaf(DataItem item)
    {
        if (item.IsElementary)
            return item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Signed: true };
        foreach (var c in item.Children)
            if (ContainsSignedNumericLeaf(c)) return true;
        return false;
    }
}
