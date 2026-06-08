CobolSharp COBOL INSPECT, STRING, UNSTRING & Text‑Processing Engine Architecture (CIL‑Only)
===========================================================================================

> **STATUS BANNER — read first.**
> **Type:** subsystem design reference for COBOL text-processing statements (INSPECT / STRING / UNSTRING).
> **Implementation status (verified vs `src/`, 2026-06-07):** **~70–90% implemented and shipping.**
> INSPECT (TALLYING / REPLACING / CONVERTING) — including the ISO 6.17.3 single left-to-right comparison
> cycle, BEFORE/AFTER region delimiters, LEADING/FIRST/TRAILING/ALL/CHARACTERS operands, and **BACKWARD**
> (ISO §14.9.21, COBOL-2002) — STRING, and UNSTRING are all live through the binder → lowerer → CIL-emitter
> → runtime pipeline. **Design-only / aspirational parts below:** NATIONAL/UTF-16 inside the
> text-processing statements (the INSPECT runtime currently operates on a `byte[]` storage area as ASCII);
> the explicit AOT/WASM-safe guarantees; and the dedicated debugger visualization. The data model is migrating
> to typed-native (`CobolString` / `string`), under which text processing on `PIC X` becomes `string`-based;
> see CURRENT TRUTH below.
> **Naming note:** there is **no `StringEngine` / `ExecutionContext.StringEngine` class** in the codebase —
> that name is a conceptual label, not a real type. The **actual** components are:
> `Semantics/Bound/Binding/StringStatementBinder.cs`, `CodeGen/Lowering/StringLowerer.cs`,
> `CodeGen/Emission/CilStringEmitter.cs`, and `Runtime/InspectRuntime.cs` (plus STRING/UNSTRING runtime helpers
> in `Runtime/StorageArea.cs` and `Runtime/CobolProgram.cs`). References to `StringEngine.*` below are read as
> "the text-processing runtime/emitter pair."
> **Stack:** .NET 10 / C# 14. **Backend: CIL-only via Mono.Cecil** — there is NO custom VM and NO bytecode
> interpreter (a Roslyn C# backend is a FUTURE additive option, Stage-5; Cecil is the oracle).
> **Plan SSOT:** `docs/MASTER_PLAN.md`. **Doctrine:** `PROMPT.md`. **Data-model migration:**
> `docs/DATA_MODEL_ARCHITECTURE.md` + `docs/RECORD_STRUCT_STORAGE_DESIGN.md`.

Purpose
-------
Define the authoritative design intent for:
- INSPECT (TALLYING, REPLACING, CONVERTING)
- STRING statement
- UNSTRING statement
- DELIMITED BY rules
- POINTER and TALLYING semantics
- NATIONAL vs DISPLAY text processing (design target; see status banner)
- Overlapping source/target behavior
- Exception routing (ON OVERFLOW, ON EXCEPTION)
- Integration with the runtime text-processing helpers
- AOT/WASM‑safe text operations (design target)
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s text‑processing facilities on .NET.

------------------------------------------------------------
SECTION 0 — ACTUAL IMPLEMENTATION MAP (authoritative)
------------------------------------------------------------

The pipeline is the standard CobolSharp flow: **parse → bind → lower to IR → emit CIL (Mono.Cecil) → run
against byte[] storage**. Concretely, for the three statements:

- **Bind:** `Semantics/Bound/Binding/StringStatementBinder.cs` produces the bound nodes
  (`BoundStringStatement`, `BoundUnstringStatement`, `BoundInspectStatement`).
- **Lower:** `CodeGen/Lowering/StringLowerer.cs` lowers the bound nodes to IR
  (`IrStringStatement`, `IrUnstringStatement`, `IrInspectTallying` / `IrInspectReplace` /
  `IrInspectConvert`, with `IrInspectTallyOp` / `IrInspectReplaceOp`). All TALLYING operands of one
  INSPECT are grouped into a *single* IR instruction because ISO 6.17.3 GR 8 makes them one comparison
  cycle (see §4.5).
- **Emit:** `CodeGen/Emission/CilStringEmitter.cs` emits CIL via Mono.Cecil
  (`EmitStringStatement`, `EmitUnstringStatement`, `EmitInspectTally`, `EmitInspectReplace`,
  `EmitInspectConvert`). The POINTER value is decoded from its COBOL numeric storage with
  `PicRuntime.DecodeNumeric`, kept in an `Int32` local for the duration of the statement, then
  re-encoded back into the user's POINTER field.
- **Runtime:** `Runtime/InspectRuntime.cs` implements the INSPECT comparison cycle over a `byte[]`
  storage area treated as ASCII text; STRING/UNSTRING runtime helpers live in `Runtime/StorageArea.cs`
  and `Runtime/CobolProgram.cs`.

**Tests:** `tests/CobolSharp.Tests.Integration/StringTests.cs` plus the over-lenient strictness suites
`tests/CobolSharp.Tests.Unit/Overlenient/M417_OverlenientUnstringTests.cs` and
`M418_OverlenientInspectTests.cs`. NIST CCVS coverage exercises STRING/UNSTRING/INSPECT in the NC/SM suites.

> CURRENT-TRUTH note on the runtime substrate: today INSPECT/STRING/UNSTRING operate on a `byte[]`
> `StorageArea`. Under the typed-native data-model migration (`EnableTypedFields`, default OFF; CORE done
> through Stage-4), `PIC X` items become .NET `string`/`CobolString` and the byte/`StorageBlock` engine is
> being **islanded**. When text-processing is flipped to the typed path it will run over `string`/`CobolString`
> rather than `byte[]`; until then the byte path is canonical.

------------------------------------------------------------
SECTION 1 — TEXT-PROCESSING RUNTIME OVERVIEW
------------------------------------------------------------

The runtime text-processing layer (the conceptual "StringEngine" role) provides:
- Concatenation
- Delimited extraction / splitting
- Character scanning / searching
- Replacement and conversion
- Case conversion
- Padding and truncation
- Bounds checking / overflow detection
- Pointer and tallying updates
- UTF‑8 / UTF‑16 bridging (design target — see status banner)
- ExceptionState population

All operations are intended to be:
- Pure managed
- Deterministic and locale-independent
- Zero‑allocation where possible (`Span<char>` / `Span<byte>` internally)
- AOT/WASM‑safe (design target)
- Compatible with CoreCLR (current) and, as a goal, AOT and WASM

------------------------------------------------------------
SECTION 2 — STRING STATEMENT
------------------------------------------------------------

2.1 Basic form
--------------
    STRING src1 src2 ...
        DELIMITED BY delim
        INTO target
        WITH POINTER ptr
        ON OVERFLOW ...
        NOT ON OVERFLOW ...
    END-STRING

2.2 Semantics
-------------
STRING:
- Concatenates source operands
- Applies DELIMITED BY rules
- Writes into target starting at POINTER
- Updates POINTER to the next free position
- Pads unused target space with spaces

2.3 DELIMITED BY rules
----------------------
DELIMITED BY:
- SIZE → use the full source length
- literal → stop at the literal
- identifier → stop at the value of the identifier
- ALL literal → treat consecutive delimiters as one

2.4 POINTER
-----------
- 1‑based index
- Updated after each write: `ptr = ptr + N` after writing N characters
- If omitted → starts at 1
- POINTER = 0 is treated as 1 (edge case 11.1)

2.5 Overflow rules
------------------
Overflow occurs when:
- The result exceeds the target length
- POINTER moves beyond the target / POINTER is out of range

> **Overflow / partial-write semantics (ISO 1989:2023 §14.9.39):** transfer stops at the moment the target is
> exhausted, so the target holds whatever was transferred up to that point (a partial concatenation), and the
> ON OVERFLOW imperative then runs. STRING does NOT roll back already-transferred characters.

ON OVERFLOW executes the overflow block; NOT ON OVERFLOW executes only when no overflow occurred.

2.6 Overlapping source/target
-----------------------------
STRING permits source and target to overlap; the implementation uses a temporary buffer for the
concatenation result to avoid mid-statement corruption.

2.7 NATIONAL support (design target)
------------------------------------
STRING is specified to support PIC X (DISPLAY, 1 byte/char) and PIC N (NATIONAL, UTF‑16, 2 bytes/char),
with conversions applied for mixed operands and the POINTER counting *characters*, not bytes. NATIONAL inside
STRING is currently design-only (see status banner).

------------------------------------------------------------
SECTION 3 — UNSTRING STATEMENT
------------------------------------------------------------

3.1 Basic form
--------------
    UNSTRING src
        DELIMITED BY delim
        INTO tgt1 tgt2 ...
        WITH POINTER ptr
        TALLYING IN tally
        ON OVERFLOW ...
        NOT ON OVERFLOW ...
    END-UNSTRING

3.2 Semantics
-------------
UNSTRING:
- Splits source into fields at the delimiters
- Writes each field into the corresponding target (trimmed/padded per the target PIC)
- Advances POINTER past each delimiter
- Updates the TALLYING count
- Pads unused target space with spaces

3.3 DELIMITED BY rules
----------------------
- literal
- identifier
- ALL literal → consecutive delimiters treated as one
- SIZE → entire remaining string

3.4 POINTER
-----------
- 1‑based, points into the **source**
- Updated to the position after the delimiter (enables iterative UNSTRING)
- If omitted → starts at 1

3.5 TALLYING
------------
TALLYING IN var counts the number of characters moved into targets; it does **not** count delimiters.

3.6 Overflow rules
------------------
Overflow occurs when:
- There are more fields than targets
- A target is too small
- POINTER is beyond the source length

> **Partial-write semantics:** UNSTRING **does** leave partial writes on overflow (per the COBOL standard);
> fields written before overflow are retained. This differs from STRING (§2.5) only in that the COBOL standard
> scopes the two statements’ partial-write semantics independently.

3.7 NATIONAL support (design target)
------------------------------------
Same encoding rules as STRING: UTF‑16 with a character-based POINTER. Currently design-only.

------------------------------------------------------------
SECTION 4 — INSPECT STATEMENT
------------------------------------------------------------

INSPECT supports TALLYING, REPLACING, and CONVERTING. Implemented in `Runtime/InspectRuntime.cs` plus the
`CilStringEmitter.EmitInspect*` methods.

4.1 INSPECT TALLYING
--------------------
    INSPECT item
        TALLYING counter
            FOR ALL literal
            FOR LEADING literal
            FOR TRAILING literal
            FOR CHARACTERS
        [BEFORE|AFTER INITIAL delimiter]

Operand kinds: ALL (count every occurrence), LEADING (contiguous run from the scan start until first
mismatch), TRAILING (contiguous run at the end), CHARACTERS (every single character), FIRST.

4.2 INSPECT REPLACING
---------------------
    INSPECT item
        REPLACING
            ALL literal-1 BY literal-2
            FIRST literal-1 BY literal-2
            LEADING literal-1 BY literal-2
            TRAILING literal-1 BY literal-2
            CHARACTERS BY literal-2
        [BEFORE|AFTER INITIAL delimiter]

Replacement requires equal lengths between the matched pattern and the replacement (compile-time/contract rule).

4.3 INSPECT CONVERTING
----------------------
    INSPECT item
        CONVERTING from-chars TO to-chars
        [BEFORE|AFTER INITIAL delimiter]

Character-by-character positional mapping `fromSet[i] → toSet[i]`; the sets must be the same length;
on duplicate characters in the FROM set, the first match wins. FROM/TO sets are positional maps and are
NOT reversed under BACKWARD.

4.4 BEFORE / AFTER regions
--------------------------
Each TALLYING/REPLACING/CONVERTING operand may carry a BEFORE INITIAL / AFTER INITIAL delimiter that
restricts the scan to a sub-region of the field. `InspectRuntime.ComputeRegion` computes the `[start, end)`
window from the BEFORE/AFTER patterns before the comparison cycle runs.

4.5 The single comparison cycle (ISO/IEC 1989:1985 §6.17.3, GR 8–9, 12, 17)
---------------------------------------------------------------------------
TALLYING and REPLACING each execute as **one** left-to-right comparison cycle over their ordered operands:
at each character position the operands are tried in source order; the first that matches tallies/replaces,
the position advances past the matched characters, and the cycle restarts from the first operand. CHARACTERS
always matches the current single character; LEADING/FIRST carry per-operand eligibility that terminates once
their contiguous run (LEADING) or single match (FIRST) is consumed. This is why, e.g., `ALL "A"` preceding
`LEADING "AH"` leaves the latter at count zero — the leading 'A' is consumed by the earlier operand before
LEADING is ever tried. The lowerer therefore groups all TALLYING operands of one INSPECT into a single
`IrInspectTallying` instruction (and likewise for REPLACING), never lowering them independently.

4.6 BACKWARD (ISO §14.9.21, COBOL-2002)
---------------------------------------
BACKWARD inspection proceeds right-to-left. It is realized as a reverse-wrapper: scanning the ORIGINAL
right-to-left equals scanning the REVERSED string left-to-right, provided each multi-character operand and
delimiter is also reversed (so "AB" read right-to-left matches "BA" forward). BEFORE/AFTER roles are
preserved under reversal. The existing forward passes thus run unchanged on the reversed inputs;
REPLACING/CONVERTING reverse the result buffer back, TALLYING needs no un-reverse (per-operand counts are
direction-independent), and CONVERTING FROM/TO sets are NOT reversed.

------------------------------------------------------------
SECTION 5 — NATIONAL vs DISPLAY PROCESSING (design target)
------------------------------------------------------------

5.1 DISPLAY (PIC X)
-------------------
- ASCII bytes, 1 byte per character; case conversion uses ASCII rules.
- **This is the path actually implemented today** (byte[] storage as ASCII).

5.2 NATIONAL (PIC N)
--------------------
- UTF‑16, 2 bytes per character; case conversion uses Unicode rules.
- No surrogate splitting; a surrogate pair counts as one character (2 bytes).
- NATIONAL data items exist in CobolSharp (M2-DATA-3), but running INSPECT/STRING/UNSTRING *over* national
  text is design-only at present.

5.3 Mixed operations
--------------------
- DISPLAY → NATIONAL: ASCII → UTF‑16.
- NATIONAL → DISPLAY: UTF‑16 → ASCII, raising ON EXCEPTION if a character is non-ASCII / cannot be represented.
- Mixing national and alphanumeric is illegal unless explicitly converted.

------------------------------------------------------------
SECTION 6 — ERROR HANDLING & EXCEPTIONSTATE
------------------------------------------------------------

6.1 Overflow sources
--------------------
- STRING: target too small, or POINTER out of range.
- UNSTRING: target field too small, POINTER beyond source, or more fields than targets.

6.2 INSPECT errors
------------------
- Invalid CONVERTING set lengths (FROM/TO not equal length).
- Invalid NATIONAL → DISPLAY conversion (non-ASCII), when NATIONAL text-processing is enabled.

6.3 ExceptionState
------------------
Populated with: operation type, source/target names, pointer position, error message.

6.4 Routing order
-----------------
1. ON OVERFLOW
2. ON EXCEPTION
3. USE AFTER EXCEPTION ON STANDARD (declaratives)

------------------------------------------------------------
SECTION 7 — CIL LOWERING RULES
------------------------------------------------------------

7.1 STRING lowering (`CilStringEmitter.EmitStringStatement`)
------------------------------------------------------------
- Allocate a shared `Int32` POINTER local for the whole statement.
- Initialize it from the user POINTER field (`PicRuntime.DecodeNumeric` → `Convert.ToInt32`) or to 1.
- For each source: evaluate, apply DELIMITED BY, append into the target via the runtime helper, advance POINTER.
- Check overflow; branch to ON OVERFLOW / NOT ON OVERFLOW.
- Re-encode the final POINTER value back into the user's POINTER field.

7.2 UNSTRING lowering (`CilStringEmitter.EmitUnstringStatement`)
---------------------------------------------------------------
- Decode source and POINTER; call the split helper.
- Assign each extracted field to its target (padded/trimmed per PIC).
- Update POINTER and TALLYING; branch to ON OVERFLOW / NOT ON OVERFLOW.

7.3 INSPECT lowering (`StringLowerer.LowerInspect` → `CilStringEmitter.EmitInspect*`)
------------------------------------------------------------------------------------
- TALLYING: group all operands into one `IrInspectTallying` (§4.5), call `InspectRuntime` tally.
- REPLACING: one `IrInspectReplace` over the ordered operands, call the replace helper.
- CONVERTING: emit the positional FROM→TO map call.
- BEFORE/AFTER patterns and the BACKWARD flag are carried on the IR ops.

7.4 Temporary buffers
---------------------
The compiler allocates temporary locals for substring extraction and a temporary buffer for overlapping STRING.

------------------------------------------------------------
SECTION 8 — RUNTIME HELPER SURFACE
------------------------------------------------------------

The conceptual runtime surface (implemented across `InspectRuntime`, `StorageArea`, `CobolProgram`):
- `ExtractUntilDelimiter` / split-with-pointer (UNSTRING)
- `AppendWithPointer` (STRING)
- `ReplaceAll` / `ReplaceLeading` / `ReplaceTrailing` / `ReplaceFirst` (INSPECT REPLACING)
- `ConvertCharacters` (INSPECT CONVERTING)
- `CountOccurrences` / `CountLeading` / `CountTrailing` (INSPECT TALLYING)
- `ComputeRegion` (BEFORE/AFTER windowing), `Reverse` / `ReverseEach` (BACKWARD)

Safety guarantees (design intent):
- No buffer overruns; Unicode-safe slicing where national is in play.
- STRING/UNSTRING partial-write semantics per §2.5 / §3.6.
- No culture-dependent behavior; `Span<char>` internally; optimized for large OCCURS tables.

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION (design target)
------------------------------------------------------------

The debugger is intended to surface:
- STRING sources and target; UNSTRING fields and delimiters; INSPECT patterns and replacements.
- POINTER before/after and TALLYING values; delimiter matches and intermediate substrings.
- NATIONAL vs DISPLAY interpretation; raw bytes for PIC X and PIC N; optional Unicode code points.
- ExceptionState.
Sequence points are intended for each STRING source, each UNSTRING target, and each INSPECT clause.
(Debugger is Phase E / design-only; see `docs/MASTER_PLAN.md`.)

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE TEXT PROCESSING (design target)
------------------------------------------------------------

Goal constraints for the text-processing runtime: no reflection (operations static), no dynamic codegen
(no runtime IL), no unsafe code (no raw pointers / `stackalloc`), and deterministic cross-platform behavior.
Status: aspirational — current builds target CoreCLR; AOT/WASM hardening is future work.

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 STRING with POINTER = 0 → treated as 1.
11.2 STRING with a zero-length source → no effect / writes nothing.
11.3 UNSTRING with no delimiters → the entire source goes to the first target.
11.4 UNSTRING with missing/insufficient delimiters → remainder goes to the last receiving field;
     surplus targets receive spaces.
11.5 UNSTRING ALL literal with empty fields → consecutive delimiters collapse to one.
11.6 INSPECT REPLACING with overlapping patterns → left-to-right, non-recursive, non-overlapping.
11.7 INSPECT CONVERTING with duplicate FROM characters → first match wins.
11.8 NATIONAL with surrogate pairs → counted as 1 character (2 bytes).
11.9 POINTER < 1 or > target/source length → overflow.
11.10 Empty INSPECT/STRING delimiter → illegal (compile-time error).
11.11 INSPECT REPLACING / CONVERTING with unequal pattern/replacement lengths → contract error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp INSPECT / STRING / UNSTRING text-processing architecture:
- Implements full COBOL STRING, UNSTRING, and INSPECT semantics, including the ISO 6.17.3 single
  comparison cycle, BEFORE/AFTER regions, and COBOL-2002 BACKWARD.
- Lowers cleanly to IR and emits verifiable CIL via Mono.Cecil (CIL-only; no custom VM).
- Runs against byte[] storage today (ASCII), migrating to typed-native `string`/`CobolString` under the
  data-model re-architecture.
- Targets deterministic, Unicode-safe behavior with DISPLAY/NATIONAL encoding rules (national text-processing
  and AOT/WASM/debugger surfaces are design targets, not yet implemented).
