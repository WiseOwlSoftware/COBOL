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
  target); **>>LEAP-SECOND** (§7.3.17) — the REPORTED side only: the .NET clock never reports a 60th second (A.1 item
  112); the directive's ARGUMENT side (a 60 in a seconds subfield, a time form to 86,400.99 under ON) is honoured —
  `LeapSecondDirectiveProcessor`, kb/Work PB65; **>>LISTING** / **>>PAGE** (§7.3.18 / §7.3.19) — no source listing
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
- **The counter registers' declared capacity — LINAGE-COUNTER, PAGE-COUNTER, LINE-COUNTER** (§8.4.3.14.4 GR1 /
  §8.4.3.15.4 GR1; NOT an Annex A.1 item — the report counters' size is simply unspecified, so this is a pinned
  implementor choice, kb/Work R26): all three registers are **unsigned integers** per their GRs, so
  `LOWEST-ALGEBRAIC` of any of them is **0** and `SMALLEST-ALGEBRAIC` is **1**. **LINAGE-COUNTER**: GR1 ties its
  size to "the page size specified in the LINAGE clause" — `HIGHEST-ALGEBRAIC(LINAGE-COUNTER OF f)` folds to the
  LINAGE literal's value directly (`LINAGE IS 66 LINES` → 66); with a data-name operand the page size is set at
  run time, and the compile-time capacity is the MAXIMUM that operand can specify, its PICTURE's all-nines.
  **PAGE-COUNTER / LINE-COUNTER**: no size in the standard, so COBOL.NET declares them **PIC 9(18)** — the
  all-nines of the runtime's 64-bit counter carrier (`ReportWriter.LineCounter`/`PageCounter`, both `long`), the
  one-constant-not-two discipline of §7 item 202 — and `HIGHEST-ALGEBRAIC` folds to 999,999,999,999,999,999.
  (§8.5.2.12 items 3/4/5 make the registers category-numeric DATA ITEMS, which is what admits them to the
  §15.43.3/§15.58.3/§15.83.3 r1 argument position at all.)
- **Floating-point value → fixed-point receiver: the conversion manner (§14.6.8.2 r1/r2/r4; kb/Work PB77,
  2026-08-18).** §14.6.8.2 r2 leaves "the manner in which the value is converted to a fixed-point value" to the
  implementor for a FLOAT-SHORT/-LONG/-EXTENDED sending item, and r1 treats an intermediate or standard-float
  sender "as if it had been converted to a fixed-point value"; COBOL.NET's FLOAT-LONG and FLOAT-BINARY-64 are the
  same `double` (item 22), and a §15.4.1 float-family returned value IS a binary64 (item 92), so ONE conversion
  serves all three: **the binary64's algebraic value, aligned by decimal point and truncated (or rounded, under a
  ROUNDED phrase) at the receiver's scale.** Inside the Int128 carrier the product v × 10^scale is formed in
  binary64 and rounded (`CobolFloat.ToScaled` — the double's own rounding of that product is part of the manner:
  a COMP-2 holding 8.2 moves 8.2 into PIC 9V9, a COMP-2 holding 1.15 moves 1.14 into PIC 9V99, exactly as the
  §14.6.8.2 r4 truncation of 1.1499999999999999 reads); **past the carrier the value's EXACT decimal expansion
  keeps supplying the low-order digits** (`CobolFloat.LowOrderDigits` — a COMP-2 holding 1.0E+40, whose exact
  value is 10000000000000000303786028427003666890752, moves 90752 into PIC 9(5); an in-carrier 1.0E+25 has always
  moved its exact 69664) — never a saturation sentinel, which has no capacity check downstream in a MOVE and used
  to store ITS low digits (884105727 / 03715). A non-finite value reaching a fixed-point landing with checking off
  (NaN, ±Infinity — EC-DATA-NOT-FINITE at the sending read under checking, §14.6.13.2 item 3) lands ZERO. The
  CHECKED landing (an arithmetic store under ON SIZE ERROR / EC-SIZE checking) keeps saturating so the receiver's
  capacity check raises the size error (item 179). Pinned by `2023/pb77_move_past_the_carrier` and
  `CarrierLandingFormTests`. ⚠ The in-carrier binary64 product is a rounding step a purely exact conversion would
  not take (8.2 → 8.2 rather than the exact 8.1); whether the manner should be the exact expansion at every
  magnitude, or the shortest-round-trip decimal, is kb/Work PB90 — a survey of GnuCOBOL's `cob_decimal_set_double`
  first, per the follow-GnuCOBOL-on-split-latitude decision.

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

> ⚖ **DETERMINATION — `FUNCTION LENGTH`'s `PHYSICAL` argument (§15.50.4 rule 8)** (2026-08-04; fix-queue PB24).
> Rule 8 splits on whether argument-1 "is physically located where it is defined": if it is not, the returned
> value "includes only the length of the implementor-defined pointer"; if it is, "LENGTH returns the same value
> that would be returned had the PHYSICAL argument not been specified".
> **COBOL.NET determines that a variable-length group IS physically located where it is defined**, so `PHYSICAL`
> is accepted and semantically transparent — it returns the rule-7 value. Grounds: this implementation exposes no
> addressable out-of-line pointer for a program to observe, and a group presents as a contiguous character image
> at its defined position; the alternative reading would require inventing a user-visible pointer width that
> nothing here exposes. Pinned by `2023/pb24_length_physical_keyword`, which asserts each PHYSICAL form equals
> its plain form. **The same determination governs `FUNCTION BYTE-LENGTH`'s `PHYSICAL` argument (§15.14.4 rule 7,
> "the length of argument-1 in number of bytes")** — accepted since 2026-08-18 (kb/Work PB61, FMT-15.14.2) and equal
> to the rule-6 sum, pinned by `2023/pb61_length_byte_length_rule_branches`.

> ⚖ **DETERMINATION — a table(ALL) intrinsic argument that ranges over NO occurrence (§15.3)** (2026-08-18; kb/Work
> PB62). §15.3: "The evaluation of an ALL subscript shall result in at least one argument, otherwise the result of
> the reference to the function-identifier is undefined." COBOL.NET defines the undefined case: the enumeration
> (`CobolTable.AllArgs`) raises EC-ARGUMENT-FUNCTION (set when checking is on, so a USE procedure can take it) and
> terminates the reference by that name either way — an empty list is never handed to a body whose result over
> nothing is itself undefined. An OCCURS DEPENDING range whose data-name-1 is 0, or a dynamic-capacity table at
> capacity 0, is how a program reaches it.

> ⚖ **DETERMINATION — `FUNCTION EXCEPTION-LOCATION`'s third part, "an implementor-defined identifier of the source
> line that contains the beginning of the statement" (§15.30.3 r2b3 / §15.31.3 r2b3)** (2026-08-18; kb/Work PB63,
> RV-15.31.3-L2.3). **The identifier is the line number of the statement's first token in the compilation unit's
> RESULTANT text** (§7.2 — the text after COPY and REPLACE processing, the text the compiler compiles), counted
> from 1. For a source element with no COPY/REPLACE it equals the source-file line. The clause's NOTE disclaims
> stability across compilations; COBOL.NET's value is stable for one compiler version and one resultant text.
> kb/Work **PB82** (a source-line map through the preprocessing chain) will revise this to the source-file line
> — and this row with it. Pinned by `2023/pb63_exception_location_procedure_field` (no COPY: source line ≡
> resultant line).

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
> **MEASURED 2026-08-09 — this register discharges 21 of the 199** (`python scripts/spec/audit_annex_a1.py`
> reports it, and the numbers below are its output, not a hand count): items 2, **22**, **33**, **56**, 59,
> **93**, **112**, **135**, 158, **171**, **179**, **180**, **188**, **202**,
> and the **storage-representation family 205–215** that V59 pinned (BINARY · the BINARY-CHAR family ·
> COMPUTATIONAL · INDEX · PACKED-DECIMAL: the byte width, the radix, the byte order and a worked example for
> each, since a COBOL developer reading this needs to know what lands on disk). Item **123** (the native
> Int128 intermediate) is documented voluntarily. **178 obligations remain** (`audit_annex_a1.py` is the
> count's owner — re-run it rather than trusting this sentence).
> ⚠ The MODULE-NAME row was filed under item **213** (which is USAGE NATIONAL) from 2026-08-05 until
> 2026-08-08 — the audit's number/element cross-check caught it; its true item is **135** (§15.65.4 r4).
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
| 205 | **USAGE BINARY clause — computer storage allocation, alignment and representation of data**, §13.18.60.4 GR4, required + documented | A BINARY item is a **two's-complement integer of the item's UNSCALED value, MOST SIGNIFICANT BYTE FIRST (big-endian)**, in a width pinned by the PICTURE's digit count: **1 byte** for 1–2 digits · **2** for 3–4 · **4** for 5–9 · **8** for 10–18 · **16** for 19–38. The implied decimal point occupies no storage (§13.18.40.4) and the sign is the two's-complement sign — there is no separate sign byte. **Alignment: none** — COBOL.NET performs no SYNCHRONIZED physical padding, so an item begins at the next byte and a group carries no implicit FILLER. The 16-byte tier exists because GR4's closing sentence requires storage "sufficient … to contain the maximum range of values implied by the associated decimal picture character-string" and a signed 19-digit picture (10^19−1) exceeds 2^63−1. Widths are sign-INDEPENDENT (GR12's precedent for the fixed-width usages). Worked example: `PIC 9(4) COMP VALUE 1234` occupies 2 bytes `04 D2`; `PIC S9(4) COMP VALUE -1234` occupies `FB 2E` (65536−1234). An UNSIGNED item holds the absolute value (§14.9.25.4 GR6d2b). `FUNCTION BYTE-LENGTH` reports exactly this width, and it is the width the item occupies in a group image, a record, a SORT key and a REDEFINES view — one representation at every byte boundary. |
| 206 | **USAGE BINARY-CHAR / -SHORT / -LONG / -DOUBLE — wider range than the minimum**, §13.18.60.4 GR12, optional | **Not provided.** Each usage holds exactly the GR12 minimum range for its width: CHAR ±128 / 0–255 · SHORT ±32768 / 0–65535 · LONG ±2^31 / 0–2^32 · DOUBLE ±2^63 / 0–2^64. |
| 207 | **USAGE BINARY-SHORT/-LONG/-DOUBLE and FLOAT-SHORT/-LONG/-EXTENDED — representation and length**, §13.18.60.4 GR13 + GR21, required + documented | BINARY-CHAR **1** byte, BINARY-SHORT **2**, BINARY-LONG **4**, BINARY-DOUBLE **8** — two's complement, big-endian, SIGNED and UNSIGNED the same width (GR21). FLOAT-SHORT is **IEEE 754 binary32 (4 bytes)**; FLOAT-LONG and FLOAT-EXTENDED are both **IEEE 754 binary64 (8 bytes)** — the standard's subset nesting (GR13) is satisfied since every binary64 value is expressible in binary64. A floating-point item has **no character image**: it never participates in a group image, a record or a REDEFINES view, and an attempt to use one there is rejected loudly rather than given an invented representation. |
| 208 | **USAGE COMPUTATIONAL clause — alignment and representation of data**, §13.18.60.4 GR6, required + documented | COMPUTATIONAL (and its COMP / COMP-4 synonyms) is **identical to USAGE BINARY** — radix 2, the item 205 width ladder and byte order, the same PICTURE-digit-count truncation discipline. **COMP-5 shares the representation but OWNS ITS FULL CONTAINER RANGE** (owner decision 2026-08-07, kb/Work R10): the value is bounded by the native two's-complement range of the byte width, never the picture's digit count, and it is therefore excluded from character images. The in-memory CARRIER follows the container so the whole range is representable: a signed item rides `long` (≤ 8-byte container) or `Int128` (16-byte); an **unsigned 8-byte container rides `ulong`** ([0, 2^64) exceeds `long`) and an **unsigned 16-byte container rides `UInt128`** ([0, 2^128) exceeds `Int128`). `FUNCTION HIGHEST-ALGEBRAIC` / `LOWEST-ALGEBRAIC` of such an item fold exactly the container's ends (§15.43.4 r2 / §15.58.4 r2 — the §15.43.4 NOTE's `BINARY-CHAR UNSIGNED → +255` row is the standard's own container-range illustration), and the folded maximum stores back into the item losslessly. Pinned by the golden `highest_algebraic_comp5_unsigned` and `AlgebraicFoldContainerAgreementTests`. |
| 211 | **USAGE INDEX clause — alignment and representation of data**, §13.18.60.4 GR10, required + documented | An index item is a **64-bit managed integer occurrence number** (8 bytes as reported by `FUNCTION BYTE-LENGTH`), never a byte offset. It has **no character image**: it takes no part in a group image, a record, a SORT key or a REDEFINES view, and only SET, SEARCH and relation conditions may reference it. A codec handed one fails loudly rather than inventing bytes. |
| 135 | **MODULE-NAME — the form of a module name, and the composition of the STACK list**, §15.65.4 r4 + r9, required + documented | **Names.** A program element is reported by its PROGRAM-ID / FUNCTION-ID; a METHOD by its METHOD-ID; a method's CURRENT (r7) is its class, the outermost element of its compilation unit. r4 permits any of these forms and the method-id form is chosen because it is the more informative. **STACK (r9) lists RUNTIME ELEMENTS, not compilation units** — entry 1 is CURRENT, every entry after it is what ACTIVATING would return within the previous one, the penultimate is TOP-LEVEL and the last is a single space, exactly as r9 composes it. ⚠ **Consequence, deliberate:** a CONTAINED (nested) program yields its outermost name TWICE (`MAIN;MAIN; `), because r9's first entry is outermost-granularity (r7) while the chain is element-granularity (r5), so for a nested program they coincide. The alternative — collapsing consecutive frames of one compilation unit, which §15.65.1's looser "a list of all the module names" wording would allow — was REJECTED (owner decision 2026-08-05) because it cannot represent RECURSION: three nested activations of one RECURSIVE program collapsed to a single entry while ACTIVATING in the same frame named the program, so STACK and ACTIVATING contradicted each other. A visible repeat can be read; missing frames cannot. |
| 134 | **MODULE-NAME — what is returned if the indicated module is not COBOL**, §15.65.4 r3, required + documented | **A single space.** The only non-COBOL runtime elements COBOL.NET can designate are the operating environment itself and a .NET host that drives a COBOL element without entering through `ProgramTable.RunMain`: with no COBOL element running every keyword returns one space; TOP-LEVEL (r10) returns one space when frame 0 was not pushed as the main (the host, not the environment, activated it — `ModuleStack.Name` case 4, kb/Work PB63 / RV-15.65.4-10); ACTIVATING and STACK's final entry are one space for the environment (r5 / r9). |
| 136 | **MODULE-NAME — how it is determined whether the program is a main program**, §15.65.4 r5, required + documented | **The main program is the element the run unit was started with** — the program `ProgramTable.RunMain` activates (the CLI's `--run` entry, a host's `RunMain` call): it is pushed as the run unit's `IsMain` frame; nothing else ever is. A program CALLed, INVOKEd or referenced as a function is never main, whatever its PROGRAM-ID; a RECURSIVE re-activation of the main is a CALLed frame. So ACTIVATING in the main is a single space (r5) and TOP-LEVEL is its name (r10). |
| 137 | **MODULE-NAME — the ACTIVATING value when the activation was in a nested program**, §15.65.4 r6, required + documented | **The nested (contained) program's OWN name**, i.e. the runtime element that executed the CALL / INVOKE / function reference — never its outermost containing program. r6 grants either; the element form is chosen because r9 defines every STACK entry after the first as "what ACTIVATING would return within the previous module", so ACTIVATING and the STACK chain agree by construction (`ModuleStack.Name` builds STACK from the same frame field): a separately-compiled program CALLed from contained `INR` inside `TOP` reports `ACTIVATING = INR` and `STACK = EXT;INR;TOP; ` (pinned by `2023/pb63_module_name_lengths`). |
| 133 | **MODULE-NAME — the length of the returned value item and whether it may have trailing spaces**, §15.65.4 r1/r2, conditionally required (documented voluntarily) | **A dynamic-length alphanumeric item with no trailing spaces**, r1's stated exception included (a single space for ACTIVATING in a main program, and STACK's final single-space entry — content, not padding). The name is delivered as its exact string; there is no fixed width between the module stack and the expression, so §15.65.4 r2's "does not fit" antecedent (EC-BOUND-FUNC-RET-VALUE) is structurally unreachable — pinned by `ModuleStackInvariantTests` (kb/Work PB63 / RV-15.65.4-2), which also fixes the r10 host-boundary arm above. |
| 209 | **USAGE BIT clause — alignment and representation of data**, §13.18.60.4 GR5 + §8.5.1.6.3, required + documented | A `USAGE BIT` item **occupies bits**, as GR5 requires. Bits per character position is **8** — §8.1.2 leaves it implementor-specified, and 8 is what makes it agree with DISPLAY's one byte per character position. Alignment follows §8.5.1.6.3 exactly: a bit item immediately following an elementary bit item **of the same level** takes the next bit position (they share a byte); any other bit item starts at the first bit of the next available byte; implicit filler advances to the next item's natural boundary and fills a trailing partial byte to an integral number of characters, and §15.50.4 r5 counts that filler. In a record image a bit run is **packed high-order bit first** — §8.5.1.6.3 numbers positions from "the first bit position" — with trailing filler bits zero. ⚠ The item's VALUE CARRIER is a `'0'`/`'1'` string, which is not observable to a COBOL program and is not a conformance claim; what is claimed is the SIZE, ALIGNMENT and IMAGE above. A boolean item with **no** USAGE clause is a different case: §13.18.60.3 SR13(b) implies DISPLAY and GR7 makes it one alphanumeric character per boolean position. |
| 215 | **USAGE PACKED-DECIMAL clause — computer storage allocation, alignment and representation of data**, §13.18.60.4 GR11, required + documented | A PACKED-DECIMAL item is **binary-coded decimal, two digits per byte, most significant first**, with a **trailing sign nibble** in the low half of the last byte: `C` positive, `D` negative, `F` for an item with no operational sign (the convention IBM, Micro Focus and GnuCOBOL all write, so a data file interchanges). The digit run is padded on the left with a zero nibble when needed, giving **`digits / 2 + 1` bytes**; the implied decimal point occupies no nibble. Under the COBOL-2023 **WITH NO SIGN** phrase the sign nibble is not reserved at all — every nibble is a digit and the width is **`ceil(digits / 2)`**. ⚠ The two forms can occupy the SAME number of bytes: 3 digits is 2 bytes either way, laid out `12 3C` signed and `01 23` unsigned-no-sign. **Alignment: none** (no SYNCHRONIZED padding). Worked example: `PIC 9(4) COMP-3 VALUE 1234` occupies 3 bytes `01 23 4F`; `PIC S9(4) COMP-3 VALUE -1234` occupies `01 23 4D`. Decoding accepts the universal readings — `B` and `D` negative, everything else positive — so a file written by another COBOL system reads correctly. |
| 87 | **FORMATTED-CURRENT-DATE function — accuracy of the returned time portion**, §15.38.4 r2 (Annex A.1 lists it under "Returned value rule 1"; the obligation's text is r2 — do not "correct" it the other way), required + documented | **The run unit's clock TICK — 100 ns, i.e. 7 significant fraction digits**, exactly SECONDS-PAST-MIDNIGHT's precision (item 171) and the same injectable `RunUnit.Clock` seam (`COBOLNET_CLOCK`-deterministic; the code once read `DateTimeOffset.Now` directly and no longer does). The value is carried at 9 fraction digits as `ticks × 100` — integer arithmetic, no binary64 conversion on the way (kb/Work PB65, 2026-08-18) — so a format's `s` fraction field renders the first 7 digits from the clock and ZEROS beyond them, up to the §15.3.3.2 maximum of 18; nothing is truncated or diagnosed. The date portion is the clock's local calendar date; the UTC-offset portion (`+hh:mm`) is the clock's offset. Pinned by `CobolDateWindowingTests` through the same clock seam. |
| 144 | **RANDOM function — seed value when no argument on first reference**, §15.75.3 r4, required (the seed VALUE need not be documented — this row is VOLUNTARY, the item-92 precedent, kept because the determination is load-bearing for users) | **Per-process OS entropy — sequences are deliberately NOT reproducible across runs without an explicit seed argument.** The unseeded generator is .NET's parameterless `Random` (xoshiro256** seeded from OS entropy); there is no fixed seed value, which is itself the determination §15.75.3 r4 requires. ⚖ Decided 2026-08-09 under the owner's standing latitude rule (follow GnuCOBOL where the standard leaves latitude): **surveyed, not assumed** — GnuCOBOL 3.x `cob_intr_random` seeds its first unseeded reference from `get_seconds_past_midnight() * (module-pointer bits)` (libcob/intrinsic.c, read 2026-08-09), i.e. per-process entropy, the SAME choice — and IBM documents the unseeded seed as unpredictable, so the vendors do not even split. A program needing a reproducible sequence writes `FUNCTION RANDOM(seed)` on first reference, which fully determines the sequence per r3/r5. |
| 145 | **RANDOM function — the subset of the domain of argument-1 that yields distinct sequences**, §15.75.4 r3 ("shall include the values from 0 through at least 32767"), required + documented | **Every seed 0 through 2,147,483,647 selects its own generator state; the seeds 0 through 65,535 are MEASURED pairwise distinct** — `RandomSeedSubsetTests` compares the first three draws of every seed in that range and finds no two alike, which discharges the required floor 0..32,767 with a margin (only what is measured is claimed: distinctness above 65,535 is the generator's design, `System.Random(int)` seeding injectively on the value, not a measurement). A seed at or above 2³¹ ALIASES to `seed AND 0x7FFFFFFF` (2,147,483,648 → 0, 2,147,483,649 → 1 — pinned), a negative seed is EC-ARGUMENT-FUNCTION (§15.75.3 r2, item 144's sibling row). The generator is `System.Random(int)` (the .NET Knuth subtractive sequence, `CobolIntrinsics.Random(long seed)`); the same seed reproduces the same sequence within and across processes on a given .NET runtime (§15.75.4 r2). |
| 112 | **LEAP-SECOND directive — whether standard numeric time form values greater than or equal to 86,400 may be reported**, §7.3.17 GR4 + §15.80.3 r4 + §15.3.3.3, required + documented | **NO — never.** The determination §15.80.3 r4 requires ("The implementor defines whether a value greater than or equal to 86,400 may be returned from the SECONDS-PAST-MIDNIGHT intrinsic function") is answered in the negative: .NET's `DateTime` cannot represent a leap second, so the day's tick count is always < 86,400 seconds and the returned value satisfies §7.3.17.4 GR4's ON-mode bound ("greater than or equal to zero and less than 86,401") on BOTH directive branches. `>>LEAP-SECOND ON` is recognized (`LeapSecondDirectiveProcessor`, kb/Work PB65) and governs the ARGUMENT side of the directive as §15.3.3.3 and §7.3.17.4 GR4 require: a formatted-time argument may carry 60 in its seconds subfield (`SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960")` = 86,400; TEST-FORMATTED-DATETIME accepts it), a standard numeric time form value is bounded at 86,401 (FORMATTED-TIME / FORMATTED-DATETIME present 86,400 as 23:59:60; COMBINED-DATETIME accepts it). The REPORTED side stays this negative determination on both directive branches. Golden pair `pb65_leap_second_on` / `pb65_leap_second_off`. Range endpoints pinned by `CobolDateWindowingTests.SecondsPastMidnight_Range_StandardNumericTimeForm`; see item 171 for the precision half of the same function. |
| 171 | **SECONDS-PAST-MIDNIGHT function returned value — precision**, §15.80.3 r3, required + documented | **100 nanoseconds — 7 fraction digits**, the .NET `DateTime` tick resolution: the function returns the day's TICK count as the unscaled value at scale 7 (the renderer's documented contract). Range **[0, 86 400)** — LEAP-SECOND's §7.3.17 default OFF is the only supported mode (§15.5.5), so a value ≥ 86 400 is unreachable. Pinned by `CobolDateWindowingTests` (05:14:27.8124791 → 188 678 124 791 ticks, plus both range endpoints) and read through the ONE injectable `RunUnit.Clock` seam, so the precision and the clock's determinism are the same mechanism. |
| 56 | **DISPLAY statement — data conversion**, §14.9.11.4 GR1, required + documented | A numeric item converts to its **PICTURE-digit image**: the digit positions the description declares, leading zeros intact, the decimal point and sign rendered per the item's description. ⛔ **A BinaryCapacity item (COMP-5 / the BINARY-CHAR family) holding a beyond-PICTURE value displays the PICTURE-digit image — the value modulo 10^digits — NOT the full container value** (owner decision 2026-08-08, kb/Work R13: the vendors are split — IBM renders the full container value, GnuCOBOL truncates to the PICTURE — and COBOL.NET **follows GnuCOBOL**). The full container value remains reachable through the spec-fixed MOVE path (§14.9.25.4 GR6a) into a wider receiver. Pinned by `comp5_display_beyond_picture` (446744073709551615 displayed for a stored 2^64−1; 18446744073709551615 after MOVE to PIC 9(20)). |
| 22 | **CHAR-NATIONAL function — which one of the multiple characters is returned**, §15.16.4, required + documented | When the national program collating sequence assigns several characters to one position (the ALPHABET clause's `ALSO` phrase), CHAR-NATIONAL returns **the FIRST character defined for that position** — deterministic, in the alphabet definition's own written order (`CobolIntrinsics.CharNational` over the emitted `__COLLATE_NAT` table; `NationalCollation.CharAt` is the one position→character reader, so the choice cannot vary by call site). |
| 180 | **SMALLEST-ALGEBRAIC function — usage of the argument when native arithmetic is in effect**, §15.83.3 r4, required + documented | **Floating-point usages are not accepted** as argument-1 (COBOLNET1516): a smallest positive increment for an IEEE float is exponent-dependent — a property of the stored VALUE, not of the PICTURE — so no meaningful description-derived answer exists, and COBOL.NET rejects rather than inventing one. Every fixed-point numeric usage is accepted, and the returned value is **10^(−scale)** (§15.83.4 RVR2 — `S999` → +1, `99V9(3)` → 0.001, `S9PP` → +100, `BINARY-CHAR` → +1). |
| 188 | **SPECIAL-NAMES ALPHABET clause, UCS-4, UTF-8 and UTF-16 phrases — correspondence with the native character set**, §12.3.7.4 GR7, required + documented | The native alphanumeric and national repertoires are **UTF-16, one code unit per character position** (the D-N1 substrate). The UTF-16 phrase's correspondence with the native set is therefore the **BMP identity** — code point = code unit, and §8.5.1.4's denial of surrogate-pair recognition makes the supplementary-plane codepoint/code-unit divergence unreachable. UCS-4 and UTF-8 name **coded character sets only** (GR7 g/h + Table 6 — no collating sequence), converted to/from the native UTF-16 form at the file boundary. |
| 123 | **Native arithmetic — techniques used, intermediate data item**, §8.8.1.3 / §11.9.5 / §14.7.7, required (the internal procedure need not be documented) | **The native intermediate data item is a scaled `Int128`** — a 128-bit two's-complement integer holding the unscaled value with a compile-time decimal scale (magnitude bound ≈ 1.7 × 10^38; every ≤ 31-digit operand and every aligned sum of them is exact). Any expression with a floating-point operand evaluates entirely in IEEE binary64 instead; a nested quotient carries up to 14 guard fraction digits and rounds once, at the final transfer (§14.7 NOTE 1). One value — the maximum of an unsigned 16-byte COMP-5 item, 2^128−1 — exceeds the intermediate range while being storable in an item; see item 179 for what an arithmetic reference to it does. |
| 70 | **Fatal exception condition — whether execution continues, and how a numeric receiving operand is affected, when checking for the condition is NOT enabled: the arithmetic size error condition without a SIZE ERROR phrase (EC-SIZE-TRUNCATION, §14.7.5 no-phrase rule 4) and a non-finite value in a fixed-point landing (EC-DATA-NOT-FINITE)**, §14.6.13.1.3 item 8, required + documented (this row discharges the numeric-store family; other fatal conditions' dispositions are documented with their statements) | **Execution continues with the next statement, and the resultant identifier receives the LOW-ORDER digits of the result aligned at its scale** — the classic high-order truncation (`CobolNum.Store`), for EVERY value carrier: the standard-decimal intermediate (`CobolDec.ToUnscaled`, kb/Work PB74), the native exact family (`CobolIntrinsics.Rescaled`'s unchecked arm — `CobolNum.RescaleStoreCap`), and a binary64 (`CobolFloat.ToScaledUnchecked` / `CobolIntrinsics.FromDouble`'s unchecked form — the exact decimal expansion past the Int128 carrier; kb/Work PB77, 2026-08-18). Before PB77 a value past the carrier landed the saturation SENTINEL and the store kept its low digits: `COMPUTE X5 = D40` (a COMP-2 holding 1.0E+40, PIC 9(5)) stored 03715 — the low digits of `Int128.MaxValue` — where the result's are 90752. A non-finite binary64 (NaN, ±Infinity) landing in a fixed-point receiver with checking off stores ZERO. The other resultant identifiers of a multiple-receiver statement are stored normally. With the phrase or with EC-SIZE checking enabled the disposition is §14.7.5's own — the receiver is left unchanged and the size error condition is processed — because the CHECKED landing saturates so the capacity check sees it (item 179, kb/Work PB13). Pinned by `2023/pb77_move_past_the_carrier` (the `CMP-*` rows against the `SIZE-*` rows) and `CarrierLandingFormTests`. |
| 179 | **Size error condition — whether the intermediate data item's value range is checked**, §14.7.5 item 5, required + documented | **Checked, in two places.** (1) Under an ON SIZE ERROR phrase, multiplication, addition and subtraction are overflow-checked at the `Int128` intermediate boundary (`MulChecked`/`AddChecked`/`SubChecked` → the size error condition, §14.7.5 case 5); without the phrase they wrap unchecked, exactly as §14.7.5's "if … the implementor defines that the range … is to be checked" conditions the case on the implementor's determination. Reachable: `HIGHEST-ALGEBRAIC` of `PIC S9(19) COMP-5` is exactly `Int128.MaxValue`, where `ADD 1` overflows the intermediate itself. (2) An arithmetic OPERAND whose value exceeds the intermediate range — possible only for an unsigned 16-byte COMP-5 item holding a value above 2^127−1 — raises the size error condition on reference (`CobolNum.Widen`), never a silent wrap: with ON SIZE ERROR the phrase takes it, without it the run fails loudly naming the operand. The value-preserving paths (MOVE, DISPLAY, relation conditions) are NOT arithmetic and carry the full container range. Pinned by `highest_algebraic_comp5_unsigned` (the `S19-ADD1=SIZE` / `W19-ADD1=SIZE` legs). **(3) An SDIDI-carried value entering the Int128 carrier as an INTERMEDIATE** — an intrinsic argument, an aligned arithmetic operand, a subscript, an integer argument (kb/Work PB69, 2026-08-18) — whose magnitude the carrier cannot hold at the required scale raises **EC-SIZE-OVERFLOW** (`CobolDec.ToUnscaledIntermediate`), never the modular low-order digits; under native arithmetic such a value arises only from an integer power past the carrier (the documented double approximation — an intrinsic with such an argument computes on its SDIDI body instead, a relation compares it exactly, an arithmetic operation with it evaluates on the SDIDI; a store of it is the checked §14.7 transfer, SIZE ERROR). Pinned by `2023/pb69_native_power_past_the_carrier` and `CobolDecDivisionTests`. |
| 202 | **Time formats and corresponding function values — maximum number of digit positions in the decimal fraction of the seconds subfield**, §15.3.3.2, required + documented | **18.** §15.3.3.2 requires the implementor to define this maximum and requires it to be **≥ 9**; 18 is chosen because it is exactly where the value stops being exactly representable — a fraction of 19 or more digits overflows the emitter's `(long)Pow10.AsWide(width)` and prints garbage, so the documented bound and the representable range are the SAME number rather than two constants that can drift apart. Enforced by `DateTimeFormatGrammar.MaxFractionDigits`, which the §15.3 format recogniser consults, so a format with a longer fraction is rejected at COMPILE time (`COBOLNET1631`) instead of fabricating a value. |
| 92 | **Characteristics and representation of a numeric intrinsic function's returned value, and its text image**, §15.4.1 + §14.9.11.4 GR1, required — documentation NOT required by A.1 (voluntary); partially addresses A.1-56 | §15.4 places a function's returned value in a **temporary elementary data item**, and under NATIVE arithmetic §15.4.1 makes "the characteristics and representation of the returned value … defined by the implementor"; §14.9.11.4 GR1 independently makes any conversion between a DISPLAY operand and the device implementor-defined. **COBOL.NET's determination: a numeric function result used as TEXT renders in the LITERAL form of its value** — the significant digits with **no leading-zero padding**, a leading `-` when negative (never a zoned over-punch, because this is not a stored item), and a decimal point followed by exactly the result's scale in fraction digits when the value is scaled. A FLOAT-valued function (SQRT, trig, financial) renders through the same shortest-round-trip `CobolFloat.Display` a COMP-2 item does, so the function and an item holding its value agree. Examples: `DISPLAY FUNCTION ORD("A")` → `66` (§15.70.1 — ordinal position, lowest is 1); `DISPLAY FUNCTION MIN(3 -14 0 8 -3)` → `-14`; `MOVE FUNCTION ORD("A") TO PIC X(10)` → `66` left-justified, space-filled (§14.9.25.4 GR6). **⛔ The rule exists to make the compile-time FOLD unobservable.** An intrinsic over constant arguments folds to a numeric literal and a literal renders as its own text (`DISPLAY 42` → `42`); had a computed result been given a padded fixed width instead, `DISPLAY FUNCTION LENGTH(X)` and `DISPLAY FUNCTION ORD(C)` would print in visibly different formats for no reason a COBOL programmer could see. One rule, both paths — pinned by the golden `da2_function_as_text`, whose first four lines are two fold/compute pairs of the same value. Note this deliberately differs from GnuCOBOL, which zero-pads a computed `ORD` to its field width while printing a folded `MAX` minimally; the standard permits either, and internal consistency was preferred over matching a compiler that is not self-consistent here. **The NUMVAL family's NATIVE working scale is part of the same §15.4.1 determination (PB60):** under native arithmetic NUMVAL/NUMVAL-C/NUMVAL-F's temporary carries `max(context scale, 6)` fraction digits capped by the Int128 headroom, where the CONTEXT scale is the arithmetic receiver's (COMPUTE), the MOVE receiver's (`MoveEmitter.SenderContext`), or the opposing relation operand's static scale (`ConditionRenderer.StaticScaleOf`) — one determination, every channel, so a program can no longer see three different values for one call; a genuinely receiver-less reference (DISPLAY of the bare call) renders at the 6-digit floor. Fraction digits beyond the working scale truncate — the §15.4.1 native-arithmetic latitude, pinned by `pb60_numval_one_scan`'s CM/MV/EQ legs. **NUMVAL-F under native arithmetic is the FLOAT family's case (PB60 / RV-15.69.4-2):** §15.69.4 r2 makes its native returned value an approximation, and the approximation IS a binary64 wherever no fixed-point scale governs the render — a receiver-less context (a DISPLAY/STRING operand, a relation operand, an argument, a subscript) or a float receiver — rendered through the same shortest-round-trip `CobolFloat.Display` a COMP-2 item uses and compared natively (§8.8.4.2.4); a fixed-point ARITHMETIC receiver and a MOVE sender (the receiver's scale is known — `ReceiverContext.MoveSender`) keep the EXACT Int128 parse at the receiver-capped `max(context scale, 9)` working scale, so `MOVE FUNCTION NUMVAL-F("1.5E-8") TO PIC V9(9)` stores 0.000000015 digit-for-digit. Pinned by `pb60_numvalf_native_channels`. **This determination is NATIVE-only.** Under `ARITHMETIC IS STANDARD-DECIMAL` (and 2002's STANDARD) there is no working scale and no latitude: §15.4.1 places the returned value in an SDIDI temporary, §15.67.4 r1 / §15.68.4 r1 fix it as "the numeric value represented by argument-1" and §15.69.4 r3 says so outright for NUMVAL-F, so the value is the parsed value EXACTLY at its own scale in every channel (`NUMVAL("1.2345678")` renders 1.2345678; a 34-digit argument is exact) — pinned by `pb60_numval_standard_decimal`. |
| 92 | **Evaluation of an exact-family intrinsic whose ARGUMENT is floating-point**, §15.4.1, required — documentation NOT required by A.1 (voluntary) | §15.4.1 makes the characteristics and representation of a returned value "defined by the implementor" under native arithmetic, and each of these functions is defined by an equivalent arithmetic expression over its own operands. **COBOL.NET's determination: when ANY argument renders as floating-point (USAGE COMP-1 / COMP-2 / FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED), the function's equivalent arithmetic expression is evaluated in IEEE binary64** and the result is then delivered to the receiver by the ordinary rules — unquantized into a floating receiver, quantized through `FromDouble` at the working scale into a fixed-point one. With an all-fixed-point argument list the function stays base-10 EXACT over scaled `Int128` and nothing changes. The determination is forced rather than chosen: a floating-point argument is a legal class-numeric argument (§8.5.2.12 item 2 + §8.5.2.1 Table 2), and once an operand arrives as binary64 there is no exact value left for an exact evaluation to preserve. Sign discipline is the spec's, not the carrier's — MOD floors and REM truncates (§15.64.4 / §15.77.4), INTEGER floors and INTEGER-PART truncates (§15.44 / §15.49) — so `MOD(-7,3)` is 2 and `REM(-7,3)` is -1 in binary64 exactly as in `Int128`. Pinned by the golden `pb2_float_argument_exact_family`. Before this was implemented, such a program did not merely compute differently: it failed to compile, surfacing a raw Roslyn `CS1503` from the generated C#. |
| 33 | **Computer's coded character set — correspondence between alphanumeric and national characters**, §14.9.25 MOVE GR6 · §8.8.4.2.11 locale-based comparison · §15.26.4 r1/r3 (DISPLAY-OF) · §15.66.4 r1/r3 (NATIONAL-OF), required + documented | Both native repertoires are **UTF-16, one code unit per character position** (item 188's substrate — note 188 itself answers a DIFFERENT question, §12.3.7.4's ALPHABET-phrase correspondence; THIS row is the character correspondence item 33 demands), so the correspondence is the **TOTAL IDENTITY, both directions**: every alphanumeric character's national correspondent is the same code unit, and vice versa. Consequences, each deliberate: **(1)** DISPLAY-OF/NATIONAL-OF **argument-2 is accepted and vacuous** — §15.26.4 r2 / §15.66.4 r2 substitute only for a character "that has no corresponding" representation, and under a total correspondence no such character exists (the §15.26.3 r2 / §15.66.3 r2 class and one-position screens still bind the argument); **(2)** the r3 **EC-DATA-CONVERSION is unreachable by declaration** from any character pathway — a conforming determination (r1 grants the correspondence to the implementor), not an accidental dead branch; **(3)** §14.9.25.4 GR6's alphanumeric→national MOVE conversion is the same identity (the reverse direction is not a MOVE at all — §14.9.25.3 SR10 / Table 16 forbid a national sender into an alphanumeric receiver, COBOLNET0819, and DISPLAY-OF is the sanctioned conversion); **(4)** CONVERT's NAT→ANUM and ANUM→NAT character legs (§15.19.4 r1/r3, whose NOTES name DISPLAY-OF/NATIONAL-OF as "the same facility") likewise never substitute. ⚠ The 8-bit BYTE serialization of usage DISPLAY (CONVERT's ANUM→HEX/BYTE legs) is a **separate** §8.1.2 NOTE 2 determination (item 209's one byte per DISPLAY position), and it is PARTIAL: a code unit above 0xFF has no one-byte image. **Decided disposition (2026-08-09, RV-15.19.4-2): the serialization substitutes `'?'` (0x3F) and sets EC-DATA-CONVERSION** — §15.19.4 r2 carries no substitution sentence because the standard's model assumes a total serialization; where ours is partial, the r1/r3-analogous substitution+EC is the smallest extension of the standard's own machinery, visible under checking and never silent (`CobolIntrinsics.ByteSub`; pinned by `Convert_ByteSerializationSetsDataConversion_WhenChecked_2023`). CONVERT's ANY source reads an item's RAW STORAGE bytes per the documented representations themselves (items 205/208/209/215, D-N1 UTF-16BE for national — `OperandText.AsStorageImage`, the one storage channel). Implemented in `CobolIntrinsics.Repertoire` (the identity, both directions); pinned by the golden `pb59_repertoire_identity` and the re-derived `national_intrinsics`. |
| 93 | **Function returned values — returned value length exceeds implementor-defined limits**, §15.4 Returned values, required + documented (the value returned on excess shall be documented) | The maximum length of a returned value is **8,191 character positions** — the §8.3.3.4.3 SR1 / §8.8.3.2 SR2 literal-and-concatenation maximum, reused so the bound and the language’s own largest string constant are the SAME number (the BOOLEAN-OF-INTEGER row documented this per-function first; it is now the project-wide determination). When a function’s returned value would exceed it, **EC-ARGUMENT-FUNCTION is set to exist** exactly as §15.4’s own sentence prescribes, and **the value returned is the §15.3 checking-off default — a zero-length value** (with checking enabled the raise is fatal and no value is returned). Enforced where a result can actually outgrow it: `CobolIntrinsics.BaseConvert` (the unbounded base-2 re-expression — a PIC X(2000) ALL "F" base 16→2 input owes 8,000 digits and still converts; 8,192+ raises), `CobolIntrinsics.BooleanOfInteger` (argument-2 > 8,191). §15.12.3 r3’s input-side maximum has no separate A.1 number — it is bounded by this determination plus the 8,191-position item bound, and the row says so rather than inventing one. |
