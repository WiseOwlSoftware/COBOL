# COBOL.NET — Conformance & Processor-Dependent Element Disposition

> **STATUS: LIVE conformance record (ISO/IEC 1989:2023 §4.2.16).** This document is the implementor's user
> documentation required by §4.2.6 (processor-dependent elements), §4.2.7 (optional elements), and §4.2.13
> (obsolete elements): it states, for every processor-dependent language element in Annex A.3 and every optional
> facility this compiler does **not** implement, whether support is claimed. The compiler emits a compile-time
> warning (the **COBOLNET1560 band**, §4.2.6 third paragraph) when a syntactically-detectable unsupported
> processor-dependent or optional element is used; this document is the authoritative catalogue behind those
> warnings. Default `--std` = COBOL-2023.

## 1. Conformance summary (§4.2.16)

COBOL.NET is a **standard-conforming** COBOL implementation targeting ISO/IEC 1989:2023, with correct support for
the 1985, 2002, and 2014 editions selected by `--std`. It implements the required nucleus and the standard modules
except the optional/processor-dependent facilities listed as **not supported** below; the per-module
optional-element dispositions are §5 (only Claimed/Partial rows there are claimed). Per §4.2.6, an
implementation need not implement processor-dependent elements for which support is not claimed; per §4.2.7, an
optional element is implemented only when support is claimed. All five **documented non-support facilities**
(§4) are now recognized at compile time with a NAMED warning, satisfying §4.2.6 ¶3's mandatory warning
mechanism: SCREEN handling → **COBOLNET1560**; MCS SEND/RECEIVE → **COBOLNET1578**; COMMIT/ROLLBACK →
**COBOLNET1579**; VALIDATE → **COBOLNET1580**. Each is a WARNING, not an error — the program compiles, runs,
and the facility is inert, and no associated exception condition is raised (§14.6.13.1.1 licenses this).

> ⚠ **One documented position where the warning does NOT fire.** When a bare facility verb whose word is also
> a legal user-name (`COMMIT`, `ROLLBACK`, `VALIDATE`) is written as the FIRST statement of an `EVALUATE … WHEN`
> arm, the WHEN selection-object list absorbs it as a data reference before the statement arm is reached, so no
> COBOLNET1579/1580 is emitted. The construct then fails LOUDLY at run time
> (`NotImplementedCobolFeatureException: reference 'COMMIT'`) — it is not a silent wrong answer — but the
> compile-time warning obligation is unmet in that one position. This is the pre-existing EVALUATE
> selection-object greediness, NOT a Wave H regression: the identical behaviour occurs at `--std 2014`, where
> `COMMIT` is a user word and the Wave H statement arm does not fire at all. `RECEIVE`/`SEND` are unaffected
> (their `FROM`/`TO` operand keyword cannot continue an object list, so the parser recovers into the statement).
> Registered as a P14 Step-0 GAP row; fixing it means constraining the EVALUATE object list, which is a shared
> grammar change and is deliberately not bundled into this wave.

## 2. Annex A.3 — processor-dependent language element disposition

Each row is one A.3 item. **Claimed** = standard-conforming support is provided. **Not claimed** = not implemented
(a §4.2.6 warning is emitted where the element is syntactically detectable). **N/A** = the element is a property
of an unsupported facility.

| A.3 # | Element | § | Disposition | Note |
|---|---|---|---|---|
| 1 | Significand/exponent >31/>3 digits in float literal / numeric-edited PICTURE | 13.18.40 | Not claimed | External floating-point `E` PICTURE is staged; standard IEEE usages cover the fixed formats |
| 2 | ARITHMETIC IS STANDARD-BINARY | 11.9.5 | Not claimed | Obsolete feature (A.3 NOTE 1); the native/standard-decimal modes are provided |
| 3 | ARITHMETIC IS STANDARD-DECIMAL | 11.9.5 | **Claimed** | Full SDIDI consumption (P10): `CobolDec` engine, decimal128 range ECs |
| 4 | Asynchronous messaging (MCS) facility | E.3.2 | **Not claimed** | Documented non-support (§4) — inter-run-unit communication not provided |
| 5 | BLOCK CONTAINS clause | 13.x | **Claimed** (inert) | Accepted; no effect on the managed I-O model (A.3 item 5 sanctions this) |
| 6–7 | Commit and rollback facility / its devices | E.3.2 | **Not claimed** | Documented non-support (§4) — no transaction manager |
| 8 | CONTINUE AFTER precision greater than .99 | 14.9.9 | Not claimed | Implementor m = 0 (integer seconds); a fractional interval truncates (§14.9.9.4 GR1) |
| 9 | DEFAULT ROUNDED clause | 11.9 | **Claimed** | The §14.7.4 rounding modes are provided (incl. PROHIBITED, TRUNCATION, NEAREST-*) |
| 10 | INTERMEDIATE ROUNDING clause | 11.9 | **Claimed** | Provided for the standard-decimal intermediate model |
| 11 | I-O status '37' for insufficient authority on OPEN | 9.1.13 | **Claimed** | `FileConnector` maps `UnauthorizedAccessException` → '37' |
| 12 | FLOAT-BINARY clause | 13.x | Partial | HIGH-ORDER-* endianness follows the native platform; binary128 not provided |
| 13 | FLOAT-DECIMAL clause | 13.x | Not claimed | Standard decimal floating-point usages are not provided (item 19) |
| 14 | MODE phrase (ROUNDED) | 14.7.4 | **Claimed** | NEAREST-AWAY-FROM-ZERO / NEAREST-EVEN / PROHIBITED / TRUNCATION provided |
| 15 | USAGE BINARY | 13.18.60 | **Claimed** | Native scaled-integer binary |
| 16 | BINARY-CHAR / -SHORT / -LONG / -DOUBLE | 13.18.60.4 | **Claimed** | Fixed-width binary usages (GR21) |
| 17 | FLOAT-BINARY-32 / -64 / -128 | 13.18.60.4 | Partial | **binary32→`float`, binary64→`double` claimed**; binary128 not (no conforming .NET type — COBOLNET1564) |
| 18 | Endianness-phrase for standard binary float | 13.18.60.4 | **Claimed** (native) | HIGH-ORDER-* per the platform for the supported binary32/64 |
| 19 | FLOAT-DECIMAL-16 / -34 | 13.18.60.4 | Not claimed | ISO/IEC 60559 decimal64/128 have no conforming .NET type — rejected COBOLNET1564 |
| 20–21 | Encoding/endianness for standard decimal float | 13.18.60.4 | N/A | Decimal floating-point usages not provided (item 19) |
| 22 | FLOAT-SHORT / -LONG / -EXTENDED | 13.18.60.4 | **Claimed** | Map to `float`/`double` (= COMP-1/COMP-2) |
| 23 | USAGE PACKED-DECIMAL | 13.18.60.4 | **Claimed** | Native scaled decimal; incl. WITH NO SIGN (2023, §13.18.60.4 GR11) |
| 24 | DISPLAY positioning ignored when N/A | 14.9.11 | **Claimed** | Console device; positioning is a no-op where inapplicable |
| 25 | STANDARD-COMPARE / EC-ORDER-NOT-SUPPORTED / ORDER TABLE (ISO/IEC 14651) | SPECIAL-NAMES | **Not claimed** | Cultural-ordering locale module not provided (P11, COBOLNET1518) |
| 26 | STANDARD-1 phrase of RECORD DELIMITER | 13.x | Not claimed | Reel-device delimiter; mass-storage model only |
| 27 | CODE-SET clause | 13.x | Partial | Native code set; alternate device code sets not provided |
| 28–30 | CLOSE REEL/UNIT, FOR REMOVAL, WITH NO REWIND | 14.9.7 | Partial | REEL/UNIT accepted (mass-storage no-op); tape positioning inert |
| 31 | DELETE statement | 14.9.10 | **Claimed** | Mass-storage DELETE (record) + DELETE FILE (2023) |
| 32 | OPEN I-O phrase | 14.9.27 | **Claimed** | Mass-storage I-O open |
| 33–34 | OPEN WITH NO REWIND / EXTEND | 14.9.27 | Partial | EXTEND claimed; WITH NO REWIND inert (no reel device) |
| 35 | REWRITE statement | 14.9.35 | **Claimed** | Mass-storage rewrite |
| 36 | USE … I-O phrase | 14.9.49 | **Claimed** | Declarative on the mass-storage I-O mode |
| 37 | WRITE BEFORE / AFTER ADVANCING (each separately) | 14.9.51 | **Claimed** | Print-control advancing incl. the 2023 combined BEFORE AND AFTER form |
| 38 | Extended letters / national literals display | 8.x | **Claimed** | National (UTF-16) repertoire supported |
| 39 | READ PREVIOUS / START LESS, NOT GREATER, LESS OR EQUAL | 14.9.30/41 | **Claimed** | Keyed reverse read + START positioning (P10) |
| 40 | SOURCE phrase of RECORD KEY / ALTERNATE RECORD KEY | 13.x | Not claimed | The `record-key-name SOURCE IS` key form is not provided |
| 41–42 | Cultural collating for keys / multiple alt keys with differing collating | 12.4.5.7 | **Partial** | The file-control COLLATING SEQUENCE clause (§12.4.5.7, Format 1 + Format 2 per-key) is supported for **alphanumeric** keys under a declared SPECIAL-NAMES alphabet — per-key weighted ordering/START/uniqueness on the greenfield IndexedConnector (COBOLNET1582/1583). **NATIONAL-key collating** (COBOLNET1584) and **LOCALE-based cultural collating** (ISO/IEC 14651 locale module, item 25) are NOT claimed — the national leg is a documented P14 GAP |
| 43 | Zero-length record for relative/sequential files | 9.x | Not claimed | Minimum record length is 1 |
| 44 | Abnormal termination indication | 14.6.12 | **Claimed** | Nonzero process exit on an unresumed fatal exception condition |
| 45 | Parametric-polymorphism method resolution | 11.x | **Claimed** | Single-dispatch OO method resolution over the class table |
| 46 | Detect a specific level-3 exception condition | 14.6.13 | **Claimed** | The EC engine detects the implemented level-3 conditions (§14.6.13.1.1 license for the rest) |

## 3. Behavior determinations (§4.2.6 / Annex E — pinned implementor choices)

- **I-O status '0x' case equivalence** (E.2 item 17): the low-order status digit for a non-'00' successful
  completion is implementor-dependent; COBOL.NET reports the specific '0x' value (e.g. '04', '05', '07') rather
  than collapsing to '00'.
- **I-O status '04' (record-length mismatch, §14.9.30 GR14 / §9.1.13.2 item 3)**: **emitted** on a
  record-sequential READ whose physical record is outside the file's min/max record size (fixed leg: shorter than
  the record width; varying leg: outside [VaryMin, VaryMax]) — the READ is successful, the record is delivered,
  status '04' (VCR 21; golden `2002/io_status_04`). Line-sequential is excluded (§14.9.30 GR15 pads short records
  with trailing spaces — its '06' write path remains unimplemented). The value is version-invariant — the E.2
  item 15 2023 delta only clarifies *when* it is set.
- **I-O status '07' restricted to OPEN/CLOSE (§9.1.13.2 item 6; E.2 item 16)**: already met at all editions — the
  only '07' setter is CLOSE REEL/UNIT on a non-reel medium (`FileRegistry.CloseReelUnit`); no READ/WRITE/START/
  REWRITE/DELETE path sets it. The 2023 restriction holds without a `DialectLevel` gate.
- **I-O status '37' insufficient authority on OPEN/DELETE FILE (§9.1.13.6 item 6b; E.2 item 18)**: emitted at all
  editions (mapped from .NET `UnauthorizedAccessException`). The spec permits it ("may") and marks detection
  processor-dependent; E.2 item 18 is a clarification that it is allowed, not a 2023 introduction — so it is NOT
  gated/suppressed below 2023.
- **I-O status '39' (fixed-file-attribute conflict, §9.1.13.6 item 7; E.3.3 item 35)**: not produced — the host-file
  model carries no persisted fixed-attribute catalog (record size / organization / code-set) to detect a conflict
  against, so DELETE FILE / OPEN never returns '39'. Documented non-support until a physical-attribute store exists.
- **Transfer of control includes sections (§14.6; E.2 item 26)**: a section is a first-class transfer target at all
  editions — `ProcedureTableBuilder` gives each section a contiguous `[StartPc,EndPc]` pc range, so GO TO / PERFORM
  of a section resolve (§14.9.17 / §14.9.28). The 2023 clarification is met without a gate.
- **WRITE with no END-OF-PAGE phrase (§14.9.51; E.2 item 30)**: when the END-OF-PAGE condition occurs and no
  END-OF-PAGE phrase is present, control falls through to the next statement (the natural code path — no branch is
  emitted for an absent phrase). Version-invariant; met without a gate (no `>>FLAG-14` option exists for it).
- **>>EVALUATE combined-condition end omission (§7.3.13 GR6/GR10; E.2 item 8)**: the preprocessor omits the
  END-EVALUATE text precisely when no WHEN matched AND no WHEN OTHER is present — the corrected 2023 AND-truth rule;
  the compile-time directive is version-invariant.
- **Figurative constant on an unspecified-length item (§8.3.3.6.4 GR3b/c; E.2 item 11)**: a bare figurative VALUE
  fills a single character (GR3b); an `ALL "literal"` fills the literal's own length (GR3c). Both are defined for
  dynamic-length items at all editions, superseding the pre-2023 undefined case.
- **Case mappings (UPPER-CASE / LOWER-CASE, §15.97.4 r4 / §15.57.4)**: absent a locale, the case correspondence is
  **implementor-defined** (§15.97.4 r4). COBOL.NET uses the .NET invariant Unicode case tables. Because the mapping
  is delegated to the implementor, the enumerated 2023 annex changes (E.2 item 14 deletions of DOTLESS I
  `(0131,0069)` and GREEK FINAL SIGMA `(03C2,03C3)`; E.3.3 item 6 additions) are not separately tuned — the invariant
  mapping is the determination (a corner case; the general Latin/basic repertoire matches exactly).
- **STOP RUN / GOBACK termination status (§14.9.42.4 GR5 / §14.9.18.4 GR10 — Annex A required items 192/193)**:
  the status "passed to the operating system" and the ERROR/NORMAL termination indication both map to the single
  observable available on .NET — the process exit code (`Environment.ExitCode`). The constraint on the STATUS
  operand (item 192): the integer value of `literal-1` / `identifier-1` (truncated toward zero) becomes the exit
  code; a non-integer display/national operand is interpreted numerically. The error-termination mechanism
  (item 193): when a status phrase specifies `ERROR` with **no** STATUS value, the exit code is 1; `NORMAL` (or
  no phrase) is 0. When a STATUS value is present it wins regardless of ERROR/NORMAL. A main-program GOBACK
  (§14.9.18.4 GR3) uses the same mapping; a status phrase on a GOBACK executed in a **called** program is inert
  (GR2). Programs with no status phrase leave the exit code at 0 (the abnormal-fatal case still forces item 44's
  nonzero exit). The status is flushed to `Environment.ExitCode` at the write site by the `RunUnit.ExitStatus`
  setter (so it crosses assembly boundaries — a separately-compiled module's STOP RUN … WITH STATUS reaches the
  process exit code). Two host clamps apply on top of the value mapping and are outside COBOL's control: the
  `long` status is narrowed to `Int32`, and a POSIX host reports only the low 8 bits of the exit code — so a
  STATUS ≥ 256 (or outside `Int32`) is reduced modulo the platform's exit-code width.
- **Compile-time arithmetic mode (§7.3.6.2 SR2 / §7.3.6.3 GR2 — Annex E.2 item 6; the required §4.2.16 implementor
  documentation)**: compile-time arithmetic expressions are evaluated in a **standard fixed-point decimal mode** —
  .NET `System.Decimal` (a 128-bit decimal type, **28–29 significant decimal digits**, magnitude up to ≈ ±7.9×10²⁸).
  A standard mode is chosen over the native binary runtime mode for **portability** (§7.3.6.3 GR2 NOTE). *Intermediate
  precision / magnitude / range (§7.3.6.2 SR2):* the same 28–29-digit decimal throughout; an intermediate result that
  exceeds it is a **diagnosed error** (`DiagnosticCatalog.ConstantEntryRule`), never a silent wrap. *Intermediate
  rounding (§7.3.6.2 SR2):* addition / subtraction / multiplication are exact within the precision; **division
  truncates** toward the decimal precision. The exponentiation operator is rejected (§7.3.6.2 SR1a); a division by
  zero is rejected (§7.3.6.2 SR1c). *Final result (§7.3.6.3 GR3):* the value of an arithmetic-expression operand is
  **truncated to its integer part** (`decimal.Truncate`) and treated as an integer numeric literal — a single numeric
  literal in the arithmetic-expression position is instead re-classified as a literal and keeps its own value and
  scale (§13.10.3 SR1). Evaluator: `DataBinder.Constants.cs EvalConstExpr`, invoked from the §13.10 constant entry
  now; the same determination governs the DEFINE / EVALUATE directive arithmetic-expression operands once the
  frontend (pre-parse) evaluator lands (today a multi-token directive operand binds only its first token — a recorded
  Wave-D GAP).
- **Recognized-and-ignored compiler directives (§7.3 — the implementor-disposition set)**: the following standard
  directives are RECOGNIZED (consumed during text manipulation so the program compiles unchanged) and carry no
  effect, each because COBOL.NET provides a single behaviour with no alternative to select: **>>CALL-CONVENTION**
  (§7.3.9) — CALL uses the single .NET managed calling convention (a native-interop convention selector has no
  target); **>>LEAP-SECOND** (§7.3.17) — the underlying .NET date/time model does not represent leap seconds, so
  leap-second handling is documented non-support; **>>LISTING** / **>>PAGE** (§7.3.18 / §7.3.19) — no source listing
  is produced, so listing on/off and page ejects are inert; **>>DISPLAY** (§7.3.12) — likewise transfers text to the
  (absent) source listing / compile-time device, so it is recognized and consumed. The **>>FLAG-02 / >>FLAG-14**
  flagging directives (§7.3.14 / §7.3.15) are RECOGNIZED (a conforming compiler must not error on a standard
  directive), but the migration / obsolescence diagnostics they request are a separate REMAINING Wave-D item — the
  flags are not yet emitted. Set: `ConditionalCompilationProcessor.KnownIgnoredDirectives`.
- **Exception-checking PERFORM — FINALLY on the fatal path (§14.9.28.4, a GENUINE STANDARD DEFECT)**: NOTE 8 says "the
  end of the PERFORM statement includes the statements in a FINALLY phrase", while GR20's fatal branch routes an
  unresumed fatal condition to §14.6.13.1.3 (abnormal termination), which never re-enters "the end of the PERFORM".
  The two cannot both hold. **Pinned choice: FINALLY runs on the NORMAL (and EXIT-PERFORM) fall-through path ONLY, NOT
  on the fatal abnormal-termination path.** (Realized structurally — a `CobolFatalException` unwinds past the inline
  FINALLY block.) Revisit only if the four-edition inventory surfaces a conformance test pinning the other reading.
- **Exception-checking PERFORM — RESUME NEXT STATEMENT in a WHEN skips WHEN COMMON (§14.9.28.4 GR17/GR19, spec silent)**:
  GR17 passes control to imp-4 (WHEN COMMON) "at the completion of the execution of imperative-statement-2"; a RESUME
  (§14.9.33) is a transfer of control OUT of imp-2, so imp-2 does not "complete" and the GR17→imp-4 hand-off is not
  taken. **Pinned choice: a WHEN that RESUMEs (NEXT STATEMENT) does NOT run WHEN COMMON.**
- **SET SIZE OF — the storage-physically-unavailable EC-STORAGE-NOT-AVAIL leg (§14.9.39.4 GR38 third sentence)**: a
  dynamic-length elementary item is a managed .NET `System.String`, always allocatable within the runtime string
  limit, so the "amount of storage required to expand … is not available" branch is unreachable. **Pinned choice:
  that third leg never raises; the GR37 negative→0 and GR38 clamp-to-maximum legs DO set the nonfatal
  EC-STORAGE-NOT-AVAIL (arithmetic-expression-5 form) under `>>TURN EC-STORAGE-NOT-AVAIL CHECKING ON` (golden
  `2023/ec_storage_not_avail`).** The integer-2 literal form is compile-time bounded by SR34, so its out-of-range
  cases are compile diagnostics, not this runtime EC.
- **EC-DATA-NOT-FINITE / EC-DATA-OVERFLOW applied to EVERY floating-point usage (§14.6.13.2 item 3 / §14.9.25.4 GR4
  step 4a)**: the standard scopes these to a "standard floating-point usage" (the ISO/IEC 60559 FLOAT-BINARY-32/64/128
  and FLOAT-DECIMAL-16/34 forms — §13.18.60 item 19; FLOAT-SHORT/-LONG/-EXTENDED and COMP-1/COMP-2 have
  implementor-defined representation, §13.18.60 item 21, with implementor-specified exception conditions per
  §14.6.13.4). In the typed-native model EVERY floating-point usage is a native IEEE `float`/`double`, so **pinned
  choice: COBOL.NET raises both ECs for all floating-point usages uniformly** — mandatory for the standard usages,
  an implementor determination (which the standard delegates) for FLOAT-SHORT/-LONG/-EXTENDED and COMP-1/COMP-2.
  EC-DATA-NOT-FINITE (fatal) fires when a NaN/±Infinity float sending operand is referenced (both the numeric-value and
  the string-image read paths) except in a class condition, a sign condition, a same-usage MOVE, or VALIDATE;
  EC-DATA-OVERFLOW (fatal) fires when a MOVE's finite value overflows a single-precision receiver to ±Infinity. Both
  default OFF (byte-identical to a pre-slice build). Goldens `2023/ec_data_not_finite`, `2023/ec_data_overflow`.
  **Documented gaps:** (a) a floating-point NUMERIC-EDITED MOVE receiver is not covered by the EC-DATA-OVERFLOW seam
  (§14.9.25.4 GR4 also names it) — deferred until floating-point numeric-edited PICTUREs are supported; (b) a
  multi-receiver `ADD/SUBTRACT … TO/FROM` with several float receivers under EC-DATA-NOT-FINITE checking can
  half-commit earlier receivers before a later non-finite receiver's read raises — only observable under USE-F3 +
  RESUME NEXT STATEMENT, where a fatal-EC-interrupted statement already leaves undefined results (§14.6.13.1.3), a
  precise follow-on.

## 4. Documented non-support facilities (§4.2.6 / §4.2.7 / §4.2.13)

The following whole facilities are **not implemented**. SCREEN handling (item 4) is recognized at compile time
and reported with the named COBOLNET1560 warning per §4.2.6; the locale facility (item 5) is rejected at bind
with the COBOLNET1518 error (the A.4.1 unclaimed-optional posture); MCS, COMMIT/ROLLBACK, and VALIDATE
(items 1–3) are today a generic parse error — their named recognize-and-warn diagnostics (the COBOLNET1560
band) are the tracked PHASE-13 Wave H code half, after which every row here meets the §4.2.6 warning mechanism
/ §4.2.13 obsolete flagging:

1. **Message Control System (MCS) asynchronous messaging** (E.3.2 item 1 / A.3 item 4): `SEND`, `RECEIVE`,
   and MESSAGE-TAG data items (the ISO/IEC 1989:2023 MCS surface — the pre-2002 COMMUNICATION SECTION is not part
   of this edition). Processor-dependent; not provided.
2. **Commit and rollback** (E.3.2 item 2 / A.3 items 6–7): `COMMIT`, `ROLLBACK`. No transaction manager.
3. **VALIDATE facility** (§14.9.50, §13.16–13.18, F.2 item 5): the `VALIDATE` statement, the validation clauses
   (`CLASS`/`DEFAULT`/`DESTINATION`/`INVALID`/`PRESENT WHEN`/`VARYING`), and EC-VALIDATE. An obsolete optional
   element (§4.2.13).
4. **Screen handling** (§13.9, optional §4.2.7): the SCREEN SECTION, ACCEPT/DISPLAY format-3 (screen), and the
   EC-SCREEN family. Not provided.
5. **Locale facility** (Annex A.4.9 optional module): the intrinsic functions `LOCALE-COMPARE`, `LOCALE-DATE`,
   `LOCALE-TIME`, `LOCALE-TIME-FROM-SECONDS`, and `STANDARD-COMPARE` (the last also disposed under A.3 item 25,
   §2 row 25), plus the `LOCALE` phrases of `LOWER-CASE`/`UPPER-CASE`/`NUMVAL-C`/`TEST-NUMVAL-C` — each rejected
   at bind time with the **COBOLNET1518 error** (per Annex A.4.1 a processor accepts optional-element syntax only
   when support is claimed, so an ERROR — not the §4.2.6 COBOLNET1560 warning band, which applies to
   processor-dependent elements — is the conforming disposition for this unclaimed optional module). The
   remaining locale entry points (`SET LC_*` formats 11/12, the SPECIAL-NAMES `LOCALE` clause,
   OBJECT-COMPUTER `CHARACTER CLASSIFICATION`) currently have **no named diagnostic** — the first two are parse
   errors, the third is silently accepted; naming them is a tracked review-ledger fix (F3).

   > ⚖ **DETERMINATION — `NUMVAL-C`'s `LOCALE` phrase, which A.4.9 does not itself list** (owner decision
   > 2026-08-03; fix-queue PB37). A.4.9 enumerates thirteen optional locale elements and names the `LOCALE`
   > keywords of `LOWER-CASE` (item 6), `TEST-NUMVAL-C` (item 12) and `UPPER-CASE` (item 13) individually, but
   > **not `NUMVAL-C`'s**. COBOL.NET reads that omission as **editorial**, so `NUMVAL-C`'s `LOCALE` phrase is an
   > optional element of this module and its non-support is documented non-support rather than a conformance gap.
   > Grounds: the `NUMVAL-C` and `TEST-NUMVAL-C` general formats are character-identical, and **§15.94 rule 1
   > states that the `TEST-NUMVAL-C` argument rules ARE §15.68's** — so the alternative reading has A.4.9 making
   > item 12 optional while leaving mandatory the very rules item 12 delegates to. Reading a whole optional module
   > as excluding one keyword of one function, on an omission the standard nowhere states, would also be the only
   > reading under which a processor could claim A.4.9 non-support and still owe `NUMVAL-C` locale parsing.

## 5. Annex A.4 optional-element disposition (§4.2.7)

One row per A.4 optional module. **Claimed** = support is claimed (§4.2.7/A.4.1); **Partial** = the supported /
not-supported split is itemized (the §4.2.7 second-sentence requirement); **Not claimed** = the module's syntax
is not accepted (per A.4.1, optional-element syntax is accepted only when support is claimed — so a parse error
or a named error is the conforming posture). This section is the user-documentation face; §1's conformance
summary claims only what is Claimed/Partial here.

| A.4 § | Module | Disposition | Note |
|---|---|---|---|
| A.4.2 | ACCEPT/DISPLAY screen handling | Not claimed | The SCREEN facility (§4 item 4; COBOLNET1560 warning) |
| A.4.3 | Commit and rollback | Not claimed | §4 item 2 |
| A.4.4 | Dynamic capacity tables | **Claimed** | OCCURS DYNAMIC (P12 §8.5.1.9; EC-BOUND-OVERFLOW raise live) |
| A.4.5 | DYNAMIC LENGTH elementary items | Partial | Alphanumeric live (§13.18.19, the 1561–1563 SR band); the NATIONAL dynamic-length FUNCTION LENGTH / BYTE-LENGTH runtime paths are staged loud (the P12 residue ledger) |
| A.4.6 | Extended letters | **Claimed** | National (UTF-16) repertoire (§2 row 38) |
| A.4.7 | File sharing and record locking | **Claimed** | SHARING/LOCK MODE/RETRY on every organization (P10 FILE-LOCK) |
| A.4.8 | FORMAT and SELECT WHEN file handling | Not claimed | No surface — a parse error today; a named diagnostic is a tracked P14 disposition row |
| A.4.9 | Locale support and related functions | Not claimed | §4 item 5 (COBOLNET1518 error) |
| A.4.10 | Object orientation optional items | Not claimed | The three OPTIONAL items only: multiple inheritance (×2 — multi-base INHERITS rejects COBOLNET0849) and parametric-polymorphism method resolution (rejected/deferred; §2 row 45 covers the supported single-dispatch resolution). The OO CORE is mandatory surface, claimed separately |
| A.4.11 | Report Writer | Partial | Implemented: the RW nucleus incl. PRESENT WHEN + VARYING (P10 RW-2002) and the SUPPRESS statement (§14.9.45 — inhibits the current instance's printing/page-advance/NEXT GROUP/LINE-COUNTER but not sum accumulation or the end-of-group reset; SR1/GR1 resolve the enclosing USE BEFORE REPORTING group at bind, COBOLNET1581 rejects a misplaced SUPPRESS). Staged LOUD (COBOLNET0899 band): cross-program CODE, LINE NEXT PAGE / multiple LINE, report-group OCCURS, several counter/SOURCE/SUM legs. NO grammar surface yet (tracked, the P13 grammar batch + ledger): COLUMN LEFT/CENTER/RIGHT, PAGE COLS, LAST CONTROL HEADING. The full itemization: `docs/COBOLNET_REPORT_WRITER_DESIGN.md` §5 |
| A.4.12 | RESUME statement | **Claimed** | §14.9.33 (the EC declarative RESUME) |
| A.4.13 | REWRITE FILE and WRITE FILE | Not claimed | No surface — a parse error today; a named diagnostic is a tracked P14 disposition row |
| A.4.14 | VALIDATE | Not claimed | §4 item 3 |

## 6. Maintenance

Update this document in the same change set as any change to the supported surface (a new usage, a facility
newly implemented or newly documented as non-support, an I-O status determination). The COBOLNET1560-band
warning sites are the code-side counterpart — keep the two in sync. This file is referenced by
`docs/VERSION_CHANGE_REFERENCE.md` (the edition-change checklist) and by `docs/DOC_INDEX.md`.

## 7. Annex A.1 — implementor-defined language element register (§4.2.5 / §4.2.16)

> **⛔ STATUS: INCOMPLETE — this is a known, registered v1.0 conformance gap, not an oversight.**
> Annex A.1 lists **222** implementor-defined language elements: **164 required · 30 optional · 27
> conditionally required · 1 whose class the standard states in prose rather than the usual sentence
> (item 176, the SET NaN payload)**, of which **199 carry the obligation "This item shall be documented in the
> implementor's user documentation"** and 23 explicitly do not. §4.2.5 requires the implementor to *specify*
> every element identified as required and to *document* every element identified as requiring documentation;
> D13 makes those 199 part of the definition of done.
> **MEASURED 2026-08-03 — this register discharges 10 of the 199** (`python scripts/spec/audit_annex_a1.py`
> reports it, and the numbers below are its output, not a hand count): items 2, 59, 158, **202**, and the
> **storage-representation family 205–215** that V59 pinned (BINARY · the BINARY-CHAR family · COMPUTATIONAL ·
> INDEX · PACKED-DECIMAL: the byte width, the radix, the byte order and a worked example for each, since a
> COBOL developer reading this needs to know what lands on disk). **189 obligations remain.**
> ⚠ **The two item-92 rows are VOLUNTARY**: A.1-92 is one of the 23 elements the standard says need *not* be
> documented. They are kept because the determination is load-bearing for users, but they discharge no
> obligation, and they only PARTIALLY address A.1-56 (DISPLAY data conversion), which remains open.
> Completing this register is **PHASE-14 Step 0** work — the four-edition traceability inventory enumerates
> every A.1 row and drives it to zero-GAP; do not attempt it piecemeal here. Items are added below as, and only
> as, the compiler's behaviour for them is actually settled — an undocumented determination and a
> wrongly-documented one are both non-conformance, and the second is worse.
> ⛔ **That last sentence earned itself on 2026-08-03**: the §15.3.3.2 fractional-seconds determination had been
> filed under **item 87**, which is *FORMATTED-CURRENT-DATE (accuracy of returned time)* — a different, still
> undocumented obligation. It belongs to **item 202**. The number was inherited and never re-derived, exactly
> the failure mode CLAUDE.md rule 1 names. `audit_annex_a1.py --check` now re-derives every number here against
> the catalog parsed straight out of Annex A.1, so it cannot recur silently.

| A.1 item | Element | Our determination |
|---|---|---|
| 2 | **ACCEPT statement — device used when FROM is unspecified**, §14.9.1 GR5, required + documented | The implementor default ACCEPT device is the process **standard input** stream. The input-capable SPECIAL-NAMES device-names (§12.3.7 Format 4, `device-name-1 IS mnemonic-name-3`) are **CONSOLE** and **SYSIN**, both naming standard input; a mnemonic bound to an output-only device fails §14.9.1.3 SR2 (`COBOLNET0817`). Implemented in `AcceptDisplayBinder` (`AcceptInputDevices`) + `AcceptDisplayEmitter.EmitAcceptDevice`. |
| 59 | **DISPLAY statement — standard display device**, §14.9.11 GR8, required + documented | When the UPON phrase is omitted the standard display device is the process **standard output** stream. The output-capable SPECIAL-NAMES device-names are **CONSOLE** and **SYSOUT** (→ standard output) and **SYSERR** (→ standard error); a mnemonic bound to an input-only device (e.g. SYSIN) fails §14.9.11.3 SR2 (`COBOLNET0817`). Implemented in `AcceptDisplayBinder` (`DisplayOutputDevices` / `BindDisplayUpon`) + `AcceptDisplayEmitter.EmitDisplay`. |
| 158 | **Reference format — rightmost character position of the program-text area (margin R)**, §6.3, required + documented | **Margin R is immediately to the right of character position 72**, i.e. the fixed-form program-text area is columns **8–72**. Characters beyond it are not part of the program text and are ignored — they are not an error (§6.3.4; comment-text likewise runs only "up to margin R"). Note §6.3.1 makes this position *implementor-defined*: the standard does **not** mandate 72, and ISO 2023 has no "identification area" (that was a COBOL-85 card-image convention). Columns 1–6, the sequence number area, are **optional** and may hold any character (§6.3.2) — a blank sequence area is ordinary fixed-form source. Free-form reference format (§6.2) is not column-bounded and is unaffected. Our auto-detection between the two formats is an implementor extension beyond the standard, which specifies fixed-form as the default and `>>SOURCE FORMAT` as the selector; the detector's rules live in `ReferenceFormatProcessor.IsFixedForm`. |
| 205 | **USAGE BINARY clause — computer storage allocation, alignment and representation of data**, §13.18.60.4 GR4, required + documented | A BINARY item is a **two's-complement integer of the item's UNSCALED value, MOST SIGNIFICANT BYTE FIRST (big-endian)**, in a width pinned by the PICTURE's digit count: **1 byte** for 1–2 digits · **2** for 3–4 · **4** for 5–9 · **8** for 10–18 · **16** for 19–38. The implied decimal point occupies no storage (§13.18.40.4) and the sign is the two's-complement sign — there is no separate sign byte. **Alignment: none** — COBOL.NET performs no SYNCHRONIZED physical padding, so an item begins at the next byte and a group carries no implicit FILLER. The 16-byte tier exists because GR4's closing sentence requires storage "sufficient … to contain the maximum range of values implied by the associated decimal picture character-string" and a signed 19-digit picture (10^19−1) exceeds 2^63−1. Widths are sign-INDEPENDENT (GR12's precedent for the fixed-width usages). Worked example: `PIC 9(4) COMP VALUE 1234` occupies 2 bytes `04 D2`; `PIC S9(4) COMP VALUE -1234` occupies `FB 2E` (65536−1234). An UNSIGNED item holds the absolute value (§14.9.25.4 GR8). `FUNCTION BYTE-LENGTH` reports exactly this width, and it is the width the item occupies in a group image, a record, a SORT key and a REDEFINES view — one representation at every byte boundary. |
| 206 | **USAGE BINARY-CHAR / -SHORT / -LONG / -DOUBLE — wider range than the minimum**, §13.18.60.4 GR12, optional | **Not provided.** Each usage holds exactly the GR12 minimum range for its width: CHAR ±128 / 0–255 · SHORT ±32768 / 0–65535 · LONG ±2^31 / 0–2^32 · DOUBLE ±2^63 / 0–2^64. |
| 207 | **USAGE BINARY-SHORT/-LONG/-DOUBLE and FLOAT-SHORT/-LONG/-EXTENDED — representation and length**, §13.18.60.4 GR13 + GR21, required + documented | BINARY-CHAR **1** byte, BINARY-SHORT **2**, BINARY-LONG **4**, BINARY-DOUBLE **8** — two's complement, big-endian, SIGNED and UNSIGNED the same width (GR21). FLOAT-SHORT is **IEEE 754 binary32 (4 bytes)**; FLOAT-LONG and FLOAT-EXTENDED are both **IEEE 754 binary64 (8 bytes)** — the standard's subset nesting (GR13) is satisfied since every binary64 value is expressible in binary64. A floating-point item has **no character image**: it never participates in a group image, a record or a REDEFINES view, and an attempt to use one there is rejected loudly rather than given an invented representation. |
| 208 | **USAGE COMPUTATIONAL clause — alignment and representation of data**, §13.18.60.4 GR6, required + documented | COMPUTATIONAL (and its COMP / COMP-4 synonyms) is **identical to USAGE BINARY** — radix 2, the item 205 width ladder and byte order, the same PICTURE-digit-count truncation discipline. COMP-5 shares the representation but is bounded by the NATIVE two's-complement range of its byte width rather than the picture's digit count, and is therefore excluded from character images (its value range can exceed what its PICTURE describes). |
| 211 | **USAGE INDEX clause — alignment and representation of data**, §13.18.60.4 GR10, required + documented | An index item is a **64-bit managed integer occurrence number** (8 bytes as reported by `FUNCTION BYTE-LENGTH`), never a byte offset. It has **no character image**: it takes no part in a group image, a record, a SORT key or a REDEFINES view, and only SET, SEARCH and relation conditions may reference it. A codec handed one fails loudly rather than inventing bytes. |
| 215 | **USAGE PACKED-DECIMAL clause — computer storage allocation, alignment and representation of data**, §13.18.60.4 GR11, required + documented | A PACKED-DECIMAL item is **binary-coded decimal, two digits per byte, most significant first**, with a **trailing sign nibble** in the low half of the last byte: `C` positive, `D` negative, `F` for an item with no operational sign (the convention IBM, Micro Focus and GnuCOBOL all write, so a data file interchanges). The digit run is padded on the left with a zero nibble when needed, giving **`digits / 2 + 1` bytes**; the implied decimal point occupies no nibble. Under the COBOL-2023 **WITH NO SIGN** phrase the sign nibble is not reserved at all — every nibble is a digit and the width is **`ceil(digits / 2)`**. ⚠ The two forms can occupy the SAME number of bytes: 3 digits is 2 bytes either way, laid out `12 3C` signed and `01 23` unsigned-no-sign. **Alignment: none** (no SYNCHRONIZED padding). Worked example: `PIC 9(4) COMP-3 VALUE 1234` occupies 3 bytes `01 23 4F`; `PIC S9(4) COMP-3 VALUE -1234` occupies `01 23 4D`. Decoding accepts the universal readings — `B` and `D` negative, everything else positive — so a file written by another COBOL system reads correctly. |
| 202 | **Time formats and corresponding function values — maximum number of digit positions in the decimal fraction of the seconds subfield**, §15.3.3.2, required + documented | **18.** §15.3.3.2 requires the implementor to define this maximum and requires it to be **≥ 9**; 18 is chosen because it is exactly where the value stops being exactly representable — a fraction of 19 or more digits overflows the emitter's `(long)Pow10.AsWide(width)` and prints garbage, so the documented bound and the representable range are the SAME number rather than two constants that can drift apart. Enforced by `DateTimeFormatGrammar.MaxFractionDigits`, which the §15.3 format recogniser consults, so a format with a longer fraction is rejected at COMPILE time (`COBOLNET1631`) instead of fabricating a value. |
| 92 | **Characteristics and representation of a numeric intrinsic function's returned value, and its text image**, §15.4.1 + §14.9.11.4 GR1, required — documentation NOT required by A.1 (voluntary); partially addresses A.1-56 | §15.4 places a function's returned value in a **temporary elementary data item**, and under NATIVE arithmetic §15.4.1 makes "the characteristics and representation of the returned value … defined by the implementor"; §14.9.11.4 GR1 independently makes any conversion between a DISPLAY operand and the device implementor-defined. **COBOL.NET's determination: a numeric function result used as TEXT renders in the LITERAL form of its value** — the significant digits with **no leading-zero padding**, a leading `-` when negative (never a zoned over-punch, because this is not a stored item), and a decimal point followed by exactly the result's scale in fraction digits when the value is scaled. A FLOAT-valued function (SQRT, trig, financial) renders through the same shortest-round-trip `CobolFloat.Display` a COMP-2 item does, so the function and an item holding its value agree. Examples: `DISPLAY FUNCTION ORD("A")` → `66` (§15.70.1 — ordinal position, lowest is 1); `DISPLAY FUNCTION MIN(3 -14 0 8 -3)` → `-14`; `MOVE FUNCTION ORD("A") TO PIC X(10)` → `66` left-justified, space-filled (§14.9.25.4 GR6). **⛔ The rule exists to make the compile-time FOLD unobservable.** An intrinsic over constant arguments folds to a numeric literal and a literal renders as its own text (`DISPLAY 42` → `42`); had a computed result been given a padded fixed width instead, `DISPLAY FUNCTION LENGTH(X)` and `DISPLAY FUNCTION ORD(C)` would print in visibly different formats for no reason a COBOL programmer could see. One rule, both paths — pinned by the golden `da2_function_as_text`, whose first four lines are two fold/compute pairs of the same value. Note this deliberately differs from GnuCOBOL, which zero-pads a computed `ORD` to its field width while printing a folded `MAX` minimally; the standard permits either, and internal consistency was preferred over matching a compiler that is not self-consistent here. |
| 92 | **Evaluation of an exact-family intrinsic whose ARGUMENT is floating-point**, §15.4.1, required — documentation NOT required by A.1 (voluntary) | §15.4.1 makes the characteristics and representation of a returned value "defined by the implementor" under native arithmetic, and each of these functions is defined by an equivalent arithmetic expression over its own operands. **COBOL.NET's determination: when ANY argument renders as floating-point (USAGE COMP-1 / COMP-2 / FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED), the function's equivalent arithmetic expression is evaluated in IEEE binary64** and the result is then delivered to the receiver by the ordinary rules — unquantized into a floating receiver, quantized through `FromDouble` at the working scale into a fixed-point one. With an all-fixed-point argument list the function stays base-10 EXACT over scaled `Int128` and nothing changes. The determination is forced rather than chosen: a floating-point argument is a legal class-numeric argument (§8.5.2.12 item 2 + §8.5.2.1 Table 2), and once an operand arrives as binary64 there is no exact value left for an exact evaluation to preserve. Sign discipline is the spec's, not the carrier's — MOD floors and REM truncates (§15.64.4 / §15.77.4), INTEGER floors and INTEGER-PART truncates (§15.44 / §15.49) — so `MOD(-7,3)` is 2 and `REM(-7,3)` is -1 in binary64 exactly as in `Int128`. Pinned by the golden `pb2_float_argument_exact_family`. Before this was implemented, such a program did not merely compute differently: it failed to compile, surfacing a raw Roslyn `CS1503` from the generated C#. |
