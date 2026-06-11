# Version Change Reference — Edition-to-Edition Changes of Standard COBOL

> **STATUS BANNER — LIVE reference (type: LEDGER / REFERENCE).**
>
> **Purpose.** This document is the **version-gating checklist** for the COBOL.NET compiler (`cobol.exe`, the greenfield `src/Cobol.Net.*` — the legacy `CobolSharp.*` oracle is reference-only). It
> catalogues every edition-to-edition change of standard COBOL documented in the ISO/IEC 1989:2023 spec
> (`specs/ISO_COBOL.md`) so that the compiler can drive **correct version-gating**:
> - every **behavior** that changed across editions must be gated by the targeted standard via
>   `DialectLevel` / `--std` (cite [[feedback_version_targeted_semantics]]);
> - **new features** are enabled only at `>=` their introducing edition;
> - **obsolete / archaic** elements are flagged (and, where the spec schedules removal, gated off in newer dialects).
>
> **Scope limit (read before gating).** The 2023 spec documents the **2014→2023 delta completely** (Annex E for
> substantive changes, Annex F for archaic/obsolete lists, plus inline per-section NOTES). The **85→2002** and
> **2002→2014** deltas are only **partially** documented in the 2023 spec — they surface here mainly through the
> **FLAG-02** directive (§7.3.14, which points at the 2002↔2014 incompatibilities) and a few inline references; their
> **full** change lists live in the ISO/IEC 1989:2002 and ISO/IEC 1989:2014 standards themselves. Rows whose
> `Edition delta` is **2002→2014** (the FLAG-02 GR4 rows) are therefore marked:
> **"delta under-documented in the 2023 spec; confirm against the older standard before gating."**
> There is **no 85→2002 row set here at all** — when gating an 85↔2002 difference, derive it from the 2002 standard, not from this file. **Gap-closure plan (the four-compilers-in-one mission needs the full deltas):** for *new-feature introduction* gating at the 85/2002 and 2002/2014 boundaries, use the post-85 feature catalog in `docs/ISO2023_CONFORMANCE_PLAN.md` (M2/M3/M4) plus the version test matrix's `introducedIn` tags (`docs/VERSION_TEST_MATRIX_DESIGN.md`); for *behavior changes* across those boundaries, extending THIS ledger with 85→2002 and 2002→2014 row sets sourced from the ISO/IEC 1989:2002 and 1989:2014 standards is planned follow-on work — add the rows here (one canonical ledger, never a fork) as each delta is researched.

---

## How to use

When implementing or auditing a feature:

1. **Find its row(s) here** by feature name / § / category.
2. **If `Old → New behavior` shows a behavior change**, gate it by `DialectLevel` (cite
   [[feedback_version_targeted_semantics]]): emit the older behavior when `--std` targets the older edition and
   the newer behavior at `>=` the introducing edition. Do **not** hard-code a single behavior across all dialects.
3. **New features** (`category: new-feature` / `new-reserved-word` / most `syntax-change`): enable / reserve only at
   `>=` the introducing edition. A word newly reserved in 2023 must still be usable as a user-defined word when the
   target is an older dialect. **Co-equal G1 obligation:** in every edition that LACKS the feature, the compiler must
   emit a **specific diagnostic** naming the construct and the edition that introduces it (e.g. "DELETE FILE requires
   --std 2023 or later") — never a generic parse error, never a silent mis-parse. The wrong-edition diagnostic ships
   in the SAME change set as the feature itself.
4. **Obsolete / archaic** (`category: obsolete` / `archaic`): flag in the dialect where the spec designates it
   archaic/obsolete; where the spec documents removal by a later edition, **reject the element at `>=` that edition
   with a specific diagnostic** naming the construct and the edition that removed it, per the row's note — accepting a
   removed construct silently, or rejecting it with a generic parse error, fails G1's co-equal diagnostic obligation.
5. **Pin-to-spec ONLY where a difference is a version-INVARIANT legacy bug** — i.e., the legacy oracle is wrong in
   *every* edition, so the spec behavior is correct for all dialects and no gating is needed. **Record that
   determination** (DEVLOG + flip Status here to `done (pin-to-spec)`). Do not pin-to-spec to dodge a genuine
   edition-dependent change; that must be gated.

**Status legend.** `TODO` = not yet gated/implemented for version-correctness (fresh checklist default).
`done (pin-to-spec)` = investigated and determined a **version-invariant legacy bug**, pinned to the spec behavior for
all dialects.

> **Already-investigated (pin-to-spec) determinations.** Three legacy-vs-spec differences were investigated and found
> to be **version-INVARIANT legacy bugs** (the legacy oracle was non-conformant in every edition), so they are pinned
> to the spec for **all** dialects rather than gated (implemented DEVLOG 509 / 516; version-invariance investigated and
> recorded DEVLOG 517 — a 3-case investigate→adversarial-verify workflow cross-checking Annex E + the NIST-85 golden
> coverage). They appear in the inline-NOTE table below
> with Status = `done (pin-to-spec)`:
> - **DISPLAY trailing-trim** — §14.9.11.4
> - **signed-vs-alphanumeric comparison de-sign** — §8.8.4.2.5
> - **signed → group de-sign** — §14.9.25.4 GR6a
>
> These three are not edition-change rows from the JSON catalogue; they are recorded here because this file is the
> ledger of "where we deliberately did NOT gate, and why."

---

## Table 1 — 2014 → 2023, Annex E.2 (substantive changes potentially affecting existing programs)

| # | Change (title) | § | Edition delta | Old → New behavior | Affects existing? | Compiler gating action | Status |
|---|---|---|---|---|---|---|---|
| 1 | Item 1a — Removal: MOVE of alphanumeric figurative constants to numeric/numeric-edited items | MOVE statement; figurative constants (E.2 item 1, bullet 1) | 2014→2023 | **Old:** moving alphanumeric figurative constants (SPACE, HIGH-VALUE, LOW-VALUE, QUOTE) to numeric / numeric-edited items was permitted (not even flagged obsolete in 2014). **New:** removed from the standard. Exception still permitted: ALL "literal" of digits only, or ALL symbolic-character representing a digit, → integer numeric items. Implementors may keep the rest as an unsupported extension. | Yes | gate-behavior-by-dialect (permit pre-2023; reject at 2023, keep the digit-only ALL exception) | TODO |
| 2 | Item 1b — Removal: Continuation of COBOL words in fixed-form reference format | Reference format / fixed form (E.2 item 1, bullet 2) | 2014→2023 | **Old:** a COBOL word could be continued across lines in fixed-form. **New:** removed (little used / error-prone); implementor extension only. | Yes | gate-behavior-by-dialect | TODO |
| 3 | Item 1c — Removal: ON OVERFLOW phrase of the CALL statement | CALL … ON OVERFLOW (E.2 item 1, bullet 3) | 2014→2023 | **Old:** CALL supported ON OVERFLOW. **New:** removed (ON EXCEPTION gives the same result); extension only. | Yes | gate-behavior-by-dialect (accept ON OVERFLOW pre-2023) | TODO |
| 4 | Item 1d — Removal: non-pseudo-text operands in the REPLACING phrase of COPY | COPY … REPLACING (E.2 item 1, bullet 4) | 2014→2023 | **Old:** COPY … REPLACING accepted non-pseudo-text operands (identifiers, literals, words). **New:** removed; extension only. | Yes | gate-behavior-by-dialect | TODO |
| 5 | Item 1e — Removal: EXIT METHOD statement | EXIT METHOD (E.2 item 1, bullet 5) | 2014→2023 | **Old:** EXIT METHOD was valid (return from a method). **New:** removed; extension only. | Yes | gate-behavior-by-dialect | TODO |
| 6 | Item 1f — Removal: EXIT FUNCTION statement | EXIT FUNCTION (E.2 item 1, bullet 6) | 2014→2023 | **Old:** EXIT FUNCTION was valid (return from a UDF). **New:** removed; extension only. | Yes | gate-behavior-by-dialect | TODO |
| 7 | Item 1g — Removal: WITH LOCK phrase of CLOSE and the related File Status 38 | CLOSE … WITH LOCK; File Status 38 (E.2 item 1, bullet 7) | 2014→2023 | **Old:** CLOSE … WITH LOCK permitted; OPEN of a file closed WITH LOCK → File Status 38. **New:** both removed; extension only. | Yes | gate-behavior-by-dialect | TODO |
| 8 | Item 2 — ALIGN clause added to consistency rules; strong-typing now includes bit-item alignment | ALIGN clause; typed-item consistency (E.2 item 2) | 2014→2023 | **Old:** ALIGN not in the typed-item consistency lists; strong-typing required byte-boundary alignment but not bit-position alignment of corresponding bit items. **New:** ALIGN added to the consistency lists; strong-typing now also requires bit-position alignment of corresponding bit items. | Yes | gate-behavior-by-dialect | TODO |
| 9 | Item 3 — Boolean shifting operators B-SHIFT-L / B-SHIFT-R / B-SHIFT-LC / B-SHIFT-RC | Boolean operators (E.2 item 3) | 2014→2023 | **Old:** no standard boolean left/right shift operators. **New:** adds B-SHIFT-L, B-SHIFT-R (logical) and B-SHIFT-LC, B-SHIFT-RC (circular) over boolean digits of an alphanumeric/national item (also new reserved words — item 25). | Yes | new-feature-gate (enable at ≥2023) | TODO |
| 10 | Item 4 — Characters permitted in user-defined words changed | User-defined words / allowed chars (ISO/IEC 10646) (E.2 item 4) | 2014→2023 | **Old:** 037A GREEK YPOGEGRAMMENI allowed; 30FB KATAKANA MIDDLE DOT allowed as first/last char. **New:** 037A deleted entirely; 30FB no longer allowed as start/last char (medial only). | Yes | gate-behavior-by-dialect | TODO |
| 11 | Item 5 — New compiler-directive words added | Compiler directives (E.2 item 5) | 2014→2023 | **Old:** these words were compilation-variable names; the directives did not exist. **New:** nine directive words added — COBOL-WORDS, DISPLAY, FLAG-14, I-O-STATUS-04, NUM-ED-ZERO-FIG-CONSTANT, POP, PUSH, REF-MOD-ZERO-LENGTH, UPON. (Gate links: REF-MOD-ZERO-LENGTH↔item 23, NUM-ED-ZERO-FIG-CONSTANT↔item 28, I-O-STATUS-04↔item 15.) | Yes | new-feature-gate | TODO |
| 12 | Item 6 — Compile-time arithmetic expression mode now implementor-defined | Compile-time arithmetic; intermediate results (E.2 item 6) | 2014→2023 | **Old:** the previous standard required a specific arithmetic mode (the now-removed Standard Arithmetic). **New:** the mode and intermediate-result handling are explicitly implementor-defined. | Yes | gate-behavior-by-dialect | TODO |
| 13 | Item 7 — Leap-year determination: reference to obsolete ISO 8601 formula removed | Leap-year determination (ISO 8601-1:2019) (E.2 item 7) | 2014→2023 | **Old:** referenced ISO 8601:2004's leap-year formula. **New:** ISO 8601-1:2019 removed the formula from normative text; COBOL no longer cites the obsolete version. | No | none | TODO |
| 14 | Item 8 — EVALUATE compiler directive: combined-condition truth corrected | EVALUATE compiler directive (E.2 item 8) | 2014→2023 | **Old:** rules about omitting end-of-directive text (no WHEN true, no WHEN OTHER) could make the whole condition true incorrectly. **New:** the whole condition is true only when both constituent conditions are true. | Yes | gate-behavior-by-dialect | TODO |
| 15 | Item 9 — External items: exception conditions added for conformance checking | External items / conformance checking (E.2 item 9) | 2014→2023 | **Old:** conformance rules existed but no exception conditions; checking unspecified, left to implementor. **New:** exception conditions provided; effective only when enabled in BOTH invoked and invoking runtime elements; prior implementor-defined checking now ignored unless invoked via implementor-defined syntax. | Yes | gate-behavior-by-dialect | TODO |
| 16 | Item 10 — External items: CONSTANT RECORD now only for strongly typed external items | External items; CONSTANT RECORD (E.2 item 10) | 2014→2023 | **Old:** CONSTANT RECORD allowed on external items, but external items couldn't be strongly typed; weak checking let the "constant" record be changed by elements not specifying CONSTANT RECORD. **New:** CONSTANT RECORD allowed ONLY for strongly typed external items (external items can now be strongly typed). | Yes | gate-behavior-by-dialect | TODO |
| 17 | Item 11 — Figurative constant with ALL where data-item length is unspecified: length now defined | Figurative constants with ALL (unspecified length) (E.2 item 11) | 2014→2023 | **Old:** undefined results (likely a compiler error). **New:** the length is now defined → well-defined results. | Yes | gate-behavior-by-dialect | TODO |
| 18 | Item 12 — FILE STATUS and the EXTERNAL clause: consistent FILE STATUS item required | FILE STATUS; EXTERNAL; SELECT (E.2 item 12) | 2014→2023 | **Old:** an external file's FILE STATUS in one SELECT didn't force every corresponding SELECT to specify FILE STATUS (same external item). **New:** all corresponding SELECTs must specify FILE STATUS with the same corresponding external data item. | Yes | gate-behavior-by-dialect | TODO |
| 19 | Item 13 — FUNCTION ALL INTRINSIC: new intrinsic functions prohibited as user-defined words | REPOSITORY; FUNCTION ALL INTRINSIC (E.2 item 13) | 2014→2023 | **Old:** these seven names could be user-defined words in a FUNCTION ALL INTRINSIC scope; the functions didn't exist. **New:** adds BASECONVERT, CONCAT, CONVERT, FIND-STRING, MODULE-NAME, SMALLEST-ALGEBRAIC, SUBSTITUTE; under FUNCTION ALL INTRINSIC they are prohibited as user-defined words in that REPOSITORY scope. | Yes | new-feature-gate | TODO |
| 20 | Item 14 — General case mappings deleted | General case mappings (ISO/IEC 10646) (E.2 item 14) | 2014→2023 | **Old:** mappings (0131,0069) and (03C2,03C3) treated DOTLESS I and GREEK FINAL SIGMA as having uppercase mappings (error — both are lowercase). **New:** both mappings deleted; affects UPPER-CASE / LOWER-CASE for those chars. | Yes | gate-behavior-by-dialect | TODO |
| 21 | Item 15 — I-O Status '04' setting clarified | I-O status '04' (E.2 item 15) | 2014→2023 | **Old:** the setting of '04' was not clearly defined (a known error). **New:** clarified when '04' is set (gateable via the I-O-STATUS-04 directive, item 5). | Yes | gate-behavior-by-dialect | TODO |
| 22 | Item 16 — I-O Status '07' now restricted to OPEN and CLOSE | I-O status '07'; OPEN/CLOSE (E.2 item 16) | 2014→2023 | **Old:** '07' could be set by I-O statements other than OPEN/CLOSE. **New:** '07' restricted to OPEN and CLOSE. | Yes | gate-behavior-by-dialect | TODO |
| 23 | Item 17 — I-O status '0x': case equivalence of letters now implementor-dependent | I-O status '0x' (E.2 item 17) | 2014→2023 | **Old:** upper/lower-case equivalence undefined in this context. **New:** implementor-dependent (affects portability). | Yes | gate-behavior-by-dialect | TODO |
| 24 | Item 18 — I-O Status '37' may be returned for insufficient authority on OPEN | I-O status '37'; OPEN (E.2 item 18) | 2014→2023 | **Old:** no standard status for OPEN with insufficient authority. **New:** OPEN may return '37' for insufficient authority. | Yes | gate-behavior-by-dialect | TODO |
| 25 | Item 19a — INVALID KEY processing: declarative now executed when phrase absent | INVALID KEY; USE declaratives (E.2 item 19a) | 2014→2023 | **Old:** no INVALID KEY phrase + invalid-key condition → INPUT/OUTPUT/I-O/EXTEND declarative NOT executed (apparent error). **New:** such a declarative is now executed. | Yes | gate-behavior-by-dialect | TODO |
| 26 | Item 19b — READ processing: declarative now executed for non-invalid-key/non-at-end exceptions | READ; USE declaratives (E.2 item 19b) | 2014→2023 | **Old:** READ exception that is not invalid-key/at-end → INPUT/I-O declarative NOT executed (apparent error). **New:** such a declarative is now executed (part of the inline-exception-checking PERFORM enhancement). | Yes | gate-behavior-by-dialect | TODO |
| 27 | Item 20 — MERGE statement restriction in output/SORT procedures | MERGE; output procedure; file-format SORT (E.2 item 20) | 2014→2023 | **Old:** a MERGE could appear in another MERGE's output procedure or a file-format SORT's input/output procedure; rules conflicted (exception or undefined). **New:** MERGE prohibited in another MERGE's output procedure or in a file-format SORT input/output procedure. | Yes | gate-behavior-by-dialect | TODO |
| 28 | Item 21 — Obsolete elements removed | FLAG-85, FLAG-NATIVE-ARITHMETIC, Standard Arithmetic, MOVE QUOTE→numeric (E.2 item 21) | 2014→2023 | **Old:** all four were obsolete-but-present in 2014. **New:** all four removed; implementors may provide corresponding extensions. | Yes | flag-obsolete (reject at 2023; permit pre-2023) | TODO |
| 29 | Item 22 — READ PREVIOUS immediately after OPEN now raises at-end | READ … PREVIOUS following OPEN (E.2 item 22) | 2014→2023 | **Old:** conflicting rule/note: rule said the first record is retrieved; note said at-end normally exists. **New:** at-end condition occurs for READ PREVIOUS following OPEN. | Yes | gate-behavior-by-dialect | TODO |
| 30 | Item 23 — Reference-modification zero-length result now controlled / raises EC-BOUND-REF-MOD | Ref-mod; REF-MOD-ZERO-LENGTH; EC-BOUND-REF-MOD (E.2 item 23) | 2014→2023 | **Old:** zero-length ref-mod result undefined. **New:** zero-length ref-mod allowed only with REF-MOD-ZERO-LENGTH in effect; otherwise EC-BOUND-REF-MOD raised. (Directive from item 5.) | Yes | gate-behavior-by-dialect | TODO |
| 31 | Item 24 — Relative keys for an external file must be the same corresponding external data item | Relative key; external file (E.2 item 24) | 2014→2023 | **Old:** not explicitly required that the relative key be the same corresponding external item across runtime elements. **New:** required to be the same corresponding external data item in all runtime elements. | Yes | gate-behavior-by-dialect | TODO |
| 32 | Item 25 — New reserved words added | Reserved words (E.2 item 25) | 2014→2023 | **Old:** these 16 words were user-defined words. **New:** reserved: B-SHIFT-L, B-SHIFT-LC, B-SHIFT-R, B-SHIFT-RC, COMMIT, EDITING, END-RECEIVE, END-SEND, EXCLUSIVE-OR, FINALLY, LOCATION, MESSAGE-TAG, RECEIVE, ROLLBACK, SEND, XOR. (B-SHIFT-* ↔ item 3.) | Yes | flag-new-reserved-word (reserve at ≥2023) | TODO |
| 33 | Item 26 — Transfer of control: checking now includes sections as well as paragraphs | Transfer of control; sections/paragraphs (E.2 item 26) | 2014→2023 | **Old:** checking of explicit/implicit transfers was unclear and did not properly include sections (only paragraphs). **New:** now includes sections as well as paragraphs. | Yes | gate-behavior-by-dialect | TODO |
| 34 | Item 27 — VALUE clause literal categories checked for numeric-edited items | VALUE clause; numeric-edited; PIC/USAGE conformance (E.2 item 27) | 2014→2023 | **Old:** unclear what value was used for an alphanumeric/national literal VALUE on a numeric-edited item; no conformance check. **New:** such literals are checked against PICTURE/USAGE. | Yes | gate-behavior-by-dialect | TODO |
| 35 | Item 28 — VALUE clause figurative constant ZERO for numeric-edited items treated as numeric zero | VALUE; ZERO/ZEROES; numeric-edited; NUM-ED-ZERO-FIG-CONSTANT (E.2 item 28) | 2014→2023 | **Old:** ZERO/ZEROES (±ALL) as VALUE could be left-justified or a plain zero-string. **New:** treated as numeric literal zero → edited per PICTURE. (↔ NUM-ED-ZERO-FIG-CONSTANT, item 5.) | Yes | gate-behavior-by-dialect | TODO |
| 36 | Item 29 — VALUE clause editing symbols required/auto-supplied for numeric-edited items | VALUE; editing symbols; numeric-edited (E.2 item 29) | 2014→2023 | **Old:** editing symbols not required in the VALUE for a numeric-edited item (omission). **New:** required when the value is an alphanumeric/national literal; auto-supplied when a numeric literal. | Yes | gate-behavior-by-dialect | TODO |
| 37 | Item 30 — WRITE END-OF-PAGE condition with no END-OF-PAGE phrase: control passes to end of WRITE | WRITE; END-OF-PAGE (E.2 item 30) | 2014→2023 | **Old:** behavior unspecified when END-OF-PAGE condition occurs but no END-OF-PAGE phrase. **New:** control passes to the end of the WRITE statement. | Yes | gate-behavior-by-dialect | TODO |

*Note on Table 1:* `Affects existing? = No` rows (13) are spec clarifications with no observable behavior change; still recorded for traceability.

---

## Table 2 — 2014 → 2023, Annex E.3.2 (substantive changes probably NOT affecting existing programs — possibly via new words/names)

| # | Change (title) | § | Edition delta | Old → New behavior | Affects existing? | Compiler gating action | Status |
|---|---|---|---|---|---|---|---|
| 38 | Asynchronous messaging facility | E.3.2 item 1 (introduces EC-MCS-* + reserved/context words) | 2014→2023 | **Old:** no standard inter-run-unit messaging. **New:** communication between run units via messages (same or different processors, not necessarily co-located). | Yes | new-feature-gate | TODO |
| 39 | Commit and rollback facility | E.3.2 item 2 (EC-FLOW-APPLY-COMMIT / EC-FLOW-COMMIT / EC-FLOW-ROLLBACK) | 2014→2023 | **Old:** no standard commit/rollback over file changes. **New:** commit file changes at specified stages; rollback to previous commit / run-unit start; specified data items saved by a commit for rollback restore. | Yes | new-feature-gate | TODO |
| 40 | New exception conditions (EC-MCS-*, EC-FLOW-*, EC-CONTINUE-*, EC-EXTERNAL-*, EC-I-O-WARNING, EC-IO-RECORD-CONTENT) | E.3.2 item 3 | 2014→2023 | **Old:** these EC-* names did not exist (words available as user-defined names). **New:** reserved EC-* names added: EC-MCS, EC-MCS-ABNORMAL-TERMINATION, EC-MCS-IMP, EC-MCS-INVALID-TAG, EC-MCS-MESSAGE-LENGTH, EC-MCS-NO-REQUESTOR, EC-MCS-NO-SERVER, EC-MCS-NORMAL-TERMINATION, EC-MCS-REQUESTOR-FAILED; EC-FLOW-APPLY-COMMIT, EC-FLOW-COMMIT, EC-FLOW-ROLLBACK; EC-CONTINUE, EC-CONTINUE-IMP, EC-CONTINUE-LESS-THAN-ZERO; EC-EXTERNAL, EC-EXTERNAL-DATA-MISMATCH, EC-EXTERNAL-FILE-MISMATCH, EC-EXTERNAL-FORMAT-CONFLICT, EC-EXTERNAL-IMP; EC-I-O-WARNING; EC-IO-RECORD-CONTENT. | Yes | flag-new-reserved-word | TODO |
| 41 | Logical operators EXCLUSIVE-OR / XOR | E.3.2 item 4 | 2014→2023 | **Old:** logical operators limited to AND/OR/NOT; EXCLUSIVE-OR and XOR were user-defined words. **New:** EXCLUSIVE-OR and XOR added (reserved). | Yes | flag-new-reserved-word | TODO |
| 42 | NO SIGN phrase of the USAGE clause (PACKED-DECIMAL with no sign) | E.3.2 item 5 | 2014→2023 | **Old:** a PACKED-DECIMAL item always stored a sign value. **New:** USAGE enhanced to allow no sign (NO SIGN phrase). | Yes | new-feature-gate | TODO |
| 43 | SYNCHRONIZED clause permitted on a group item | E.3.2 item 6 | 2014→2023 | **Old:** SYNCHRONIZED only on elementary items. **New:** allowed on a group item (as if specified for each permitted contained elementary item). | Yes | new-feature-gate | TODO |

---

## Table 3 — 2014 → 2023, Annex E.3.3 (substantive changes NOT affecting existing programs)

| # | Change (title) | § | Edition delta | Old → New behavior | Affects existing? | Compiler gating action | Status |
|---|---|---|---|---|---|---|---|
| 44 | NUMVAL-C ANYCASE keyword clarified | E.3.3 item 1 | 2014→2023 | **Old:** ANYCASE of NUMVAL-C specified inconsistently. **New:** clarified to be consistent. | No | none | TODO |
| 45 | BEFORE and AFTER both allowed in WRITE ADVANCING | E.3.3 item 2 | 2014→2023 | **Old:** BEFORE and AFTER could not both be specified. **New:** both allowed together. | No | new-feature-gate | TODO |
| 46 | Binary operators B-SHIFT-L / B-SHIFT-LC / B-SHIFT-R / B-SHIFT-RC | E.3.3 item 3 | 2014→2023 | **Old:** no standard binary bit-shift operators; words not reserved. **New:** B-SHIFT-L, B-SHIFT-LC, B-SHIFT-R, B-SHIFT-RC added. | No | flag-new-reserved-word | TODO |
| 47 | Characters CHANGED to be permitted as the first character of a user-defined word (Unicode reclassification) | E.3.3 item 4 | 2014→2023 | **Old:** listed code points not allowed as first char. **New:** now allowed as first char across scripts — Armenian (0559); Common (00B5, 02BB-02C1, 02D0-02D1, 02EE, 2102, 2107, 210A-2113, 2115, 2119-211D, 2124, 2128, 212C-212D, 212F-2131, 2133-2139, 3006); Greek (1FBE, 2126); Han (3005, 3007, 3021-3029, 3038-303A); Latin (02B0-02B8, 02E0-02E4, 212A-212B, 2160-2183); Tamil (0B83). | No | new-feature-gate | TODO |
| 48 | Characters ADDED as permitted in user-defined words (large Unicode block additions) | E.3.3 item 5 | 2014→2023 | **Old:** listed code points not permitted. **New:** a large set of Unicode ranges across ~120 scripts (Adlam … Zanabazar_Square) added. (Scope-level entry; full per-script tables not enumerated — see specLines.) | No | new-feature-gate | TODO |
| 49 | General case mappings added (Unicode upper/lower-case pairs) | E.3.3 item 6 | 2014→2023 | **Old:** listed pairs not defined as case-equivalent. **New:** large table of additional case mappings added; affects case-insensitive matching/conversion. (Scope-level entry; full table not enumerated.) | No | new-feature-gate | TODO |
| 50 | Clarification of exception handling procedures | E.3.3 item 7 | 2014→2023 | **Old:** inconsistencies existed. **New:** resolved (clarification only). | No | none | TODO |
| 51 | Clarification that GLOBAL clause rules do not contradict EXTERNAL clause rules | E.3.3 item 8 | 2014→2023 | **Old:** apparent contradiction GLOBAL vs EXTERNAL. **New:** clarified they do not contradict. | No | none | TODO |
| 52 | Clarification that real zeroes are permitted when checking floating-point underflow | E.3.3 item 9 (ISO/IEC 60559:2020) | 2014→2023 | **Old:** unclear whether real zeroes were permitted values when checking underflow. **New:** clarified real zeroes are permitted per ISO/IEC 60559:2020. | No | none | TODO |
| 53 | Clarification of size-error rules vs ROUNDED MODE IS PROHIBITED | §14.7.5 (E.3.3 item 10) | 2014→2023 | **Old:** §14.7.5 partially self-contradicting on when rounding raises EC-SIZE-TRUNCATION. **New:** clarified — rounding raises EC-SIZE-TRUNCATION only when DEFAULT ROUNDED MODE IS PROHIBITED / ROUNDED MODE IS PROHIBITED is in effect. | No | none | TODO |
| 54 | COBOL words may be 63 characters long | E.3.3 item 11 | 2014→2023 | **Old:** shorter max length (30 chars in prior editions). **New:** up to 63 chars. | No | new-feature-gate | TODO |
| 55 | COBOL-WORDS directive | E.3.3 item 12 | 2014→2023 | **Old:** new directive. **New:** COBOL-WORDS may modify reserved/context-sensitive/function-name lists and prohibit specific user-defined words. | No | new-feature-gate | TODO |
| 56 | Context-sensitive words added/expanded | E.3.3 item 13 | 2014→2023 | **Old:** these words not context-sensitive (or reserved in fewer contexts). **New:** added/expanded: ACTIVATING, ANUM, APPLY, BACKWARD, BYTE, BYTES, CURRENT, HEX, NAT, SECONDS, STACK, TOP-LEVEL. | No | flag-new-reserved-word | TODO |
| 57 | CONTINUE statement enhanced to pause execution for a specified time | E.3.3 item 14 | 2014→2023 | **Old:** CONTINUE was a no-op placeholder. **New:** can pause runtime execution for a specified period. | No | new-feature-gate | TODO |
| 58 | DELETE FILE statement | E.3.3 item 15 | 2014→2023 | **Old:** no standard way to remove files. **New:** DELETE FILE removes referenced files from mass storage. | No | new-feature-gate | TODO |
| 59 | DISPLAY directive | E.3.3 item 16 | 2014→2023 | **Old:** new directive. **New:** DISPLAY directive shows compile-time information during compilation. | No | new-feature-gate | TODO |
| 60 | Dynamic-length elementary items — SET to set length | E.3.3 item 17 | 2014→2023 | **Old:** no way to set the length of a dynamic-length elementary item. **New:** SET enhanced to set its length. | No | new-feature-gate | TODO |
| 61 | EC-I-O-WARNING exception condition | E.3.3 item 18 | 2014→2023 | **Old:** no EC for nonzero successful I-O status. **New:** EC-I-O-WARNING enables detection of nonzero successful I-O status by declaratives / PERFORM WHEN phrases (also E.3.2 item 3). | No | flag-new-reserved-word | TODO |
| 62 | EDITING phrase of the PICTURE clause — literal of any size | E.3.3 item 19 | 2014→2023 | **Old:** simple/sign-sensitive fixed insertion couldn't specify an arbitrary-size literal. **New:** EDITING phrase adds a literal of any size for simple and sign-sensitive fixed insertion. | No | new-feature-gate | TODO |
| 63 | EXTERNAL data items may be strongly typed | E.3.3 item 20 | 2014→2023 | **Old:** EXTERNAL items couldn't be strongly typed. **New:** they may now be strongly typed. | No | new-feature-gate | TODO |
| 64 | FLAG-14 directive | E.3.3 item 21 | 2014→2023 | **Old:** new directive. **New:** FLAG-14 flags elements possibly incompatible between the previous (2014) and this (2023) standard. | No | new-feature-gate | TODO |
| 65 | FUNCTION BASECONVERT | E.3.3 item 22 | 2014→2023 | **Old:** new intrinsic. **New:** BASECONVERT converts between number bases 2–16. | No | new-feature-gate | TODO |
| 66 | FUNCTION CONCAT | E.3.3 item 23 | 2014→2023 | **Old:** new intrinsic. **New:** CONCAT concatenates data items like the literal concatenation operator. | No | new-feature-gate | TODO |
| 67 | FUNCTION CONVERT | E.3.3 item 24 | 2014→2023 | **Old:** new intrinsic. **New:** CONVERT converts between data representations (alphanumeric/national, natural/hex, most data-item types in hex). | No | new-feature-gate | TODO |
| 68 | FUNCTION EXCEPTION-FILE — optional file-connector argument | E.3.3 item 25 | 2014→2023 | **Old:** no argument; reported on the last-referenced file connector only. **New:** optional argument to specify the file connector; unchanged when omitted. | No | new-feature-gate | TODO |
| 69 | FUNCTION EXCEPTION-FILE-N — optional file-connector argument | E.3.3 item 26 | 2014→2023 | **Old:** no argument; last-referenced connector only. **New:** optional argument to specify the file connector; unchanged when omitted. | No | new-feature-gate | TODO |
| 70 | FUNCTION FIND-STRING | E.3.3 item 27 | 2014→2023 | **Old:** new intrinsic. **New:** FIND-STRING locates the position of one string within another. | No | new-feature-gate | TODO |
| 71 | FUNCTION MODULE-NAME | E.3.3 item 28 | 2014→2023 | **Old:** new intrinsic. **New:** MODULE-NAME reports modules in the running application's hierarchy. | No | new-feature-gate | TODO |
| 72 | FUNCTION SMALLEST-ALGEBRAIC | E.3.3 item 29 | 2014→2023 | **Old:** new intrinsic. **New:** SMALLEST-ALGEBRAIC gives the smallest number representable in any elementary numeric item. | No | new-feature-gate | TODO |
| 73 | FUNCTION SUBSTITUTE | E.3.3 item 30 | 2014→2023 | **Old:** new intrinsic. **New:** SUBSTITUTE replaces portions of strings with possibly different-length substitutions. | No | new-feature-gate | TODO |
| 74 | FUNCTION TRIM enhanced to remove characters other than space | E.3.3 item 31 | 2014→2023 | **Old:** TRIM removed only leading/trailing spaces. **New:** TRIM can remove characters other than space. | No | new-feature-gate | TODO |
| 75 | GOBACK statement allows status phrase like STOP (in a main program) | E.3.3 item 32 | 2014→2023 | **Old:** GOBACK had no status phrase. **New:** GOBACK allows the STOP status phrase, effective only in a COBOL main program. | No | new-feature-gate | TODO |
| 76 | INITIALIZE clause of the OPTIONS paragraph | E.3.3 item 33 | 2014→2023 | **Old:** content of non-explicitly-initialized items was implementor-defined. **New:** with OPTIONS INITIALIZE, that content is explicitly defined. | No | new-feature-gate | TODO |
| 77 | INSPECT statement — BACKWARD context-sensitive word added | E.3.3 item 34 | 2014→2023 | **Old:** INSPECT scanned forward only. **New:** BACKWARD context-sensitive word added (backward scan). | No | new-feature-gate | TODO |
| 78 | I-O status values '05','37','39','41','62' settable by DELETE FILE | E.3.3 item 35 | 2014→2023 | **Old:** these statuses not produced by DELETE FILE (which didn't exist). **New:** DELETE FILE may set '05','37','39','41','62'. | No | new-feature-gate | TODO |
| 79 | PERFORM statement — exception-checking variant | E.3.3 item 36 | 2014→2023 | **Old:** no exception-checking PERFORM. **New:** exception-checking variant added. | No | new-feature-gate | TODO |
| 80 | PERFORM … UNTIL EXIT (infinite loop) | E.3.3 item 37 | 2014→2023 | **Old:** no UNTIL EXIT phrase. **New:** PERFORM … UNTIL EXIT (infinite loop). | No | new-feature-gate | TODO |
| 81 | PUSH and POP directives | E.3.3 item 38 | 2014→2023 | **Old:** new directives. **New:** PUSH/POP save and restore the state of compiler directives. | No | new-feature-gate | TODO |
| 82 | RAISE statement — exception-processing clarified | E.3.3 item 39 | 2014→2023 | **Old:** RAISE exception processing specified locally. **New:** clarified to follow rules elsewhere. | No | none | TODO |
| 83 | Reserved Words — restriction on formation of new reserved words removed | E.3.3 item 40 | 2014→2023 | **Old:** formation restrictions (no leading 0–9 or X/Y/Z; no one/two-letter + hyphen or double hyphen; ≥2 basic letters except special-char words). **New:** no formation restriction (now only suggested); loosens future-proofing assumptions. | No | none | TODO |
| 84 | REWRITE statement — identifier-1 contents unavailable after execution (clarified) | E.3.3 item 41 | 2014→2023 | **Old:** unclear whether identifier-1 subordinate to the FD stayed available after REWRITE. **New:** clarified — not available after REWRITE. | No | none | TODO |
| 85 | SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause | E.3.3 item 42 | 2014→2023 | **Old:** no way to suppress alternate-key access by key value. **New:** SUPPRESS WHEN suppresses access via a particular alternate key when its value equals the specified value. | No | new-feature-gate | TODO |
| 86 | VALUE clause — numeric literals permitted for numeric-edited items | E.3.3 item 43 | 2014→2023 | **Old:** numeric-edited items couldn't take a numeric-literal VALUE. **New:** numeric-edited items may be assigned numeric-literal values. | No | new-feature-gate | TODO |
| 87 | WRITE statement — impossible identifier-1 subordination condition removed (clarified) | E.3.3 item 44 | 2014→2023 | **Old:** rules implied an impossible "both subordinate and not subordinate to the FD" condition. **New:** removed. | No | none | TODO |
| 88 | WRITE statement — identifier-1 contents unavailable after execution (clarified) | E.3.3 item 45 | 2014→2023 | **Old:** unclear whether identifier-1 subordinate to the FD stayed available after WRITE. **New:** clarified — not available after WRITE. | No | none | TODO |

---

## Table 4 — Annex F: Archaic (F.1) and Obsolete (F.2) language-element designations

| # | Change (title) | § | Edition delta | Old → New behavior | Affects existing? | Compiler gating action | Status |
|---|---|---|---|---|---|---|---|
| 89 | EXIT PROGRAM statement designated archaic | §F.1 item 1 (cf. §14.9.14 EXIT Program format NOTE @27372) | archaic-in-2023 (remains supported; superseded by GOBACK + MODULE-NAME intrinsic) | **Old:** ordinary statement — in a subprogram returns to caller (like GOBACK); in a main program acts like CONTINUE. **New:** archaic, discouraged; GOBACK + MODULE-NAME provide its features; no removal schedule but may cause future compiler errors. | Yes | flag-obsolete (flag as archaic) | TODO |
| 90 | NEXT SENTENCE phrase in IF and SEARCH designated archaic | §F.1 item 2 (IF §14.9.19; SEARCH §14.9.37; NOTES @27792, 30804, 30829) | archaic-in-2023 (remains supported; CONTINUE + scope delimiters preferred) | **Old:** transfers control to the statement after the next separator period. **New:** archaic, discouraged (confusing in delimited-scope statements, error-prone with stray periods); CONTINUE + scope delimiters are clearer; no removal schedule. | Yes | flag-obsolete (flag as archaic) | TODO |
| 91 | FLAG-02 compiler directive designated obsolete | §F.2 item 1 (defined §7.3.14.1, NOTE @4366) | obsolete-in-2023 (FLAG-02 flagged 2002↔2014; superseded by FLAG-14) | **Old:** FLAG-02 flagged 2002↔2014 incompatibilities. **New:** obsolete, scheduled for removal next edition (FLAG-14 flags 2014↔2023); a conforming 2023 impl must still support but should flag use. | Yes | flag-obsolete | TODO |
| 92 | MOVE of ALL "literal" / ALL symbolic-character (digits only) to integer numeric items designated obsolete | §F.2 item 2 (MOVE §14.9.25; GR5 + NOTE @28811-28813) | obsolete-in-2023 (the surviving digit-only ALL→integer MOVE now scheduled for removal) | **Old:** moving a digit-only ALL figurative constant to an integer numeric item was permitted. **New:** obsolete, to be removed next edition; use ZERO, HIGHEST-/LOWEST-ALGEBRAIC, or a numeric literal instead; 2023 impl must support but flag. | Yes | flag-obsolete | TODO |
| 93 | STANDARD-BINARY arithmetic and STANDARD BINARY Intermediate Data Item designated obsolete | §F.2 item 3 (ARITHMETIC STANDARD-BINARY §8.8.x; NOTES @9086, 9099, 13404, 45853, 46001, 40076) | obsolete-in-2023 (reevaluation deferred to next revision before removal) | **Old:** STANDARD-BINARY mode and SBIDI were specified arithmetic facilities. **New:** obsolete (never implemented; no interest); reevaluated next revision before any removal; impls claiming support must support but flag. | Yes | flag-obsolete | TODO |
| 94 | Fixed continuation indicator (hyphen in column 7) and literal continuation via it designated obsolete | §F.2 item 4 (fixed-form continuation §6.x; NOTES @2932, 3066) | obsolete-in-2023 (scheduled for removal) | **Old:** column-7 hyphen as fixed continuation indicator, incl. literal continuation. **New:** obsolete, to be removed next edition (error-prone; use the floating continuation indicator); 2023 impl must support but flag. | Yes | flag-obsolete | TODO |
| 95 | VALIDATE facility designated obsolete | §F.2 item 5 (VALIDATE statement + validation-format data description; many NOTES) | obsolete-in-2023 (reevaluation deferred to next revision before removal) | **Old:** VALIDATE statement + its validation clauses + EC-VALIDATE were a specified facility. **New:** obsolete (never implemented; no interest); reevaluated next revision before any removal; impls claiming support must support but flag. | Yes | flag-obsolete | TODO |

---

## Table 5 — FLAG-02 (2002↔2014) and FLAG-14 (2014↔2023) edition-incompatibility flagging directives (§7.3.14 / §7.3.15)

> **The FLAG-02 GR4 rows below are 2002→2014 — delta under-documented in the 2023 spec; confirm against the
> ISO/IEC 1989:2014 (and 1989:2002) standard before gating.** The FLAG-14 GR4 rows are 2014→2023 and are the
> per-construct flags corresponding to the Table 1 behavior changes.

| # | Change (title) | § | Edition delta | Old → New behavior | Affects existing? | Compiler gating action | Status |
|---|---|---|---|---|---|---|---|
| 96 | FLAG-02 directive itself is obsolete (to be deleted next edition) | §7.3.14.1 General (NOTE) | obsolete-in-2023 | **Old:** FLAG-02 was a live directive flagging 2002↔2014 incompatibilities. **New:** marked obsolete in 2023, to be deleted next edition; FLAG-14 is the live replacement. | No | flag-obsolete | TODO |
| 97 | FLAG-02 EC-PROGRAM-EXCEPTIONS — TURN of EC-PROGRAM exceptions in a calling/invoking element | §7.3.14.4 GR4 b) | 2002→2014 *(delta under-documented in the 2023 spec; confirm against the older standard before gating)* | **Old:** 2002 TURN behavior for EC-ALL/EC-PROGRAM/EC-PROGRAM-ARG-OMITTED/EC-PROGRAM-NOT-FOUND in a source element that calls/invokes may differ. **New:** such a TURN shall be flagged when the element calls any function or invokes any method. | Yes | gate-behavior-by-dialect | TODO |
| 98 | FLAG-02 IO-STATUS-07 — CLOSE with WITH NO REWIND or UNIT | §7.3.14.4 GR4 c) | 2002→2014 *(delta under-documented; confirm against the older standard before gating)* | **Old:** 2002 I-O status from CLOSE with WITH NO REWIND / UNIT may differ. **New:** such a CLOSE shall be flagged. (Figure spells it I-O-STATUS-07; GR4 spells IO-STATUS-07.) | Yes | gate-behavior-by-dialect | TODO |
| 99 | FLAG-02 MOVE-TO-SAME-NAME — MOVE where sender/receiver share the same data description entry | §7.3.14.4 GR4 d) | 2002→2014 *(delta under-documented; confirm against the older standard before gating)* | **Old:** 2002 may differ for a MOVE whose operands share one data description entry. **New:** such a MOVE shall be flagged when (1) operands are alphanumeric-edited, or (2) the entry includes a subordinate OCCURS … DEPENDING whose controlling item is subordinate to the operand's entry. | Yes | gate-behavior-by-dialect | TODO |
| 100 | FLAG-02 RANGE-EXCEPTION-FOR-INDEX — SET into an index with EC-RANGE-INDEX checking enabled | §7.3.14.4 GR4 e) | 2002→2014 *(delta under-documented; confirm against the older standard before gating)* | **Old:** 2002 range-exception behavior for an index-receiving SET may differ. **New:** an index-assignment/index-arithmetic SET with an index receiver shall be flagged when EC-RANGE-INDEX checking is enabled. | Yes | gate-behavior-by-dialect | TODO |
| 101 | FLAG-02 TERMINATE-WITH-VARYING — TERMINATE of a report containing a VARYING clause | §7.3.14.4 GR4 f) | 2002→2014 *(delta under-documented; confirm against the older standard before gating)* | **Old:** 2002 TERMINATE behavior for a report with a VARYING clause may differ. **New:** such a TERMINATE shall be flagged. | Yes | gate-behavior-by-dialect | TODO |
| 102 | FLAG-14 COMPILE-TIME-ARITHMETIC-EXPRESSIONS — compile-time arithmetic that may differ | §7.3.15.4 GR4 b) | 2014→2023 | **Old:** 2014 compile-time arithmetic gives a particular result. **New:** an expression that could give a different 2014↔2023 result shall be flagged. (↔ Table 1 #12.) | Yes | gate-behavior-by-dialect | TODO |
| 103 | FLAG-14 EVALUATE — EVALUATE directive with both WHEN and WHEN OTHER | §7.3.15.4 GR4 c) | 2014→2023 | **Old:** 2014 EVALUATE directive with WHEN + WHEN OTHER behaved as in 2014. **New:** such a directive shall be flagged. (↔ Table 1 #14.) | Yes | gate-behavior-by-dialect | TODO |
| 104 | FLAG-14 I-O-DECLARATIVE — I-O statement missing INVALID KEY / AT END when a matching declarative is present | §7.3.15.4 GR4 d) | 2014→2023 | **Old:** 2014 behavior for an I-O statement lacking INVALID KEY (or a READ lacking AT END) with a relevant declarative present. **New:** an I-O statement that can take INVALID KEY but omits it shall be flagged when an INPUT/OUTPUT/I-O/EXTEND declarative is present; a READ that can take AT END but omits it shall be flagged when an INPUT/I-O declarative is present. (↔ Table 1 #25/#26.) | Yes | gate-behavior-by-dialect | TODO |
| 105 | FLAG-14 I-O-STATUS-04 — reference to a FILE STATUS item that tests for '04' | §7.3.15.4 GR4 e) | 2014→2023 | **Old:** 2014 '04' handling differed. **New:** a reference to a FILE STATUS item that tests for '04' shall be flagged. (↔ Table 1 #21.) | Yes | gate-behavior-by-dialect | TODO |
| 106 | FLAG-14 I-O-STATUS-07 — reference to a FILE STATUS item that specifies '07' | §7.3.15.4 GR4 f) | 2014→2023 | **Old:** 2014 '07' handling differed. **New:** a reference to a FILE STATUS item that specifies '07' shall be flagged. (↔ Table 1 #22.) | Yes | gate-behavior-by-dialect | TODO |
| 107 | FLAG-14 NUM-ED-ZERO-FIG-CONSTANT — figurative constant ZERO in VALUE of a numeric-edited item | §7.3.15.4 GR4 g) | 2014→2023 | **Old:** 2014 figurative ZERO VALUE on numeric-edited behaved as in 2014. **New:** such use shall be flagged. (Figure: NUM-ED-ZERO-FIGCONST; GR4: NUM-ED-ZERO-FIG-CONSTANT.) (↔ Table 1 #35.) | Yes | gate-behavior-by-dialect | TODO |
| 108 | FLAG-14 READ-PREVIOUS — READ PREVIOUS statement | §7.3.15.4 GR4 h) | 2014→2023 | **Old:** 2014 READ PREVIOUS behaved as in 2014. **New:** a READ PREVIOUS shall be flagged. (↔ Table 1 #29.) | Yes | gate-behavior-by-dialect | TODO |
| 109 | FLAG-14 REF-MOD-ZERO-LENGTH — zero-length reference modification with EC-BOUND-REF-MOD on | §7.3.15.4 GR4 i) | 2014→2023 | **Old:** 2014 zero-length ref-mod behaved as in 2014. **New:** a ref-mod shall be flagged when REF-MOD-ZERO-LENGTH is not explicitly ON/OFF and the TURN for EC-BOUND-REF-MOD is on. (↔ Table 1 #30.) | Yes | gate-behavior-by-dialect | TODO |
| 110 | FLAG-14 VALUE-EDITING — literal VALUE for a numeric-edited item with no editing symbols | §7.3.15.4 GR4 j) | 2014→2023 | **Old:** 2014 behaved as in 2014. **New:** a literal VALUE on a numeric-edited item with no editing symbols shall be flagged. (Figure: VALUE – EDITING; GR4: VALUE-EDITING.) (↔ Table 1 #36.) | Yes | gate-behavior-by-dialect | TODO |
| 111 | FLAG-14 VALUE-FIG-CON-NO-LENGTH — figurative constant in VALUE of an item with no specified length | §7.3.15.4 GR4 k) | 2014→2023 | **Old:** 2014 behaved as in 2014. **New:** a figurative constant in the VALUE of an item with no specified length shall be flagged. (Figure: VALUE-FIG-CON-LENGTH; GR4: VALUE-FIG-CON-NO-LENTH — spec typo for NO-LENGTH.) (↔ Table 1 #17.) | Yes | gate-behavior-by-dialect | TODO |
| 112 | FLAG-14 VALUE-ZERO — numeric-edited item with VALUE figurative constant ZERO | §7.3.15.4 GR4 l) | 2014→2023 | **Old:** 2014 behaved as in 2014. **New:** a numeric-edited item with VALUE figurative ZERO shall be flagged. (↔ Table 1 #35.) | Yes | gate-behavior-by-dialect | TODO |
| 113 | FLAG-14 WRITE-END-OF-PAGE — WRITE permitting END-OF-PAGE but omitting it | §7.3.15.4 GR4 m) | 2014→2023 | **Old:** 2014 behaved as in 2014. **New:** a WRITE that allows END-OF-PAGE but omits it shall be flagged. (↔ Table 1 #37.) | Yes | gate-behavior-by-dialect | TODO |

---

## Table 6 — Inline per-section edition-change NOTES (obsolete / archaic / edition designations scattered through the spec body)

> These are the in-body §-level NOTES that back the Annex F designations (and a few standalone edition references).
> **The three already-investigated pin-to-spec determinations are appended at the end of this table** (rows 130 a/b/c) —
> they are version-INVARIANT legacy bugs pinned to the spec for all dialects, not edition-change rows.

| # | Change (title) | § | Edition delta | Old → New behavior | Affects existing? | Compiler gating action | Status |
|---|---|---|---|---|---|---|---|
| 114 | Fixed continuation indicator (hyphen in column 7) and continuation of literals via it — obsolete | §6.2.2 Fixed indicators; §6.3.5 Continuation of lines (annex F.2 #4) | obsolete-in-2023 (scheduled for removal) | **Old:** ordinary fixed-form features. **New:** flagged obsolete (error-prone; floating continuation indicator is the replacement); still supported, removal next edition. | Yes | flag-obsolete | TODO |
| 115 | FLAG-02 directive — obsolete, to be deleted from the next edition | §7.3.14.1 General (annex F.2 #1) | 2023→next (FLAG-02 obsolete in 2023, scheduled deletion) | **Old:** normal directive flagging 2002↔2014. **New:** obsolete in 2023, to be deleted next edition (superseded by FLAG-14). | Yes | flag-obsolete | TODO |
| 116 | STANDARD-BINARY mode of arithmetic and the STANDARD BINARY Intermediate Data Item (SBIDI) — obsolete | §8.8.1.4.1; §8.8.1.4.2; §11.9.5.2; §11.9.11.2; §A.3; §D.18.1; §D.18.3.1 (annex F.2 #3) | obsolete-in-2023 (reevaluation deferred before removal) | **Old:** defined arithmetic facilities. **New:** flagged obsolete at every §; reevaluated next revision before removal (NOT auto-scheduled for deletion). | Yes | flag-obsolete | TODO |
| 117 | Validation format of the data description (VALIDATE facility) — obsolete | §13.16.2 General formats | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** normal VALIDATE data-description format. **New:** flagged obsolete (constituent of the obsolete VALIDATE facility, F.2 #5). | Yes | flag-obsolete | TODO |
| 118 | DEFAULT clause feature of the VALIDATE facility — obsolete | §13.18.17.1 DEFAULT clause General | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** supported VALIDATE clause. **New:** flagged obsolete. | Yes | flag-obsolete | TODO |
| 119 | DESTINATION clause feature of the VALIDATE facility — obsolete | §13.18.18.1 DESTINATION clause General | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** supported VALIDATE clause. **New:** flagged obsolete. | Yes | flag-obsolete | TODO |
| 120 | INVALID clause feature of the VALIDATE facility — obsolete | §13.18.31.1 INVALID clause General | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** supported VALIDATE clause. **New:** flagged obsolete. | Yes | flag-obsolete | TODO |
| 121 | PRESENT WHEN clause feature of the VALIDATE facility — obsolete | §13.18.41.1 PRESENT WHEN clause General | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** supported VALIDATE clause. **New:** flagged obsolete. | Yes | flag-obsolete | TODO |
| 122 | VALIDATE-STATUS clause feature of the VALIDATE facility — obsolete | §13.18.62.1 VALIDATE-STATUS clause General | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** supported VALIDATE clause. **New:** flagged obsolete. | Yes | flag-obsolete | TODO |
| 123 | CONTENT-VALIDATION-ENTRY feature of the VALIDATE facility — obsolete | §13.18.63.2 General formats | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** supported VALIDATE construct. **New:** flagged obsolete. | Yes | flag-obsolete | TODO |
| 124 | VARYING clause feature of the VALIDATE facility — obsolete | §13.18.64.1 VARYING clause General | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** supported VALIDATE-context VARYING clause. **New:** flagged obsolete. | Yes | flag-obsolete | TODO |
| 125 | Level-2 EC-VALIDATE exception and related level-3 exceptions — obsolete | §14.6.13.1.6 Exception-names and exception conditions | obsolete-in-2023 (part of the obsolete VALIDATE facility) | **Old:** active exception conditions. **New:** EC-VALIDATE (level-2) and all related level-3 exceptions flagged obsolete. | Yes | flag-obsolete | TODO |
| 126 | Program format of the EXIT statement (EXIT PROGRAM) — archaic | §14.9.14.2 EXIT General formats; glossary §3.74 (annex F.1 #1) | archaic-in-2023 (no removal schedule) | **Old:** ordinary statement (= GOBACK in a subprogram, = CONTINUE in a main program). **New:** flagged archaic (GOBACK + MODULE-NAME provide its capabilities); discouraged, still supported. | Yes | flag-obsolete (flag as archaic) | TODO |
| 127 | NEXT SENTENCE phrase in the IF and SEARCH statements — archaic | §14.9.19.2 IF General formats; §14.9.37.2 SEARCH General formats (annex F.1 #2) | archaic-in-2023 (no removal schedule) | **Old:** ordinary phrase transferring control past the next separator period. **New:** flagged archaic (confusing/error-prone; CONTINUE + scope delimiters clearer); discouraged, still supported. | Yes | flag-obsolete (flag as archaic) | TODO |
| 128 | MOVE of ALL "literal" (only digits) or ALL symbolic-character (a digit) to an integer numeric item — obsolete, to be removed next edition | §14.9.25.3 MOVE Syntax rules (SR5) (annex F.2 #2; E.2 #1 exception) | 2023→next (the only surviving alphanumeric-figurative→numeric MOVE; remnant now obsolete) | **Old:** alphanumeric figurative constants (SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal", ALL symbolic-char) could move to numeric / numeric-edited items. **New:** SR5 prohibits these except the single case (digit-only ALL "literal" / ALL symbolic-char digit → integer numeric item); that survivor is itself flagged obsolete, to be removed next edition. | Yes | flag-obsolete | TODO |
| 129 | VALIDATE facility (umbrella) — obsolete | §14.9.50.1 VALIDATE General; §A.4.14; §D.22.1 General (annex F.2 #5) | obsolete-in-2023 (reevaluation deferred before removal) | **Old:** a defined COBOL facility. **New:** whole VALIDATE facility flagged obsolete (no provider implemented it; no interest); reevaluated next revision before removal. | Yes | flag-obsolete | TODO |
| 130 | Implicit INTERMEDIATE ROUNDING (TRUNCATION) — unchanged across editions | §D.17.2 Intermediate rounding | unchanged across editions (explicit no-change edition reference) | **Old:** earlier editions implied INTERMEDIATE ROUNDING IS TRUNCATION when omitted. **New:** same — explicitly stated as unchanged; a deliberate no-gate edition reference. | No | none | TODO |
| 130a | **DISPLAY trailing-trim** — version-INVARIANT legacy bug, pinned to spec | §14.9.11.4 (DISPLAY statement) | version-invariant (legacy oracle non-conformant in every edition) | Legacy oracle behaved non-conformantly across all editions; spec behavior is correct for all dialects → **pinned to spec, not gated** (DEVLOG 509/516). | n/a (all dialects) | pin-to-spec (no gating) | done (pin-to-spec) |
| 130b | **signed-vs-alphanumeric comparison de-sign** — version-INVARIANT legacy bug, pinned to spec | §8.8.4.2.5 | version-invariant (legacy oracle non-conformant in every edition) | A signed numeric operand compared as alphanumeric drops its sign per §8.8.4.2.5 in every edition; legacy was wrong → **pinned to spec, not gated** (DEVLOG 509/516). | n/a (all dialects) | pin-to-spec (no gating) | done (pin-to-spec) |
| 130c | **signed → group de-sign** — version-INVARIANT legacy bug, pinned to spec | §14.9.25.4 GR6a (MOVE) | version-invariant (legacy oracle non-conformant in every edition) | A signed numeric moved to a group item drops its sign per §14.9.25.4 GR6a in every edition; legacy was wrong → **pinned to spec, not gated** (DEVLOG 509/516). | n/a (all dialects) | pin-to-spec (no gating) | done (pin-to-spec) |

---

## Table 7 — 85→2002 deletions (the planned 85→2002 row set — grown as each delta is researched and GATED)

| # | Change | Gate | Implemented |
|---|--------|------|-------------|
| 7.1 | **DATA RECORDS clause deleted** (an obsolete element of ANSI X3.23-1985; ISO/IEC 1989:2002 removed it — the 2023 SD format §13.4.6 admits only the record clause, and the FD set likewise omits it). NIST-85 writes it on every SD/FD. | accepted-inert at `--std 85`; rejected ≥2002 | SD: COBOLNET0873 (DataBinder.BindFileSection, DEVLOG 552). FD: follow-up (same gate, same code). |
| 7.2 | **ALTER + target-less GO TO deleted** (obsolete in '85, removed by 2002 — see Table 4 context). | accepted at 85; rejected ≥2002 | COBOLNET0810/0811 (DEVLOG 543). |
| 7.3 | **CURRENCY SIGN ... WITH PICTURE SYMBOL introduced** (ISO/IEC 1989:2002 §12.3.7 separates the currency STRING from the PICTURE symbol; ANSI X3.23-1985 had only the bare single-character form — an introduction, not a deletion). | rejected at `--std 85` with a specific diagnostic; accepted ≥2002 | COBOLNET0893 (DataBinder.SwitchBindCurrency, DEVLOG 558); matrix row `currency-picture-symbol-2002`. Multi-character currency STRINGS stay rejected everywhere (COBOLNET0896 — the M2-deferred size-changing surface). |

## Appendix — spec line references (for jump-to-spec)

Each catalogued change carries its `specLines` so a reader can jump straight to `specs/ISO_COBOL.md`. Listed by row #.

| # | specLines |
|---|---|
| 1 | 49024, 49026 |
| 2 | 49024, 49028 |
| 3 | 49024, 49030 |
| 4 | 49024, 49032 |
| 5 | 49024, 49034 |
| 6 | 49024, 49036 |
| 7 | 49024, 49038 |
| 8 | 49052, 49056, 49058 |
| 9 | 49060, 49064 |
| 10 | 49068, 49070, 49072, 49074 |
| 11 | 49082, 49084, 49085, 49094, 49095, 49096, 49097, 49098, 49099, 49100 |
| 12 | 49108, 49112 |
| 13 | 49116, 49120 |
| 14 | 49124, 49128, 49130 |
| 15 | 49138, 49142, 49144 |
| 16 | 49148, 49152 |
| 17 | 49156, 49160, 49162 |
| 18 | 49164, 49174, 49176 |
| 19 | 49178, 49180, 49181, 49182, 49183, 49184, 49185, 49186, 49190 |
| 20 | 49196, 49198, 49202, 49204, 49206 |
| 21 | 49216, 49220 |
| 22 | 49224, 49228, 49230 |
| 23 | 49232, 49236, 49238 |
| 24 | 49240, 49244 |
| 25 | 49248, 49250, 49262 |
| 26 | 49248, 49258, 49262 |
| 27 | 49266, 49268 |
| 28 | 49272, 49274, 49276, 49278, 49280 |
| 29 | 49290, 49300, 49302 |
| 30 | 49304, 49308, 49310 |
| 31 | 49312, 49316, 49318 |
| 32 | 49320, 49322–49336, 49344 |
| 33 | 49352, 49356 |
| 34 | 49360, 49364 |
| 35 | 49368, 49372 |
| 36 | 49376, 49386, 49388 |
| 37 | 49390, 49394, 49396 |
| 38 | 49409 |
| 39 | 49411 |
| 40 | 49413–49432 |
| 41 | 49434 |
| 42 | 49436 |
| 43 | 49438 |
| 44 | 49443 |
| 45 | 49445 |
| 46 | 49447 |
| 47 | 49449–49478 |
| 48 | 49479–50046 |
| 49 | 50048–50227 |
| 50 | 50229 |
| 51 | 50231 |
| 52 | 50233 |
| 53 | 50235 |
| 54 | 50237 |
| 55 | 50239 |
| 56 | 50241–50263 |
| 57 | 50265 |
| 58 | 50267 |
| 59 | 50269 |
| 60 | 50271 |
| 61 | 50273 |
| 62 | 50275 |
| 63 | 50277 |
| 64 | 50279 |
| 65 | 50281 |
| 66 | 50283 |
| 67 | 50285 |
| 68 | 50287 |
| 69 | 50296 |
| 70 | 50298 |
| 71 | 50300 |
| 72 | 50302 |
| 73 | 50304 |
| 74 | 50306 |
| 75 | 50308 |
| 76 | 50310 |
| 77 | 50312 |
| 78 | 50314 |
| 79 | 50316 |
| 80 | 50318 |
| 81 | 50320 |
| 82 | 50322 |
| 83 | 50324 |
| 84 | 50334 |
| 85 | 50336 |
| 86 | 50338 |
| 87 | 50340 |
| 88 | 50342 |
| 89 | 50369 (also 27372, 1813) |
| 90 | 50371 (also 27792, 30804, 30829) |
| 91 | 50395 (also 4364, 4366) |
| 92 | 50397 (also 28811, 28813) |
| 93 | 50399–50401 (also 9086–9099, 13404–13406, 45853–45855, 46001, 40076) |
| 94 | 50403 (also 2932, 3066) |
| 95 | 50405–50407 (also 17196, 18436, 18502, 19207, 21044, 23027, 23211, 23508, 24867, 33117, 40499, 47712) |
| 96 | 4366 |
| 97 | 4401–4416 |
| 98 | 4418 |
| 99 | 4420–4424 |
| 100 | 4426 |
| 101 | 4428 |
| 102 | 4509 |
| 103 | 4511 |
| 104 | 4513 |
| 105 | 4515 |
| 106 | 4517 |
| 107 | 4519 |
| 108 | 4521 |
| 109 | 4523–4527 |
| 110 | 4529 |
| 111 | 4531 |
| 112 | 4533 |
| 113 | 4542 |
| 114 | 2932; 3066 (annex F.2 #4 at 50403) |
| 115 | 4366 (annex F.2 #1 at 50395) |
| 116 | 9086; 9099; 13404; 13694; 40076; 45853; 46001 (annex F.2 #3 at 50399–50401) |
| 117 | 17196 |
| 118 | 18436 |
| 119 | 18502 |
| 120 | 19207 |
| 121 | 21044 |
| 122 | 23027 |
| 123 | 23211 |
| 124 | 23508 |
| 125 | 24867 |
| 126 | 1813; 27372 (annex F.1 #1 at 50369) |
| 127 | 27792 (IF); 30804, 30829 (SEARCH) (annex F.1 #2 at 50371) |
| 128 | 28811 (SR5 rule); 28813 (NOTE) (annex F.2 #2 at 50397) |
| 129 | 33117; 40499; 47712 (annex F.2 #5 at 50405–50407) |
| 130 | 45803 |
| 130a | §14.9.11.4 |
| 130b | §8.8.4.2.5 |
| 130c | §14.9.25.4 GR6a |
