CobolSharp COBOL Numeric Engine, Arithmetic, ROUNDED, SIZE ERROR & Packed Decimal Architecture (CIL-Only)
========================================================================================================

> **STATUS — authoritative design reference for the COBOL arithmetic / numeric subsystem** (ADD/SUBTRACT/MULTIPLY/
> DIVIDE/COMPUTE, ROUNDED, SIZE ERROR, DISPLAY/COMP/COMP-3/COMP-5 numeric formats, packed-decimal encode/decode,
> scaling, mixed-type promotion). **Implementation status: ~85-90% IMPLEMENTED and GREEN.** The arithmetic verbs,
> COMPUTE expression evaluation, all 8 ISO ROUNDED MODE methods, ON/NOT ON SIZE ERROR, and the DISPLAY/COMP/COMP-3/
> COMP-5 codecs are all working in the shipping compiler — M1 (COBOL-85) is COMPLETE; guard is **1196 unit / 509
> integration / 364 NIST** (2026-06). **Stack: .NET 10 / C# 14.** Backend is **CIL-only via Mono.Cecil — there is NO
> custom VM and NO bytecode interpreter** (a Roslyn C# backend is a FUTURE additive option, data-model Stage 5, with
> Cecil as the differential oracle). The numeric **substrate is migrating** from the legacy `System.Decimal`/byte path
> to the exact typed-native engine: `CobolNum` + `CobolDecimal` (a `BigInteger`-backed exact carrier, NOT
> `System.Decimal`) + `NumProfile` + the 8-mode `CobolRounding` enum, gated behind `EnableTypedFields` (default OFF →
> byte-identical corpus). The legacy byte `PicRuntime` packed-decimal/binary engine is being **islanded** (demoted to
> the `Bytes/` boundary codec), not deleted.
>
> **SSOTs:** plan = `docs/MASTER_PLAN.md`; doctrine = `PROMPT.md`; the numeric-substrate migration =
> `docs/DATA_MODEL_ARCHITECTURE.md` (§5 runtime shape, §13 `CobolNum` contract) + `docs/RECORD_STRUCT_STORAGE_DESIGN.md`.
> Where this doc and `DATA_MODEL_ARCHITECTURE.md` disagree on the numeric **substrate**, the latter wins.

Purpose
-------
Define the authoritative architecture for:
- COBOL numeric types (DISPLAY, COMP, COMP-5, COMP-3/PACKED-DECIMAL, COMP-1/COMP-2, scaled `V`/`P`)
- COBOL arithmetic operations (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE)
- Packed decimal (COMP-3) encoding/decoding; binary numeric formats (COMP, COMP-5); DISPLAY numeric formats
- Decimal alignment, precision and scaling rules
- ROUNDED semantics (all 8 ISO modes); ON / NOT ON SIZE ERROR routing; overflow / division-by-zero detection
- Mixed-type arithmetic (DISPLAY, COMP, COMP-3, COMP-5) and promotion rules
- Temporary arithmetic registers and evaluation order
- CIL-friendly lowering; deterministic AOT/WASM-safe numeric operations
- Integration with the runtime numeric engine

This document governs how CobolSharp implements COBOL numeric and arithmetic behavior on .NET.

------------------------------------------------------------
SECTION 0 — SUBSTRATE: THE TYPED NUMERIC ENGINE (current truth)
------------------------------------------------------------

The numeric substrate is the typed-native data model, not raw `System.Decimal`. The shipping building blocks
(implemented; see `src/CobolSharp.Runtime/Numeric/`):

- **`CobolDecimal`** — an EXACT decimal carrier `(BigInteger Unscaled, int Scale)`. This replaces `System.Decimal`
  precisely because COBOL mandates 1–31 digits and `System.Decimal` caps at 28–29; a `BigInteger` unscaled mantissa
  holds the full 31-digit range and all intermediates without overflow or silent decode-to-zero.
- **`CobolNum`** — the value-level store: `ScaleAndRound` → bound-check against the receiver's `NumProfile` capacity
  → SIZE ERROR. `TryStore` returns a `bool` for ON SIZE ERROR and **NEVER throws** (a result that does not fit is
  reported through the boolean, not an exception). It is the single source of truncation / ROUNDED / SIZE-ERROR
  semantics shared by both pipelines.
- **`NumProfile`** — a `readonly record struct` carrying digits / fraction-scale / trailing-P / signed — the only
  numeric metadata handed to the runtime (the compile-time `FieldShape` keeps usage/editing/sign-storage).
- **`CobolRounding`** — the 8 ISO/IEC 1989:2023 ROUNDED MODE methods as an enum whose integer values are identical
  to the legacy `PicRuntime.Round*` constants (identity cast while both pipelines coexist).
- **Legacy `PicRuntime`** — the byte packed-decimal / binary / DISPLAY codec. It is being **demoted** from "the only
  path" to "the byte-island engine + `IDataSlot` boundary codec" (`docs/DATA_MODEL_ARCHITECTURE.md` §5). It is the
  migration safety floor — always a valid implementation — never deleted.

Gating: typed numeric flips (numeric → `long` for unsigned-int, `decimal` for signed-scaled, over DISPLAY/COMP/
BINARY) are behind `EnableTypedFields` (default OFF). COMP-5/float/packed are currently excluded from the typed flip
and stay on the byte path. With the flag OFF the corpus is byte-identical.

------------------------------------------------------------
SECTION 1 — NUMERIC TYPE SYSTEM
------------------------------------------------------------

CobolSharp supports the full COBOL numeric type system:

1. DISPLAY numeric
2. COMP (binary)
3. COMP-5 (native binary)
4. COMP-3 (packed decimal)
5. COMP-1 / COMP-2 (IEEE 754 single/double — see §3.3)
6. Decimal with implied `V`
7. Signed/unsigned variants
8. Scaled decimals (`PIC 9(n)V9(m)`, `P` scaling)

Each numeric item is described by:
- NumericType / usage
- TotalDigits
- Scale (digits after the decimal, incl. trailing/leading `P`)
- Signed flag and sign-storage kind
- Storage format (DISPLAY / BINARY / PACKED / float)

(Compile-time this lives in `FieldShape`; the runtime slice is `NumProfile` — digits / scale / signed.)

------------------------------------------------------------
SECTION 2 — DISPLAY NUMERIC FORMAT
------------------------------------------------------------

DISPLAY numeric:
- Stored as ASCII/Unicode characters, right-justified, zero-padded.
- Sign stored per the SIGN clause:
  - Leading separate
  - Trailing separate
  - Leading embedded (overpunch)
  - Trailing embedded (overpunch — the default for a signed DISPLAY item)

On the byte path, the engine normalizes DISPLAY numeric to a value: parse digits (with overpunch sign decode) →
`CobolDecimal` for arithmetic → re-encode after arithmetic with the correct sign storage. On the typed path an
unsigned plain DISPLAY field flips to `long`/`int`, a signed/scaled one to `decimal` (`EnableTypedFields`).

**DISPLAY numeric with spaces / non-digits:** treated as zero on a permissive read; `IS NUMERIC` on such content
is FALSE.

------------------------------------------------------------
SECTION 3 — BINARY & FLOATING NUMERIC FORMATS
------------------------------------------------------------

3.1 COMP (binary)
-----------------
Fixed digit→width mapping:
- 1–4 digits → Int16 (2 bytes)
- 5–9 digits → Int32 (4 bytes)
- 10–18 digits → Int64 (8 bytes)

Stored in the platform layout used by the byte engine. Converted to `CobolDecimal` for mixed arithmetic; on the
typed path an unsigned COMP integer flips to `long`/`int`.

3.2 COMP-5 (native binary)
--------------------------
Same digit→width mapping as COMP, but:
- **No truncation on assignment** (full binary capacity, e.g. `9(4) COMP-5` = 0..65535 with defined wraparound).
- Overflow only on arithmetic.

**Truncation policy distinction (load-bearing — `NumProfile.TruncationPolicy`):** COMP/BINARY truncate by **digit
count** (`value mod 10^n`); COMP-5 truncates by **binary capacity** (defined wraparound). A store keyed off digit
count alone is wrong for COMP-5.

3.3 COMP-1 / COMP-2 (IEEE 754)
------------------------------
`COMP-1` → `float` (single), `COMP-2` → `double`. Implemented (`PicRuntime.DecodeComp1/2`). These are floating types,
NOT part of the exact decimal substrate; they have their own IEEE codec and stay on the float path.

------------------------------------------------------------
SECTION 4 — PACKED DECIMAL (COMP-3)
------------------------------------------------------------

4.1 Encoding
------------
Packed decimal stores two digits per byte; the last (low) nibble is the sign:
- Positive: 0xC or 0xF
- Negative: 0xD

Example: `PIC S9(5)V99 COMP-3` → total digits = 7 → bytes = ceil((7 + 1 sign-nibble) / 2) = 4 bytes.

4.2 Decoding
------------
The codec reads each nibble, validates digits, extracts the sign, and produces a `CobolDecimal` value. Multi-sign
decode accepts 0x0A–0x0F as positive; encode normalizes to 0x0C. An invalid digit nibble is an exception path.

4.3 Encoding after arithmetic
-----------------------------
Convert `CobolDecimal` → digit array → pack into nibbles → write the sign nibble → pad a leading zero when the digit
count is odd.

------------------------------------------------------------
SECTION 5 — DECIMAL ALIGNMENT, SCALING & PRECISION
------------------------------------------------------------

5.1 Decimal alignment
---------------------
Before arithmetic:
- Align decimal points; promote operands to a common scale (extend fractional digits with zeros).
- Preserve sign. No implicit truncation during intermediate arithmetic.

Example:
```
A = PIC 9(3)V9(2) → scale 2 → xxx.yy00   (promoted to scale 4)
B = PIC 9(2)V9(4) → scale 4 → xx.yyyy
```

5.2 Result precision
--------------------
Intermediate arithmetic carries **full exact precision** on the `CobolDecimal` (`BigInteger` mantissa) substrate —
1–31 digits and beyond, never the 28–29-digit `System.Decimal` cap. COBOL truncation/scaling rules apply only when
**storing into the target**.

5.3 Truncation when storing
---------------------------
- If the target has fewer fractional digits → truncate (or ROUND if ROUNDED).
- If the target has fewer integer digits than the result requires → SIZE ERROR (target left unchanged).

------------------------------------------------------------
SECTION 6 — ARITHMETIC STATEMENTS
------------------------------------------------------------

6.1 ADD
-------
```
ADD a TO b.
ADD a b c TO d.
ADD a GIVING b.
ADD a TO b ROUNDED c ROUNDED.   (each target rounded independently)
```
Lowering: promote operands to common scale → exact add → store into each target with scaling/rounding/SIZE-ERROR.

6.2 SUBTRACT
------------
```
SUBTRACT a FROM b.
SUBTRACT a b FROM c.
SUBTRACT a FROM b GIVING c.
```
`b = b - a` (or GIVING target = …).

6.3 MULTIPLY
------------
```
MULTIPLY a BY b.
MULTIPLY a BY b GIVING c.
```
Multiply full-precision; result scale = sum of operand scales; apply rounding if requested; check overflow.

6.4 DIVIDE
----------
```
DIVIDE a INTO b.                          b = b / a
DIVIDE a INTO b GIVING c.                 c = b / a
DIVIDE a BY b GIVING c.                   c = a / b
DIVIDE a BY b GIVING c REMAINDER r.       c = a / b ;  r = a - (b * c)   (≡ a % b)
```
Result scale = max(target scale, operand scales); apply rounding if requested; **division by zero → SIZE ERROR**.

6.5 COMPUTE
-----------
```
COMPUTE x = expression.
COMPUTE x ROUNDED = expression.
```
Expression features:
- Parentheses; unary +/-; binary `+ - * /`.
- **Exponentiation (`**`) IS supported** (COBOL arithmetic exponentiation; implemented — see CLAUDE.md grammar fix).
- Nested expressions; intrinsic function calls inside the expression (`FUNCTION ABS`, etc.).
- Mixed numeric types (DISPLAY / COMP / COMP-3 / COMP-5) per the §9 promotion rules.

Evaluation order:
- Left-to-right within a precedence level; `**` before `* /` before `+ -`; parentheses override precedence.
- Use exact decimal arithmetic for mixed types; native binary only when all operands are binary.

------------------------------------------------------------
SECTION 7 — TEMPORARY ARITHMETIC REGISTERS / LOCALS
------------------------------------------------------------

7.1 Purpose
-----------
Used for intermediate results, COMPUTE expression evaluation, and GIVING targets. The compiler generates one local
per intermediate value (the COMPUTE expression tree → temp locals).

7.2 Representation
------------------
Always the exact decimal carrier (`CobolDecimal`) for mixed/decimal arithmetic; native binary locals only when all
operands are binary.

7.3 Lifetime & evaluation discipline
------------------------------------
- Allocated per statement; released after storing into the target.
- A multi-target source is evaluated **once** (`IrCachedLocation`) and then stored to each target — so subscripted
  sources and side-effecting sources are not re-evaluated per target.

------------------------------------------------------------
SECTION 8 — ROUNDED SEMANTICS
------------------------------------------------------------

8.1 ROUNDED clause
------------------
```
ADD a TO b ROUNDED.
COMPUTE x ROUNDED = expression.
COMPUTE x ROUNDED MODE IS NEAREST-EVEN = expression.   (explicit mode, ISO-2002+)
```
ROUNDED applies **only when storing into the target**, never during intermediate arithmetic. Each of multiple
targets is rounded independently.

8.2 The 8 ISO ROUNDED MODE methods (implemented — `CobolRounding`)
------------------------------------------------------------------
| Mode | Behavior |
|---|---|
| TRUNCATION (default, no ROUNDED) | drop the excess fraction toward zero |
| NEAREST-AWAY-FROM-ZERO (bare `ROUNDED`) | round to nearest; tie → away from zero |
| AWAY-FROM-ZERO | always round up in magnitude when any nonzero fraction is dropped |
| NEAREST-EVEN | round to nearest; tie → nearest even digit (banker's) |
| NEAREST-TOWARD-ZERO | round to nearest; tie → toward zero |
| PROHIBITED | rounding not permitted — an inexact result raises SIZE ERROR (EC-SIZE-TRUNCATION), receiver unchanged |
| TOWARD-GREATER | round toward +∞ (ceiling) |
| TOWARD-LESSER | round toward -∞ (floor) |

(Implementation note: `PROHIBITED` must set EC-SIZE-TRUNCATION and leave the receiver unchanged — it must NOT
silently truncate, a defect that was extracted-and-corrected out of the legacy `PicRuntime` path into `CobolNum`.)

------------------------------------------------------------
SECTION 9 — SIZE ERROR SEMANTICS
------------------------------------------------------------

9.1 Trigger conditions
----------------------
SIZE ERROR occurs when:
- The integer part is too large for the target field (result cannot fit).
- Packed-decimal overflow; binary overflow; COMP/COMP-5 out of range.
- Division by zero.
- ROUNDED MODE PROHIBITED with an inexact result.
- (On the byte path) an invalid COMP-3 sign nibble / invalid numeric conversion.

9.2 ON SIZE ERROR / NOT ON SIZE ERROR
-------------------------------------
```
ADD a TO b
    ON SIZE ERROR      ...      (executes the handler; target left UNCHANGED)
    NOT ON SIZE ERROR  ...      (executes only when no overflow)
```
- On SIZE ERROR the target is **not modified**; the `ON SIZE ERROR` block runs; `NOT ON SIZE ERROR` is skipped.
- Without an `ON SIZE ERROR` phrase, on overflow the target is left unchanged and execution continues.
- `ExceptionState.Category = SIZE ERROR`, with detail message (and EC-SIZE-* for the 2002+ exception model).

9.3 No-throw guarantee
----------------------
`CobolNum.TryStore` returns a `bool`, never an exception — so SIZE ERROR can always fire before any
`OverflowException` would. **Every** expression-tree operator (`SafeAdd`/`SafeSub`/`SafeMul`/`SafePow`) is a
no-throw, size-error-setting helper, not just the final store (the legacy path could throw on an intermediate
`decimal.op_*` before `ON SIZE ERROR` could fire — fixed).

------------------------------------------------------------
SECTION 10 — MIXED-TYPE ARITHMETIC & PROMOTION
------------------------------------------------------------

Promotion rules:
```
DISPLAY  + DISPLAY  → exact decimal
DISPLAY  + COMP     → exact decimal
DISPLAY  + COMP-3   → exact decimal
COMP     + COMP     → binary
COMP     + COMP-5   → binary
COMP-3   + COMP-3   → exact decimal
COMP-3   + decimal  → exact decimal
decimal  + decimal  → exact decimal
```
Boolean in a numeric context: TRUE → 1, FALSE → 0.

Mixed alphanumeric/numeric arithmetic converts the alphanumeric operand to numeric (exception if invalid).

------------------------------------------------------------
SECTION 11 — TYPE CONVERSION RULES (byte boundary codec)
------------------------------------------------------------

These are the boundary conversions the byte-island codec performs (`PicRuntime` → `Bytes/` engine):

- **DISPLAY numeric → value**: ASCII digits parsed; leading/trailing spaces allowed; overpunch/separate sign decoded.
- **COMP / COMP-5 → value**: binary integer → `CobolDecimal`.
- **COMP-3 → value**: packed decimal unpacked; sign nibble validated.
- **value → DISPLAY**: formatted per PIC; truncation/rounding applied; correct sign storage written.
- **value → COMP / COMP-5**: checked for overflow; truncated to the binary width (digit-count vs binary-capacity per
  §3.2).
- **value → COMP-3**: packed encoding; sign nibble applied; odd-digit leading-zero pad.

------------------------------------------------------------
SECTION 12 — CIL LOWERING RULES (CIL-only via Mono.Cecil)
------------------------------------------------------------

There is no custom VM and no bytecode interpreter — arithmetic lowers to verifiable CIL emitted via Mono.Cecil.

12.1 Statement lowering
-----------------------
- **ADD / SUBTRACT / MULTIPLY / DIVIDE**: load operands → promote to common scale → exact op → store into target via
  `CobolNum.Store/TryStore` (scaling + rounding + SIZE-ERROR) where `NumProfile` proves normalization is needed.
- **DIVIDE REMAINDER**: `c = a / b`; `r = a % b`.
- **COMPUTE**: the expression tree lowers to temp-local ops (load/op/store) → store into target with scaling/rounding.

12.2 Binary fast path
---------------------
When both operands are COMP/COMP-5, lower directly to CIL integer opcodes (`add`/`sub`/`mul`/`div`). Packed decimal
always routes through the runtime engine.

12.3 SIZE ERROR lowering
------------------------
The runtime store returns a success flag; CIL branches to the `ON SIZE ERROR` block on `false` (no exception-based
control flow on the no-throw path). Where the legacy path used `try/catch (DivideByZeroException)`, the corrected
path returns a flag and branches.

12.4 Overflow detection
-----------------------
The numeric store returns `{ success, value }`; CIL checks the flag and branches to the SIZE ERROR handler on false.

------------------------------------------------------------
SECTION 13 — DEBUGGER INTEGRATION (design-only — Phase E)
------------------------------------------------------------

The debugger (a DESIGN-ONLY, not-yet-implemented product surface) would show:
- Intermediate decimal values and temporary locals; target field before/after.
- COMP-3 decoded values; binary COMP/COMP-5 values; DISPLAY numeric strings.
- Scale and sign; decimal-alignment visualization; ROUNDED behavior; SIZE ERROR / overflow state; ExceptionState.
- Raw bytes of numeric fields (byte-island view).

Sequence points emitted for each arithmetic operation, each rounding operation, and each SIZE ERROR check.

------------------------------------------------------------
SECTION 14 — AOT/WASM-SAFE NUMERIC EXECUTION
------------------------------------------------------------

- **No floating-point in the exact path.** All exact arithmetic uses the `CobolDecimal`/`BigInteger` substrate;
  COMP-1/COMP-2 are the only IEEE float types and are explicitly floating by COBOL definition.
- **No unsafe code.** No raw pointers / `stackalloc` in the numeric engine (`ManagedPointer` is a safe managed
  interior reference; see `docs/DATA_MODEL_ARCHITECTURE.md` §6).
- **Deterministic rounding and scaling.** Same results across CoreCLR, AOT, and WASM.

------------------------------------------------------------
SECTION 15 — EDGE-CASE BEHAVIOR
------------------------------------------------------------

- **Division by zero** → SIZE ERROR.
- **Negative zero** → normalized to zero for DISPLAY; preserved for packed decimal (byte form).
- **COMP-3 with odd digit count** → high nibble of the leading byte padded with zero.
- **COMP-3 invalid sign nibble** → exception path (byte codec).
- **DISPLAY numeric with spaces** → treated as zero (permissive read); `IS NUMERIC` is FALSE.
- **ROUNDED with insufficient target precision** → the rounded result may still overflow → SIZE ERROR.
- **Overflow in an intermediate expression** → SIZE ERROR even if the final target could hold the result.
- **Division producing an infinite repeating fraction** → rounded to the target scale.
- **`PIC 9(19..31)` extended precision** → handled exactly via `BigInteger` (the legacy `decimal`/`(long)` path
  silently returned zero above 28 digits — corrected; Risk R5 in `DATA_MODEL_ARCHITECTURE.md`).
- **COMPUTE with no target** → runtime/compile error.
- **Mixed NATIONAL + numeric** → illegal unless explicitly converted (NATIONAL→DISPLAY only when ASCII-representable).

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Numeric & Arithmetic Architecture:
- Implements full COBOL arithmetic semantics (ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE, exponentiation, REMAINDER).
- Supports all 8 ISO ROUNDED MODE methods, ON/NOT ON SIZE ERROR, and mixed-type arithmetic with deterministic rules.
- Uses the EXACT `CobolDecimal`/`BigInteger` substrate (`CobolNum` + `NumProfile`) for intermediate arithmetic —
  full 1–31-digit COBOL precision, never the 28–29-digit `System.Decimal` cap — with `long`/`decimal` typed flips
  under `EnableTypedFields`.
- Provides packed-decimal (COMP-3) encode/decode, binary (COMP/COMP-5) and IEEE (COMP-1/COMP-2) formats, decimal
  alignment, and digit-count-vs-binary-capacity truncation.
- Reports SIZE ERROR through a no-throw boolean, never an exception, so the handler always fires deterministically.
- Generates clean, verifiable CIL via Mono.Cecil (no custom VM); correct across CoreCLR, AOT, and WASM.
- Shares one truncation/ROUNDED/SIZE-ERROR source (`CobolNum`) between the typed and byte pipelines; the byte
  `PicRuntime` engine is being islanded as the boundary codec and migration safety floor.
