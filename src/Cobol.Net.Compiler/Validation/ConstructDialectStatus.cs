// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;

namespace CobolNet.Validation;

/// <summary>A construct's availability at a targeted edition (the registry's verdict shape).</summary>
public enum ConstructAvailability
{
    /// <summary>Valid at the edition, no flag.</summary>
    Available,
    /// <summary>Newer than the edition — introduction gating (COBOLNET0900 band; error on BOTH axes).</summary>
    NotYetIntroduced,
    /// <summary>Removed by the edition — error strict / warning permissive (<see cref="EditionContext.Removed"/>).</summary>
    Removed,
    /// <summary>Obsolete at the edition (ISO §4.2.13 over Annex F.2) — warning always (0903).</summary>
    Obsolete,
}

/// <summary>
/// One construct's per-edition dialect status (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.5):
/// the in-code rendering of a <c>tests/version-matrix/constructs.json</c> row — THE canonical catalogue; the
/// drift test (<c>ConstructRegistryDriftTests</c>) asserts registry↔json equality BOTH directions, so a gate
/// cannot land without its matrix row nor a row without its registry entry.
/// </summary>
/// <param name="Id">The constructs.json row id.</param>
/// <param name="Display">Human name used in diagnostics.</param>
/// <param name="IntroducedIn">First edition that HAS the construct (85/2002/2014/2023).</param>
/// <param name="RemovedIn">First edition that REMOVED it (null = never).</param>
/// <param name="ObsoleteIn">First edition marking it obsolete/archaic (null = never; drives 0903).</param>
/// <param name="DiagnosticCode">The code its gate emits (the TARGET code where surfacing is still a raw
/// parse error today — the W1.5 upgrade wires it).</param>
/// <param name="Citation">ISO § / VCR row / roadmap-D citation.</param>
public sealed record ConstructDialectStatus(
    string Id, string Display, int IntroducedIn, int? RemovedIn, int? ObsoleteIn,
    string DiagnosticCode, string Citation)
{
    /// <summary>The availability verdict at <paramref name="edition"/>.</summary>
    public ConstructAvailability StatusAt(int edition)
    {
        if (edition < IntroducedIn) return ConstructAvailability.NotYetIntroduced;
        if (RemovedIn is { } r && edition >= r) return ConstructAvailability.Removed;
        if (ObsoleteIn is { } o && edition >= o) return ConstructAvailability.Obsolete;
        return ConstructAvailability.Available;
    }
}

/// <summary>
/// The construct registry + the ONE gating entry point (P2.5): every edition gate — validator override or
/// binder-side — routes through <see cref="Check"/>, which maps availability onto the
/// <see cref="EditionContext"/> channels (one policy, several emit sites; feedback_singular_pattern).
/// </summary>
public static class ConstructRegistry
{
    /// <summary>The in-code rendering of constructs.json (drift-tested against it both directions). Pending
    /// rows (not yet implemented) are REGISTERED — their edition metadata is frozen here even before their
    /// owning roadmap phase lands.</summary>
    public static readonly IReadOnlyList<ConstructDialectStatus> Entries =
    [
        new("nucleus-move-display", "nucleus MOVE/DISPLAY", 85, null, null, EditionCodes.Introduction, "edition-invariant baseline"),
        new("read-previous-2002", "READ PREVIOUS", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.30 Format 1; VCR rows 29/108 gate after-OPEN behavior"),
        new("start-first-last-2002", "START FIRST/LAST", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.41"),
        new("delete-file-2023", "DELETE FILE", 2023, null, null, EditionCodes.Introduction, "ISO 2023 §14.9.10 Format 2; Annex E.3.3 item 15"),
        new("allocate-2002", "ALLOCATE", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.3"),
        new("free-2002", "FREE", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.15"),
        new("invoke-2002", "INVOKE", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.23 (OO)"),
        new("goback-returning-2002", "GOBACK RETURNING", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.18"),
        new("stop-run-status-2002", "STOP RUN WITH status", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.42"),
        new("based-clause-2002", "BASED clause", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.5"),
        new("procedure-returning-2002", "PROCEDURE DIVISION RETURNING", 2002, null, null, EditionCodes.Introduction, "ISO §14.2"),
        new("currency-picture-symbol-2002", "CURRENCY SIGN WITH PICTURE SYMBOL", 2002, null, null, "COBOLNET0893", "ISO §12.3.7; pinned pre-band code (VCR Table 7 row 7.3, DEVLOG 558 — in the roadmap traceability band); the W1.5 registry truth-fix (was mislabeled 0900)"),
        new("pic-wide-19-digits-2002", "fixed-point item wider than 18 digits", 2002, null, null, "COBOLNET0802", "ISO §8.3.1.2 / §13.18.40 (the LIVE digit-capacity gate)"),
        new("options-arithmetic-native-2014", "OPTIONS paragraph / ARITHMETIC IS NATIVE", 2014, null, null, EditionCodes.Introduction, "ISO §11.9"),
        new("rounded-mode-is-2014", "ROUNDED MODE IS", 2014, null, null, EditionCodes.Introduction, "ISO §14.7.4"),
        new("arithmetic-standard-decimal-2014", "ARITHMETIC IS STANDARD-DECIMAL", 2014, null, null, EditionCodes.Introduction, "ISO §11.9.5 / §8.8.1.5"),
        new("type-clause-2002", "TYPE clause (TYPEDEF family)", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.58; PROVISIONAL 2002 edge (ISO-validation DEVLOG 582; decision-1 policy)"),
        new("usage-float-short-2002", "USAGE FLOAT-SHORT", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.59; D16 split (provisional 2002); PENDING (Phase 6)"),
        new("usage-float-binary32-2014", "USAGE FLOAT-BINARY-32", 2014, null, null, EditionCodes.Introduction, "ISO §13.18.59; D16 split (provisional 2014); PENDING (Phase 6)"),
        new("constant-entry-2002", "constant entry (01 … CONSTANT AS)", 2002, null, null, EditionCodes.Introduction, "ISO §13.10 + §13.18.15; D5; PENDING (Phase 6)"),
        new("concat-operator-2002", "concatenation expression (&)", 2002, null, null, EditionCodes.Introduction, "ISO §8.8.3; D6; PENDING (Phase 4g)"),
        // ── Removal gates (P2.6) + reserved-word interval rows (P2.7): RemovedIn drives Removed()/0901 ──
        new("label-records-removed-2002", "the LABEL RECORDS clause", 85, 2002, null, EditionCodes.RemovedConstruct, "obsolete '85 FD element DELETED by ISO 2002; the 2023 FD clause set (§13.18) has no LABEL clause; VCR Table 7"),
        new("user-word-commit-2023", "the word COMMIT as a user-defined word", 85, 2023, null, EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable until 2023 reserved it (Annex E.2 item 25 = VCR row 32)"),
        new("user-word-raising-2002", "the word RAISING as a user-defined word", 85, 2002, null, EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable at 85, reserved since 2002 (the EC family — DEVLOG 585 correction)"),
        new("receive-as-user-word", "the word RECEIVE as a user-defined word", 2002, 2023, null, EditionCodes.ReservedWord, "§8.9 interval encoding of the RE-reservation: 85-reserved (communication) → user-definable 2002/2014 → re-reserved 2023 (Annex E.2 item 25)"),
        new("end-receive-as-user-word", "the word END-RECEIVE as a user-defined word", 2002, 2023, null, EditionCodes.ReservedWord, "§8.9 interval encoding: the THIRD re-reserved communication word — discovered mechanically (DEVLOG 585); same interval as RECEIVE"),
        // ── The P2.6 Wave-1 gate batch (DEVLOG 589) ──
        new("value-of-removed-2002", "the VALUE OF clause (FD)", 85, 2002, null, EditionCodes.RemovedConstruct, "obsolete '85 label-field clause, deleted by ISO 2002; VCR Table 7"),
        new("data-records-removed-2002", "the DATA RECORDS clause (FD/SD)", 85, 2002, null, "COBOLNET0873", "obsolete '85 element deleted by ISO 2002 (§13.4.6 admits only the record clause); pinned code kept; VCR Table 7 row 7.1"),
        new("multiple-file-tape-removed-2002", "the MULTIPLE FILE [TAPE] clause (I-O-CONTROL)", 85, 2002, null, EditionCodes.RemovedConstruct, "obsolete '85 reel-sharing description, deleted by ISO 2002; VCR Table 7"),
        new("memory-size-removed-2002", "the MEMORY SIZE clause (OBJECT-COMPUTER)", 85, 2002, null, EditionCodes.RemovedConstruct, "obsolete '85 element deleted by ISO 2002; token-scan of the computerAttributes sink; VCR Table 7"),
        new("segment-limit-removed-2002", "the SEGMENT-LIMIT clause (OBJECT-COMPUTER)", 85, 2002, null, EditionCodes.RemovedConstruct, "segmentation deleted by ISO 2002; token-scan of the computerAttributes sink; VCR Table 7"),
        new("debugging-mode-removed-2002", "the WITH DEBUGGING MODE clause (SOURCE-COMPUTER)", 85, 2002, null, EditionCodes.RemovedConstruct, "the '85 debug facility deleted by ISO 2002; token-scan of the computerAttributes sink; VCR Table 7"),
        new("identification-comments-removed-2002", "an identification comment paragraph (AUTHOR/INSTALLATION/DATE-WRITTEN/DATE-COMPILED/SECURITY)", 85, 2002, null, EditionCodes.RemovedConstruct, "the five obsolete '85 comment paragraphs deleted by ISO 2002; one row, paragraph named per site; VCR Table 7"),
        new("remarks-removed-2002", "the REMARKS paragraph", 85, 2002, null, EditionCodes.RemovedConstruct, "'74 carryover accepted at 85 for CCVS (never flagged there); absent from ISO 2002+; VCR Table 7"),
        new("stop-literal-removed-2002", "the STOP literal statement", 85, 2002, null, EditionCodes.RemovedConstruct, "X3.23-1985 Format 2 (operator message + continue — implemented via BoundStopLiteral), deleted by ISO 2002 (§14.9.42 has no literal form); the DEVLOG-578 mis-bind fixed same change set"),
        new("open-reversed-removed-2002", "the OPEN REVERSED phrase", 85, 2002, null, EditionCodes.RemovedConstruct, "obsolete '85 tape phrase deleted by ISO 2002 (NO REWIND survives, §14.9.26); VCR Table 7"),
        new("close-with-lock-removed-2023", "the CLOSE WITH LOCK phrase", 85, 2023, null, EditionCodes.RemovedConstruct, "REMOVED 2014→2023 (Annex E deletion; VCR row 7)"),
        new("exit-method-window", "the EXIT METHOD statement", 2002, 2023, null, EditionCodes.RemovedConstruct, "introduced 2002 (OO), REMOVED 2023 (Annex E @49034; VCR row 5; the OO deep-dive correction #2) — the dual-obligation window: 0900 below 2002, 0902 at 2023"),
        new("method-working-storage-window", "a WORKING-STORAGE SECTION in a method definition", 2002, 2023, null, EditionCodes.RemovedConstruct, "legal 2002/2014 (OO deep-dive D3: STATIC-field semantics — shared across instances, persistent across activations, ISO §11.7), BANNED by 2023 §13.5.3 SR 1 (spec @16461; deep-dive Spec correction #1; VCR Table 6 row 130e) — 0900 below 2002, 0902 at 2023; --permissive keeps the pre-removal static semantics (slice 2, DEVLOG 602)"),
        new("exit-function-window", "the EXIT FUNCTION statement", 2002, 2023, null, EditionCodes.RemovedConstruct, "introduced 2002 (UDF), REMOVED 2023 (Annex E @49036; VCR row 6) — dual-obligation window"),
        new("exit-program-archaic-2023", "the EXIT PROGRAM statement", 85, null, 2023, EditionCodes.ObsoleteFlag, "ARCHAIC in ISO 2023 (Annex F.1; §4.2.12; VCR row 89) — warning only, the element remains conforming"),
        new("next-sentence-archaic-2023", "the NEXT SENTENCE phrase", 85, null, 2023, EditionCodes.ObsoleteFlag, "ARCHAIC in ISO 2023 (Annex F.1; §4.2.12; VCR row 90) — warning only"),
        // ── The P2.8 W2 wave (roadmap Phase 2): MOVE rows (track A) + the loud-guard skeleton rows (track B) ──
        new("move-alphanumeric-figurative-removed-2023", "MOVE of an alphanumeric figurative constant (SPACE/HIGH-VALUE/LOW-VALUE/non-digit ALL) to a numeric or numeric-edited item", 85, 2023, null, EditionCodes.RemovedConstruct, "Annex E.2 item 1 bullet 1; §14.9.25.3 SR5; VCR row 1 — permitted through 2014 (pre-removal semantics preserved permissive), REMOVED 2023 except the digit-only-ALL-to-integer case; QUOTE rides its own dual row (obsolete 2014 first — E.2 item 21)"),
        new("move-quote-numeric-obsolete-2014", "MOVE of the figurative constant QUOTE to a numeric or numeric-edited item", 85, 2023, 2014, EditionCodes.RemovedConstruct, "Annex E.2 item 21 (obsolete-in-2014) + E.2 item 1 (removed 2023); §14.9.25.3 SR5 — the ONE figurative the change annex tracks separately (W2 adversarial-review correction, DEVLOG 595): 0903 warning at 2014, 0902 at 2023"),
        new("move-all-digit-integer-obsolete-2023", "MOVE of a digit-only ALL literal (or a digit symbolic-character) to an integer numeric item", 85, null, 2023, EditionCodes.ObsoleteFlag, "§14.9.25.3 SR5 + its NOTE; Annex F.2 item 2; VCR rows 92/128 — the sole surviving figurative→numeric MOVE, obsolete-flagged at 2023"),
        new("national-data-2002", "national data (PICTURE symbol N / USAGE NATIONAL)", 2002, null, null, EditionCodes.Introduction, "ISO §8.5.2 category national / §13.18.40 / §13.18.60; LIVE (Phase 4a track (a)): compiles at 2002+, COBOLNET0900 below; byte-surface legs (REDEFINES/cells/records) + national-form numerics stage 0899"),
        new("boolean-data-2002", "boolean data (PICTURE symbol 1 / USAGE BIT)", 2002, null, null, EditionCodes.Introduction, "ISO §8.5.2 category boolean / §13.18.40 / §13.18.60; LIVE (Phase 4a track (a)): compiles at 2002+, COBOLNET0900 below; one '0'/'1' char per position (GR14 R14, D-B1); the boolean-operators leg is boolean-operators-2002"),
        new("national-edited-2002", "national-edited data (PICTURE N with B 0 /)", 2002, null, null, EditionCodes.Introduction, "ISO §8.5.2.11 / §13.18.40.4 GR10; recognized + edition-gated, representation PENDING (Phase 4a residue #2 — 0899 at 2002+)"),
        new("pic-external-float-2002", "an external floating-point PICTURE (symbol E)", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.40 external float; skeleton W2 (loud), full Phase 6 (IEEE float catchall); PENDING"),
        new("usage-pointer-2002", "USAGE POINTER", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.60 / §8.5.2.6; the ManagedPointer carrier — LIVE end-to-end (increment 1: SET TO NULL/pointer + structural equality, DEVLOG 613; increment 2: ADDRESS OF / BASED / SET ADDRESS OF / ALLOCATE-FREE / F10 arithmetic on the StorageCell+CellPointer model, DEVLOG 617); introduction gate 0900 below 2002"),
        new("set-address-2002", "SET ADDRESS OF (data-pointer assignment)", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.39 Format 7 + §8.4.3.11 ADDRESS OF; LIVE (Phase-4b increment 2, DEVLOG 617) — both directions (SR18 BASED receiver; the sender form takes a CellPointer window); binder-side gate in PtrBindSetAddress; 2002 introduction (derive from the 2002 standard — Annex E covers 2014→2023 only)"),
        new("pointer-arithmetic-2002", "SET pointer UP/DOWN BY (data-pointer arithmetic)", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.39 Format 10 (GR20 — byte-granular movement; GR18 NULL → EC-DATA-PTR-NULL; GR19 — a non-integer amount VALUE → runtime EC-SIZE-ADDRESS fatal via CobolPtr.UpByScaled, the exact value rule); LIVE (Phase-4b increment 2, DEVLOG 617) — the D-U7 category re-route on the shared 85 index grammar shape; binder-side gate in PtrTryBindSetUpDown; 2002 introduction (derive from the 2002 standard — Annex E covers 2014→2023 only)"),
        new("usage-object-reference-2002", "USAGE OBJECT REFERENCE", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.60.4 / §8.5.2.14 (OO); LIVE as of the Phase-3 spine (PicInfo.ObjectReferenceItem — typed/universal reference fields; 0900 below 2002)"),
        new("usage-binary-char-family-2002", "a fixed-width binary usage (BINARY-CHAR/-SHORT/-LONG/-DOUBLE)", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.60.4 GR12/GR21 — native two's-complement 1/2/4/8-byte integers (SIGNED default / UNSIGNED widens), the COMP-5 BinaryCapacity discipline; LIVE (Phase 4 M2-DATA-1, DEVLOG 614); introduction gate 0900 below 2002"),
        new("usage-float-long-2002", "USAGE FLOAT-LONG", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.59; D16 split (provisional 2002); PENDING (Phase 6)"),
        new("usage-float-extended-2002", "USAGE FLOAT-EXTENDED", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.59; D16 split (provisional 2002); PENDING (Phase 6)"),
        // ── The W1.5 parse-layer mapping rows (EditionGateHints; roadmap Phase 2 W1.5, DEVLOG 594) ──
        new("repository-class-2002", "the REPOSITORY CLASS entry", 2002, null, null, EditionCodes.Introduction, "ISO §12.3.8 (OO); grammar-gated (repositoryEntry); W1.5 parse-layer 0900 mapping"),
        new("start-with-length-2002", "the START KEY … WITH LENGTH phrase", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.41 (provisional 2002 edge); grammar-gated (startKeyPhrase); W1.5 parse-layer 0900 mapping"),
        new("special-names-for-national-2002", "the FOR ALPHANUMERIC/NATIONAL phrase (ALPHABET/CLASS/SYMBOLIC CHARACTERS)", 2002, null, null, EditionCodes.Introduction, "ISO §12.3.7; grammar-gated at three SPECIAL-NAMES sites; W1.5 parse-layer 0900 mapping"),
        new("call-by-value-2002", "the CALL BY VALUE phrase", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.4; grammar-gated (callByValue); W1.5 parse-layer 0900 mapping"),
        new("class-definition-2002", "a class definition (CLASS-ID compilation unit)", 2002, null, null, EditionCodes.Introduction, "ISO §11.2/§11.3 (OO); grammar-gated in compilationGroup; LIVE since the Phase-3 spine part 2 (the ClassUnit collection + pass-1 class symbol table, DEVLOG 601)"),
        // ── The INTERFACE/PROPERTY wave (roadmap Phase 3; DEVLOG 606) ──
        new("interface-definition-2002", "an interface definition (INTERFACE-ID compilation unit)", 2002, null, null, EditionCodes.Introduction, "ISO §11.5/§11.6 (OO); grammar-gated in compilationGroup; LIVE at the INTERFACE wave — C# interface emission, prototype LINKAGE binding (§10.6.2 SR4), the §11.8.4 GR2 closure + the binder-authoritative §9.3.8.2.3 conformance pass (0841; Roslyn is provably insufficient both directions)"),
        new("repository-interface-2002", "the REPOSITORY INTERFACE entry", 2002, null, null, EditionCodes.Introduction, "ISO §12.3.8 (OO); grammar-gated (repositoryEntry); W1.5 parse-layer 0900 mapping"),
        new("repository-property-2002", "the REPOSITORY PROPERTY entry", 2002, null, null, EditionCodes.Introduction, "ISO §12.3.8 (OO; required by §8.4.3.9.3 SR1 property references); grammar-gated (repositoryEntry); W1.5 parse-layer 0900 mapping"),
        new("implements-clause-2002", "the IMPLEMENTS clause (FACTORY/OBJECT paragraph)", 2002, null, null, EditionCodes.Introduction, "ISO §11.8; transitively grammar-gated inside classDefinition/interfaceDefinition (no separate W1.5 hint — the unit's own gate fires first); conformance via the §11.8.4 GR2 closure = COBOLNET0841; the word IMPLEMENTS is §8.10 CONTEXT-SENSITIVE (spec :10853) — a user word at EVERY edition, never in CheckedTokenTypes"),
        new("property-clause-2002", "the PROPERTY data-description clause", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.42; {is2002()}?-gated in dataDescriptionClause + the valueItem loop guard (at 2002+ PROPERTY terminates a VALUE clause — reserved, never a constant-name operand); accessor synthesis per GR1/GR2 under the pinned __GET_/__SET_ names (§11.7.4 GR1a); W1.5 parse-layer 0900 mapping"),
        new("set-object-reference-2002", "SET … TO object-reference (Format 5)", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.39 F5 (OO); grammar-gated (setObjectReferenceStatement) for NULL/SELF senders + the BindSetTo semantic re-route for data senders (ANTLR alternative-order reality); LIVE as of the universal wave (D-U7) — SR8/SR9/SR12/SR13 = COBOLNET0867"),
        new("method-property-selector-2002", "the METHOD-ID GET/SET PROPERTY selector", 2002, null, null, EditionCodes.Introduction, "ISO §11.7 (the SR6/SR7 accessor shapes → COBOLNET0842); transitively grammar-gated inside classDefinition; explicit accessors join the roster under the same pinned names as clause-synthesized ones — override/implements machinery applies unchanged"),
        // ── Phase 4c: user-defined functions (M2-UDF-1; DEVLOG 615) ──
        new("user-function-invocation-2002", "a user-defined function reference (FUNCTION function-prototype-name)", 2002, null, null, EditionCodes.Introduction, "ISO §8.4.3.2 function-identifier / §9.4 user-defined functions / §12.3.8.2 GR12; LIVE (Phase 4c M2-UDF-1) — in-group whole-source activation lowered onto the program-activation ABI (hoisted CALL…RETURNING over a §8.4.3.2.4 GR1 result temp); introduction gate 0900 below 2002 (the PD-header RETURNING parse hint fires there too)"),
        // ── The W3 XOR regating (VCR rows 32/41; Annex E.2 item 25 — DEVLOG 596) ──
        new("logical-xor-operator-2023", "the logical XOR/EXCLUSIVE-OR operator", 2023, null, null, EditionCodes.Introduction, "ISO §8.8.4.9; a 2023 addition per Annex E.2 item 25 (VCR rows 32/41 — the chair-adjudicated regating of the former '2002' mislabel); {is2023()}?-gated operator + W1.5 parse-layer 0900 mapping"),
        new("boolean-operators-2002", "the boolean operators B-AND/B-OR/B-XOR/B-NOT (boolean expressions)", 2002, null, null, EditionCodes.Introduction, "ISO §8.7.2/§8.8.2; COMPUTE F2 §14.9.8; boolean relation §8.8.4.2.2; simple boolean condition §8.8.4.3; {is2002()}?-gated operator tiers + W1.5 parse-layer 0900 mapping; 2002 introduction (derive from the 2002 standard — Annex E covers 2014→2023 only)"),
        new("user-word-b-and-2002", "the word B-AND as a user-defined word", 85, 2002, null, EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable at 85, reserved since 2002 (ReservedWords.Table rows cover all four B-operators; single representative — the user-word-raising-2002 precedent)"),
        new("user-word-xor-2023", "the word XOR as a user-defined word", 85, 2023, null, EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable until 2023 reserved it (Annex E.2 item 25 = VCR row 32); cobolWord-admitted at the W3 regating"),
        new("user-word-exclusive-or-2023", "the word EXCLUSIVE-OR as a user-defined word", 85, 2023, null, EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable until 2023 reserved it (Annex E.2 item 25 = VCR row 32); cobolWord-admitted at the W3 regating"),
        // ── The W3 preprocessor threading (VCR rows 2/4/94; DEVLOG 598) — emit sites live in the FRONTEND
        //    (only the column-aware pass sees the col-7 indicator; only the COPY expander sees the operands);
        //    the severity policy mirrors EditionContext there, and the metadata stays registry-canonical. ──
        new("fixed-form-word-continuation-removed-2023", "continuation of a COBOL word in fixed-form reference format", 85, 2023, null, EditionCodes.RemovedConstruct, "Annex E.2 item 1 bullet 2; VCR row 2 — emit site: ReferenceFormatProcessor.EditionGates (frontend)"),
        new("copy-replacing-non-pseudo-text-removed-2023", "a non-pseudo-text COPY REPLACING operand (identifier/literal/word)", 85, 2023, null, EditionCodes.RemovedConstruct, "Annex E.2 item 1 bullet 4; VCR row 4 — emit site: CopyProcessor.OnNonPseudoTextOperand (frontend)"),
        new("col7-continuation-obsolete-2023", "the fixed continuation indicator (hyphen in column 7)", 85, null, 2023, EditionCodes.ObsoleteFlag, "Annex F.2 item 4; §4.2.13; VCR row 94 — emit site: ReferenceFormatProcessor.EditionGates (frontend); warning only"),
        // ── The W3 notInGrammar 85-acceptance batch (VCR Table 7 rows 7.15–7.18; DEVLOG 599): obsolete '85
        //    elements deleted by ISO 2002, formerly absent from the grammar entirely (generic parse errors at
        //    every edition). All four cite the §8.9 ABSENCE pinpoints — no 2023 removal note exists (Annex E
        //    covers only 2014→2023). ──
        new("rerun-removed-2002", "the RERUN clause (I-O-CONTROL)", 85, 2002, null, EditionCodes.RemovedConstruct, "obsolete '85 checkpoint hint deleted by ISO 2002 (absent from the whole 2023 text; §8.9 absence @10661–10662); parsed-and-ignored at 85; VCR Table 7 row 7.15"),
        new("enter-removed-2002", "the ENTER statement", 85, 2002, null, EditionCodes.RemovedConstruct, "obsolete '85 other-language entry deleted by ISO 2002 (§8.9 absence @10459–10460); comment-equivalent (BoundNop) at 85; VCR Table 7 row 7.16"),
        new("use-for-debugging-removed-2002", "the USE FOR DEBUGGING declarative", 85, 2002, null, EditionCodes.RemovedConstruct, "the '85 debug facility's declarative, deleted by ISO 2002 with the whole facility incl. DEBUG-* registers (§8.9 absence @10407–10408); inert at 85 (comment-treated without WITH DEBUGGING MODE, never-triggered with it); companion of debugging-mode-removed-2002; VCR Table 7 row 7.17"),
        new("segment-numbers-removed-2002", "a section-header segment-number", 85, 2002, null, EditionCodes.RemovedConstruct, "the '85 Segmentation module's section priority number, deleted by ISO 2002 ('segment' is absent from the 2023 text; §8.9 absence @10681–10682); parsed-and-ignored at 85 (all segments resident); companion of segment-limit-removed-2002; VCR Table 7 row 7.18"),
        // ── Phase 4d: the file-sharing / record-locking subsystem (M2-FILE-1; DEVLOG 623) — five introduction
        //    rows + one representative reserved-word row (SHARING/RETRY/UNLOCK are §8.9 reserved-since-2002; the
        //    six §8.10 context-sensitive words MANUAL/AUTOMATIC/IGNORING/FOREVER/SECONDS/ONLY get no table row). ──
        new("file-sharing-clause-2002", "the SHARING clause / OPEN SHARING phrase", 2002, null, null, EditionCodes.Introduction, "ISO §12.4.5.15 / §14.9.27; LIVE (Phase 4d M2-FILE-1) — the physical-file connector registry (Table-19 → status 61); 2002 introduction (derive from the 2002 standard — Annex E covers 2014→2023 only)"),
        new("lock-mode-clause-2002", "the LOCK MODE clause", 2002, null, null, EditionCodes.Introduction, "ISO §12.4.5.9 (LOCK MODE IS MANUAL/AUTOMATIC [WITH LOCK ON [MULTIPLE] RECORD(S)]); LIVE (Phase 4d) — MANUAL lock-on-explicit vs AUTOMATIC lock-on-READ; 2002 introduction"),
        new("retry-phrase-2002", "the RETRY phrase", 2002, null, null, EditionCodes.Introduction, "ISO §14.7.9 (OPEN/READ/WRITE/REWRITE/DELETE; RETRY n TIMES | FOR n SECONDS | FOREVER); LIVE (Phase 4d) — n-times loops the registry check; SECONDS/FOREVER deadlock-bail to 52 (single-run-unit, GR4a); 2002 introduction"),
        new("unlock-statement-2002", "the UNLOCK statement", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.47 (UNLOCK file [RECORD[S]]); LIVE (Phase 4d) — releases all this-connector record locks; 2002 introduction"),
        new("record-lock-phrase-2002", "a record-lock phrase (WITH LOCK / WITH NO LOCK / IGNORING LOCK / ADVANCING ON LOCK)", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.30 / §14.9.51 / §14.9.35; LIVE (Phase 4d) — explicit MANUAL locking + IGNORING-LOCK bypass; 2002 introduction"),
        new("user-word-sharing-2002", "the word SHARING as a user-defined word", 85, 2002, null, EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable at 85, reserved since 2002 (ReservedWords.Table covers SHARING/RETRY/UNLOCK; single representative — the user-word-raising-2002 precedent)"),
    ];

    private static Dictionary<string, ConstructDialectStatus>? _byId;

    /// <summary>Look up a registry entry by its constructs.json row id.</summary>
    public static ConstructDialectStatus? Find(string id) =>
        (_byId ??= Entries.ToDictionary(e => e.Id, StringComparer.Ordinal)).GetValueOrDefault(id);

    /// <summary>
    /// THE gating entry point: evaluate <paramref name="id"/> at the context's edition and route the verdict
    /// onto the channels — NotYetIntroduced ⇒ error (both axes, 0900 band); Removed ⇒
    /// <see cref="EditionContext.Removed"/> (strict error / permissive warning); Obsolete ⇒ 0903 warning.
    /// <paramref name="where"/> localizes the diagnostic ("FD OUT-FILE", "paragraph P1", …).
    /// </summary>
    public static void Check(EditionContext edition, string id, string where)
    {
        var c = Find(id) ?? throw new ArgumentException($"unregistered construct id '{id}'", nameof(id));
        switch (c.StatusAt(edition.DialectLevel))
        {
            case ConstructAvailability.NotYetIntroduced:
                // Dual-obligation rows (an availability WINDOW: DiagnosticCode names the removal edge) use the
                // 0900 band for the introduction edge; single-edge rows keep their pinned code (pic-wide's 0802).
                edition.Error(c.RemovedIn is null ? c.DiagnosticCode : EditionCodes.Introduction,
                    $"{c.Display} requires COBOL-{c.IntroducedIn} (targeting COBOL-{edition.DialectLevel}) — {where} ({c.Citation})");
                break;
            case ConstructAvailability.Removed:
                edition.Removed(c.DiagnosticCode,
                    $"{c.Display} was removed in COBOL-{c.RemovedIn} (targeting COBOL-{edition.DialectLevel}) — {where} ({c.Citation})");
                break;
            case ConstructAvailability.Obsolete:
                edition.Warning(EditionCodes.ObsoleteFlag,
                    $"{c.Display} is obsolete as of COBOL-{c.ObsoleteIn} — {where} ({c.Citation})");
                break;
        }
    }
}
