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
except the optional/processor-dependent facilities listed as **not supported** below. Per §4.2.6, an
implementation need not implement processor-dependent elements for which support is not claimed; per §4.2.7, an
optional element is implemented only when support is claimed. Of the four **documented non-support
facilities** (§4), SCREEN handling is recognized at compile time with the named COBOLNET1560 warning; MCS
(SEND/RECEIVE), COMMIT/ROLLBACK, and VALIDATE are today a generic parse error — their named recognize-and-warn
diagnostics are the tracked PHASE-13 Wave H code half (they ship before the §4.2.6 warning-mechanism claim can
be made for those three).

## 2. Annex A.3 — processor-dependent language element disposition

Each row is one A.3 item. **Claimed** = standard-conforming support is provided. **Not claimed** = not implemented
(a §4.2.6 warning is emitted where the element is syntactically detectable). **N/A** = the element is a property
of an unsupported facility.

| A.3 # | Element | § | Disposition | Note |
|---|---|---|---|---|
| 1 | Significand/exponent >31/>3 digits in float literal / numeric-edited PICTURE | 13.18.40 | Not claimed | External floating-point `E` PICTURE is staged; standard IEEE usages cover the fixed formats |
| 2 | ARITHMETIC IS STANDARD-BINARY | 11.9.5 | Not claimed | Obsolete feature (A.3 NOTE 1); the native/standard-decimal modes are provided |
| 3 | ARITHMETIC IS STANDARD-DECIMAL | 11.9.5 | **Claimed** | Full SDIDI consumption (P10): `CobolDec` engine, decimal128 range ECs |
| 4 | Asynchronous messaging (MCS) facility | E.3.2 | **Not claimed** | Documented non-support (§2.4) — inter-run-unit communication not provided |
| 5 | BLOCK CONTAINS clause | 13.x | **Claimed** (inert) | Accepted; no effect on the managed I-O model (A.3 item 5 sanctions this) |
| 6–7 | Commit and rollback facility / its devices | E.3.2 | **Not claimed** | Documented non-support (§2.4) — no transaction manager |
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
| 41–42 | Cultural collating for keys / multiple alt keys with differing collating | 13.x | Not claimed | Depends on the ISO/IEC 14651 locale module (item 25) |
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
- **Case mappings (UPPER-CASE / LOWER-CASE, §15.97.4 GR4 / §15.57.4)**: absent a locale, the case correspondence is
  **implementor-defined** (§15.97.4 GR4). COBOL.NET uses the .NET invariant Unicode case tables. Because the mapping
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
  nonzero exit).

## 4. Documented non-support facilities (§4.2.6 / §4.2.7 / §4.2.13)

The following whole facilities are **not implemented**. SCREEN handling (item 4) is recognized at compile time
and reported with the named COBOLNET1560 warning per §4.2.6; MCS, COMMIT/ROLLBACK, and VALIDATE (items 1–3) are
today a generic parse error — their named recognize-and-warn diagnostics (the COBOLNET1560 band) are the tracked
PHASE-13 Wave H code half, after which every row here meets the §4.2.6 warning mechanism / §4.2.13 obsolete
flagging:

1. **Message Control System (MCS) asynchronous messaging** (E.3.2 item 1 / A.3 item 4): `SEND`, `RECEIVE`,
   and MESSAGE-TAG data items (the ISO/IEC 1989:2023 MCS surface — the pre-2002 COMMUNICATION SECTION is not part
   of this edition). Processor-dependent; not provided.
2. **Commit and rollback** (E.3.2 item 2 / A.3 items 6–7): `COMMIT`, `ROLLBACK`. No transaction manager.
3. **VALIDATE facility** (§14.9.50, §13.16–13.18, F.2 item 5): the `VALIDATE` statement, the validation clauses
   (`CLASS`/`DEFAULT`/`DESTINATION`/`INVALID`/`PRESENT WHEN`/`VARYING`), and EC-VALIDATE. An obsolete optional
   element (§4.2.13).
4. **Screen handling** (§13.9, optional §4.2.7): the SCREEN SECTION, ACCEPT/DISPLAY format-3 (screen), and the
   EC-SCREEN family. Not provided.

## 5. Maintenance

Update this document in the same change set as any change to the supported surface (a new usage, a facility
newly implemented or newly documented as non-support, an I-O status determination). The COBOLNET1560-band
warning sites are the code-side counterpart — keep the two in sync. This file is referenced by
`docs/VERSION_CHANGE_REFERENCE.md` (the edition-change checklist) and by `docs/DOC_INDEX.md`.
